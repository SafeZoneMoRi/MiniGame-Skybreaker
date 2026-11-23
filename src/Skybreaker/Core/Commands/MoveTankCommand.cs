using HeavyWeapons.Core.Receivers;

namespace HeavyWeapons.Core.Commands;

/// <summary>
/// คำสั่งเคลื่อนที่รถถัง (Concrete Command)
/// แคปซูลทิศทางแนวนอน: -1.0f (ซ้าย), 0.0f (หยุดนิ่ง), +1.0f (ขวา)
/// </summary>
public class MoveTankCommand : ICommand
{
    public float DirectionX { get; }

    public string Name => DirectionX switch
    {
        > 0f => "MoveRight",
        < 0f => "MoveLeft",
        _ => "StopMoving"
    };

    public MoveTankCommand(float directionX)
    {
        DirectionX = Math.Clamp(directionX, -1f, 1f);
    }

    public void Execute(ITankReceiver target)
    {
        target.Move(DirectionX);
    }

    public override string ToString() => $"[CMD: {Name} (Dir={DirectionX:0.0})]";
}

