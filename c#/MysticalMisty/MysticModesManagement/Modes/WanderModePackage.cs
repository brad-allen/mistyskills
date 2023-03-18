using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;
using Wander;

namespace MysticModesManagement
{
    public class WanderModePackage : BaseModePackage
    {
        private bool _repeatTime;
        private IList<string> _responses1 = new List<string>();
        private IList<string> _responses2 = new List<string>();
        private Random _random = new Random();
        private DriveManager _driveManager;

        public WanderModePackage(IRobotMessenger misty) : base(misty) {}

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {

            _responses1.Add("Signs point to Yes.");
            _responses1.Add("Signs seem to indicate No.");

            _repeatTime = true;
            _ = Misty.RegisterVoiceRecordEvent(0, true, "Test-is-this-needed", null); //shouldn't be needed, but might be still - oops

                //listen for stop
            _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
            
            _ = Misty.SpeakAsync($"To get me to stop, grab my scruff or say, Hey Misty, stop!", true, "Wander");

            _driveManager = new DriveManager(Misty);
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
            //Do cleanup here...
            _repeatTime = false;
            Misty.UnregisterEvent("Test-is-this-needed", null);
            //TODO make a disposable?
            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("wander");
            samples.Add("walk around");
            samples.Add("go away");

            intent = new Intent
            {
                Name = "Wander",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        private async void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            if (robotInteractionEvent.DialogState?.Step == MistyRobotics.Common.Types.DialogActionStep.CompletedASR)
            {
                if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
                {
                    // if unsure, ask again while stopped

                    _driveManager.Stop();

                    _ = Misty.SpeakAsync($"I didn't hear anything. Say, Hey Misty, and try again!", true, "WanderPhraseRetry");
                }
                else
                {
                    try
                    {

                        //speech event, was it stop?

                        _driveManager.Stop();

                        //if not, process, and then continue
                        await Task.Delay(5000); //TODO

                        _driveManager.StartDriving(new DriveManagerParameters
                        {
                            DriveMode = Wander.Types.DriveMode.Careful,
                            DebugMode = true
                        });

                    }
                    catch
                    {
                        //should never happen
                        _ = Misty.SpeakAsync($"All my ones and zeroes are acting strange. Say, Hey Misty, and speak to me!", true, "WanderPhraseError");                        
                    }
                }

                _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
            }
            //else scruff?
                //motors are stopped when grabbed, but need to stop wander process too or it will start right back up

            //actual wander handled in wander project, ignore other events
        }
    }
}