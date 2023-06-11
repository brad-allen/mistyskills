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
    public class RepeatModePackage : BaseAllModesPackage
    {
        public override event EventHandler<PackageData> CallSwitchMode;
        private ModeCommon _modeCommon;

        public RepeatModePackage(IRobotMessenger misty) : base(misty)
        {
            _modeCommon = ModeCommon.LoadCommonOptions(misty);
        }

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            await base.Start(packageData);
            _ = _modeCommon.ShowWarningLayer();

            _ = Misty.SpeakAndListenAsync("Say something and I will repeat it!", true, "saying", null);

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
            samples.Add("repeat");
            samples.Add("peat");
            samples.Add("Pete");
            samples.Add("repeat after me");
            samples.Add("copy me");
            samples.Add("after me");

            intent = new Intent
            {
                Name = "Repeat",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        public override async void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            if (robotInteractionEvent.DialogState?.Step == DialogActionStep.FinalIntent)
            {
                _ = _modeCommon.WriteToWarningLayer(robotInteractionEvent.DialogState.Text);
            }
            else if (robotInteractionEvent.Step == RobotInteractionStep.StartingState)
            {
                _ = _modeCommon.HideWarningLayer();
            }

            if (robotInteractionEvent.Step != RobotInteractionStep.Dialog &&
                   robotInteractionEvent.Step != RobotInteractionStep.BumperPressed &&
                   robotInteractionEvent.Step != RobotInteractionStep.CapTouched)
            {
                return;
            }

            if (robotInteractionEvent.Step == RobotInteractionStep.CapTouched && robotInteractionEvent.CapTouchState.Scruff == TouchSensorOption.Contacted)
            {

                Misty.StartAction("body-reset", true, null);
                PackageData pd = new PackageData(MysticMode.Repeat, "idle")
                {
                    ModeContext = PackageData.ModeContext,
                    Parameters = PackageData.Parameters
                };

                CallSwitchMode?.Invoke(this, pd);
                return;
            }

            if (robotInteractionEvent.DialogState?.Step == DialogActionStep.FinalIntent)
            {
                if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
                {
                    await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                    _ = Misty.SpeakAsync($"Oh no, I didn't hear you. Please speak up and try to minimize other noise in the room. Okay, when you are ready say Hey Misty and then what you what me to repeat!", true, "RepeatPhrase");
                }
                else if (robotInteractionEvent.DialogState.Contexts.Contains("all-modes") && !robotInteractionEvent.DialogState.Intent.Equals("repeat", StringComparison.OrdinalIgnoreCase) && !robotInteractionEvent.DialogState.Intent.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                {
                    PackageData pd = new PackageData(MysticMode.Repeat, robotInteractionEvent.DialogState.Intent)
                    {
                        ModeContext = PackageData.ModeContext,
                        Parameters = PackageData.Parameters
                    };

                    CallSwitchMode?.Invoke(this, pd);
                }
                else
                {
                    _ = Misty.SpeakAndListenAsync($"{robotInteractionEvent.DialogState.Text}. Okay. Say something else.", true, "RepeatPhrase", null);
                }
            }
        }
    }
}
