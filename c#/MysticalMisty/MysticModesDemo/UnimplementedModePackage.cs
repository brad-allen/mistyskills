using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Messengers;
using MysticCommon;

namespace MysticModesDemo
{
    public class UnimplementedModePackage : BaseModePackage
    {
        public UnimplementedModePackage(IRobotMessenger misty) : base(misty) {}

        public override async Task<ResponsePacket> Start(IDictionary<string, object> parameters)
        {
            return new ResponsePacket 
            { 
                Success = false, 
                ErrorDetails = new ErrorDetails 
                { 
                    ResponseMessage = "Mode is not implented.", 
                    ErrorLevel = SkillLogLevel.Warning 
                } 
            };
        }

        public override async Task<ResponsePacket> Stop()
        {
            return await Task.FromResult(new ResponsePacket
            {
                Success = false,
                ErrorDetails = new ErrorDetails
                {
                    ResponseMessage = "Mode is not implented.",
                    ErrorLevel = SkillLogLevel.Warning
                }
            });
        }

        public override void ProcessEvent(MysticEvent mysticEvent)
        {
            //Any processing of the event from sensors, etc, that is not handled in the conversation, can be done here...
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            intent = null;
            return false;
        }
    }
}
