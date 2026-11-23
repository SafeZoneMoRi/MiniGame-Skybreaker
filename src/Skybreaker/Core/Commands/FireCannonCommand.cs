using HeavyWeapons.Core.Receivers;

namespace HeavyWeapons.Core.Commands;

/// <summary>
/// คำสั่งยิงปืนใหญ่หลัก (Concrete Command)
/// ยิงกระสุนออกจากปลายกระบอกปืนใหญ่ตามมุมปัจจุบัน
/// </summary>
public class FireCannonCommand : ICommand
{
    public string Name => "FireCannon";

    public void Execute(ITankReceiver target)
    {
        target.Fire();
    }

    public override string ToString() => "[CMD: FireCannon]";
}

