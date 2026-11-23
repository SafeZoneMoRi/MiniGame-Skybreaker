using Game_CommandPattern_Completed.Core.Characters;

namespace Game_CommandPattern_Completed.Core.Commands;

/// <summary>
/// สัญญาแม่บทของ Command Pattern (GoF)
/// แปลงการกระทำเป็นข้อมูลระดับ First-Class Object ที่สามารถส่งต่อ, เก็บลงคิว, และจำลองภาพซ้ำ (Replay) ได้
/// </summary>
public interface ICommand
{
    string Name { get; }

    /// <summary>
    /// ปฏิบัติการคำสั่งลงบนตัวละครเป้าหมาย (Receiver/Target)
    /// </summary>
    void Execute(IPlatformerCharacter target);
}

