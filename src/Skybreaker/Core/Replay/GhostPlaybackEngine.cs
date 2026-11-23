using System.Numerics;
using HeavyWeapons.Core.Receivers;

namespace HeavyWeapons.Core.Replay;

/// <summary>
/// เครื่องยนต์ฉายภาพซ้ำของร่างเงามืออาชีพ (Ghost Playback Engine - Invoker for Ghost)
/// อ่านกระแสคำสั่งจาก Master Tape ตามเวลาที่ผ่านไป แล้วสั่ง Execute ลงบน GhostTank แบบเรียลไทม์
/// </summary>
public class GhostPlaybackEngine
{
    private readonly List<TimedCommand> _ghostTape = new(512);
    private int _playbackIndex = 0;

    public bool HasTape => _ghostTape.Count > 0;
    public bool IsActive { get; private set; }
    public int BestScore { get; private set; } = 0;
    public int TotalCommands => _ghostTape.Count;
    public bool HasReachedEnd => HasTape && _playbackIndex >= _ghostTape.Count;
    public float LastCommandTimestamp => _ghostTape.Count > 0 ? _ghostTape[^1].Timestamp : 0f;

    public void LoadNewPersonalBest(int bestScore, List<TimedCommand> newTape)
    {
        BestScore = bestScore;
        _ghostTape.Clear();
        _ghostTape.AddRange(newTape);
    }

    public void StartPlayback(GhostTank ghost, Vector2 startPos)
    {
        _playbackIndex = 0;
        IsActive = HasTape;
        ghost.Reset(startPos);
        ghost.IsActive = HasTape;
    }

    public void Update(float currentWaveTime, GhostTank ghost)
    {
        if (!IsActive || !HasTape || !ghost.IsActive) return;

        // ประมวลผลคำสั่งทั้งหมดที่ถึงเวลาประทับ (Timestamp) ในเสี้ยววินาทีนี้
        while (_playbackIndex < _ghostTape.Count && currentWaveTime >= _ghostTape[_playbackIndex].Timestamp)
        {
            _ghostTape[_playbackIndex].Command.Execute(ghost);
            _playbackIndex++;
        }
    }

    public void StopPlayback(GhostTank ghost)
    {
        IsActive = false;
        ghost.Stop();
        ghost.IsActive = false;
    }
}

