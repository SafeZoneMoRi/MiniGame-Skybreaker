using System.Numerics;
using HeavyWeapons.Core.Entities.Enemies;

namespace HeavyWeapons.Core.Systems;

/// <summary>
/// ตัวกำกับฝูงบินโจมตีต่อเนื่องแบบไม่มีที่สิ้นสุด (Continuous Survival Air Raid Director)
/// สปอว์นอากาศยานศัตรูอย่างต่อเนื่อง ยิ่งเวลาผ่านไปความถี่จะยิ่งเร็วขึ้นและศัตรูจะยิ่งโหดขึ้น
/// ใช้ Deterministic Seed เพื่อให้การ Replay ทุกครั้งสปอว์นศัตรูเหมือนรอบที่เล่น 100%
/// </summary>
public class WaveDirector
{
    public float ElapsedTime { get; private set; } = 0f;
    public int EnemiesKilled { get; set; } = 0;
    public int ThreatLevel => 1 + (int)(ElapsedTime / 25f); // ระดับความอันตรายเพิ่มขึ้นทุกๆ 25 วินาที

    private float _spawnTimer = 0f;
    private int _runSeed = 42;
    private Random _seededRng = new(42);
    private float _bossTimer = 45f;

    public void StartRun(int seed)
    {
        _runSeed = seed;
        ElapsedTime = 0f;
        EnemiesKilled = 0;
        _spawnTimer = 0.6f;
        _bossTimer = 45f;
        _seededRng = new Random(_runSeed);
    }

    public void Update(float dt, Action<EnemyPlaneBase> spawnCallback)
    {
        ElapsedTime += dt;

        // คำนวณความถี่ในการสปอว์น: ยิ่งเวลาผ่านไปศัตรูจะเกิดถี่ขึ้นเรื่อยๆ (จาก 1.5s ลงไปถึง 0.35s)
        float currentInterval = MathF.Max(0.35f, 1.5f - (ElapsedTime * 0.012f));

        _spawnTimer -= dt;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer = currentInterval;
            EnemyPlaneBase enemy = GenerateNextEnemy();
            spawnCallback(enemy);
        }

        // อีเวนต์บอสขนาดยักษ์ (Dreadnought Air Fortress) โผล่มาทุกๆ 50 วินาที
        _bossTimer -= dt;
        if (_bossTimer <= 0f)
        {
            _bossTimer = 55f;
            spawnCallback(new BossGunship(new Vector2(640f, 120f)));
        }
    }

    private EnemyPlaneBase GenerateNextEnemy()
    {
        bool spawnFromLeft = _seededRng.Next(2) == 0;
        float startX = spawnFromLeft ? -80f : 1360f;
        float startY = 85f + (float)_seededRng.NextDouble() * 240f; // เพดานบิน Y: 85 - 325
        Vector2 spawnPos = new(startX, startY);

        int roll = _seededRng.Next(100);

        // ความยากและชนิดศัตรูแปรผันตามเวลาที่รอดชีวิต
        if (ElapsedTime > 45f && roll > 65)
        {
            return new AttackHelicopter(spawnPos, spawnFromLeft);
        }
        else if (ElapsedTime > 20f && roll > 40)
        {
            return new BomberPlane(spawnPos, spawnFromLeft);
        }
        else
        {
            return new FighterJet(spawnPos, spawnFromLeft);
        }
    }
}
