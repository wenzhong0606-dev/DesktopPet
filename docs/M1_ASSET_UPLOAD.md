# M1 Runtime Asset Upload

## Prepared asset

The exact user-provided Bichon source was found in the user's Library as 圣诞草莓裙比熊萌宠.png. Its high-resolution original remains unchanged.

A runtime derivative has been prepared:

- Filename to use in the repository: bichon-idle.png
- Target folder: assets/characters/bichon/base/
- Dimensions: 320 × 410
- Format: indexed PNG, 128-color palette, transparency retained
- File size: 29,548 bytes
- SHA-256: 8f75b0f2de66c3d6193403327401aa688c3f679effff7b653730a2308deb011a

## Upload

The GitHub connector available in this session can create and update UTF-8 text files, but it does not expose a binary-file upload action. The WPF image-loading code and manifest are already committed. To finish the binary asset step:

1. Download the prepared bichon-320-128.png file supplied with this conversation.
2. Open the repository folder assets/characters/bichon/base/ on GitHub.
3. Upload the file and name it exactly bichon-idle.png.
4. Confirm its SHA-256 matches the value above.

The project file already copies files under assets/ into the app output folder. Once this PNG exists at the target path, the WPF app will load it automatically and hide the temporary vector fallback.

## Acceptance

- GitHub Actions build and tests pass.
- The file exists at the exact target path and its SHA-256 matches.
- On Windows, the 3D Bichon appears with a transparent background and fits the 260 × 333 DIP display area.
- Dragging, click reaction, double-click gift reaction, snow, decorative lights, tray controls, and local settings still work.
