using HeavyWeapons.Core.Receivers;

namespace HeavyWeapons.Core.Commands;

/// <summary>
/// สัญญาแม่บทของ Command Pattern (GoF) สำหรับเกม Heavy Weapons
/// แปลงการกระทำของผู้เล่นหรือระบบ (เคลื่อนที่, เล็งป้อมปืน, ยิง) ให้เป็น First-Class Object
/// เพื่อให้สามารถนำไปส่งต่อ, เข้าคิว, บันทึกลง Replay Tape และฉายซ้ำลงบน GhostTank ได้
/// </summary>
public interface ICommand
{
    string Name { get; }

    /// <summary>
    /// สั่งการทำงานของคำสั่งลงบนรถถังเป้าหมาย (Receiver)
    /// </summary>
    void Execute(ITankReceiver target);
}

