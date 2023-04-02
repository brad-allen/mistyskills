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
    public class RubberDuckeyModePackage : BaseAllModesPackage
    {
        private IList<string> _responses1 = new List<string>();
        private IList<string> _responses2 = new List<string>();
        private Random _random = new Random();
        public override event EventHandler<PackageData> CallSwitchMode;
        public RubberDuckeyModePackage(IRobotMessenger misty) : base(misty) {}

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            await base.Start(packageData);

            _responses1.Add("Hmmmm.");
            _responses1.Add(" ... ");
            _responses1.Add(" Okay, let me think.");
            _responses1.Add("So think it through.");
            _responses1.Add("Well, let me ponder that.");
            _responses1.Add("What you are saying is ");

            _responses2.Add("Are you sure about that?");
            _responses2.Add("Seems to make sense.");
            _responses2.Add("I'm not so sure.");
            _responses2.Add("Can that be true?");
            _responses2.Add("Seems clear to me?");
            _responses2.Add("Seems unclear to me? Can you say that a different way?");

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
            samples.Add("rubber duckey");
            samples.Add("duckey");
            samples.Add("duck");
            samples.Add("rubber duck");

            intent = new Intent
            {
                Name = "RubberDuckey",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        public override async void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            if (robotInteractionEvent.DialogState?.Step == MistyRobotics.Common.Types.DialogActionStep.FinalIntent)
            {
                if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text) || robotInteractionEvent.DialogState.Intent.Equals("silence", StringComparison.OrdinalIgnoreCase))
                {
                    await Misty.SpeakAndListenAsync($"I didn't hear anything. Say something else now!", true, "RepeatPhraseRetry", null);
                }
                else if (robotInteractionEvent.DialogState.Contexts.Contains("all-modes") && !robotInteractionEvent.DialogState.Intent.Equals("rubberduckey", StringComparison.OrdinalIgnoreCase))
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
                        int random2 = _random.Next(0, _responses2.Count);
                        string response1 = _responses1[random1];
                        string response2 = _responses2[random2];
                        _ = Misty.SpeakAsync($"{response1}. {robotInteractionEvent.DialogState.Text}. {response2}. To talk to me about something else, say Hey Misty and then talk to me!", true, "RubberDuckeyPhrase");
                        await Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
                    }
                    catch
                    {
                        //should never happen
                        await Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000); 
                        _ = Misty.SpeakAsync($"All my ones and zeroes are acting strange. Say, Hey Misty, and speak to me!", true, "RubberDuckeyPhraseError");                        
                    }
                }
            }
        }
    }
}