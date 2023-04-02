
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;
using MysticModesManagement.Conversations;

namespace MysticModesManagement
{
    public class RepeatModePackage : BaseAllModesPackage
    {
        public override event EventHandler<PackageData> CallSwitchMode;
        public RepeatModePackage(IRobotMessenger misty) : base(misty) {}

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            await base.Start(packageData);

            _ = Misty.SpeakAndListenAsync("Say something!", true, "saying", null);

            return new ResponsePacket { Success = true };
        }

        public override async Task<ResponsePacket> Stop()
        {
           // await BreakdownMode();
            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("repeat");
            samples.Add("peat");
            samples.Add("Pete");
            samples.Add("repeat after me");
            samples.Add("copy me");
            samples.Add("after me");

            intent = new Intent
            {
                Name = "Repeat",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        public override async void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            if (robotInteractionEvent.DialogState?.Step == MistyRobotics.Common.Types.DialogActionStep.FinalIntent)
            {
                if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
                {
                    await Misty.SpeakAndListenAsync($"I didn't hear anything. Try something else now!", true, "RepeatPhraseRetry", null);
                }
                else if (robotInteractionEvent.DialogState.Contexts.Contains("all-modes") && !robotInteractionEvent.DialogState.Intent.Equals("repeat", StringComparison.OrdinalIgnoreCase))
                {
                    PackageData pd = new PackageData(MysticMode.Start, robotInteractionEvent.DialogState.Intent)
                    {
                        ModeContext = PackageData.ModeContext,
                        Parameters = PackageData.Parameters
                    };

                    CallSwitchMode?.Invoke(this, pd);
                }
                else
                {
                    _ = Misty.SpeakAndListenAsync($"{robotInteractionEvent.DialogState.Text}. Okay. Say something else.", true, "RepeatPhrase", null);
                }
            }
        }
    }
}
