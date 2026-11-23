using Game_CommandPattern_Completed.Core.Characters;

namespace Game_CommandPattern_Completed.Core.Commands;

/// <summary>
/// คำสั่งเคลื่อนที่แนวราบ (Move Horizontal Command)
/// รองรับทิศทาง: 1 (ขวา), -1 (ซ้าย), หรือ 0 (หยุดเดิน)
/// </summary>
public class MoveCommand : ICommand
{
    public float DirectionX { get; }
    public string Name => DirectionX > 0 ? "MoveRight" : (DirectionX < 0 ? "MoveLeft" : "StopMove");

    public MoveCommand(float directionX)
    {
        DirectionX = directionX;
    }

    public void Execute(IPlatformerCharacter target)
    {
        if (DirectionX != 0f)
        {
            target.Move(DirectionX);
        }
        else
        {
            target.Stop();
        }
    }

    public override string ToString() => DirectionX switch
    {
        > 0 => "MoveRight(+1)",
        < 0 => "MoveLeft(-1)",
        _ => "StopMove(0)"
    };
}
