using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Anteater.Intercom.Core.Events;

public partial class AlarmEvent
{
    public enum EventType
    {
        SensorAlarm,
        SensorOutAlarm,
        MotionDetection
    }

    [JsonInclude]
    public long Timestamp { get; private set; }

    [JsonInclude]
    public DateTime DateTime { get; private set; }

    [JsonInclude]
    public EventType Type { get; private set; }

    [JsonInclude]
    public bool Status { get; private set; }

    [JsonInclude]
    public ushort[] Numbers { get; private set; }

    public static bool TryParse(string value, out AlarmEvent alarmEvent)
    {
        alarmEvent = default;

        var match = EventRegEx().Match(value ?? "");

        if (match.Success && Enum.TryParse<EventType>(match.Groups["type"].Value, out var eventType))
        {
            alarmEvent = new AlarmEvent
            {
                Timestamp = DateTime.UtcNow.Ticks,
                DateTime = DateTime.Parse($"{match.Groups["date"]} {match.Groups["time"]}"),
                Type = eventType,
                Status = match.Groups["status"].Value == "1",
                Numbers = match.Groups["numbers"].Value?
                    .Split(',')
                    .Select(x => ushort.TryParse(x, out var num) ? num : (ushort?)null)
                    .Where(x => x.HasValue)
                    .Select(x => x.Value)
                    .ToArray() ?? Array.Empty<ushort>(),
            };

            return true;
        }

        return false;
    }

    [GeneratedRegex("^(?<date>\\d\\d\\d\\d-\\d\\d-\\d\\d);(?<time>\\d\\d:\\d\\d:\\d\\d);(?<type>\\w+);(?<status>[01]);(?<numbers>\\d+(,\\d+)*)$", RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
    private static partial Regex EventRegEx();
}
