# M1 Runtime Asset Status

## Source asset

The user-provided source image is `圣诞草莓裙比熊萌宠.png`. The high-resolution original remains preserved in the user's Library and must not be overwritten.

## Repository runtime asset

The runtime path is:

`assets/characters/bichon/base/bichon-idle.png`

The file is present in the `main` branch. Its binary was read from the repository and independently decoded for PNG metadata and SHA-256:

- Dimensions: 420 × 540
- PNG format: 8-bit RGBA (alpha channel present)
- File size: 132,158 bytes
- SHA-256: `326d10ace28d53e8a4289b9f103c648ecb79d6b1b32e4a8f76d715793b97d210`
- Git blob SHA: `11bc5493f21febd72706948f1dbfc848fcd9f9ae`

The earlier prepared derivative record (45,559 bytes, SHA-256 `049ab8ebe317a4934d60546da4646076053a59af689d3bca6313a6cf3dbf3b3c`) does not match the committed file and is retained only as historical metadata. The current committed file's integrity values above are authoritative.

## Local verification

From the repository root in PowerShell:

```powershell
Get-Item .\assets\characters\bichon\base\bichon-idle.png | Select-Object Name,Length
Get-FileHash .\assets\characters\bichon\base\bichon-idle.png -Algorithm SHA256
```

Then run the app and confirm that the actual PNG is displayed (not the vector fallback), the alpha background is transparent, and the character fits the 260 × 333 DIP image area.

## Acceptance

- [x] The file exists at the exact repository path.
- [x] Actual dimensions and SHA-256 have been recorded.
- [ ] The image loads from the application output directory on Windows.
- [ ] The transparent silhouette renders correctly on the Windows desktop.
- [ ] Dragging, click reaction, double-click gift reaction, snow, decorative lights, tray controls, and local settings continue to work.
