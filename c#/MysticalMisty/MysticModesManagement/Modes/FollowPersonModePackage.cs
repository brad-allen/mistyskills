
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;
using MistyRobotics.Common.Types;
using System.Threading;

namespace MysticModesManagement
{
    public class FollowPersonModePackage : BaseAllModesPackage
    {
        public override event EventHandler<PackageData> CallSwitchMode;
        public FollowPersonModePackage(IRobotMessenger misty) : base(misty) { }

        private bool _isFollowing;
        private bool _isSpeaking;
        private bool _isInitialized;
        private Timer _timer;
        private string _lastFace;

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            base.Start(packageData);

            _isFollowing = true;
            _isSpeaking = true;

            //Register a face event so we get more than just New Face events
            Misty.RegisterFaceRecognitionEvent(FaceSeen, 1000, true, null, "advanced-face-event", null);
            _ = Misty.StartFaceRecognitionAsync();

            //hanging on end of speech?
            _timer = new Timer(LookForPerson, null, 10000, 25000);            
            _ = Misty.CreateActionAsync("stop-follow-mystic1", "LED:255,0,0;STOP-FOLLOW;", true);
            _ = Misty.CreateActionAsync("follow-face-mystic1", "LED:0,255,0;FOLLOW-FACE;", true);

            _ = Misty.SpeakAsync("Press my front bumpers to stop following and speech. Say hey Misty and tell me my new mode when you are done. Okay. I'm looking for a person!", true, "saying");
            //await Misty.StopKeyPhraseRecognitionAsync()
            _ = Misty.StartActionAsync("follow-face-mystic1", true);
            _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
            
            _isInitialized = true;

            return new ResponsePacket { Success = true };
        }

        private void FaceSeen(IFaceRecognitionEvent faceEvent)
        {
            _lastFace = faceEvent.Label;
        }

        public override async Task<ResponsePacket> Stop()
        {
            await Misty.StartActionAsync("stop-follow-mystic1", true);
            _isInitialized = false;
            Misty.UnregisterEvent("advanced-face-event", null);
            _timer.Dispose();

            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        private void LookForPerson(object timerData)
        {
            if(_isSpeaking && _isInitialized)
            {
                if (string.IsNullOrWhiteSpace(_lastFace))
                {
                    _ = Misty.SpeakAsync($"I don't seem to see anyone!", true, "FollowFace");
                }
                else
                {
                    _ = Misty.SpeakAsync($"The last person I saw was {(_lastFace == "unknown person" ? "someone I don't think I know. Hello!" : _lastFace)}", true, "FollowFace");
                }
            }
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("follow me");
            samples.Add("follow");
            samples.Add("follow face");
            samples.Add("follow person");
            samples.Add("where am I");

            intent = new Intent
            {
                Name = "follow-face",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        public override async void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            if(!_isInitialized)
            {
                return;
            }

            if (robotInteractionEvent.DialogState?.Step != MistyRobotics.Common.Types.DialogActionStep.FinalIntent &&
                   robotInteractionEvent.Step != RobotInteractionStep.BumperPressed &&
                   (robotInteractionEvent.Step != RobotInteractionStep.NewFace && string.IsNullOrWhiteSpace(robotInteractionEvent.FaceSeen)) &&
                   robotInteractionEvent.Step != RobotInteractionStep.CapTouched)
            {
                return;
            }

            //Keep it moving as some other items can stop following

          /*  if (_isFollowing)
            {
                _ = Misty.StartActionAsync("follow-face-mystic1", true);
            }
            else
            {
                _ = Misty.StartActionAsync("stop-follow-mystic1", true);
            }*/

            if (robotInteractionEvent.Step == RobotInteractionStep.CapTouched && robotInteractionEvent.CapTouchState.Scruff == TouchSensorOption.Contacted)
            {
                Misty.StartAction("body-reset", true, null);
                PackageData pd = new PackageData(MysticMode.FollowPerson, "idle")
                {
                    ModeContext = PackageData.ModeContext,
                    Parameters = PackageData.Parameters
                };

                CallSwitchMode?.Invoke(this, pd);
                return;
            }

            if (robotInteractionEvent.Step == RobotInteractionStep.BumperPressed)
            {
                if (robotInteractionEvent.BumperState.FrontRight == TouchSensorOption.Contacted)
                {
                    //stop follow button
                    if (_isFollowing)
                    {
                        _ = Misty.StartActionAsync("stop-follow-mystic1", true);
                        _ = Misty.SpeakAsync("Stop following.", true, "FollowFace");
                    }
                    else
                    {
                        _ = Misty.StartActionAsync("follow-face-mystic1", true);
                        _ = Misty.SpeakAsync("Follow person.", true, "FollowFace");
                    }
                    _isFollowing = !_isFollowing;
                }
                else if (robotInteractionEvent.BumperState.FrontLeft == TouchSensorOption.Contacted)
                {
                    //shush button
                    _isSpeaking = !_isSpeaking;
                    _ = Misty.SpeakAsync(_isSpeaking ? "Verbosity is good." : "Okay, I can be quiet", true, "FollowFace");
                }
            }

            if (_isSpeaking && robotInteractionEvent.Step == RobotInteractionStep.NewFace)              
            {
                _lastFace = robotInteractionEvent.FaceSeen;
                _ = Misty.SpeakAsync($"I see a new person! I see {(robotInteractionEvent.FaceSeen == "unknown person" ? "someone I don't think I know. Hello!" : robotInteractionEvent.FaceSeen)}", true, "FollowFace");
            }

            if (robotInteractionEvent.DialogState?.Step == DialogActionStep.FinalIntent)
            {
                Misty.SkillLogger.LogInfo($"Heard: {robotInteractionEvent.DialogState.Text}");
                if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
                {
                    _ = Misty.SpeakAsync($"I didn't hear anything. Say hey misty and Try something else !", true, "RepeatPhraseRetry");
                }
                else if (robotInteractionEvent.DialogState.Contexts.Contains("all-modes") && !robotInteractionEvent.DialogState.Intent.Equals("follow-face", StringComparison.OrdinalIgnoreCase) && !robotInteractionEvent.DialogState.Intent.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                {
                    //TODO This doesn't work with the flurry of events, need more control/awaiting


                    PackageData pd = new PackageData(MysticMode.FollowPerson, robotInteractionEvent.DialogState.Intent)
                    {
                        ModeContext = PackageData.ModeContext,
                        Parameters = PackageData.Parameters
                    };

                    CallSwitchMode?.Invoke(this, pd);
                    return;
                }
                else
                {
                    _ = Misty.SpeakAsync($"Sorry, I didn't understand that. Say hey misty and Try something else !", true, "RepeatPhraseRetry");
                }

                _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
            }

          /*  if (robotInteractionEvent.Step == RobotInteractionStep.Dialog && robotInteractionEvent.DialogState?.Step == DialogActionStep.CompletedSpeaking)
            {
                _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                if (_isFollowing)
                {
                    _ = Misty.StartActionAsync("follow-face-mystic1", true);
                }
                else
                {
                    _ = Misty.StartActionAsync("stop-follow-mystic1", true);
                }
            }*/
        }
    }
}