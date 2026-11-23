using System.Numerics;
using Raylib_cs;
using HeavyWeapons.Core.Commands;

namespace HeavyWeapons.Core.Replay;

/// <summary>
/// ตัวแปลงอินพุตจากฮาร์ดแวร์ (Keyboard / Mouse) ให้กลายเป็นอ็อบเจกต์คำสั่ง (Invoker for Live Player)
/// ช่วยตัดขาดการผูกติดระหว่างฮาร์ดแวร์และคลาสรถถัง (Decoupling)
/// </summary>
public class InputHandler
{
    private float _lastDirectionX = 0f;
    private float _lastAimAngleDeg = -90f; // เริ่มต้นชี้ตรงขึ้นฟ้า 90 องศา (-90 deg)
    private float _fireCooldown = 0f;
    private const float FireRate = 0.18f; // หน่วงเวลาการยิง 0.18 วินาทีต่อหนึ่งนัด

    public void Update(float dt)
    {
        if (_fireCooldown > 0f)
        {
            _fireCooldown -= dt;
        }
    }

    /// <summary>
    /// ตรวจจับการเคลื่อนที่แนวนอน: ส่งคำสั่งเมื่อมีการเปลี่ยนทิศทางเท่านั้น (Event-driven เพื่อประหยัด Stream)
    /// </summary>
    public ICommand? CheckHorizontalMovement()
    {
        float currentDir = 0f;

        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) currentDir += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) currentDir -= 1f;

        if (currentDir != _lastDirectionX)
        {
            _lastDirectionX = currentDir;
            return new MoveTankCommand(currentDir);
        }

        return null;
    }

    /// <summary>
    /// ตรวจจับการเล็งป้อมปืนจากตำแหน่งเมาส์: ส่งคำสั่งเมื่อมุมเบี่ยงเบนเกิน 1.5 องศา
    /// </summary>
    public ICommand? CheckTurretAim(Vector2 tankPivotPos)
    {
        Vector2 mousePos = Raylib.GetMousePosition();
        float dx = mousePos.X - tankPivotPos.X;
        float dy = mousePos.Y - tankPivotPos.Y;

        // คำนวณมุมเป็นองศา
        float angleDeg = MathF.Atan2(dy, dx) * (180f / MathF.PI);

        // จำกัดมุมให้ชี้ขึ้นท้องฟ้าเท่านั้น (-170 ถึง -10 องศา)
        if (angleDeg > 0f)
        {
            angleDeg = dx < 0 ? -170f : -10f;
        }
        angleDeg = Math.Clamp(angleDeg, -170f, -10f);

        if (MathF.Abs(angleDeg - _lastAimAngleDeg) >= 1.5f)
        {
            _lastAimAngleDeg = angleDeg;
            return new AimTurretCommand(angleDeg);
        }

        return null;
    }

    /// <summary>
    /// ตรวจจับการกดยิงปืนใหญ่ (Spacebar หรือ คลิกซ้าย)
    /// </summary>
    public ICommand? CheckFire()
    {
        bool isFireRequested = Raylib.IsMouseButtonDown(MouseButton.Left) ||
                               Raylib.IsKeyDown(KeyboardKey.Space) ||
                               Raylib.IsKeyPressed(KeyboardKey.W) ||
                               Raylib.IsKeyPressed(KeyboardKey.Up);

        if (isFireRequested && _fireCooldown <= 0f)
        {
            _fireCooldown = FireRate;
            return new FireCannonCommand();
        }

        return null;
    }

    /// <summary>
    /// ตรวจจับการกดปล่อยระเบิดนิวเคลียร์ (คลิกขวา หรือ ปุ่ม B / F)
    /// </summary>
    public ICommand? CheckNuke()
    {
        bool isNukePressed = Raylib.IsMouseButtonPressed(MouseButton.Right) ||
                             Raylib.IsKeyPressed(KeyboardKey.B) ||
                             Raylib.IsKeyPressed(KeyboardKey.F);

        if (isNukePressed)
        {
            return new DeployNukeCommand();
        }

        return null;
    }

    public void Reset()
    {
        _lastDirectionX = 0f;
        _lastAimAngleDeg = -90f;
        _fireCooldown = 0f;
    }
}

