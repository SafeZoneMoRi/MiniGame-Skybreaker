using System.Numerics;
using Raylib_cs;

namespace HeavyWeapons.Core.Entities.Enemies;

/// <summary>
/// เฮลิคอปเตอร์จู่โจม (Attack Helicopter)
/// บินลอยลำแกว่งขึ้นลง และยิงจรวดมิสไซล์ (Guided Rocket) ล็อคเป้าใส่รถถัง
/// </summary>
public class AttackHelicopter : EnemyPlaneBase
{
    private readonly bool _facingRight;
    private readonly float _baseY;
    private float _bobTimer = 0f;
    private float _rotorAngle = 0f;
    private float _rocketTimer = 1.0f;

    public override Rectangle Bounds => new(Position.X - 26f, Position.Y - 12f, 52f, 24f);

    public AttackHelicopter(Vector2 startPos, bool flyRight)
        : base("Gunship Heli", startPos, new Vector2(flyRight ? 170f : -170f, 0f), health: 50, scoreValue: 180)
    {
        _facingRight = flyRight;
        _baseY = startPos.Y;
    }

    public override void Update(float dt, Vector2 playerPos)
    {
        _bobTimer += dt * 3.5f;
        _rotorAngle += dt * 1400f;

        // เคลื่อนที่แนวนอนพร้อมแกว่งขึ้นลงตามคลื่นไซน์
        float nextX = Position.X + Velocity.X * dt;
        float nextY = _baseY + MathF.Sin(_bobTimer) * 22f;
        Position = new Vector2(nextX, nextY);

        if (Position.X < -150 || Position.X > 1430)
        {
            IsActive = false;
        }

        _rocketTimer -= dt;
        if (_rocketTimer <= 0f && Position.X > 100f && Position.X < 1180f)
        {
            _rocketTimer = 2.0f;
            Vector2 launchPos = new(Position.X, Position.Y + 12f);
            Vector2 toPlayer = playerPos - launchPos;
            Vector2 dir = toPlayer.Length() > 0 ? Vector2.Normalize(toPlayer) : new Vector2(0f, 1f);
            Vector2 rocketVel = dir * 280f;
            OnSpawnEnemyAttack?.Invoke(launchPos, rocketVel, EnemyAttackType.GuidedRocket);
        }
    }

    public override void Draw()
    {
        if (!IsActive) return;

        float dir = _facingRight ? 1f : -1f;

        // 1. ใบพัดหลักด้านบน (Spinning Rotor)
        Vector2 mast = new(Position.X, Position.Y - 14f);
        Raylib.DrawLineEx(new Vector2(Position.X, Position.Y - 6f), mast, 3f, Color.Black);
        float rotorWidth = MathF.Cos(_rotorAngle * (MathF.PI / 180f)) * 36f;
        Raylib.DrawLineEx(new Vector2(mast.X - rotorWidth, mast.Y), new Vector2(mast.X + rotorWidth, mast.Y), 3f, Color.LightGray);

        // 2. ตัวห้องเครื่อง (Heli Pod)
        Raylib.DrawCircleV(Position, 13f, new Color(55, 65, 75, 255));
        Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, 13f, Color.SkyBlue);

        // 3. หางและใบพัดหาง (Tail Boom)
        Vector2 tailEnd = new(Position.X - dir * 28f, Position.Y - 4f);
        Raylib.DrawLineEx(Position, tailEnd, 4f, new Color(45, 55, 65, 255));
        Raylib.DrawCircleV(tailEnd, 3.5f, Color.Red);

        // 4. หน้าต่างห้องนักบิน (Canopy)
        Vector2 canopy = new(Position.X + dir * 6f, Position.Y - 2f);
        Raylib.DrawCircleV(canopy, 5.5f, Color.Gold);

        // 5. สกีลงจอด (Landing Skids)
        Vector2 skidL = new(Position.X - 14f, Position.Y + 14f);
        Vector2 skidR = new(Position.X + 14f, Position.Y + 14f);
        Raylib.DrawLineEx(skidL, skidR, 2.5f, Color.DarkGray);
    }
}
