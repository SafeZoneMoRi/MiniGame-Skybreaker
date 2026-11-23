using System.Numerics;
using Raylib_cs;

namespace Game_CommandPattern_Completed.Core.Characters;

/// <summary>
/// ตัวละครร่างเงา (Ghost Runner) - เล่นซ้ำจากการป้อน Command ใน Ghost Tape
/// ถูก Render ให้มีลักษณะโปร่งแสงสีฟ้าคราม (Cyan Tint) เลียนแบบ Ghost Replay ในเกมสปีดรันจริง
/// </summary>
public class GhostRunner : PlatformerCharacterBase
{
    public bool IsActive { get; set; } = false;

    // สีฟ้าครามกึ่งโปร่งใสสำหรับร่างเงา (Sky Blue Semi-Transparent)
    private static readonly Color GhostTint = new(130, 215, 255, 140);

    public GhostRunner(Vector2 startPos) : base("PB Ghost", startPos)
    {
    }

    public void Draw(Texture2D texture)
    {
        if (!IsActive) return;

        // วาดตัวละครด้วยสีโปรงแสงสีฟ้าคราม
        base.Draw(texture, GhostTint);

        // วาดป้ายชื่อบอกว่าเป็นร่างเงา Personal Best
        Raylib.DrawText("PB GHOST", (int)Position.X - 6, (int)Position.Y - 24, 10, new Color(130, 220, 255, 220));
    }
}

