using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;
using MysticModesManagement.Conversations;

namespace MysticModesManagement
{
    public class BackwardModePackage : BaseModePackage
    {
        public override event EventHandler<PackageData> CallSwitchMode;
        public BackwardModePackage(IRobotMessenger misty) : base(misty) {}
        private AllModesConversation _allModesConversation;

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            PackageData = packageData;
            await PrepareModeConversation();

            //Start empty conversation for now to get the intents and contexts returned in the RobotInteractionEvent
            _allModesConversation = new AllModesConversation(Misty);
            await _allModesConversation.Initialize();
            await Misty.StartConversationAsync(_allModesConversation.ConversationName);

            _ = Misty.SpeakAndListenAsync("Say something!", true, "saying", null);
            
            return new ResponsePacket { Success = true };
        }

        public override async Task<ResponsePacket> Stop()
        {
            await BreakdownMode();
            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("backward");
            samples.Add("back ward");
            samples.Add("reverse");

            intent = new Intent
            {
                Name = "Backward",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        protected override async void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            if (robotInteractionEvent.DialogState?.Step == MistyRobotics.Common.Types.DialogActionStep.CompletedASR)
            {
                if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
                {
                    await Misty.SpeakAndListenAsync($"I didn't hear anything. Try something else now!", true, "RepeatPhraseRetry", null);
                }
                else if (!robotInteractionEvent.DialogState.Intent.Equals("unknown", StringComparison.OrdinalIgnoreCase) && !robotInteractionEvent.DialogState.Intent.Equals("silence", StringComparison.OrdinalIgnoreCase))
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
                    char[] charArray = robotInteractionEvent.DialogState.Text.ToCharArray();
                    Array.Reverse(charArray);
                    _ = Misty.SpeakAndListenAsync($"{new string(charArray)}. Okay, say something else.", true, "BackwardPhrase", null);
                }
            }
        }
    }
}