using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;
using MistyRobotics.Common.Types;

namespace MysticModesManagement
{
    public class WordReverseModePackage : BaseAllModesPackage
    {
        public override event EventHandler<PackageData> CallSwitchMode;
        public WordReverseModePackage(IRobotMessenger misty) : base(misty) {}

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            await base.Start(packageData);
            _ = Misty.SpeakAndListenAsync("Say something!", true, "saying", null);
            
            return new ResponsePacket { Success = true };
        }

        public override async Task<ResponsePacket> Stop()
        {
            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("word reverse");
            samples.Add("ward reverse");
            samples.Add("reverse word");
            samples.Add("reverse ward");
            samples.Add("reverse");

            intent = new Intent
            {
                Name = "Reverse",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        public override async void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            if (robotInteractionEvent.Step != RobotInteractionStep.Dialog &&
                robotInteractionEvent.Step != RobotInteractionStep.BumperPressed &&
                robotInteractionEvent.Step != RobotInteractionStep.CapTouched)
            {
                return;
            }

            if (robotInteractionEvent.DialogState?.Step == DialogActionStep.FinalIntent)
            {
                if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
                {
                    await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                    _ = Misty.SpeakAsync($"Oh no, I didn't hear you. Please speak up and try to minimize other noise in the room. Okay, when you are ready say Hey Misty and then what you what me to reverse by word!", true, "WordReversePhrase");
                }
                else if (robotInteractionEvent.DialogState.Contexts.Contains("all-modes") && !robotInteractionEvent.DialogState.Intent.Equals("backward", StringComparison.OrdinalIgnoreCase) && !robotInteractionEvent.DialogState.Intent.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                {
                    PackageData pd = new PackageData(MysticMode.Backward, robotInteractionEvent.DialogState.Intent)
                    {
                        ModeContext = PackageData.ModeContext,
                        Parameters = PackageData.Parameters
                    };

                    CallSwitchMode?.Invoke(this, pd);
                }
                else
                {
                    string[] textArray = robotInteractionEvent.DialogState.Text.Split(new char[] { ' ', ',', ';', ':', '!', '?', '.' });

                    IList<string> reversedString = textArray.Reverse().ToList();

                    string newText = "";
                    foreach(string word in reversedString)
                    {
                        newText += " " + word;
                    }

                    _ = Misty.SpeakAndListenAsync($"{newText}. Okay, say something else.", true, "BackwardPhrase", null);
                }
            }
        }
    }
}