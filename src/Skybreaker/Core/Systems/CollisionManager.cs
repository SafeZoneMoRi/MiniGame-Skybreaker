using System.Numerics;
using Raylib_cs;
using HeavyWeapons.Core.Entities;
using HeavyWeapons.Core.Entities.Enemies;
using HeavyWeapons.Core.Receivers;

namespace HeavyWeapons.Core.Systems;

/// <summary>
/// ระบบตรวจสอบการชนและคำนวณความเสียหาย (Collision & Damage Manager)
/// ประสานงานระหว่างกระสุน, ระเบิด, อากาศยาน, และรถถัง พร้อมกระจายอนุภาคการระเบิด
/// </summary>
public class CollisionManager
{
    public static void ProcessCollisions(
        List<Projectile> projectiles,
        List<Bomb> bombs,
        List<EnemyPlaneBase> enemies,
        TankBase? targetTank,
        bool isReplayMode,
        ParticleSystem particles,
        ref int currentScore,
        WaveDirector waveDirector,
        Action<string, string> logCallback)
    {
        // 1. ตรวจสอบกระสุนปืนใหญ่ vs เครื่องบินศัตรู
        foreach (var proj in projectiles)
        {
            if (!proj.IsActive) continue;

            foreach (var enemy in enemies)
            {
                if (!enemy.IsActive) continue;

                if (Raylib.CheckCollisionCircleRec(proj.Position, proj.Radius, enemy.Bounds))
                {
                    proj.IsActive = false;
                    enemy.TakeDamage(proj.Damage);

                    // สร้างสะเก็ดระเบิดเล็กตรงจุดกระแทก
                    particles.SpawnExplosion(proj.Position, count: 12, maxSpeed: 140f, isCyan: proj.IsGhost);

                    if (enemy.IsDestroyed)
                    {
                        // ระเบิดใหญ่เมื่อเครื่องบินตก
                        particles.SpawnExplosion(enemy.Position, count: 36, maxSpeed: 260f, isCyan: proj.IsGhost);

                        if (!proj.IsGhost || isReplayMode)
                        {
                            if (!isReplayMode) currentScore += enemy.ScoreValue;
                            logCallback(isReplayMode ? "Replay" : "Combat", $"Shot down {enemy.TypeName}! +{enemy.ScoreValue} pts");
                        }
                        else
                        {
                            logCallback("Ghost", $"PB Ghost destroyed {enemy.TypeName}!");
                        }

                        waveDirector.EnemiesKilled++;
                    }
                    break;
                }
            }
        }

        // 2. ตรวจสอบลูกระเบิดศัตรู vs รถถังเป้าหมาย
        if (targetTank != null && targetTank.IsAlive)
        {
            foreach (var bomb in bombs)
            {
                if (!bomb.IsActive) continue;

                if (Raylib.CheckCollisionCircleRec(bomb.Position, bomb.Radius, targetTank.Bounds))
                {
                    bomb.IsActive = false;
                    targetTank.TakeDamage(bomb.Damage);
                    particles.SpawnExplosion(bomb.Position, count: 30, maxSpeed: 200f);

                    if (!isReplayMode)
                    {
                        logCallback("Damage", $"Direct Hit! Took {bomb.Damage} damage. HP: {targetTank.Health}/{targetTank.MaxHealth}");

                        if (!targetTank.IsAlive)
                        {
                            particles.SpawnExplosion(targetTank.Position, count: 60, maxSpeed: 300f);
                            logCallback("System", "TANK DESTROYED! Mission Failed.");
                        }
                    }
                    else
                    {
                        logCallback("Replay", $"[Replay Tank Hit] Took {bomb.Damage} damage.");
                    }
                }
            }
        }
    }

    public static void ProcessSupplyPickups(
        List<SupplyDrop> supplyDrops,
        PlayerTank player,
        ParticleSystem particles,
        Action<string, string> logCallback)
    {
        if (!player.IsAlive) return;

        foreach (var crate in supplyDrops)
        {
            if (!crate.IsActive) continue;

            if (Raylib.CheckCollisionRecs(crate.Bounds, player.Bounds))
            {
                crate.IsActive = false;
                player.Health = Math.Min(player.MaxHealth, player.Health + 35);
                particles.SpawnExplosion(crate.Position, count: 25, maxSpeed: 150f, isCyan: true);
                logCallback("Supply", $"Repairs Secured! +35 Armor (HP: {player.Health}/{player.MaxHealth})");
            }
        }
    }
}

