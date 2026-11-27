using System;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BichonDesktopPet.Core.Models;

namespace BichonDesktopPet.App.Services;

/// <summary>
/// Renders Core behavior presentations using subtle transforms on the current static image.
/// This is not skeletal animation; real tail, limb and body actions require animated assets or 3D rigs.
/// </summary>
public sealed class PetBehaviorAnimationService : IDisposable
{
    private readonly RotateTransform _rotation;
    private readonly ScaleTransform _scale;
    private readonly TranslateTransform _translation;
    private bool _disposed;

    public PetBehaviorAnimationService(
        RotateTransform rotation,
        ScaleTransform scale,
        TranslateTransform translation)
    {
        _rotation = rotation ?? throw new ArgumentNullException(nameof(rotation));
        _scale = scale ?? throw new ArgumentNullException(nameof(scale));
        _translation = translation ?? throw new ArgumentNullException(nameof(translation));
    }

    public void Apply(PetVisualEffect effect)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Reset();

        switch (effect)
        {
            case PetVisualEffect.GreetingBounce:
                _translation.BeginAnimation(
                    TranslateTransform.YProperty,
                    new DoubleAnimation(0, -8, TimeSpan.FromMilliseconds(260))
                    {
                        AutoReverse = true,
                        RepeatBehavior = new RepeatBehavior(2),
                        FillBehavior = FillBehavior.Stop
                    });
                break;

            case PetVisualEffect.HappyBounce:
                _translation.BeginAnimation(
                    TranslateTransform.YProperty,
                    new DoubleAnimation(0, -11, TimeSpan.FromMilliseconds(180))
                    {
                        AutoReverse = true,
                        RepeatBehavior = new RepeatBehavior(2),
                        FillBehavior = FillBehavior.Stop
                    });
                break;

            case PetVisualEffect.ExcitedWiggle:
                _rotation.BeginAnimation(
                    RotateTransform.AngleProperty,
                    new DoubleAnimation(-5, 5, TimeSpan.FromMilliseconds(100))
                    {
                        AutoReverse = true,
                        RepeatBehavior = new RepeatBehavior(3),
                        FillBehavior = FillBehavior.Stop
                    });
                _scale.BeginAnimation(
                    ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(1, 1.035, TimeSpan.FromMilliseconds(150))
                    {
                        AutoReverse = true,
                        RepeatBehavior = new RepeatBehavior(2),
                        FillBehavior = FillBehavior.Stop
                    });
                _scale.BeginAnimation(
                    ScaleTransform.ScaleYProperty,
                    new DoubleAnimation(1, 1.035, TimeSpan.FromMilliseconds(150))
                    {
                        AutoReverse = true,
                        RepeatBehavior = new RepeatBehavior(2),
                        FillBehavior = FillBehavior.Stop
                    });
                break;

            case PetVisualEffect.SleepySway:
                _rotation.BeginAnimation(
                    RotateTransform.AngleProperty,
                    new DoubleAnimation(-0.8, 0.8, TimeSpan.FromSeconds(1.8))
                    {
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    });
                _scale.BeginAnimation(
                    ScaleTransform.ScaleYProperty,
                    new DoubleAnimation(1, 0.99, TimeSpan.FromSeconds(1.8))
                    {
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    });
                break;

            case PetVisualEffect.IdleBreathing:
            default:
                _scale.BeginAnimation(
                    ScaleTransform.ScaleXProperty,
                    new DoubleAnimation(1, 1.012, TimeSpan.FromSeconds(1.6))
                    {
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    });
                _scale.BeginAnimation(
                    ScaleTransform.ScaleYProperty,
                    new DoubleAnimation(1, 0.99, TimeSpan.FromSeconds(1.6))
                    {
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    });
                break;
        }
    }

    private void Reset()
    {
        _rotation.BeginAnimation(RotateTransform.AngleProperty, null);
        _scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        _scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        _translation.BeginAnimation(TranslateTransform.YProperty, null);

        _rotation.Angle = 0;
        _scale.ScaleX = 1;
        _scale.ScaleY = 1;
        _translation.X = 0;
        _translation.Y = 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Reset();
        _disposed = true;
    }
}
