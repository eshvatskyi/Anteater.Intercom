using System;
using System.Linq;
using System.Text.RegularExpressions;
using Anteater.Pipe;

namespace Anteater.Intercom.Services.Events;

public class AlarmEvent : IEvent
{
    private static readonly Regex EventRegEx = new("""^(?<date>\d\d\d\d-\d\d-\d\d);(?<time>\d\d:\d\d:\d\d);(?<type>\w+);(?<status>[01]);(?<numbers>\d+(,\d+)*)$""", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

    public static bool TryParse(string value, out AlarmEvent alarmEvent)
    {
        alarmEvent = default;

        var match = EventRegEx.Match(value ?? string.Empty);

        if (match.Success && Enum.TryParse<EventType>(match.Groups["type"].Value, out var eventType))
        {
            alarmEvent = new AlarmEvent
            {
                DateTime = DateTime.Parse($"{match.Groups["date"]} {match.Groups["time"]}"),
                Type = eventType,
                Status = match.Groups["status"].Value == "1",
                Numbers = match.Groups["numbers"].Value?
                    .Split(',')
                    .Select(x => ushort.TryParse(x, out var num) ? num : (ushort?)null)
                    .Where(x => x.HasValue)
                    .Select(x => x.Value)
                    .ToArray() ?? Array.Empty<ushort>()
            };

            return true;
        }

        return false;
    }

    public DateTime DateTime { get; private set; }

    public EventType Type { get; private set; }

    public bool Status { get; private set; }

    public ushort[] Numbers { get; private set; }

    public enum EventType
    {
        SensorAlarm,
        SensorOutAlarm,
        MotionDetection
    }
}
