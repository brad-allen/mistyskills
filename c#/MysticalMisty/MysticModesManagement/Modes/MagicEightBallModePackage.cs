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
    public class MagicEightBallModePackage : BaseModePackage
    {
        private bool _repeatTime;
        private AllModesConversation _allModesConversation;
        private IList<string> _responses1 = new List<string>();
        private Random _random = new Random();
        public override event EventHandler<PackageData> CallSwitchMode;
        public MagicEightBallModePackage(IRobotMessenger misty) : base(misty) {}

        public override async Task<ResponsePacket> Start(PackageData packageData)
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
                    try
                    {
                        int random1 = _random.Next(0, _responses1.Count);
                        string response1 = _responses1[random1];
                        _ = Misty.SpeakAsync($"{robotInteractionEvent.DialogState.Text}? {response1}", true, "M8BPhrase");
                    }
                    catch
                    {
                        //should never happen
                        _ = Misty.SpeakAsync($"All my ones and zeroes are acting strange. Say, Hey Misty, and speak to me!", true, "M8BPhraseError");
                    }
                }
            }
        }
    }
}