# Visual Specification

## Character identity

The approved mascot is a cute white Bichon Frise in a refined 3D cartoon / collectible plush aesthetic.

Preserve these identity anchors across all future generations:

- Large, rounded, cloud-like white curly coat.
- Oversized glossy dark-brown eyes with bright catchlights.
- Small dark-brown nose and tiny gentle mouth.
- Soft pink cheek blush.
- Pink-and-white gingham outfit with strawberry details.
- Layered white frills and red wavy trim.
- Festive red bow, gold edging, holly accents, and a small gold bell.

## Desktop composition

- Target WPF window: 280 × 360 DIP.
- Full-body front-facing character, centered and readable at small size.
- Keep transparent pixels around the silhouette; do not bake a checkerboard into the image.
- Keep the character silhouette and outfit details inside the safe area.
- Avoid text, watermarks, decorative frames, or a baked-in desktop background.
- Use separate layers or frame assets for animation when practical.

## Asset pipeline

- assets/characters/bichon/reference/: user's original, unmodified source.
- assets/characters/bichon/base/: approved neutral full-body character render.
- assets/characters/bichon/expressions/: blink, happy, curious, sleepy, and surprised expressions.
- assets/characters/bichon/animations/: numbered frames or sprite sheets with a manifest.
- assets/characters/bichon/outfits/christmas/: Christmas outfit variants and accessories.
- assets/audio/: optional licensed or original audio only.

Keep source files. Runtime-optimized PNG/WebP files must be derived outputs and must not overwrite the originals.
