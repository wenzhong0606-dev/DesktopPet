# M1 Runtime Asset Status

## Source asset

The user-provided source image is `圣诞草莓裙比熊萌宠.png`. The high-resolution original remains preserved in the user's Library and must not be overwritten.

## Repository runtime asset

The runtime path is:

`assets/characters/bichon/base/bichon-idle.png`

The file is present in the `main` branch. The latest rename commit is:

- Commit: `4b48de096f81c39f3c29cbb3816c5b19eb9cc271`
- Message: `Rename bichon-420-128.png to bichon-idle.png`
- Git tree file size: 132,158 bytes
- Git blob SHA: `11bc5493f21febd72706948f1dbfc848fcd9f9ae`

## Integrity discrepancy — verification still required

The earlier prepared derivative record stated:

- Dimensions: 420 × 540
- File size: 45,559 bytes
- SHA-256: `049ab8ebe317a4934d60546da4646076053a59af689d3bca6313a6cf3dbf3b3c`

The committed file size is 132,158 bytes, so the earlier size/hash record must not be treated as verified for the committed binary. Recalculate the local file's dimensions and SHA-256 after pulling `main`, then update `asset-manifest.json` with the measured values.

## Local verification

From the repository root in PowerShell:

```powershell
Get-Item .\assets\characters\bichon\base\bichon-idle.png | Select-Object Name,Length
Get-FileHash .\assets\characters\bichon\base\bichon-idle.png -Algorithm SHA256
```

Then run the app and confirm that the actual PNG is displayed (not the vector fallback), the alpha background is transparent, and the character fits the 260 × 333 DIP image area.

## Acceptance

- [ ] The file exists at the exact repository path.
- [ ] Actual dimensions and SHA-256 have been recorded.
- [ ] The image loads from the application output directory.
- [ ] The transparent silhouette renders correctly on the Windows desktop.
- [ ] Dragging, click reaction, double-click gift reaction, snow, decorative lights, tray controls, and local settings continue to work.
