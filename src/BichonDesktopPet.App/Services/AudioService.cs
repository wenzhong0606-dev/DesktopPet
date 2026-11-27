using System.IO;
using System.Windows.Media;

namespace BichonDesktopPet.App.Services;

/// <summary>
/// Optional local background audio. The feature remains unavailable until an authorized
/// audio file is added to assets/audio and copied beside the application.
/// </summary>
public sealed class AudioService : IDisposable
{
    private readonly MediaPlayer _player = new();

    public AudioService()
    {
        _player.Volume = 0.25;
        _player.MediaEnded += (_, _) =>
        {
            _player.Position = TimeSpan.Zero;
            _player.Play();
        };
    }

    public bool IsAvailable => File.Exists(
        Path.Combine(AppContext.BaseDirectory, "Assets", "audio", "holiday-ambient.mp3"));

    public bool SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            _player.Stop();
            return true;
        }

        var audioPath = Path.Combine(AppContext.BaseDirectory, "Assets", "audio", "holiday-ambient.mp3");
        if (!File.Exists(audioPath))
        {
            return false;
        }

        _player.Open(new Uri(audioPath));
        _player.Play();
        return true;
    }

    public void Dispose()
    {
        _player.Stop();
        _player.Close();
    }
}
