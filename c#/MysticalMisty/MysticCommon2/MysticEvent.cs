using MistyRobotics.SDK.Events;

namespace MysticCommon
{
    public class MysticEvent
    {
        public IRobotInteractionEvent RobotInteractionEvent { get; set; } = new RobotInteractionEvent();
        public string Details { get; set; }

        //public IRobotEvent RobotEvent { get; set; } = new RobotEvent();
        //public EventType EventType { get; set; } = EventType.Unknown;
    }
}