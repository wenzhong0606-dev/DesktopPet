using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BichonDesktopPet.App.Services;

public sealed class SnowAnimationService : IDisposable
{
    private const int MaximumParticles = 26;
    private readonly Canvas _canvas;
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private readonly List<Particle> _particles = [];

    public SnowAnimationService(Canvas canvas)
    {
        _canvas = canvas;
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _timer.Tick += OnTick;
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
        }
        else
        {
            _timer.Stop();
            _particles.Clear();
            _canvas.Children.Clear();
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        while (_particles.Count < MaximumParticles)
        {
            AddParticle();
        }

        var width = _canvas.ActualWidth > 0 ? _canvas.ActualWidth : 280;
        var height = _canvas.ActualHeight > 0 ? _canvas.ActualHeight : 360;

        for (var index = _particles.Count - 1; index >= 0; index--)
        {
            var particle = _particles[index];
            particle.Y += particle.Speed;
            particle.X += particle.Drift;

            if (particle.Y > height + 8 || particle.X < -8 || particle.X > width + 8)
            {
                _canvas.Children.Remove(particle.Shape);
                _particles.RemoveAt(index);
                continue;
            }

            Canvas.SetLeft(particle.Shape, particle.X);
            Canvas.SetTop(particle.Shape, particle.Y);
        }
    }

    private void AddParticle()
    {
        var radius = 1.2 + _random.NextDouble() * 2.2;
        var shape = new Ellipse
        {
            Width = radius * 2,
            Height = radius * 2,
            Fill = System.Windows.Media.Brushes.White,
            Opacity = 0.35 + _random.NextDouble() * 0.55,
            IsHitTestVisible = false
        };

        var particle = new Particle
        {
            X = _random.NextDouble() * 280,
            Y = -_random.NextDouble() * 360,
            Speed = 0.7 + _random.NextDouble() * 1.6,
            Drift = -0.25 + _random.NextDouble() * 0.5,
            Shape = shape
        };

        _particles.Add(particle);
        _canvas.Children.Add(shape);
        Canvas.SetLeft(shape, particle.X);
        Canvas.SetTop(shape, particle.Y);
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
        _particles.Clear();
        _canvas.Children.Clear();
    }

    private sealed class Particle
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Speed { get; set; }
        public double Drift { get; set; }
        public required Ellipse Shape { get; init; }
    }
}
