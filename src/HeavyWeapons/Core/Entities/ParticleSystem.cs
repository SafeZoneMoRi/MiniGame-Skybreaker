using System.Numerics;
using Raylib_cs;

namespace HeavyWeapons.Core.Entities;

/// <summary>
/// ระบบอนุภาคประกายไฟและควันระเบิด (Arcade Particle System)
/// สร้างผลกระทบทางสายตา (Game Juice) สไตล์ Heavy Weapon
/// </summary>
public class ParticleSystem
{
    private readonly struct Particle
    {
        public readonly Vector2 Position;
        public readonly Vector2 Velocity;
        public readonly float Size;
        public readonly float Life;
        public readonly float MaxLife;
        public readonly Color StartColor;
        public readonly Color EndColor;

        public Particle(Vector2 pos, Vector2 vel, float size, float life, float maxLife, Color startColor, Color endColor)
        {
            Position = pos;
            Velocity = vel;
            Size = size;
            Life = life;
            MaxLife = maxLife;
            StartColor = startColor;
            EndColor = endColor;
        }

        public Particle(Vector2 pos, Vector2 vel, float size, float maxLife, Color startColor, Color endColor)
            : this(pos, vel, size, maxLife, maxLife, startColor, endColor)
        {
        }

        public Particle Step(float dt)
        {
            return new Particle(
                Position + Velocity * dt,
                Velocity * (1f - dt * 1.5f),
                MathF.Max(1f, Size * (Life / MaxLife)),
                Life - dt,
                MaxLife,
                StartColor,
                EndColor
            );
        }

        public bool IsAlive => Life > 0f;

        public Color CurrentColor
        {
            get
            {
                float t = 1f - (Life / MaxLife);
                byte r = (byte)(StartColor.R + (EndColor.R - StartColor.R) * t);
                byte g = (byte)(StartColor.G + (EndColor.G - StartColor.G) * t);
                byte b = (byte)(StartColor.B + (EndColor.B - StartColor.B) * t);
                byte a = (byte)(StartColor.A * (Life / MaxLife));
                return new Color(r, g, b, a);
            }
        }
    }

    private readonly List<Particle> _particles = new(512);
    private readonly Random _rand = new();

    public void SpawnExplosion(Vector2 origin, int count = 28, float maxSpeed = 220f, bool isCyan = false)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (float)(_rand.NextDouble() * Math.PI * 2.0);
            float speed = (float)(_rand.NextDouble() * maxSpeed + 40f);
            Vector2 vel = new(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);

            float size = (float)(_rand.NextDouble() * 5.0 + 3.0);
            float life = (float)(_rand.NextDouble() * 0.45 + 0.25);

            Color start = isCyan ? new Color(150, 240, 255, 255) : new Color(255, 220, 80, 255);
            Color end = isCyan ? new Color(20, 80, 200, 0) : new Color(220, 40, 20, 0);

            _particles.Add(new Particle(origin, vel, size, life, start, end));
        }
    }

    public void SpawnMuzzleFlash(Vector2 tipPos, float angleDeg, bool isCyan)
    {
        float rad = angleDeg * (MathF.PI / 180f);
        Vector2 forward = new(MathF.Cos(rad), MathF.Sin(rad));

        for (int i = 0; i < 6; i++)
        {
            float spread = (float)((_rand.NextDouble() - 0.5) * 0.4);
            Vector2 vel = (forward + new Vector2(-forward.Y * spread, forward.X * spread)) * (float)(_rand.NextDouble() * 120f + 60f);
            Color col = isCyan ? Color.SkyBlue : Color.Orange;
            _particles.Add(new Particle(tipPos, vel, 3.5f, 0.12f, Color.White, col));
        }
    }

    public void Update(float dt)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            Particle p = _particles[i].Step(dt);
            if (p.IsAlive)
            {
                _particles[i] = p;
            }
            else
            {
                _particles.RemoveAt(i);
            }
        }
    }

    public void Draw()
    {
        foreach (var p in _particles)
        {
            Raylib.DrawCircleV(p.Position, p.Size, p.CurrentColor);
        }
    }

    public void Clear()
    {
        _particles.Clear();
    }
}

