# BichonDesktopPet

A lightweight Windows desktop companion featuring a cute 3D-style Bichon Frise mascot.

> Project status: M0 engineering baseline is complete. M1 image-loading integration is committed; the optimized PNG is prepared, but the binary asset still needs to be uploaded to assets/characters/bichon/base/bichon-idle.png.

## Technology

- C# / .NET 10
- WPF (Windows desktop)
- Shared core library for models, settings, and animation state
- Local JSON settings; optional audio is disabled by default

## Character asset

The runtime view loads Assets/characters/bichon/base/bichon-idle.png from the application output directory. When the file is absent, the application keeps the temporary vector fallback and reports that the 3D asset is pending.

The optimized 420 × 540 transparent PNG was prepared from the user's source image. See docs/M1_ASSET_UPLOAD.md and assets/characters/bichon/base/asset-manifest.json for the exact file and integrity hashes.

## Build (Windows)

~~~powershell
dotnet restore BichonDesktopPet.sln
dotnet build BichonDesktopPet.sln -c Release
dotnet test BichonDesktopPet.sln -c Release
~~~

## Run

~~~powershell
dotnet run --project src/BichonDesktopPet.App/BichonDesktopPet.App.csproj
~~~

See docs/PROJECT.md, docs/ARCHITECTURE.md, and docs/VISUAL_SPECIFICATION.md for the project baseline.
