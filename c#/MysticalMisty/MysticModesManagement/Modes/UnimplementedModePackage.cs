using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;

namespace MysticModesManagement
{
    public class UnimplementedModePackage : BaseAllModesPackage
    {
        public UnimplementedModePackage(IRobotMessenger misty) : base(misty) {}

        public override async Task<ResponsePacket> Start(PackageData packageData)
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

        public override async void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
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
