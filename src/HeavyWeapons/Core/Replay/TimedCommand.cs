using HeavyWeapons.Core.Commands;

namespace HeavyWeapons.Core.Replay;

/// <summary>
/// โครงสร้างข้อมูลคำสั่งคู่กับเวลา (Timestamped Command)
/// บันทึกเฉพาะ Event ที่เกิดขึ้น ณ เสี้ยววินาทีที่กำหนด (Zero-Alloc Stream)
/// ทำให้ไฟล์ Replay มีขนาดเพียงไม่กี่กิโลไบต์ (KBs) แทนที่จะต้องบันทึกภาพวิดีโอหรือพิกัดทุกเฟรม
/// </summary>
public readonly record struct TimedCommand(float Timestamp, ICommand Command);

