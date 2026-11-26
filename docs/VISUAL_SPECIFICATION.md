# Visual Specification

## Character identity

The mascot is the user's uploaded white Bichon render in a refined 3D plush / collectible-character aesthetic.

Preserve these identity anchors:

- Large, rounded, cloud-like white curly coat.
- Oversized glossy dark-brown eyes with bright catchlights.
- Small dark-brown nose and tiny gentle mouth.
- Soft pink cheek blush.
- Pink-and-white gingham strawberry outfit.
- Layered white frills and red wavy trim.
- Festive red bow, gold edging, holly accents, and a small gold bell.

## Desktop composition

- Target WPF window: 280 × 360 DIP.
- Render the character inside a 260 × 333 DIP image box, centered horizontally.
- Preserve transparent pixels around the silhouette; do not bake a checkerboard or solid background into the image.
- Use Stretch="Uniform" and high-quality bitmap scaling.
- Keep the small status badge below the character's feet.
- Keep snow and decorative lights behind or above the character without changing the source artwork.

## Asset provenance and runtime derivative

- User source filename: 圣诞草莓裙比熊萌宠.png.
- Source dimensions: 1106 × 1422 RGBA PNG with an alpha channel.
- Source SHA-256: 1e18305fe2245ff2e2a0a011dd11153ec0c2c52bb61ce51565294098cd26b643.
- Source original remains preserved in the user's Library; do not overwrite it.
- Runtime derivative target: assets/characters/bichon/base/bichon-idle.png.
- Runtime derivative: 420 × 540 indexed PNG, 128-color palette with transparency, 45,559 bytes.
- Runtime derivative SHA-256: 049ab8ebe317a4934d60546da4646076053a59af689d3bca6313a6cf3dbf3b3c.
- The derivative was resized with Lanczos and palette-optimized for the 280 × 360 DIP window. It does not replace the high-resolution source.

See assets/characters/bichon/base/asset-manifest.json and docs/M1_ASSET_UPLOAD.md for exact asset status and upload instructions.

## Asset pipeline

- assets/characters/bichon/reference/: user's original, unmodified source.
- assets/characters/bichon/base/: optimized runtime character render and asset manifest.
- assets/characters/bichon/expressions/: blink, happy, curious, sleepy, and surprised expressions.
- assets/characters/bichon/animations/: numbered animation frames or sprite sheets with a manifest.
- assets/characters/bichon/outfits/christmas/: Christmas outfit variants and accessories.
- assets/audio/: optional licensed or original audio only.

Keep source files. Runtime-optimized assets must be derived outputs and must not overwrite the originals.
