using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;
using Wander;

namespace MysticModesManagement
{
    public class WanderModePackage : BaseModePackage
    {
        private DriveManager _driveManager;
        public override event EventHandler<PackageData> CallSwitchMode;

        public WanderModePackage(IRobotMessenger misty) : base(misty) {}

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            await base.Start(packageData);

            //await Misty.StopKeyPhraseRecognitionAsync()
            await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
            _ = Misty.SpeakAsync($"I am going to try and drive around. Please make sure there is good light, and I would prefer an open area! Driving in 10 seconds. To get me to stop, grab my scruff or say, Hey Misty, stop!", true, "Wander");
            
            await Task.Delay(10000);

            _driveManager = new DriveManager(Misty);
            _paused = false;
            _driveManager.StartDriving(
                new DriveManagerParameters
                {
                    DriveMode = Wander.Types.DriveMode.Wander,
                    DebugMode = false
                });

            return new ResponsePacket { Success = true };
        }

        public override async Task<ResponsePacket> Stop()
        {
            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("wander");
            samples.Add("wonder");
            samples.Add("wondering");
            samples.Add("walk around");
            samples.Add("go away");
            samples.Add("drive");
            samples.Add("patrol");

            intent = new Intent
            {
                Name = "wander",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        private bool _paused;

        public override async void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            if(_driveManager == null)
            {
                //not ready yet!
                return;
            }

            if (robotInteractionEvent.Step != RobotInteractionStep.Dialog &&
                robotInteractionEvent.Step != RobotInteractionStep.BumperPressed &&
                robotInteractionEvent.Step != RobotInteractionStep.CapTouched)
            {
                return;
            }

            if (_driveManager.DriveMode == Wander.Types.DriveMode.Stopped)
            {
                if(robotInteractionEvent.BumperState.BackLeft == TouchSensorOption.Contacted || robotInteractionEvent.BumperState.BackRight == TouchSensorOption.Contacted)
                {

                    //await Misty.StopKeyPhraseRecognitionAsync()
                    await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                    _ = Misty.SpeakAsync("Wandering in 10 seconds. When you are ready Say, Hey Misty, and tell me to stop, drive slow, or pick a new mode!", true, "WanderPhraseError");
                    
                    await Task.Delay(10000);
                    _driveManager.StartDriving(
                        new DriveManagerParameters
                        {
                            DriveMode = Wander.Types.DriveMode.Wander,
                            DebugMode = false
                        });
                }
                if (robotInteractionEvent.BumperState.FrontLeft == TouchSensorOption.Contacted || robotInteractionEvent.BumperState.FrontRight == TouchSensorOption.Contacted)
                {
                    //await Misty.StopKeyPhraseRecognitionAsync()
                    await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                    _ = Misty.SpeakAsync("Driving carefully in 10 seconds. When you are ready Say, Hey Misty, and tell me to stop, wander, or pick a new mode!", true, "WanderPhraseError");
                    
                    await Task.Delay(10000);
                    _driveManager.StartDriving(
                        new DriveManagerParameters
                        {
                            DriveMode = Wander.Types.DriveMode.Wander,
                            DebugMode = false
                        });
                }
            }
            
            if (robotInteractionEvent.CapTouchState.Scruff == TouchSensorOption.Contacted)
            {
                _driveManager.Stop();

                //await Misty.StopKeyPhraseRecognitionAsync()
                await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                _ = Misty.SpeakAsync("Stopping. When you are ready, Press my bumper, or Say, Hey Misty, and tell me to wander, drive slow, or pick a new mode!", true, "WanderPhraseError");
                
                return;
            }

            if (robotInteractionEvent.DialogState?.Step == DialogActionStep.StartedRecording)
            {
                _paused = true;
                _driveManager.Stop();
            }

            if (robotInteractionEvent.DialogState?.Step == DialogActionStep.FinalIntent)
            {
                if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
                {
                    //await Misty.StopKeyPhraseRecognitionAsync()
                    await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                    _ = Misty.SpeakAsync($"I didn't hear anything. Say, Hey Misty, and speak to me!", true, "WanderPhraseError");                    
                }                
                //hacky voice commands by string comparisons - TODO make context
                else if(robotInteractionEvent.DialogState.Text.Contains("stop"))
                {
                    _driveManager.Stop();

                    _ = Misty.SpeakAsync("Stopping. When you are ready Say, Hey Misty, and tell me to wander, drive slow, or pick a new mode!", true, "WanderPhraseError");

                    //await Misty.StopKeyPhraseRecognitionAsync()
                    await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                    return;
                }
                else if (robotInteractionEvent.DialogState.Text.Contains("start") && robotInteractionEvent.DialogState.Text.Contains("driving") ||
                    (robotInteractionEvent.DialogState.Text.Contains("begin") && robotInteractionEvent.DialogState.Text.Contains("driving") ||
                    robotInteractionEvent.DialogState.Text.Contains("wander") ||
                    (robotInteractionEvent.DialogState.Text.Contains("drive"))))
                {
                    _ = Misty.SpeakAsync("Wandering in 10 seconds. When you are ready Say, Hey Misty, and tell me to stop, drive slow, or pick a new mode!", true, "WanderPhraseError");

                    //await Misty.StopKeyPhraseRecognitionAsync()
                    await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                    await Task.Delay(10000);

                    _driveManager.StartDriving(
                        new DriveManagerParameters
                        {
                            DriveMode = Wander.Types.DriveMode.Wander,
                            DebugMode = false
                        });
                }
                else if (robotInteractionEvent.DialogState.Text.Contains("be careful") ||
                    (robotInteractionEvent.DialogState.Text.Contains("slow")))
                {
                    _ = Misty.SpeakAsync("Driving carefully in 10 seconds. When you are ready Say, Hey Misty, and tell me to stop, wander, or pick a new mode!", true, "WanderPhraseError");

                    //await Misty.StopKeyPhraseRecognitionAsync()
                    await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                    await Task.Delay(10000);

                    _driveManager.StartDriving(
                        new DriveManagerParameters
                        {
                            DriveMode = Wander.Types.DriveMode.Careful,
                            DebugMode = false
                        });
                }
                else if (robotInteractionEvent.DialogState.Contexts.Contains("all-modes") && !robotInteractionEvent.DialogState.Intent.Equals("Wander", StringComparison.OrdinalIgnoreCase) && !robotInteractionEvent.DialogState.Intent.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                {
                    PackageData pd = new PackageData(MysticMode.Wander, robotInteractionEvent.DialogState.Intent)
                    {
                        ModeContext = PackageData.ModeContext,
                        Parameters = PackageData.Parameters
                    };

                    CallSwitchMode?.Invoke(this, pd);
                    return;
                }
                else
                {
                    _ = Misty.SpeakAsync($"Not sure I understood that!. Say, Hey Misty, and speak to me!", true, "WanderPhraseError");

                    //await Misty.StopKeyPhraseRecognitionAsync()
                    await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                }
                return;
            }
            if (_paused && robotInteractionEvent.DialogState?.Step == DialogActionStep.CompletedSpeaking)
            {
                _paused = false;
                //_ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 5000, 4000);
                _driveManager.StartDriving(
                    new DriveManagerParameters
                    {
                        DriveMode = _driveManager.DriveMode,
                        DebugMode = false
                    });
            }
        }
    }
}