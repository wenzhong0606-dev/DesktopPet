# BichonDesktopPet

A lightweight Windows desktop companion featuring a cute 3D-style Bichon Frise mascot.

> Project status: initial repository bootstrap. The first milestone is a transparent, draggable WPF desktop pet window. The approved character artwork will be added under `assets/` in a later asset-integration milestone.

## Technology

- C# / .NET 10
- WPF (Windows desktop)
- Shared core library for models, settings, and animation state
- Local JSON settings; optional audio is disabled by default

## Build (Windows)

```powershell
dotnet restore BichonDesktopPet.sln
dotnet build BichonDesktopPet.sln -c Release
dotnet test BichonDesktopPet.sln -c Release
```

## Run

```powershell
dotnet run --project src/BichonDesktopPet.App/BichonDesktopPet.App.csproj
```

See `docs/PROJECT.md`, `docs/ARCHITECTURE.md`, and `docs/VISUAL_SPECIFICATION.md` for the project baseline.
