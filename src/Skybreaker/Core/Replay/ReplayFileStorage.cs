using System.Text.Json;
using HeavyWeapons.Core.Commands;

namespace HeavyWeapons.Core.Replay;

public record ReplayEntryDto(float Time, string Command, float Value);

public record ReplayFileDto(
    string GameTitle,
    string RecordedAt,
    int Wave,
    int Score,
    float DurationSeconds,
    int CommandCount,
    List<ReplayEntryDto> Commands
);

/// <summary>
/// ระบบบันทึกและส่งออก Replay Tape ลงเป็นไฟล์ JSON บนดิสก์ (Disk Serialization)
/// สะท้อนข้อดีหลักของ Command Pattern จากสไลด์ Week 8:
/// "Timestamped Command Stream มีขนาดเพียงไม่กี่กิโลไบต์ (KBs) เทียบกับคลิปวิดีโอ (MBs)"
/// </summary>
public static class ReplayFileStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string SaveReplay(int wave, int score, float duration, List<TimedCommand> commands, string directory = "replays")
    {
        Directory.CreateDirectory(directory);
        string filename = Path.Combine(directory, $"replay_wave{wave}_{DateTime.Now:yyyyMMdd_HHmmss}.json");

        var entries = new List<ReplayEntryDto>(commands.Count);
        foreach (var tc in commands)
        {
            float param = tc.Command switch
            {
                MoveTankCommand m => m.DirectionX,
                AimTurretCommand a => a.AngleDeg,
                _ => 0f
            };
            entries.Add(new ReplayEntryDto(MathF.Round(tc.Timestamp, 3), tc.Command.Name, param));
        }

        var dto = new ReplayFileDto(
            GameTitle: "Heavy Weapons - Week 8 Command Pattern Replay",
            RecordedAt: DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Wave: wave,
            Score: score,
            DurationSeconds: MathF.Round(duration, 2),
            CommandCount: commands.Count,
            Commands: entries
        );

        string json = JsonSerializer.Serialize(dto, JsonOptions);
        File.WriteAllText(filename, json);

        return filename;
    }
}

