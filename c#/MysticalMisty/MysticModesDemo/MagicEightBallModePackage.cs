using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Messengers;
using MysticCommon;

namespace MysticModesDemo
{
    public class MagicEightBallModePackage : BaseModePackage
    {
        private bool _repeatTime;
        private IList<string> _responses1 = new List<string>();
        private Random _random = new Random();
        public MagicEightBallModePackage(IRobotMessenger misty) : base(misty) {}

        public override async Task<ResponsePacket> Start(IDictionary<string, object> parameters)
        {
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

            _repeatTime = true;
            _ = Misty.RegisterVoiceRecordEvent(0, true, "Test-is-this-needed", null); //shouldn't be needed, but might be still - oops
            _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
            _ = Misty.SpeakAsync($"Say, Hey Misty, and talk to me!", true, "RubberDuckey");

            return new ResponsePacket { Success = true };
        }

        public override async Task<ResponsePacket> Stop()
        {
            //Do cleanup here...
            _repeatTime = false;
            Misty.UnregisterEvent("Test-is-this-needed", null);
            //TODO make a disposable?
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
                Samples = samples
            };
            return true;
        }

        public override void ProcessEvent(MysticEvent mysticEvent)
        {
            if (_repeatTime && mysticEvent.RobotInteractionEvent.DialogState?.Step == MistyRobotics.Common.Types.DialogActionStep.CompletedASR)
            {
                if (string.IsNullOrWhiteSpace(mysticEvent.RobotInteractionEvent.DialogState.Text))
                {
                    _ = Misty.SpeakAsync($"I didn't hear anything. Say, Hey Misty, and try again!", true, "M8BPhraseRetry");
                }
                else
                {
                    try
                    {
                        int random1 = _random.Next(0, _responses1.Count);
                        string response1 = _responses1[random1];
                        _ = Misty.SpeakAsync($"{mysticEvent.RobotInteractionEvent.DialogState.Text}? {response1}", true, "M8BPhrase");
                    }
                    catch
                    {
                        //should never happen
                        _ = Misty.SpeakAsync($"All my ones and zeroes are acting strange. Say, Hey Misty, and speak to me!", true, "M8BPhraseError");                        
                    }
                }

                _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
            }
        }
    }
}