using System.Numerics;
using Raylib_cs;

namespace HeavyWeapons.Core.Entities;

/// <summary>
/// กระสุนปืนใหญ่หลัก (Tank Projectile)
/// เคลื่อนที่ด้วยความเร็วสูงตามมุมของป้อมปืน รองรับทั้งกระสุนของรถถังจริงและรถถังร่างเงา (Ghost)
/// </summary>
public class Projectile
{
    public Vector2 Position { get; private set; }
    public Vector2 Velocity { get; }
    public float Radius { get; } = 4f;
    public int Damage { get; } = 25;
    public bool IsGhost { get; }
    public bool IsActive { get; set; } = true;

    private readonly Color _coreColor;
    private readonly Color _glowColor;

    public Projectile(Vector2 startPos, float angleDeg, float speed, bool isGhost)
    {
        Position = startPos;
        IsGhost = isGhost;

        float rad = angleDeg * (MathF.PI / 180f);
        Velocity = new Vector2(MathF.Cos(rad) * speed, MathF.Sin(rad) * speed);

        if (isGhost)
        {
            _coreColor = new Color(180, 240, 255, 180);
            _glowColor = new Color(0, 180, 255, 90);
        }
        else
        {
            _coreColor = new Color(255, 230, 100, 255);
            _glowColor = new Color(255, 100, 0, 140);
        }
    }

    public void Update(float dt)
    {
        Position += Velocity * dt;

        // หากออกนอกขอบจอ ให้ทำลายทิ้ง
        if (Position.X < -50 || Position.X > 1330 || Position.Y < -50 || Position.Y > 750)
        {
            IsActive = false;
        }
    }

    public void Draw()
    {
        if (!IsActive) return;

        // วาดหางแสง (Tracer Line)
        Vector2 tail = Position - Vector2.Normalize(Velocity) * 12f;
        Raylib.DrawLineEx(tail, Position, 3f, _glowColor);

        // วาดหัวกระสุนเรืองแสง
        Raylib.DrawCircleV(Position, Radius + 2f, _glowColor);
        Raylib.DrawCircleV(Position, Radius, _coreColor);
    }
}

