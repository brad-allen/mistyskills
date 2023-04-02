﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;
using MysticModesManagement.Conversations;

namespace MysticModesManagement
{
    public class IdleModePackage : BaseAllModesPackage
    {
        public override event EventHandler<PackageData> CallSwitchMode;
        private AllModesConversation _allModesConversation;

        public IdleModePackage(IRobotMessenger misty) : base(misty) { }

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            await base.Start(packageData);

            await Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);

            return new ResponsePacket { Success = true };
        }

        public override async Task<ResponsePacket> Stop()
        {
           // await BreakdownMode();
            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("idle");
            samples.Add("be quiet");
            samples.Add("silence");
            samples.Add("silent");
            samples.Add("shut up");
            samples.Add("stop");

            intent = new Intent
            {
                Name = "Idle",
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
                    _ = Misty.SpeakAsync($"I didn't hear anything. When you are ready, say, Hey Misty, and try again!", true, "IdlePackageRetry");
                    _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
                    
                }
                else if (robotInteractionEvent.DialogState.Contexts.Contains("all-modes"))
                {
                    PackageData pd = new PackageData(MysticMode.Start, robotInteractionEvent.DialogState.Intent)
                    {
                        ModeContext = PackageData.ModeContext,
                        Parameters = PackageData.Parameters
                    };

                    //see if this is a mode switch?
                    CallSwitchMode?.Invoke(this, pd);
                }
                else
                {
                    _ = Misty.SpeakAndListenAsync($"Did you say? {robotInteractionEvent.DialogState.Text}I don't know what that means, if you want to keep trying, say something else now!", true, "RepeatPhrase", null);
                }
            }
        }
    }
}