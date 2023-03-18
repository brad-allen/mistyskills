using System;
using MistyRobotics.Common.Types;

namespace MysticCommon
{
    public class ResponsePacket
    {
        public bool Success { get; set; }
        public ErrorDetails ErrorDetails { get; set; }
    }

    public class ErrorDetails
    {
        public Exception ResponseException { get; set; }
        public bool ExceptionHandled { get; set; }
        public string ResponseMessage { get; set; }
        public SkillLogLevel ErrorLevel { get; set; }
    }
}