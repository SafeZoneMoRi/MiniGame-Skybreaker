using System.Numerics;
using Game_CommandPattern_Completed.Core.Characters;

namespace Game_CommandPattern_Completed.Core.Replay;

/// <summary>
/// เครื่องยนต์ฉายภาพซ้ำของร่างเงา (Ghost Playback Engine - Invoker for Ghost)
/// อ่านกระแสคำสั่งจาก Master Tape ตามเวลาที่ผ่านไป แล้วสั่ง Execute ลงบน GhostRunner แบบเรียลไทม์
/// </summary>
public class GhostPlaybackEngine
{
    private readonly List<TimedCommand> _ghostTape = new(256);
    private int _playbackIndex = 0;

    public bool HasTape => _ghostTape.Count > 0;
    public bool IsActive { get; private set; }
    public float BestTime { get; private set; } = float.MaxValue;
    public int TotalCommands => _ghostTape.Count;

    public void LoadNewPersonalBest(float finishTime, List<TimedCommand> newTape)
    {
        BestTime = finishTime;
        _ghostTape.Clear();
        _ghostTape.AddRange(newTape);
    }

    public void StartPlayback(GhostRunner ghost, Vector2 startPos)
    {
        _playbackIndex = 0;
        IsActive = HasTape;
        ghost.Reset(startPos);
        ghost.IsActive = HasTape;
    }

    public void Update(float currentLevelTime, GhostRunner ghost)
    {
        if (!IsActive || !HasTape || !ghost.IsActive) return;

        // วนลูปประมวลผลคำสั่งทั้งหมดที่ถึงเวลา Execute ณ เสี้ยววินาทีนี้
        while (_playbackIndex < _ghostTape.Count && currentLevelTime >= _ghostTape[_playbackIndex].Timestamp)
        {
            _ghostTape[_playbackIndex].Command.Execute(ghost);
            _playbackIndex++;
        }
    }

    public void StopPlayback(GhostRunner ghost)
    {
        IsActive = false;
        ghost.Stop();
        ghost.IsActive = false;
    }
}

