using System.Numerics;
using Raylib_cs;

namespace Game_CommandPattern_Completed.Core.Characters;

/// <summary>
/// ตัวละครผู้เล่นสด (Live Player) - รับคำสั่งสดจากฮาร์ดแวร์ผ่าน InputHandler
/// </summary>
public class MarioPlayer : PlatformerCharacterBase
{
    public MarioPlayer(Vector2 startPos) : base("Speedrunner", startPos)
    {
    }

    public void Draw(Texture2D texture)
    {
        // วาดตัวละครด้วยสีปกติ (ทึบแสง 100%)
        base.Draw(texture, Color.White);

        // วาดป้ายชื่อเหนือศีรษะ
        Raylib.DrawText("P1", (int)Position.X + 10, (int)Position.Y - 24, 12, Color.Gold);
    }
}

