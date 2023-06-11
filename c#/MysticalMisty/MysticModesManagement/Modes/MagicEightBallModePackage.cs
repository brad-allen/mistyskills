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
    public class MagicEightBallModePackage : BaseAllModesPackage
    {
        private IList<string> _responses1 = new List<string>();
        private Random _random = new Random();
        public override event EventHandler<PackageData> CallSwitchMode;
        public MagicEightBallModePackage(IRobotMessenger misty) : base(misty) {}

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            await base.Start(packageData);

            _responses1.Add("I say that signs point to Yes.");
            _responses1.Add("I say that Signs seem to indicate No.");
            _responses1.Add("I say, Ask again later.");
            _responses1.Add("I say I am uncertain, but the fog is clearing, ask again now!");
            _responses1.Add("I say, It is certain.");
            _responses1.Add("I say, Without a doubt");
            _responses1.Add("I believe I better not tell you now.");
            _responses1.Add("It is unclear, concentrate and ask again.");
            _responses1.Add("I say, it is Very doubtful");
            _responses1.Add("I believe the outlook is not so good");
            _responses1.Add("My reply is no");
            _responses1.Add("I say, the outlook is very good!");
            _responses1.Add("I see good things!");

            _ = Misty.SpeakAndListenAsync("Magic Eight Ball! Ask me a Yes or no question!", true, "saying", null);

            return new ResponsePacket { Success = true };
        }

        public override async Task<ResponsePacket> Stop()
        {
            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("magic eight ball");
            samples.Add("eight");
            samples.Add("magic");
            
            intent = new Intent
            {
                Name = "MagicEightBall",
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
                    await Misty.SpeakAndListenAsync($"I didn't hear anything. Ask me a Yes or No question now!", true, "RepeatPhraseRetry", null);
                }
                else if (robotInteractionEvent.DialogState.Contexts.Contains("all-modes") && !robotInteractionEvent.DialogState.Intent.Equals("magiceightball", StringComparison.OrdinalIgnoreCase) && !robotInteractionEvent.DialogState.Intent.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                {
                    PackageData pd = new PackageData(MysticMode.MagicEightBall, robotInteractionEvent.DialogState.Intent)
                    {
                        ModeContext = PackageData.ModeContext,
                        Parameters = PackageData.Parameters
                    };

                    CallSwitchMode?.Invoke(this, pd);
                    return;
                }
                else
                {
                    try
                    {
                        int random1 = _random.Next(0, _responses1.Count);
                        string response1 = _responses1[random1];
                        //await Misty.StopKeyPhraseRecognitionAsync()
                        await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                        _ = Misty.SpeakAsync($"In response to your question, {robotInteractionEvent.DialogState.Text}? {response1}.  Say hey Misty and ask me another Yes or No question!", true, "M8BPhrase");

                    }
                    catch
                    {
                        //should never happen
                        //await Misty.StopKeyPhraseRecognitionAsync()
                        await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                        _ = Misty.SpeakAsync($"All my ones and zeroes are acting strange. Say, Hey Misty, and speak to me!", true, "M8BPhraseError");

                    }
                }
            }
        }
    }
}