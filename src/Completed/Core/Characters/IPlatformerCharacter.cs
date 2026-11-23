using System.Numerics;

namespace Game_CommandPattern_Completed.Core.Characters;

/// <summary>
/// สัญญาสำหรับตัวละคร Platformer (Receiver ใน Command Pattern)
/// กำหนดเมธอดและคุณสมบัติฟิสิกส์ เพื่อให้คำสั่งสามารถนำไปปฏิบัติการได้ทั้งกับตัวละครจริงและร่างเงา (Polymorphic Action)
/// </summary>
public interface IPlatformerCharacter
{
    string Name { get; }
    Vector2 Position { get; set; }
    Vector2 Velocity { get; set; }
    bool IsGrounded { get; set; }
    int FacingDirection { get; set; } // 1 = Right, -1 = Left
    bool IsRunning { get; }
    bool IsJumping { get; }

    void Move(float directionX);
    void Jump();
    void Stop();
    void Reset(Vector2 startPos);
}

