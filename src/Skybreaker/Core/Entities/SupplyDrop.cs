using System.Numerics;
using Raylib_cs;

namespace HeavyWeapons.Core.Entities;

public enum SupplyType
{
    ArmorRepair   // กล่องซ่อมแซมเกราะ (+35 HP)
}

/// <summary>
/// กล่องเสบียงทิ้งร่มชูชีพ (Parachuting Supply Drop)
/// ลอยช้าๆ ลงมาจากฟากฟ้า ให้ผู้เล่นบังคับรถถังไปเก็บเพื่อฟื้นฟูพลังชีวิต
/// </summary>
public class SupplyDrop
{
    public Vector2 Position { get; private set; }
    public SupplyType Type { get; }
    public bool IsActive { get; set; } = true;
    public Rectangle Bounds => new(Position.X - 14f, Position.Y - 14f, 28f, 28f);

    private const float FallSpeed = 85f;
    private float _swayTimer = 0f;
    private readonly float _initialX;

    public SupplyDrop(Vector2 startPos, SupplyType type = SupplyType.ArmorRepair)
    {
        Position = startPos;
        _initialX = startPos.X;
        Type = type;
    }

    public void Update(float dt, float groundLevelY)
    {
        _swayTimer += dt * 3f;

        // แกว่งไป-มาตามลมระหว่างที่ร่มชูชีพค่อยๆ ร่อนลงพื้น
        float nextX = _initialX + MathF.Sin(_swayTimer) * 16f;
        float nextY = Position.Y + FallSpeed * dt;

        Position = new Vector2(nextX, nextY);

        // เมื่อตกถึงพื้นดินค้างไว้สักครู่ หรือหลุดนอกจอ
        if (Position.Y >= groundLevelY - 14f)
        {
            Position = new Vector2(Position.X, groundLevelY - 14f);
        }
    }

    public void Draw()
    {
        if (!IsActive) return;

        // 1. วาดร่มชูชีพ (Parachute Dome)
        Vector2 chuteCenter = new(Position.X, Position.Y - 22f);
        Color chuteColor = Color.Green;
        Raylib.DrawCircleSector(chuteCenter, 18f, 180f, 360f, 16, chuteColor);
        Raylib.DrawCircleSectorLines(chuteCenter, 18f, 180f, 360f, 16, Color.White);

        // เชือกร่ม (Strings)
        Raylib.DrawLineEx(new Vector2(Position.X - 16f, Position.Y - 22f), Position, 1.2f, Color.LightGray);
        Raylib.DrawLineEx(new Vector2(Position.X + 16f, Position.Y - 22f), Position, 1.2f, Color.LightGray);

        // 2. กล่องสัมภาระ (Supply Crate)
        Rectangle crateRect = new(Position.X - 12f, Position.Y - 12f, 24f, 24f);
        Raylib.DrawRectangleRounded(crateRect, 0.2f, 4, new Color(35, 65, 40, 255));
        Raylib.DrawRectangleRoundedLines(crateRect, 0.2f, 4, Color.Lime);

        // 3. สัญลักษณ์บนกล่อง (กากบาทพยาบาลสีเขียว +)
        Raylib.DrawRectangle((int)Position.X - 2, (int)Position.Y - 7, 4, 14, Color.Lime);
        Raylib.DrawRectangle((int)Position.X - 7, (int)Position.Y - 2, 14, 4, Color.Lime);
    }
}

