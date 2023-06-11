using System;
using MistyRobotics.Common.Types;

namespace MysticCommon
{
    public class ErrorDetails
    {
        public Exception ResponseException { get; set; }
        public bool ExceptionHandled { get; set; }
        public string ResponseMessage { get; set; }
        public SkillLogLevel ErrorLevel { get; set; }
    }
}