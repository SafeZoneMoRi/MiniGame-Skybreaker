using System.Numerics;
using Raylib_cs;

namespace HeavyWeapons.Core.Entities;

public enum EnemyAttackType
{
    HeavyBomb,
    MachineGunBullet,
    GuidedRocket
}

/// <summary>
/// อาวุธโจมตีจากอากาศยานศัตรู (Air-to-Ground Projectile / Bomb)
/// ปรับระดับความเสียหาย (Damage) ให้รุนแรงและท้าทายสไตล์เกม Heavy Weapon
/// </summary>
public class Bomb
{
    public Vector2 Position { get; private set; }
    public Vector2 Velocity { get; private set; }
    public float Radius { get; }
    public int Damage { get; }
    public bool IsActive { get; set; } = true;
    public EnemyAttackType AttackType { get; }

    private readonly float _gravity;
    private float _timer = 0f;

    public Bomb(Vector2 startPos, Vector2 initialVelocity, EnemyAttackType attackType = EnemyAttackType.HeavyBomb)
    {
        Position = startPos;
        Velocity = initialVelocity;
        AttackType = attackType;

        switch (attackType)
        {
            case EnemyAttackType.MachineGunBullet:
                Radius = 4f;
                Damage = 12;
                _gravity = 0f;      // พุ่งตรงเป็นเลเซอร์
                break;

            case EnemyAttackType.GuidedRocket:
                Radius = 6f;
                Damage = 24;
                _gravity = 60f;     // โค้งเล็กน้อย
                break;

            case EnemyAttackType.HeavyBomb:
            default:
                Radius = 8.5f;
                Damage = 35;        // ปรับดาเมจระเบิดหนักเป็น 35 ตามที่ผู้ใช้ต้องการ
                _gravity = 350f;    // ทิ้งดิ่งตามแรงโน้มถ่วง
                break;
        }
    }

    public void Update(float dt, float groundLevelY)
    {
        Velocity = new Vector2(Velocity.X * 0.995f, Velocity.Y + _gravity * dt);
        Position += Velocity * dt;
        _timer += dt * 15f;

        // ชนพื้นดินหรือออกนอกขอบล่าง
        if (Position.Y >= groundLevelY || Position.X < -60 || Position.X > 1340)
        {
            IsActive = false;
        }
    }

    public void Draw()
    {
        if (!IsActive) return;

        switch (AttackType)
        {
            case EnemyAttackType.MachineGunBullet:
                // กระสุนปืนกลเลเซอร์สีแดงส้ม (Red Tracer)
                Vector2 tracerTail = Position - Vector2.Normalize(Velocity) * 16f;
                Raylib.DrawLineEx(tracerTail, Position, 3.5f, Color.Red);
                Raylib.DrawCircleV(Position, Radius, Color.Yellow);
                break;

            case EnemyAttackType.GuidedRocket:
                // จรวดมิสไซล์พร้อมควันไฟพุ่ง
                Vector2 rocketTail = Position - Vector2.Normalize(Velocity) * 14f;
                Raylib.DrawLineEx(rocketTail, Position, 4.5f, Color.DarkGray);
                Raylib.DrawCircleV(Position, Radius, Color.Orange);
                Color flame = ((int)_timer % 2 == 0) ? Color.Gold : Color.Red;
                Raylib.DrawCircleV(rocketTail, 3.5f, flame);
                break;

            case EnemyAttackType.HeavyBomb:
            default:
                // ลูกระเบิดทิ้งจากบอมเบอร์
                Raylib.DrawCircleV(Position, Radius, new Color(40, 44, 52, 255));
                Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, Radius, Color.DarkGray);

                // ครีบระเบิดด้านบน
                Raylib.DrawTriangle(
                    new Vector2(Position.X - 6, Position.Y - 7),
                    new Vector2(Position.X, Position.Y - 13),
                    new Vector2(Position.X + 6, Position.Y - 7),
                    Color.Red
                );

                // ไฟเตือนกระพริบตรงกลางระเบิด
                Color blink = ((int)_timer % 2 == 0) ? Color.Red : Color.Yellow;
                Raylib.DrawCircleV(Position, 3f, blink);
                break;
        }
    }
}
