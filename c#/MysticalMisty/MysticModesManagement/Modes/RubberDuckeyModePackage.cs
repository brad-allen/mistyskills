using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;

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

            //really just another version of magic eight ball, but with surrounding random comments
            _responses1.Add("Could be, let's think about what you said.");
            _responses1.Add("I'm not sure.");
            _responses1.Add("Okay, let me think.");
            _responses1.Add("So think it through.");
            _responses1.Add("Well, let me ponder that.");
            _responses1.Add("What you are saying is ");

            _responses2.Add("Are you sure about that?");
            _responses2.Add("Seems to make sense.");
            _responses2.Add("I'm not so sure.");
            _responses2.Add("Can that be true?");
            _responses2.Add("Seems clear to me?");
            _responses2.Add("Seems unclear to me? Can you say that a different way?");
            await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
            _ = Misty.SpeakAsync($"Say Hey Misty and then tell me what you think about solving your next puzzle!", true, "RubberDuckeyPhrase");

            return new ResponsePacket { Success = true };
        }

        public override async Task<ResponsePacket> Stop()
        {
            await base.Stop();
            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("rubber duckey");
            samples.Add("rubber ducky");
            samples.Add("duckey");
            samples.Add("ducky");
            samples.Add("duck");
            samples.Add("doc");
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

            if (robotInteractionEvent.DialogState?.Step == DialogActionStep.FinalIntent)
            {
                if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text) || robotInteractionEvent.DialogState.Intent.Equals("silence", StringComparison.OrdinalIgnoreCase))
                {
                    await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                    _ = Misty.SpeakAsync($"Oh no, I didn't hear you. Please speak up and try to minimize other noise in the room. Okay, when you are ready say Hey Misty and then tell me what you think about solving your next puzzle!", true, "RubberDuckeyPhrase");
                }
                else if (robotInteractionEvent.DialogState.Contexts.Contains("all-modes") && !robotInteractionEvent.DialogState.Intent.Equals("rubberduckey", StringComparison.OrdinalIgnoreCase) && !robotInteractionEvent.DialogState.Intent.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                {
                    PackageData pd = new PackageData(MysticMode.RubberDuckey, robotInteractionEvent.DialogState.Intent)
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
                        int random2 = _random.Next(0, _responses2.Count);
                        string response1 = _responses1[random1];
                        string response2 = _responses2[random2];
                        await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                        _ = Misty.SpeakAsync($"{response1}. {robotInteractionEvent.DialogState.Text}. {response2}. To talk to me about something else, say Hey Misty and then talk to me!", true, "RubberDuckeyPhrase");
                    }
                    catch
                    {
                        //should never happen                        
                        await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                        _ = Misty.SpeakAsync($"All my ones and zeroes are acting strange. Say, Hey Misty, and speak to me!", true, "RubberDuckeyPhraseError");

                    }
                }
            }
        }
    }
}