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
    public class MagicEightBallModePackage : BaseAllModesPackage
    {
        private IList<string> _responses1 = new List<string>();
        private Random _random = new Random();
        public override event EventHandler<PackageData> CallSwitchMode;
        public MagicEightBallModePackage(IRobotMessenger misty) : base(misty) {}

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            await base.Start(packageData);

            _responses1.Add("Signs point to Yes.");
            _responses1.Add("Signs seem to indicate No.");
            _responses1.Add("Ask again later.");
            _responses1.Add("The fog is clearing, ask again now!");
            _responses1.Add("It is certain.");
            _responses1.Add("Without a doubt");
            _responses1.Add("Better not tell you now.");
            _responses1.Add("Concentrate and ask again.");
            _responses1.Add("Very doubtful");
            _responses1.Add("Outlook not so good");
            _responses1.Add("My reply is no");
            _responses1.Add("Outlook good!");
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
            if (robotInteractionEvent.DialogState?.Step == MistyRobotics.Common.Types.DialogActionStep.FinalIntent)
            {
                if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
                {
                    await Misty.SpeakAndListenAsync($"I didn't hear anything. Ask me a Yes or No question now!", true, "RepeatPhraseRetry", null);
                }
                else if (robotInteractionEvent.DialogState.Contexts.Contains("all-modes") && !robotInteractionEvent.DialogState.Intent.Equals("magiceightball", StringComparison.OrdinalIgnoreCase))
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
                    try
                    {
                        int random1 = _random.Next(0, _responses1.Count);
                        string response1 = _responses1[random1];
                        _ = Misty.SpeakAsync($"The answer to your question. {robotInteractionEvent.DialogState.Text}? {response1}.  Say hey Misty and ask me another Yes or No question!", true, "M8BPhrase");
                        _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
                    }
                    catch
                    {
                        //should never happen
                        _ = Misty.SpeakAsync($"All my ones and zeroes are acting strange. Say, Hey Misty, and speak to me!", true, "M8BPhraseError");
                        _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
                    }
                }
            }
        }
    }
}