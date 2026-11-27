using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BichonDesktopPet.Core.Models;

namespace BichonDesktopPet.Core.Services;

/// <summary>
/// Stores the local avatar catalog and imported image files.
/// Built-in avatar art remains in the application Assets folder; imported files
/// are copied into LocalAppData and never modify the repository assets.
/// </summary>
public sealed class AvatarCatalogService
{
    public const string DefaultAvatarId = "bichon";
    public const long MaximumImageBytes = 20 * 1024 * 1024;
    public const int CurrentSchemaVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp"
    };

    private readonly string _assetsDirectory;
    private readonly string _avatarsDirectory;
    private readonly string _catalogPath;
    private readonly Action<string, Exception?>? _log;
    private List<AvatarProfile> _avatars = [];
    private bool _isLoaded;
    private bool _isReadOnly;

    public AvatarCatalogService(
        string assetsDirectory,
        string userDataDirectory,
        Action<string, Exception?>? log = null)
    {
        if (string.IsNullOrWhiteSpace(assetsDirectory))
        {
            throw new ArgumentException("An assets directory is required.", nameof(assetsDirectory));
        }

        if (string.IsNullOrWhiteSpace(userDataDirectory))
        {
            throw new ArgumentException("A user data directory is required.", nameof(userDataDirectory));
        }

        _assetsDirectory = Path.GetFullPath(assetsDirectory);
        _avatarsDirectory = Path.Combine(Path.GetFullPath(userDataDirectory), "avatars");
        _catalogPath = Path.Combine(_avatarsDirectory, "catalog.json");
        _log = log;
    }

    public string CatalogPath => _catalogPath;

    public IReadOnlyList<AvatarProfile> Load()
    {
        Directory.CreateDirectory(_avatarsDirectory);

        var catalogExists = File.Exists(_catalogPath);
        var needsSave = !catalogExists;
        AvatarCatalogDocument? document = null;

        if (catalogExists)
        {
            try
            {
                document = JsonSerializer.Deserialize<AvatarCatalogDocument>(
                    File.ReadAllText(_catalogPath),
                    JsonOptions);
            }
            catch (Exception exception)
            {
                Report("Unable to read the avatar catalog. The built-in Bichon profile will be restored.", exception);
                needsSave = true;
            }
        }

        var sourceProfiles = document?.Avatars ?? [];
        var schemaVersion = document?.SchemaVersion ?? 0;
        var hasDefault = sourceProfiles.Any(profile => profile?.Id == DefaultAvatarId);
        _isReadOnly = schemaVersion > CurrentSchemaVersion;

        if (_isReadOnly)
        {
            Report(
                $"Avatar catalog schema {schemaVersion} is newer than this application supports (version {CurrentSchemaVersion}). The catalog will be opened read-only to avoid downgrading user data.",
                null);
        }

        _avatars = Normalize(sourceProfiles, schemaVersion);

        if (!_isReadOnly && (!hasDefault || schemaVersion < CurrentSchemaVersion || needsSave))
        {
            SaveCatalog();
        }

        _isLoaded = true;
        return _avatars.Select(Clone).ToArray();
    }

    public IReadOnlyList<AvatarProfile> GetAvatars()
    {
        EnsureLoaded();
        return _avatars.Select(Clone).ToArray();
    }

    public AvatarProfile? GetAvatar(string id)
    {
        EnsureLoaded();

        var profile = _avatars.FirstOrDefault(item =>
            string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));

        return profile is null ? null : Clone(profile);
    }

    public string? ResolveImagePath(string id)
    {
        var profile = GetAvatar(id);
        if (profile is null)
        {
            return null;
        }

        var root = profile.IsBuiltIn ? _assetsDirectory : _avatarsDirectory;
        var relativePath = profile.ImagePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));

        if (!IsWithinDirectory(root, fullPath))
        {
            Report($"Avatar '{profile.Id}' contains an invalid image path.", null);
            return null;
        }

        return File.Exists(fullPath) ? fullPath : null;
    }

    public AvatarProfile ImportImage(string sourcePath, string displayName, string species)
    {
        EnsureLoaded();

        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            throw new FileNotFoundException("找不到所选图片文件。", sourcePath);
        }

        var normalizedName = displayName?.Trim() ?? string.Empty;
        if (normalizedName.Length is < 1 or > 40)
        {
            throw new ArgumentException("角色名称长度必须为 1 到 40 个字符。", nameof(displayName));
        }

        var normalizedSpecies = string.IsNullOrWhiteSpace(species)
            ? "custom"
            : species.Trim();

        if (normalizedSpecies.Length > 24)
        {
            throw new ArgumentException("角色类型不能超过 24 个字符。", nameof(species));
        }

        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
        {
            throw new InvalidDataException("目前仅支持 PNG、JPG、JPEG 和 BMP 图片。");
        }

        var sourceInfo = new FileInfo(sourcePath);
        if (sourceInfo.Length <= 0 || sourceInfo.Length > MaximumImageBytes)
        {
            throw new InvalidDataException("图片文件必须大于 0 字节且不能超过 20 MB。");
        }

        if (!HasMatchingImageSignature(sourcePath, extension))
        {
            throw new InvalidDataException("文件内容与图片扩展名不匹配，无法导入。");
        }

        var id = Guid.NewGuid().ToString("N");
        var avatarFolder = Path.Combine(_avatarsDirectory, id);
        var destinationPath = Path.Combine(avatarFolder, "avatar" + extension);
        Directory.CreateDirectory(avatarFolder);

        try
        {
            File.Copy(sourcePath, destinationPath, overwrite: false);

            var profile = new AvatarProfile
            {
                Id = id,
                Name = normalizedName,
                Species = normalizedSpecies,
                Breed = string.Empty,
                Persona = PersonaProfileValidator.CreateDefault(),
                ImagePath = id + "/avatar" + extension,
                IsBuiltIn = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            _avatars.Add(profile);
            if (!SaveCatalog())
            {
                _avatars.RemoveAll(item => item.Id == id);
                throw new IOException("无法保存角色清单，请检查本地目录权限和应用日志。");
            }

            return Clone(profile);
        }
        catch
        {
            try
            {
                if (Directory.Exists(avatarFolder))
                {
                    Directory.Delete(avatarFolder, recursive: true);
                }
            }
            catch (Exception cleanupException)
            {
                Report("Unable to clean up an incomplete avatar import.", cleanupException);
            }

            throw;
        }
    }

    public AvatarProfile UpdateProfile(
        string id,
        string displayName,
        string species,
        string breed,
        PersonaProfile persona)
    {
        EnsureLoaded();

        var index = _avatars.FindIndex(item =>
            string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
        {
            throw new KeyNotFoundException("找不到要编辑的角色。");
        }

        var normalizedName = displayName?.Trim() ?? string.Empty;
        if (normalizedName.Length is < 1 or > 40)
        {
            throw new ArgumentException("角色名称长度必须为 1 到 40 个字符。", nameof(displayName));
        }

        var normalizedSpecies = string.IsNullOrWhiteSpace(species) ? "custom" : species.Trim();
        if (normalizedSpecies.Length > 24)
        {
            throw new ArgumentException("角色类型不能超过 24 个字符。", nameof(species));
        }

        var normalizedBreed = breed?.Trim() ?? string.Empty;
        if (normalizedBreed.Length > 40)
        {
            throw new ArgumentException("品种/子类型不能超过 40 个字符。", nameof(breed));
        }

        if (_avatars[index].IsBuiltIn)
        {
            normalizedSpecies = _avatars[index].Species;
            normalizedBreed = _avatars[index].Breed;
        }

        if (!PersonaProfileValidator.TryNormalize(persona, out var normalizedPersona, out var personaError))
        {
            throw new ArgumentException(personaError, nameof(persona));
        }

        var original = Clone(_avatars[index]);
        var updated = Clone(original);
        updated.Name = normalizedName;
        updated.Species = normalizedSpecies;
        updated.Breed = normalizedBreed;
        updated.Persona = normalizedPersona;
        _avatars[index] = updated;

        if (!SaveCatalog())
        {
            _avatars[index] = original;
            throw new IOException("无法保存角色资料，请检查本地目录权限和应用日志。");
        }

        return Clone(updated);
    }

    public bool Delete(string id)
    {
        EnsureLoaded();

        var index = _avatars.FindIndex(item =>
            string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));

        if (index < 0 || _avatars[index].IsBuiltIn ||
            string.Equals(_avatars[index].Id, DefaultAvatarId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var removed = _avatars[index];
        _avatars.RemoveAt(index);

        if (!SaveCatalog())
        {
            _avatars.Insert(index, removed);
            return false;
        }

        try
        {
            var folder = Path.Combine(_avatarsDirectory, removed.Id);
            if (Directory.Exists(folder) && IsWithinDirectory(_avatarsDirectory, Path.GetFullPath(folder)))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (Exception exception)
        {
            Report($"Avatar '{removed.Id}' was removed from the catalog, but its files could not be deleted.", exception);
        }

        return true;
    }

    private List<AvatarProfile> Normalize(List<AvatarProfile> sourceProfiles, int schemaVersion)
    {
        var normalized = new List<AvatarProfile>();
        var sourceDefault = sourceProfiles.FirstOrDefault(profile =>
            string.Equals(profile?.Id, DefaultAvatarId, StringComparison.OrdinalIgnoreCase));
        var defaultAvatar = CreateDefaultBichon();

        if (sourceDefault is not null)
        {
            var name = sourceDefault.Name?.Trim() ?? string.Empty;
            if (name.Length is >= 1 and <= 40)
            {
                defaultAvatar.Name = name;
            }

            var species = sourceDefault.Species?.Trim() ?? string.Empty;
            if (species.Length is >= 1 and <= 24)
            {
                defaultAvatar.Species = species;
            }

            var breed = sourceDefault.Breed?.Trim() ?? string.Empty;
            if (breed.Length <= 40 &&
                (schemaVersion >= CurrentSchemaVersion || !string.IsNullOrWhiteSpace(breed)))
            {
                defaultAvatar.Breed = breed;
            }

            defaultAvatar.Persona = PersonaProfileValidator.NormalizeOrDefault(sourceDefault.Persona);
            defaultAvatar.CreatedAtUtc = sourceDefault.CreatedAtUtc == default
                ? DateTime.UnixEpoch
                : sourceDefault.CreatedAtUtc;
        }

        normalized.Add(defaultAvatar);

        foreach (var profile in sourceProfiles)
        {
            if (profile is null ||
                string.Equals(profile.Id, DefaultAvatarId, StringComparison.OrdinalIgnoreCase) ||
                profile.IsBuiltIn ||
                !Guid.TryParseExact(profile.Id, "N", out _) ||
                !IsSafeCustomImagePath(profile.Id, profile.ImagePath))
            {
                continue;
            }

            if (normalized.Any(item => string.Equals(item.Id, profile.Id, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var name = profile.Name?.Trim() ?? string.Empty;
            if (name.Length is < 1 or > 40)
            {
                continue;
            }

            var species = string.IsNullOrWhiteSpace(profile.Species) ? "custom" : profile.Species.Trim();
            if (species.Length > 24)
            {
                continue;
            }

            var breed = profile.Breed?.Trim() ?? string.Empty;
            if (breed.Length > 40)
            {
                breed = string.Empty;
            }

            normalized.Add(new AvatarProfile
            {
                Id = profile.Id,
                Name = name,
                Species = species,
                Breed = breed,
                Persona = PersonaProfileValidator.NormalizeOrDefault(profile.Persona),
                ImagePath = profile.ImagePath,
                IsBuiltIn = false,
                CreatedAtUtc = profile.CreatedAtUtc == default ? DateTime.UtcNow : profile.CreatedAtUtc
            });
        }

        return normalized;
    }

    private static AvatarProfile CreateDefaultBichon() => new()
    {
        Id = DefaultAvatarId,
        Name = "比熊",
        Species = "dog",
        Breed = "比熊",
        Persona = PersonaProfileValidator.CreateDefault(),
        ImagePath = "characters/bichon/base/bichon-idle.png",
        IsBuiltIn = true,
        CreatedAtUtc = DateTime.UnixEpoch
    };

    private static bool IsSafeCustomImagePath(string id, string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return false;
        }

        var normalized = imagePath.Replace('\\', '/');
        var expectedPrefix = id + "/";
        if (!normalized.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var fileName = normalized[expectedPrefix.Length..];
        if (fileName.Contains('/') || fileName.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        if (!fileName.StartsWith("avatar.", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return SupportedExtensions.Contains(Path.GetExtension(fileName));
    }

    private static bool HasMatchingImageSignature(string path, string extension)
    {
        Span<byte> header = stackalloc byte[8];

        using var stream = File.OpenRead(path);
        var read = stream.Read(header);
        var bytes = header[..read];

        return extension switch
        {
            ".png" => read >= 8 && bytes.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".jpg" or ".jpeg" => read >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            ".bmp" => read >= 2 && bytes[0] == (byte)'B' && bytes[1] == (byte)'M',
            _ => false
        };
    }

    private bool SaveCatalog()
    {
        string? temporaryPath = null;

        try
        {
            Directory.CreateDirectory(_avatarsDirectory);
            temporaryPath = _catalogPath + ".tmp";

            if (_isReadOnly)
            {
                Report("The avatar catalog is read-only because it was created by a newer application version.", null);
                return false;
            }

            var document = new AvatarCatalogDocument
            {
                SchemaVersion = CurrentSchemaVersion,
                Avatars = _avatars.Select(Clone).ToList()
            };

            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, JsonOptions));
            File.Move(temporaryPath, _catalogPath, overwrite: true);
            return true;
        }
        catch (Exception exception)
        {
            Report("Unable to save the local avatar catalog.", exception);

            try
            {
                if (!string.IsNullOrWhiteSpace(temporaryPath) && File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch
            {
                // Best-effort cleanup; preserve the original save failure.
            }

            return false;
        }
    }

    private void EnsureLoaded()
    {
        if (!_isLoaded)
        {
            Load();
        }
    }

    private void Report(string message, Exception? exception)
    {
        try
        {
            _log?.Invoke(message, exception);
        }
        catch
        {
            // Diagnostics must never prevent avatar management from running.
        }
    }

    private static bool IsWithinDirectory(string root, string path)
    {
        var fullRoot = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        return path.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static AvatarProfile Clone(AvatarProfile profile) => new()
    {
        Id = profile.Id,
        Name = profile.Name,
        Species = profile.Species,
        Breed = profile.Breed,
        Persona = PersonaProfileValidator.NormalizeOrDefault(profile.Persona),
        ImagePath = profile.ImagePath,
        IsBuiltIn = profile.IsBuiltIn,
        CreatedAtUtc = profile.CreatedAtUtc
    };

    private sealed class AvatarCatalogDocument
    {
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        public List<AvatarProfile> Avatars { get; set; } = [];
    }
}
