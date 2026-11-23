using System.Numerics;
using Raylib_cs;

namespace HeavyWeapons.Core.Receivers;

/// <summary>
/// ตัวละครร่างเงารถถัง (Ghost Tank / Replay Tank)
/// เล่นซ้ำจากการป้อนกระแสคำสั่ง (Command Stream) ใน Master Tape ของรอบการเล่นล่าสุด
/// รองรับทั้งโหมด Replay Theater (สีเด่นชัด 100%) และโหมด PB Ghost เคียงข้างผู้เล่น (Cyan Semi-Transparent)
/// </summary>
public class GhostTank : TankBase
{
    public bool IsActive { get; set; } = false;
    public bool IsTheaterMode { get; set; } = false;

    // โทนสีฟ้าครามโปร่งแสงสำหรับร่างเงา Personal Best
    private static readonly Color GhostBody = new(100, 200, 255, 130);
    private static readonly Color GhostAccent = new(180, 240, 255, 170);
    private static readonly Color GhostBarrel = new(80, 180, 240, 140);

    // โทนสีเขียวทหารเข้มสำหรับ Replay Theater
    private static readonly Color ReplayBody = new(70, 135, 65, 255);
    private static readonly Color ReplayAccent = new(255, 215, 0, 255);
    private static readonly Color ReplayBarrel = new(140, 150, 160, 255);

    public GhostTank(Vector2 startPos) : base("Replay Ghost Tank", startPos)
    {
    }

    public void Draw()
    {
        if (!IsActive || !IsAlive) return;

        if (IsTheaterMode)
        {
            // วาดตัวรถถังชัดเจน 100% ในโหมด Replay Theater
            base.Draw(ReplayBody, ReplayAccent, ReplayBarrel);
            Raylib.DrawText("★ REPLAY TANK ★", (int)Position.X - 44, (int)Position.Y - 50, 11, Color.Gold);

            // แสดงหลอด HP ของรอบ Replay
            float hpPercent = (float)Health / MaxHealth;
            Raylib.DrawRectangle((int)Position.X - 24, (int)Position.Y - 38, 48, 5, new Color(40, 40, 40, 200));
            Color hpColor = hpPercent > 0.5f ? Color.Green : (hpPercent > 0.25f ? Color.Yellow : Color.Red);
            Raylib.DrawRectangle((int)Position.X - 24, (int)Position.Y - 38, (int)(48 * hpPercent), 5, hpColor);
        }
        else
        {
            // วาดตัวรถถังสีฟ้าครามโปร่งแสง
            base.Draw(GhostBody, GhostAccent, GhostBarrel);
            Raylib.DrawText("PB GHOST", (int)Position.X - 26, (int)Position.Y - 50, 11, new Color(130, 225, 255, 210));
        }
    }
}
