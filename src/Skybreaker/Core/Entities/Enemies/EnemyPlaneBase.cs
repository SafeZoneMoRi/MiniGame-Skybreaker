using System.Numerics;
using Raylib_cs;

namespace HeavyWeapons.Core.Entities.Enemies;

/// <summary>
/// คลาสแม่ของอากาศยานศัตรู (Polymorphic Enemy Base)
/// ควบคุมวงจรชีวิต, การเคลื่อนที่บนท้องฟ้า, การเล็งโจมตีรถถัง, และการรับความเสียหาย
/// </summary>
public abstract class EnemyPlaneBase
{
    public string TypeName { get; protected set; }
    public Vector2 Position { get; protected set; }
    public Vector2 Velocity { get; protected set; }
    public int Health { get; protected set; }
    public int MaxHealth { get; protected set; }
    public int ScoreValue { get; protected set; }
    public bool IsActive { get; set; } = true;
    public bool IsDestroyed => Health <= 0;
    public bool IsBoss { get; protected set; } = false;

    public Action<Vector2, Vector2, EnemyAttackType>? OnSpawnEnemyAttack;

    public abstract Rectangle Bounds { get; }

    protected EnemyPlaneBase(string typeName, Vector2 startPos, Vector2 velocity, int health, int scoreValue)
    {
        TypeName = typeName;
        Position = startPos;
        Velocity = velocity;
        Health = health;
        MaxHealth = health;
        ScoreValue = scoreValue;
    }

    public virtual void TakeDamage(int damage)
    {
        Health = Math.Max(0, Health - damage);
        if (IsDestroyed)
        {
            IsActive = false;
        }
    }

    public virtual void Update(float dt, Vector2 playerPos)
    {
        Position += Velocity * dt;

        // หากบินทะลุจอออกไปไกล ให้ลบทิ้ง
        if (Position.X < -160 || Position.X > 1440 || Position.Y < -120 || Position.Y > 750)
        {
            IsActive = false;
        }
    }

    public abstract void Draw();
}
