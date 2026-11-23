using Game_CommandPattern_Completed.Core.Commands;

namespace Game_CommandPattern_Completed.Core.Replay;

/// <summary>
/// โครงสร้างข้อมูลคำสั่งคู่กับเวลา (Timestamped Command)
/// บันทึกเฉพาะ Event ที่เกิดขึ้น ณ เสี้ยววินาทีที่ระบุ ทำให้ไม่ต้องบันทึกพิกัดทุกเฟรม 60 FPS (Zero-Alloc Stream)
/// </summary>
public readonly record struct TimedCommand(float Timestamp, ICommand Command);

