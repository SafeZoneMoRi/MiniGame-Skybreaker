using System.Numerics;
using Raylib_cs;

namespace HeavyWeapons.Core.Receivers;

/// <summary>
/// สัญญาสำหรับยานรบรถถัง (Receiver ใน Command Pattern)
/// กำหนดเมธอดและคุณสมบัติพื้นฐาน เพื่อให้คำสั่ง (Commands) สามารถกระทำลงบนตัวละครจริง (PlayerTank)
/// และตัวละครร่างเงา (GhostTank) ได้แบบ Polymorphic โดยไม่ผูกติดกับ Input ฮาร์ดแวร์โดยตรง
/// </summary>
public interface ITankReceiver
{
    string Name { get; }
    Vector2 Position { get; set; }
    float TurretAngleDeg { get; set; }
    int Health { get; set; }
    int MaxHealth { get; }
    bool IsAlive { get; }
    Rectangle Bounds { get; }

    void Move(float directionX);
    void AimTurret(float angleDeg);
    void Fire();
    void TakeDamage(int damage);
    void Stop();
    void Reset(Vector2 startPos);
}

