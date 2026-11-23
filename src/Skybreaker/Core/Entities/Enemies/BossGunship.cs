using System.Numerics;
using Raylib_cs;

namespace HeavyWeapons.Core.Entities.Enemies;

/// <summary>
/// บอสป้อมปราการลอยฟ้าขนาดยักษ์ (Super Dreadnought Gunship - Final Boss)
/// บอสประจำ Wave 3 ที่มีเกราะหนาพิเศษ อาวุธรอบทิศทาง และเป็นเงื่อนไขสู่การจบเกม (Game Victory)
/// </summary>
public class BossGunship : EnemyPlaneBase
{
    private float _strafeTimer = 0.5f;
    private float _bombTimer = 1.8f;
    private float _moveDirX = 1f;
    private float _flashTimer = 0f;

    public override Rectangle Bounds => new(Position.X - 80f, Position.Y - 24f, 160f, 48f);

    public BossGunship(Vector2 startPos)
        : base("Dreadnought Fortress", startPos, new Vector2(90f, 0f), health: 450, scoreValue: 2000)
    {
        IsBoss = true;
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        _flashTimer = 0.08f;
    }

    public override void Update(float dt, Vector2 playerPos)
    {
        if (_flashTimer > 0f) _flashTimer -= dt;

        // บินลาดตระเวนไป-กลับบนท้องฟ้าอย่างทรงพลัง
        float nextX = Position.X + _moveDirX * 100f * dt;
        if (nextX > 1050f)
        {
            nextX = 1050f;
            _moveDirX = -1f;
        }
        else if (nextX < 230f)
        {
            nextX = 230f;
            _moveDirX = 1f;
        }
        Position = new Vector2(nextX, Position.Y);

        // 1. ปืนกลเลเซอร์คู่จากปีกซ้าย-ขวา
        _strafeTimer -= dt;
        if (_strafeTimer <= 0f)
        {
            _strafeTimer = 0.55f;
            Vector2 leftWing = new(Position.X - 60f, Position.Y + 10f);
            Vector2 rightWing = new(Position.X + 60f, Position.Y + 10f);

            Vector2 toPlayerL = Vector2.Normalize(playerPos - leftWing) * 320f;
            Vector2 toPlayerR = Vector2.Normalize(playerPos - rightWing) * 320f;

            OnSpawnEnemyAttack?.Invoke(leftWing, toPlayerL, EnemyAttackType.MachineGunBullet);
            OnSpawnEnemyAttack?.Invoke(rightWing, toPlayerR, EnemyAttackType.MachineGunBullet);
        }

        // 2. ทิ้งระเบิดหนักกลางลำตัว
        _bombTimer -= dt;
        if (_bombTimer <= 0f)
        {
            _bombTimer = 2.2f;
            Vector2 bombBay = new(Position.X, Position.Y + 24f);
            OnSpawnEnemyAttack?.Invoke(bombBay, new Vector2(0f, 90f), EnemyAttackType.HeavyBomb);
        }
    }

    public override void Draw()
    {
        if (!IsActive) return;

        Color hullColor = _flashTimer > 0f ? Color.White : new Color(45, 50, 65, 255);
        Color accentColor = new(200, 50, 50, 255);

        // ปีกยักษ์คู่ (Giant Armor Wings)
        Rectangle wings = new(Position.X - 75f, Position.Y - 14f, 150f, 28f);
        Raylib.DrawRectangleRounded(wings, 0.3f, 4, hullColor);
        Raylib.DrawRectangleRoundedLines(wings, 0.3f, 4, accentColor);

        // ลำตัวหลัก (Central Heavy Hull)
        Rectangle mainHull = new(Position.X - 40f, Position.Y - 20f, 80f, 40f);
        Raylib.DrawRectangleRounded(mainHull, 0.4f, 4, new Color(30, 35, 48, 255));
        Raylib.DrawRectangleRoundedLines(mainHull, 0.4f, 4, Color.Gold);

        // ห้องบัญชาการเรดาร์ (Command Bridge)
        Raylib.DrawCircleV(new Vector2(Position.X, Position.Y - 10f), 12f, Color.Red);
        Raylib.DrawCircleV(new Vector2(Position.X, Position.Y - 10f), 6f, Color.Gold);

        // ป้อมปืนใหญ่ 2 ข้าง
        Raylib.DrawCircleV(new Vector2(Position.X - 60f, Position.Y + 8f), 7f, Color.DarkGray);
        Raylib.DrawCircleV(new Vector2(Position.X + 60f, Position.Y + 8f), 7f, Color.DarkGray);

        // แถบหลอดเลือดบอสขนาดใหญ่บนหัว
        float hpPercent = (float)Health / MaxHealth;
        Raylib.DrawRectangle((int)Position.X - 70, (int)Position.Y - 38, 140, 8, Color.Black);
        Raylib.DrawRectangle((int)Position.X - 70, (int)Position.Y - 38, (int)(140 * hpPercent), 8, Color.Red);
        Raylib.DrawRectangleLines((int)Position.X - 70, (int)Position.Y - 38, 140, 8, Color.Gold);
        Raylib.DrawText("★ BOSS: DREADNOUGHT ★", (int)Position.X - 64, (int)Position.Y - 54, 11, Color.Gold);
    }
}

