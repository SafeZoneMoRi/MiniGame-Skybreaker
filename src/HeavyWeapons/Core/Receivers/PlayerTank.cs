using System.Numerics;
using Raylib_cs;

namespace HeavyWeapons.Core.Receivers;

/// <summary>
/// ตัวละครรถถังผู้เล่นสด (Live Player Tank)
/// รับคำสั่งสดจากฮาร์ดแวร์ผ่าน InputHandler
/// </summary>
public class PlayerTank : TankBase
{
    private static readonly Color CamoGreen = new(65, 125, 60, 255);
    private static readonly Color ArmorDarkGreen = new(35, 75, 30, 255);
    private static readonly Color SteelGray = new(120, 130, 140, 255);

    public PlayerTank(Vector2 startPos) : base("Heavy Gunner", startPos)
    {
    }

    public void Draw()
    {
        if (!IsAlive) return;

        // วาดตัวรถถังสีเขียวลายพรางกองทัพ
        base.Draw(CamoGreen, ArmorDarkGreen, SteelGray);

        // วาดป้ายชื่อและสถานะเหนือตัวรถ
        Raylib.DrawText("P1 TANK", (int)Position.X - 22, (int)Position.Y - 54, 12, Color.Gold);

        // วาดแถบ HP จิ๋วเหนือตัวถัง
        float hpPercent = (float)Health / MaxHealth;
        Raylib.DrawRectangle((int)Position.X - 24, (int)Position.Y - 40, 48, 5, new Color(40, 40, 40, 200));
        Color hpColor = hpPercent > 0.5f ? Color.Green : (hpPercent > 0.25f ? Color.Yellow : Color.Red);
        Raylib.DrawRectangle((int)Position.X - 24, (int)Position.Y - 40, (int)(48 * hpPercent), 5, hpColor);
    }
}

