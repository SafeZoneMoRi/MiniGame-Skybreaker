using System.Numerics;
using Raylib_cs;

namespace HeavyWeapons.Core.Entities.Enemies;

/// <summary>
/// เครื่องบินทิ้งระเบิดยุทธศาสตร์ขนาดใหญ่ (Heavy Bomber)
/// เคลื่อนที่ช้า มีเกราะหนา และทิ้งลูกระเบิดทำลายล้างดิ่งลงมาสู่รถถัง
/// </summary>
public class BomberPlane : EnemyPlaneBase
{
    private readonly bool _facingRight;
    private float _bombTimer = 0.8f;
    private const float BombInterval = 1.6f;

    public override Rectangle Bounds => new(Position.X - 40f, Position.Y - 14f, 80f, 28f);

    public BomberPlane(Vector2 startPos, bool flyRight)
        : base("Heavy Bomber", startPos, new Vector2(flyRight ? 135f : -135f, 0f), health: 80, scoreValue: 250)
    {
        _facingRight = flyRight;
    }

    public override void Update(float dt, Vector2 playerPos)
    {
        base.Update(dt, playerPos);

        _bombTimer -= dt;
        // ปล่อยลูกระเบิดหนักเมื่ออยู่บนน่านฟ้า
        if (_bombTimer <= 0f && Position.X > 80f && Position.X < 1200f)
        {
            _bombTimer = BombInterval;
            Vector2 bombBay = new(Position.X, Position.Y + 14f);
            Vector2 bombVelocity = new(Velocity.X * 0.35f, 60f);
            OnSpawnEnemyAttack?.Invoke(bombBay, bombVelocity, EnemyAttackType.HeavyBomb);
        }
    }

    public override void Draw()
    {
        if (!IsActive) return;

        float dir = _facingRight ? 1f : -1f;

        // ลำตัวหลักขนาดใหญ่ (Heavy Fuselage)
        Rectangle body = new(Position.X - 36f, Position.Y - 8f, 72f, 16f);
        Raylib.DrawRectangleRounded(body, 0.4f, 4, new Color(75, 85, 60, 255));
        Raylib.DrawRectangleRoundedLines(body, 0.4f, 4, new Color(45, 55, 35, 255));

        // ปีกยาวคู่ (Large Wings)
        Vector2 wingCenter = new(Position.X - dir * 4f, Position.Y);
        Raylib.DrawRectangle((int)wingCenter.X - 10, (int)wingCenter.Y - 22, 20, 44, new Color(90, 100, 75, 255));

        // หัวเครื่องบินโค้งมน
        Vector2 nose = new(Position.X + dir * 38f, Position.Y);
        Raylib.DrawCircleV(nose, 7f, new Color(60, 70, 50, 255));

        // กระจกห้องนักบิน
        Raylib.DrawCircleV(new Vector2(Position.X + dir * 24f, Position.Y - 4f), 4.5f, Color.Gold);

        // แถบ HP ของบอมเบอร์
        float hpPercent = (float)Health / MaxHealth;
        Raylib.DrawRectangle((int)Position.X - 25, (int)Position.Y - 26, 50, 4, Color.DarkGray);
        Raylib.DrawRectangle((int)Position.X - 25, (int)Position.Y - 26, (int)(50 * hpPercent), 4, Color.Orange);
    }
}
