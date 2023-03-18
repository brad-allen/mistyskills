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
    public class RubberDuckeyModePackage : BaseModePackage
    {
        private bool _repeatTime;
        private IList<string> _responses1 = new List<string>();
        private IList<string> _responses2 = new List<string>();
        private Random _random = new Random();
        public override event EventHandler<PackageData> CallSwitchMode;
        private AllModesConversation _allModesConversation;
        public RubberDuckeyModePackage(IRobotMessenger misty) : base(misty) {}

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
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
            samples.Add("rubber duckey");
            samples.Add("duckey");
            samples.Add("duck");

            intent = new Intent
            {
                Name = "RubberDuckey",
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
                        int random2 = _random.Next(0, _responses2.Count);
                        string response1 = _responses1[random1];
                        string response2 = _responses2[random2];
                        _ = Misty.SpeakAsync($"{response1}. {robotInteractionEvent.DialogState.Text}. {response2}", true, "RubberDuckeyPhrase");
                    }
                    catch
                    {
                        //should never happen
                        _ = Misty.SpeakAsync($"All my ones and zeroes are acting strange. Say, Hey Misty, and speak to me!", true, "RubberDuckeyPhraseError");
                    }
                }
            }
        }
    }
}