# M1 Runtime Asset Upload

## Prepared asset

The exact user-provided Bichon source was found in the user's Library as 圣诞草莓裙比熊萌宠.png. Its high-resolution original remains unchanged.

A runtime derivative has been prepared:

- Filename to use in the repository: bichon-idle.png
- Target folder: assets/characters/bichon/base/
- Dimensions: 420 × 540
- Format: indexed PNG, 128-color palette, transparency retained
- File size: 45,559 bytes
- SHA-256: 049ab8ebe317a4934d60546da4646076053a59af689d3bca6313a6cf3dbf3b3c

## Upload

The WPF image-loading code and asset manifest are already committed. The prepared binary image has not yet been added to the Git repository.

1. Download the prepared image attachment named bichon-420-128.png.
2. Open the repository folder assets/characters/bichon/base/ on GitHub.
3. Upload the file and name it exactly bichon-idle.png.
4. Confirm its SHA-256 matches the value above.

The project file already copies files under assets/ into the app output folder. Once this PNG exists at the target path, the WPF app will load it automatically and hide the temporary vector fallback.

## Acceptance

- GitHub Actions build and tests pass.
- The file exists at the exact target path and its SHA-256 matches.
- On Windows, the 3D Bichon appears with a transparent background and fits the 260 × 333 DIP display area.
- Dragging, click reaction, double-click gift reaction, snow, decorative lights, tray controls, and local settings still work.
