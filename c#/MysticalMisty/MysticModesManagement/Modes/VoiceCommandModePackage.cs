using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;
using SkillTools.AssetTools;

namespace MysticModesManagement
{
    public class VoiceCommandModePackage : BaseAllModesPackage
    {
        private Random _random = new Random();
        public VoiceCommandModePackage(IRobotMessenger misty) : base(misty) 
        {
            _modeCommon = ModeCommon.LoadCommonOptions(Misty);
        }
        public override event EventHandler<PackageData> CallSwitchMode;
        private AssetWrapper _assetWrapper;
        private ModeCommon _modeCommon;

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            await base.Start(packageData);

            _assetWrapper = new AssetWrapper(Misty);
            _ = _modeCommon.ShowWarningLayer();
            await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
            _ = Misty.SpeakAsync($"Say, Hey Misty, and ask me to do things!", true, "VoiceCommand");

            return new ResponsePacket { Success = true };
        }

        public override async Task<ResponsePacket> Stop()
        {
            await base.Stop();
            await _modeCommon.DeleteWarningLayer();

            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("voice command");
            samples.Add("command");
            samples.Add("voice control");
            samples.Add("voice");
            samples.Add("do what I say");
            samples.Add("follow my lead");
            samples.Add("follow the leader");

            intent = new Intent
            {
                Name = "VoiceCommand",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        public override async void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            string text = "";

            if (robotInteractionEvent.DialogState?.Step == DialogActionStep.FinalIntent)
            {
                _ = _modeCommon.WriteToWarningLayer(robotInteractionEvent.DialogState.Text);
            }

            if (robotInteractionEvent.Step != RobotInteractionStep.BumperPressed &&
                robotInteractionEvent.Step != RobotInteractionStep.CapTouched
                && robotInteractionEvent.DialogState?.Step != MistyRobotics.Common.Types.DialogActionStep.FinalIntent)
            {
                return;
            }

            if (robotInteractionEvent.Step == RobotInteractionStep.CapTouched && robotInteractionEvent.CapTouchState.Scruff == TouchSensorOption.Contacted)
            {

                Misty.StartAction("body-reset", true, null);
                PackageData pd = new PackageData(MysticMode.VoiceCommand, "idle")
                {
                    ModeContext = PackageData.ModeContext,
                    Parameters = PackageData.Parameters
                };

                CallSwitchMode?.Invoke(this, pd);
                return;
            }

            if (robotInteractionEvent.EventType == EventType.BumpSensor)
            {
                if (robotInteractionEvent.BumperState.BackLeft == TouchSensorOption.Contacted)
                {
                    _assetWrapper.PlaySystemSound(SystemSound.Joy);
                    _assetWrapper.ShowSystemImage(SystemImage.Joy);
                }
                else if (robotInteractionEvent.BumperState.BackRight == TouchSensorOption.Contacted)
                {
                    _assetWrapper.PlaySystemSound(SystemSound.Joy4);
                    _assetWrapper.ShowSystemImage(SystemImage.JoyGoofy3);
                }
                else if (robotInteractionEvent.BumperState.FrontLeft == TouchSensorOption.Contacted)
                {
                    _assetWrapper.PlaySystemSound(SystemSound.Amazement);
                    _assetWrapper.ShowSystemImage(SystemImage.Amazement);
                }
                else if (robotInteractionEvent.BumperState.FrontLeft == TouchSensorOption.Contacted)
                {
                    _assetWrapper.PlaySystemSound(SystemSound.Sadness2);
                    _assetWrapper.ShowSystemImage(SystemImage.Sadness);
                }
            }
            if (robotInteractionEvent.EventType == EventType.TouchSensor)
            {
                if (robotInteractionEvent.CapTouchState.Scruff == TouchSensorOption.Contacted)
                {
                    _assetWrapper.PlaySystemSound(SystemSound.PhraseNoNoNo);
                    _assetWrapper.ShowSystemImage(SystemImage.Rage3);
                }
                else if (robotInteractionEvent.CapTouchState.Chin == TouchSensorOption.Contacted)
                {
                    _assetWrapper.PlaySystemSound(SystemSound.Love);
                    _assetWrapper.ShowSystemImage(SystemImage.Love);
                }
                else if (robotInteractionEvent.CapTouchState.Front == TouchSensorOption.Contacted)
                {
                    _assetWrapper.PlaySystemSound(SystemSound.PhraseEvilAhHa);
                    _assetWrapper.ShowSystemImage(SystemImage.Terror);
                }
                else if (robotInteractionEvent.CapTouchState.Back == TouchSensorOption.Contacted)
                {
                    _assetWrapper.PlaySystemSound(SystemSound.PhraseHello);
                    _assetWrapper.ShowSystemImage(SystemImage.DefaultContent);
                }
                else if (robotInteractionEvent.CapTouchState.Left == TouchSensorOption.Contacted)
                {
                    _assetWrapper.PlaySystemSound(SystemSound.Sleepy);
                    _assetWrapper.ShowSystemImage(SystemImage.SleepingZZZ);
                }
                else if (robotInteractionEvent.CapTouchState.Right == TouchSensorOption.Contacted)
                {
                    _assetWrapper.PlaySystemSound(SystemSound.PhraseOopsy);
                    _assetWrapper.ShowSystemImage(SystemImage.Disoriented);
                }
            }

            if (robotInteractionEvent.DialogState?.Step == DialogActionStep.FinalIntent)
            {
                try
                {
                    await _modeCommon.WriteToWarningLayer(robotInteractionEvent.DialogState.Text);

                    if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
                    {

                        await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                        _ = Misty.SpeakAsync($"Oh no, I didn't hear you. Please speak up and try to minimize other noise in the room. Okay, when you are ready say Hey Misty and then what you what me to do!", true, "VoiceCommandPhrase");

                        return;
                    }
                    else if (robotInteractionEvent.DialogState.Contexts.Contains("all-modes") && !robotInteractionEvent.DialogState.Intent.Equals("VoiceCommand", StringComparison.OrdinalIgnoreCase) && !robotInteractionEvent.DialogState.Intent.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                    {
                        PackageData pd = new PackageData(MysticMode.VoiceCommand, robotInteractionEvent.DialogState.Intent)
                        {
                            ModeContext = PackageData.ModeContext,
                            Parameters = PackageData.Parameters
                        };

                        CallSwitchMode?.Invoke(this, pd);
                        return;
                    }

                    text = robotInteractionEvent.DialogState.Text.ToLower();
                    //TODO Hacky speech check / Allow more and allow degrees and colors...
                    if (text.Contains("arm"))
                    {
                        if(text.Contains("left") || text.Contains("off"))
                        {
                            Misty.MoveArm(_random.Next(-40, 91), MistyRobotics.Common.Types.RobotArm.Left, 70, null, MistyRobotics.Common.Types.AngularUnit.Degrees, null);
                        }
                        else if (text.Contains("right") || text.Contains("write"))
                        {
                            Misty.MoveArm(_random.Next(-40, 91), MistyRobotics.Common.Types.RobotArm.Right, 70, null, MistyRobotics.Common.Types.AngularUnit.Degrees, null);
                        }
                        else
                        {
                            Misty.MoveArms(_random.Next(-40, 91), _random.Next(-40, 91), 70, 70, null, MistyRobotics.Common.Types.AngularUnit.Degrees, null);
                        }
                    }
                    
                    if (text.Contains("head") || text.Contains("neck"))
                    {
                        Misty.MoveHead(_random.Next(-40, 20), _random.Next(-30, 31), _random.Next(-70, 71), 70, null, MistyRobotics.Common.Types.AngularUnit.Degrees, null);
                    }
                }
                catch (Exception ex)
                {
                    Misty.SkillLogger.LogError($"Failed handling robot interaction callback.", ex);
                }
                finally
                {
                    //start key phrase changes led light, so do before led change
                    await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);

                    //if (text.Contains("l.e.d.") || text.Contains("light") || text.Contains("color") || text.Contains("led") || text.Contains("l e d") || text.Contains("lead") || text.Contains("led") || text.Contains("chest"))
                    //{
                        if (robotInteractionEvent.DialogState.Text.Contains("yellow"))
                        {
                            _ = Misty.ChangeLEDAsync(255, 255, 0);
                        }
                        else if (robotInteractionEvent.DialogState.Text.Contains("red") || robotInteractionEvent.DialogState.Text.Contains("read"))
                        {
                            _ = Misty.ChangeLEDAsync(255, 0, 0);
                        }
                        else if (robotInteractionEvent.DialogState.Text.Contains("blue"))
                        {
                            _ = Misty.ChangeLEDAsync(0, 0, 255);
                        }
                        else if (robotInteractionEvent.DialogState.Text.Contains("green"))
                        {
                            _ = Misty.ChangeLEDAsync(0, 255, 0);
                        }
                        else if (robotInteractionEvent.DialogState.Text.Contains("off"))
                        {
                            _ = Misty.ChangeLEDAsync(0, 0, 0);
                        }
                        else if (robotInteractionEvent.DialogState.Text.Contains("white"))
                        {
                            _ = Misty.ChangeLEDAsync(255, 255, 255);
                        }
                  //  }
                }

                return;
            }
        }
    }
}