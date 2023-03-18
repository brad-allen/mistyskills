using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using MysticCommon;
using MysticModesManagement.Conversations;

namespace MysticModesManagement
{
    public class StartModePackage : BaseModePackage
    {
        private AllModesConversation _allModesConversation;
        public override event EventHandler<PackageData> CallSwitchMode;

        public StartModePackage(IRobotMessenger misty) : base(misty) 
        {
        }

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            PackageData = packageData;

            await PrepareModeConversation();

            //Start empty conversation for now to get the intents and contexts returned in the RobotInteractionEvent
            _allModesConversation = new AllModesConversation(Misty);
            await _allModesConversation.Initialize();
            await Misty.StartConversationAsync(_allModesConversation.ConversationName);

            _ = Misty.SpeakAsync("Okay, when you are ready, say, Hey Misty, and tell me what you want to do.", true, "idleSpeech");
            await Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);

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
            samples.Add("start");
            samples.Add("restart");

            intent = new Intent
            {
                Name = "Start",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        protected override void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            //Process
            try
            {
                //Note, to get Dialog events, you must be in a conversation at this time, otherwise use the voicecommand callbacks
                if (robotInteractionEvent.Step == RobotInteractionStep.Dialog && robotInteractionEvent.DialogState?.Step == MistyRobotics.Common.Types.DialogActionStep.CompletedASR)
                {
                    if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
                    {
                        _ = Misty.SpeakAsync($"I didn't hear anything. Say, Hey Misty, and try again!", true, "StartPackageRetry");
                        _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
                    }
                    else if (robotInteractionEvent.DialogState.Intent.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                    {
                        _ = Misty.SpeakAsync($"Sorry! I didn't understand that request. Did you say {robotInteractionEvent.DialogState.Text}?", true, "StartPackageRetry2");
                        _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
                    }
                    else
                    {
                        PackageData pd = new PackageData(MysticMode.Start, robotInteractionEvent.DialogState.Intent)
                        {
                            ModeContext = PackageData.ModeContext,
                            Parameters = PackageData.Parameters
                        };

                        CallSwitchMode?.Invoke(this, pd);
                    }
                }
                else if (robotInteractionEvent.Step == RobotInteractionStep.BumperPressed)
                {
                    _ = Misty.SpeakAsync($"You pressed a bumper.", true, "BumperPress");
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
            }            
        }
    }
}