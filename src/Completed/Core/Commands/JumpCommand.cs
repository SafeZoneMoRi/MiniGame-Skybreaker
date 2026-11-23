using Game_CommandPattern_Completed.Core.Characters;

namespace Game_CommandPattern_Completed.Core.Commands;

/// <summary>
/// คำสั่งกระโดด (Jump Command)
/// สั่งให้ตัวละครออกแรงกระโดดตามกฎฟิสิกส์เมื่อเท้าแตะพื้น
/// </summary>
public class JumpCommand : ICommand
{
    public string Name => "Jump";

    public void Execute(IPlatformerCharacter target)
    {
        target.Jump();
    }

    public override string ToString() => "Jump()";
}
