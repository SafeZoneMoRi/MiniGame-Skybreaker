using HeavyWeapons.Core.Receivers;

namespace HeavyWeapons.Core.Commands;

/// <summary>
/// คำสั่งปล่อยระเบิดนิวเคลียร์ล้างจอ (Special Weapon Command)
/// ซิกเนเจอร์อันเป็นเอกลักษณ์ของเกม Heavy Weapon: ทำลายล้างอากาศยานศัตรูทั้งหมดบนท้องฟ้า
/// </summary>
public class DeployNukeCommand : ICommand
{
    public string Name => "DeployNuke";

    public void Execute(ITankReceiver target)
    {
        target.DeployNuke();
    }

    public override string ToString() => "[CMD: ★ DEPLOY NUKE ★]";
}

