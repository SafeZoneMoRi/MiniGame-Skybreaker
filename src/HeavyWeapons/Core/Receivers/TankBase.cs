using System.Numerics;
using Raylib_cs;

namespace HeavyWeapons.Core.Receivers;

/// <summary>
/// คลาสฐานของรถถัง (Base Receiver)
/// บรรจุสถานะฟิสิกส์, แถบพลังชีวิต, องศาป้อมปืน, และตรรกะการวาดตัวรถแบบ Procedural Vector Art
/// </summary>
public abstract class TankBase : ITankReceiver
{
    public string Name { get; protected set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; protected set; }
    public float TurretAngleDeg { get; set; } = -90f; // เล็งตรงขึ้นฟ้าเป็นค่าเริ่มต้น
    public int Health { get; set; } = 100;
    public int MaxHealth { get; protected set; } = 100;
    public bool IsAlive => Health > 0;

    public const float MoveSpeed = 280f;
    public const float ChassisWidth = 68f;
    public const float ChassisHeight = 22f;
    public const float BarrelLength = 34f;

    public Action<Vector2, float, bool>? OnFireAction;

    public Rectangle Bounds => new(
        Position.X - ChassisWidth / 2f,
        Position.Y - ChassisHeight - 12f,
        ChassisWidth,
        ChassisHeight + 12f
    );

    public Vector2 TurretPivot => new(Position.X, Position.Y - ChassisHeight - 2f);

    public Vector2 MuzzleTip
    {
        get
        {
            float rad = TurretAngleDeg * (MathF.PI / 180f);
            return TurretPivot + new Vector2(MathF.Cos(rad) * BarrelLength, MathF.Sin(rad) * BarrelLength);
        }
    }

    protected TankBase(string name, Vector2 startPos)
    {
        Name = name;
        Position = startPos;
    }

    public virtual void Move(float directionX)
    {
        Velocity = new Vector2(directionX * MoveSpeed, 0f);
    }

    public virtual void AimTurret(float angleDeg)
    {
        TurretAngleDeg = angleDeg;
    }

    public virtual void Fire()
    {
        if (!IsAlive) return;
        bool isGhost = this is GhostTank;
        OnFireAction?.Invoke(MuzzleTip, TurretAngleDeg, isGhost);
    }

    public virtual void TakeDamage(int damage)
    {
        Health = Math.Max(0, Health - damage);
    }

    public virtual void Stop()
    {
        Velocity = Vector2.Zero;
    }

    public virtual void Reset(Vector2 startPos)
    {
        Position = startPos;
        Velocity = Vector2.Zero;
        TurretAngleDeg = -90f;
        Health = MaxHealth;
    }

    public virtual void UpdatePhysics(float dt, float minX, float maxX)
    {
        if (!IsAlive) return;

        Position += Velocity * dt;
        Position = new Vector2(Math.Clamp(Position.X, minX, maxX), Position.Y);
    }

    /// <summary>
    /// วาดตัวรถถัง ป้อมปืน และล้อสายพาน
    /// </summary>
    public virtual void Draw(Color bodyColor, Color accentColor, Color barrelColor)
    {
        if (!IsAlive) return;

        // 1. วาดกระบอกปืนใหญ่ (Cannon Barrel)
        Vector2 pivot = TurretPivot;
        Vector2 tip = MuzzleTip;
        Raylib.DrawLineEx(pivot, tip, 8f, barrelColor);
        Raylib.DrawCircleV(tip, 4.5f, barrelColor); // ปลอกหัวปืนใหญ่

        // 2. วาดโดมป้อมปืน (Turret Dome)
        Raylib.DrawCircleV(pivot, 14f, bodyColor);
        Raylib.DrawCircleLines((int)pivot.X, (int)pivot.Y, 14f, accentColor);

        // 3. วาดตัวถังรถ (Chassis Hull)
        Rectangle hullRect = new(Position.X - ChassisWidth / 2f + 4f, Position.Y - ChassisHeight, ChassisWidth - 8f, ChassisHeight - 6f);
        Raylib.DrawRectangleRounded(hullRect, 0.3f, 4, bodyColor);
        Raylib.DrawRectangleRoundedLines(hullRect, 0.3f, 4, accentColor);

        // 4. ล้อสายพาน (Treads)
        Rectangle treadRect = new(Position.X - ChassisWidth / 2f, Position.Y - 10f, ChassisWidth, 10f);
        Raylib.DrawRectangleRounded(treadRect, 0.5f, 4, new Color(30, 35, 42, (int)bodyColor.A));
        Raylib.DrawRectangleRoundedLines(treadRect, 0.5f, 4, accentColor);

        // ดุมล้อ (Road Wheels 4 ล้อ)
        for (int i = 0; i < 4; i++)
        {
            float wx = Position.X - ChassisWidth / 2f + 10f + i * 16f;
            Raylib.DrawCircleV(new Vector2(wx, Position.Y - 5f), 4f, accentColor);
        }
    }
}

