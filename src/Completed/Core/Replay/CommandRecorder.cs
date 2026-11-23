using Game_CommandPattern_Completed.Core.Commands;

namespace Game_CommandPattern_Completed.Core.Replay;

/// <summary>
/// ผู้บันทึกกระแสคำสั่ง (Command Stream Recorder)
/// ทำหน้าที่เก็บรวบรวมคำสั่งพร้อมเวลาที่เกิดขึ้นระหว่างที่ผู้เล่นกำลังวิ่งจับเวลา
/// </summary>
public class CommandRecorder
{
    private readonly List<TimedCommand> _recordedCommands = new(256);
    public bool IsRecording { get; private set; }

    public int CommandCount => _recordedCommands.Count;

    public void StartRecording()
    {
        _recordedCommands.Clear();
        IsRecording = true;
    }

    public void Record(float timestamp, ICommand command)
    {
        if (!IsRecording) return;
        _recordedCommands.Add(new TimedCommand(timestamp, command));
    }

    public void StopRecording()
    {
        IsRecording = false;
    }

    public List<TimedCommand> GetTapeCopy()
    {
        return new List<TimedCommand>(_recordedCommands);
    }

    public void Clear()
    {
        _recordedCommands.Clear();
        IsRecording = false;
    }
}

