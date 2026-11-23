using System.Numerics;
using Raylib_cs;

namespace Game_CommandPattern_Completed.Core.Characters;

/// <summary>
/// คลาสฐานสำหรับตัวละครแบบ 2D Platformer (Base Receiver)
/// บรรจุฟิสิกส์การเคลื่อนที่ กราวิตี้ ระบบชนขอบบล็อก และแอนิเมชันสไปรต์ชีต 32x32
/// รองรับการสืบทอดโดย MarioPlayer (ผู้เล่นสด) และ GhostRunner (ร่างเงาจากบันทึกคำสั่ง)
/// </summary>
public abstract class PlatformerCharacterBase : IPlatformerCharacter
{
    public string Name { get; protected set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public bool IsGrounded { get; set; }
    public int FacingDirection { get; set; } = 1; // 1 = หันขวา, -1 = หันซ้าย

    public bool IsRunning => MathF.Abs(Velocity.X) > 15f;
    public bool IsJumping => !IsGrounded;

    // ขอบเขตการชน (Collision Hitbox) ขนาด 20x30 พิกเซล กึ่งกลางสไปรต์ 32x32
    public Rectangle Bounds => new Rectangle(Position.X + 6, Position.Y + 2, 20, 30);

    // ค่าพารามิเตอร์ทางฟิสิกส์ (Tuning Parameters)
    protected const float MoveSpeed = 220f;
    protected const float JumpForce = -430f;
    protected const float Gravity = 950f;
    protected const float MaxFallSpeed = 650f;

    protected float TargetDirectionX = 0f;

    // ระบบอนิเมชันสไปรต์ชีต (80x64 พิกเซลต่อเฟรม, ชีตรวม 800x448)
    private enum AnimState
    {
        Idle,
        Running,
        Jumping,
        Falling
    }

    private AnimState _currentAnimState = AnimState.Idle;
    private float _animTimer = 0f;
    private int _currentFrame = 0;

    protected PlatformerCharacterBase(string name, Vector2 startPos)
    {
        Name = name;
        Position = startPos;
        Velocity = Vector2.Zero;
        IsGrounded = true;
    }

    public virtual void Move(float directionX)
    {
        TargetDirectionX = directionX;
        if (directionX > 0.05f) FacingDirection = 1;
        else if (directionX < -0.05f) FacingDirection = -1;
    }

    public virtual void Jump()
    {
        if (IsGrounded)
        {
            Velocity = new Vector2(Velocity.X, JumpForce);
            IsGrounded = false;
        }
    }

    public virtual void Stop()
    {
        TargetDirectionX = 0f;
        Velocity = Vector2.Zero;
    }

    public virtual void Reset(Vector2 startPos)
    {
        Position = startPos;
        Velocity = Vector2.Zero;
        TargetDirectionX = 0f;
        FacingDirection = 1;
        IsGrounded = true;
        _animTimer = 0f;
        _currentFrame = 0;
        _currentAnimState = AnimState.Idle;
    }

    /// <summary>
    /// อัปเดตฟิสิกส์แบบแยกแกน X และ Y เพื่อป้องกันปัญหาตกทะลุบล็อก (AABB Collision Resolution)
    /// </summary>
    public virtual void UpdatePhysics(float dt, IReadOnlyList<Rectangle> solidBlocks)
    {
        // 1. คำนวณความเร็วแกน X (มีแรงเฉื่อยและการตอบสนองรวดเร็ว)
        float targetVelX = TargetDirectionX * MoveSpeed;
        float accelRate = IsGrounded ? 22f : 14f;
        float newVelX = MathF.Abs(Velocity.X - targetVelX) < 1f 
            ? targetVelX 
            : Velocity.X + (targetVelX - Velocity.X) * MathF.Min(1f, accelRate * dt);

        Velocity = new Vector2(newVelX, Velocity.Y);

        // เลื่อนแกน X และตรวจการชนกำแพง
        Position = new Vector2(Position.X + Velocity.X * dt, Position.Y);
        foreach (var block in solidBlocks)
        {
            if (Raylib.CheckCollisionRecs(Bounds, block))
            {
                if (Velocity.X > 0) // ชนกำแพงขวา
                {
                    Position = new Vector2(block.X - 26, Position.Y);
                }
                else if (Velocity.X < 0) // ชนกำแพงซ้าย
                {
                    Position = new Vector2(block.X + block.Width - 6, Position.Y);
                }
                Velocity = new Vector2(0, Velocity.Y);
            }
        }

        // 2. คำนวณกราวิตี้และความเร็วแกน Y
        float newVelY = MathF.Min(Velocity.Y + Gravity * dt, MaxFallSpeed);
        Velocity = new Vector2(Velocity.X, newVelY);

        // เลื่อนแกน Y และตรวจการชนพื้น/เพดาน
        Position = new Vector2(Position.X, Position.Y + Velocity.Y * dt);
        IsGrounded = false;

        foreach (var block in solidBlocks)
        {
            if (Raylib.CheckCollisionRecs(Bounds, block))
            {
                if (Velocity.Y > 0) // กำลังตกลงมาเหยียบพื้น
                {
                    Position = new Vector2(Position.X, block.Y - 32);
                    Velocity = new Vector2(Velocity.X, 0);
                    IsGrounded = true;
                }
                else if (Velocity.Y < 0) // กระโดดโหม่งเพดาน
                {
                    Position = new Vector2(Position.X, block.Y + block.Height - 2);
                    Velocity = new Vector2(Velocity.X, 0);
                }
            }
        }

        // 3. อัปเดตเฟรมแอนิเมชัน
        UpdateAnimation(dt);
    }

    private void UpdateAnimation(float dt)
    {
        // กำหนดสถานะแอนิเมชันตามการเคลื่อนที่และฟิสิกส์
        AnimState newState;
        if (IsJumping)
        {
            newState = Velocity.Y < 0 ? AnimState.Jumping : AnimState.Falling;
        }
        else if (IsRunning)
        {
            newState = AnimState.Running;
        }
        else
        {
            newState = AnimState.Idle;
        }

        // หากมีการเปลี่ยนสถานะ ให้รีเซ็ตเฟรมและตัวนับเวลา เพื่อไม่ให้เกิดอาการสะดุดหรือกระพริบ
        if (newState != _currentAnimState)
        {
            _currentAnimState = newState;
            _currentFrame = 0;
            _animTimer = 0f;
        }

        _animTimer += dt;

        float frameTime = _currentAnimState switch
        {
            AnimState.Idle => 0.16f,
            AnimState.Running => 0.08f,
            AnimState.Jumping => 0.08f,
            AnimState.Falling => 0.08f,
            _ => 0.10f
        };

        int maxFrames = _currentAnimState switch
        {
            AnimState.Idle => 5,      // Row 0 มี 5 เฟรม
            AnimState.Running => 8,   // Row 2 มี 8 เฟรม
            AnimState.Jumping => 4,   // Row 3 มี 4 เฟรม
            AnimState.Falling => 4,   // Row 4 มี 4 เฟรม
            _ => 1
        };

        if (_animTimer >= frameTime)
        {
            _animTimer = 0f;
            if (_currentAnimState == AnimState.Jumping || _currentAnimState == AnimState.Falling)
            {
                // ขณะลอยกลางอากาศ ค้างที่เฟรมสุดท้ายจนกว่าจะสัมผัสพื้น ไม่วนกลับไปเฟรมเริ่มแรก
                _currentFrame = Math.Min(_currentFrame + 1, maxFrames - 1);
            }
            else
            {
                _currentFrame = (_currentFrame + 1) % maxFrames;
            }
        }
    }

    /// <summary>
    /// วาดตัวละครจาก Sprite Sheet ขนาด 800x448 (10 คอลัมน์ x 7 แถว, ขนาดเฟรมละ 80x64 พิกเซล)
    /// จัดตำแหน่งปลายเท้าและกึ่งกลางลำตัวให้ตรงกับ Collision Box อย่างแม่นยำ
    /// </summary>
    public virtual void Draw(Texture2D texture, Color tint)
    {
        int row;
        int maxFrames;

        switch (_currentAnimState)
        {
            case AnimState.Jumping:
                row = 3;
                maxFrames = 4;
                break;
            case AnimState.Falling:
                row = 4;
                maxFrames = 4;
                break;
            case AnimState.Running:
                row = 2;
                maxFrames = 8;
                break;
            case AnimState.Idle:
            default:
                row = 0;
                maxFrames = 5;
                break;
        }

        int frame = (_currentAnimState == AnimState.Jumping || _currentAnimState == AnimState.Falling)
            ? Math.Clamp(_currentFrame, 0, maxFrames - 1)
            : _currentFrame % maxFrames;

        // สไปรต์ชีตขนาดเฟรมละ 80x64 พิกเซล
        const int frameWidth = 80;
        const int frameHeight = 64;

        // คำนวณ Source Rectangle (หาก FacingDirection = -1 จะกลับด้านพิกเซลซ้ายขวา)
        Rectangle sourceRec = new Rectangle(
            frame * frameWidth,
            row * frameHeight,
            FacingDirection * frameWidth,
            frameHeight
        );

        // จัดตำแหน่ง Destination Rectangle:
        // ตัวละครสูง 64px (ปลายเท้าอยู่ที่ Y=64 ของเฟรม) ให้ตรงกับพื้นผิว Position.Y + 32
        // กึ่งกลางตัวละครอยู่ที่ X=40 ของเฟรม ให้ตรงกับกึ่งกลาง Hitbox Position.X + 16
        Rectangle destRec = new Rectangle(
            Position.X - 24,
            Position.Y - 32,
            frameWidth,
            frameHeight
        );

        Raylib.DrawTexturePro(texture, sourceRec, destRec, Vector2.Zero, 0f, tint);
    }
}

