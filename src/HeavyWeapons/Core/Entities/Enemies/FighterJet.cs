using System.Numerics;
using Raylib_cs;

namespace HeavyWeapons.Core.Entities.Enemies;

/// <summary>
/// เครื่องบินขับไล่ความเร็วสูง (Fighter Jet)
/// บินโฉบผ่านท้องฟ้าอย่างรวดเร็ว กราดยิงกระสุนปืนกลเลเซอร์ (Machine Gun Strafing) ลงมาสู่รถถัง
/// </summary>
public class FighterJet : EnemyPlaneBase
{
    private readonly bool _facingRight;
    private float _afterburnerTimer = 0f;
    private float _strafeCooldown = 0.4f;

    public override Rectangle Bounds => new(Position.X - 28f, Position.Y - 10f, 56f, 20f);

    public FighterJet(Vector2 startPos, bool flyRight) 
        : base("Fighter Jet", startPos, new Vector2(flyRight ? 290f : -290f, 0f), health: 40, scoreValue: 100)
    {
        _facingRight = flyRight;
    }

    public override void Update(float dt, Vector2 playerPos)
    {
        base.Update(dt, playerPos);
        _afterburnerTimer += dt * 25f;

        _strafeCooldown -= dt;
        if (_strafeCooldown <= 0f && Position.X > 60f && Position.X < 1220f)
        {
            _strafeCooldown = 0.75f;
            // เล็งกระสุนปืนกลลงมายังพิกัดรถถัง
            Vector2 toPlayer = playerPos - Position;
            Vector2 dir = toPlayer.Length() > 0 ? Vector2.Normalize(toPlayer) : new Vector2(0f, 1f);
            Vector2 bulletVel = dir * 350f;
            OnSpawnEnemyAttack?.Invoke(Position, bulletVel, EnemyAttackType.MachineGunBullet);
        }
    }

    public override void Draw()
    {
        if (!IsActive) return;

        float dir = _facingRight ? 1f : -1f;

        // เปลวไฟไอพ่นหลังเครื่อง (Jet Afterburner)
        Color flameColor = ((int)_afterburnerTimer % 2 == 0) ? Color.Gold : Color.Orange;
        Vector2 engineTip = new(Position.X - dir * 26f, Position.Y);
        Vector2 flameEnd = new(Position.X - dir * (36f + ((int)_afterburnerTimer % 3) * 4f), Position.Y);
        Raylib.DrawLineEx(engineTip, flameEnd, 5f, flameColor);

        // ปีกเครื่องบิน (Delta Wings)
        Vector2 nose = new(Position.X + dir * 28f, Position.Y);
        Vector2 wingTop = new(Position.X - dir * 8f, Position.Y - 14f);
        Vector2 wingBottom = new(Position.X - dir * 8f, Position.Y + 14f);
        Vector2 tail = new(Position.X - dir * 24f, Position.Y);

        Raylib.DrawTriangle(nose, wingTop, tail, new Color(50, 70, 95, 255));
        Raylib.DrawTriangle(nose, tail, wingBottom, new Color(40, 55, 75, 255));

        // ลำตัวหลักและห้องนักบิน (Cockpit)
        Raylib.DrawLineEx(tail, nose, 6f, new Color(180, 195, 215, 255));
        Vector2 cockpit = new(Position.X + dir * 6f, Position.Y - 2f);
        Raylib.DrawCircleV(cockpit, 4f, Color.SkyBlue);

        // หางเสือ (Tail Fin)
        Raylib.DrawTriangle(
            tail,
            new Vector2(tail.X - dir * 4f, tail.Y - 12f),
            new Vector2(tail.X + dir * 8f, tail.Y),
            Color.Red
        );
    }
}
