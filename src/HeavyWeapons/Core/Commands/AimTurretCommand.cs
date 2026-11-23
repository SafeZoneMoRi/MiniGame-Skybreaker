using HeavyWeapons.Core.Receivers;

namespace HeavyWeapons.Core.Commands;

/// <summary>
/// คำสั่งปรับองศาป้อมปืน (Concrete Command)
/// แคปซูลค่ามุมการเล็งเป็นองศา (Degrees) เพื่อให้การ Replay มีความสม่ำเสมอแม่นยำ (Deterministic)
/// </summary>
public class AimTurretCommand : ICommand
{
    public float AngleDeg { get; }

    public string Name => "AimTurret";

    public AimTurretCommand(float angleDeg)
    {
        AngleDeg = angleDeg;
    }

    public void Execute(ITankReceiver target)
    {
        target.AimTurret(AngleDeg);
    }

    public override string ToString() => $"[CMD: AimTurret ({AngleDeg:0.0}°)]";
}

