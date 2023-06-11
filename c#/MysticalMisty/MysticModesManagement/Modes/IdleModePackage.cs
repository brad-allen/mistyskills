using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;
using MistyRobotics.Common.Types;
using SkillTools.AssetTools;

namespace MysticModesManagement
{
    public class IdleModePackage : BaseAllModesPackage
    {
        public override event EventHandler<PackageData> CallSwitchMode;
        private ModeCommon _modeCommon;
        private AssetWrapper _assetWrapper;

        public IdleModePackage(IRobotMessenger misty, AssetWrapper assetWrapper) : base(misty) 
        {
            _assetWrapper = assetWrapper;
            _modeCommon = ModeCommon.LoadCommonOptions(misty);
        }

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            await base.Start(packageData);
            _ = _modeCommon.ShowWarningLayer();

            _assetWrapper.PlaySystemSound(SystemSound.Sleepy);
            await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);

            return new ResponsePacket { Success = true };
        }

        public override async Task<ResponsePacket> Stop()
        {
            await _modeCommon.DeleteWarningLayer();
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
            if (robotInteractionEvent.Step != RobotInteractionStep.Dialog &&
                   robotInteractionEvent.Step != RobotInteractionStep.BumperPressed &&
                   robotInteractionEvent.Step != RobotInteractionStep.CapTouched)
            {
                return;
            }

            if (robotInteractionEvent.DialogState?.Step == DialogActionStep.FinalIntent)
            {
                _ = _modeCommon.WriteToWarningLayer(robotInteractionEvent.DialogState.Text);
            }

            if (robotInteractionEvent.DialogState?.Step == DialogActionStep.CompletedSpeaking)
            {
                _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
            }

            if (robotInteractionEvent.DialogState?.Step == DialogActionStep.FinalIntent)
            {

                if (robotInteractionEvent.Step == RobotInteractionStep.CapTouched && robotInteractionEvent.CapTouchState.Scruff == TouchSensorOption.Contacted)
                {
                    Misty.StartAction("body-reset", true, null);
                    PackageData pd = new PackageData(MysticMode.TrackObject, "idle")
                    {
                        ModeContext = PackageData.ModeContext,
                        Parameters = PackageData.Parameters
                    };

                    CallSwitchMode?.Invoke(this, pd);
                    return;
                }

                if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
                {
                    //await Misty.StopKeyPhraseRecognitionAsync()
                    await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                    _ = Misty.SpeakAsync($"I didn't hear anything. When you are ready, say, Hey Misty, and try again!", true, "IdlePackageRetry");
                }
                else if (robotInteractionEvent.DialogState.Contexts.Contains("all-modes") && !robotInteractionEvent.DialogState.Intent.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                {
                    PackageData pd = new PackageData(MysticMode.Idle, robotInteractionEvent.DialogState.Intent)
                    {
                        ModeContext = PackageData.ModeContext,
                        Parameters = PackageData.Parameters
                    };

                    //see if this is a mode switch?
                    CallSwitchMode?.Invoke(this, pd);
                }
                else
                {
                    _ = Misty.SpeakAndListenAsync($"Did you say? {robotInteractionEvent.DialogState.Text}. I don't know what that means, if you want to keep trying, say something else now!", true, "RepeatPhrase", null);
                }
            }
        }
    }
}