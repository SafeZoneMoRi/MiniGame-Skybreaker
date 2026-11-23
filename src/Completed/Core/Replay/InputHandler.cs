using Raylib_cs;
using Game_CommandPattern_Completed.Core.Commands;

namespace Game_CommandPattern_Completed.Core.Replay;

/// <summary>
/// ตัวแปลงอินพุตจากฮาร์ดแวร์ (Keyboard / Gamepad) เป็นอ็อบเจกต์คำสั่ง (Invoker for Live Player)
/// ช่วยตัดขาดการผูกติดระหว่างฮาร์ดแวร์และคลาสตัวละคร (Decoupling)
/// </summary>
public class InputHandler
{
    private float _lastDirectionX = 0f;

    /// <summary>
    /// ตรวจจับอินพุตแนวราบ: ส่งคืนคำสั่งเมื่อมีการเปลี่ยนแปลงทิศทางเท่านั้น (Event-driven)
    /// </summary>
    public ICommand? CheckHorizontalMovement()
    {
        float currentDir = 0f;

        // 1. คีย์บอร์ด (A/D หรือ ลูกศร)
        if (Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right)) currentDir += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left)) currentDir -= 1f;

        // 2. คอนโทรลเลอร์ (Left Stick หรือ D-Pad)
        if (Raylib.IsGamepadAvailable(0))
        {
            float axisX = Raylib.GetGamepadAxisMovement(0, GamepadAxis.LeftX);
            if (MathF.Abs(axisX) > 0.25f)
            {
                currentDir = axisX > 0 ? 1f : -1f;
            }
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceRight)) currentDir = 1f;
            if (Raylib.IsGamepadButtonDown(0, GamepadButton.LeftFaceLeft)) currentDir = -1f;
        }

        // ตรวจสอบว่ามีการเปลี่ยนทิศทางหรือไม่ (เริ่มเดิน, สลับข้าง, หรือหยุดเดิน)
        if (currentDir != _lastDirectionX)
        {
            _lastDirectionX = currentDir;
            return new MoveCommand(currentDir);
        }

        return null;
    }

    /// <summary>
    /// ตรวจจับปุ่มกระโดด (Space / W / Gamepad Button A)
    /// </summary>
    public ICommand? CheckJump()
    {
        bool isJumpPressed = Raylib.IsKeyPressed(KeyboardKey.Space) ||
                             Raylib.IsKeyPressed(KeyboardKey.W) ||
                             Raylib.IsKeyPressed(KeyboardKey.Up) ||
                             (Raylib.IsGamepadAvailable(0) && (
                                 Raylib.IsGamepadButtonPressed(0, GamepadButton.RightFaceDown) ||
                                 Raylib.IsGamepadButtonPressed(0, GamepadButton.RightTrigger2)));

        if (isJumpPressed)
        {
            return new JumpCommand();
        }

        return null;
    }

    public void Reset()
    {
        _lastDirectionX = 0f;
    }
}

