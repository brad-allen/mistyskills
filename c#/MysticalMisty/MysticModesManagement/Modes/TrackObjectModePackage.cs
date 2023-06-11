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
    public class TrackObjectModePackage : BaseAllModesPackage
    {
        public override event EventHandler<PackageData> CallSwitchMode;
        public TrackObjectModePackage(IRobotMessenger misty) : base(misty) { }

        private bool _isFollowing;
        private bool _isSpeaking;
        private bool _isInitialized;
        private Timer _timer;
        private string _lastObject;

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            await base.Start(packageData);

            _ = Misty.SpeakAsync("Press my front bumpers to stop following and speech. Say hey Misty and tell me my new mode when you are done. Okay. I'm looking for a person!", true, "saying");
            _isFollowing = true;
            _isSpeaking = true;

            //Register an object event so we get more than just New Object events
            Misty.RegisterObjectDetectionEvent(ObjectSeen, 1000, true, null, "advanced-object-event", null);
            await Misty.StartObjectDetectorAsync(0.6, 0, 2);

            //hanging on end of speech?
            _timer = new Timer(LookForObject, null, 10000, 15000);
            await Misty.CreateActionAsync("stop-follow1.mystic.en", "LED:255,0,0;STOP-FOLLOW;", true);
            await Misty.CreateActionAsync("follow-object1.mystic.en", "FOLLOW-OBJECT:book,0.5,2;LED:0,255,0;", true);

            //await Misty.StopKeyPhraseRecognitionAsync()
            _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
            _ = Misty.StartActionAsync("follow-object1.mystic.en", true);

            _isInitialized = true;

            return new ResponsePacket { Success = true };
        }

        private void ObjectSeen(IObjectDetectionEvent objectEvent)
        {
            _lastObject = objectEvent.Description;
        }

        public override async Task<ResponsePacket> Stop()
        {
            await Misty.StartActionAsync("stop-follow1.mystic.en", true);
            _isInitialized = false;
            Misty.UnregisterEvent("advanced-object-event", null);
            _timer.Dispose();

            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        private void LookForObject(object timerData)
        {
            if (_isFollowing)
            {
                _ = Misty.StartActionAsync("follow-object1.mystic.en", true);
            }
            else
            {
                _ = Misty.StartActionAsync("stop-follow1.mystic.en", true);
            }

            if (_isSpeaking && _isInitialized)
            {
                if (string.IsNullOrWhiteSpace(_lastObject))
                {
                    _ = Misty.SpeakAsync($"I don't seem to see a specific object!", true, "FollowObject");
                }
                else
                {
                    _ =Misty.SpeakAsync($"The last object I saw was {(_lastObject == "unknown" ? "something I don't think I know. Hello object!" : _lastObject)}", true, "FollowObject");
                }
            }
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("track object");
            samples.Add("track");
            samples.Add("track book");
            samples.Add("book");
            
            intent = new Intent
            {
                Name = "follow-object",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        public override async void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            if (!_isInitialized)
            {
                return;
            }

            if (robotInteractionEvent.DialogState?.Step != MistyRobotics.Common.Types.DialogActionStep.FinalIntent &&
                   robotInteractionEvent.Step != RobotInteractionStep.BumperPressed &&
                   (robotInteractionEvent.Step != RobotInteractionStep.NewObject && string.IsNullOrWhiteSpace(robotInteractionEvent.ObjectSeen)) &&
                   robotInteractionEvent.Step != RobotInteractionStep.CapTouched)
            {
                return;
            }

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

            if (robotInteractionEvent.Step == RobotInteractionStep.BumperPressed)
            {
                if (robotInteractionEvent.BumperState.FrontRight == TouchSensorOption.Contacted)
                {
                    //stop follow button
                    if (_isFollowing)
                    {
                        _ = Misty.StartActionAsync("stop-follow1.mystic.en", true);
                        _ = Misty.SpeakAsync("Stop following.", true, "TrackObject");
                    }
                    else
                    {
                        _ = Misty.StartActionAsync("follow-object1.mystic.en", true);
                        _ = Misty.SpeakAsync("Follow object.", true, "TrackObject");
                    }
                    _isFollowing = !_isFollowing;
                }
                else if (robotInteractionEvent.BumperState.FrontLeft == TouchSensorOption.Contacted)
                {
                    //shush button
                    _isSpeaking = !_isSpeaking;
                    _ = Misty.SpeakAsync(_isSpeaking ? "Verbose. I can do that." : "Here I am being very quiet.", true, "TrackObject");
                }
            }

            if (_isSpeaking && robotInteractionEvent.Step == RobotInteractionStep.NewObject)
            {
                _lastObject = robotInteractionEvent.ObjectSeen;
                _ = Misty.SpeakAsync($"I see a new object! I see {(robotInteractionEvent.ObjectSeen == "" ? "something I don't think I know. Hello!" : robotInteractionEvent.ObjectSeen)}", true, "TrackObject");
            }

            if (robotInteractionEvent.DialogState?.Step == DialogActionStep.FinalIntent)
            {
                Misty.SkillLogger.LogInfo($"Heard: {robotInteractionEvent.DialogState.Text}");
                if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
                {
                    _ = Misty.SpeakAsync($"I didn't hear anything. Say hey misty and Try something else !", true, "RepeatPhraseRetry");
                }
                else if (robotInteractionEvent.DialogState.Contexts.Contains("all-modes") && !robotInteractionEvent.DialogState.Intent.Equals("follow-object", StringComparison.OrdinalIgnoreCase) && !robotInteractionEvent.DialogState.Intent.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                {
                    PackageData pd = new PackageData(MysticMode.TrackObject, robotInteractionEvent.DialogState.Intent)
                    {
                        ModeContext = PackageData.ModeContext,
                        Parameters = PackageData.Parameters
                    };

                    CallSwitchMode?.Invoke(this, pd);
                    return;
                }
                else
                {
                    //Add adjustments for diff types
                    //await Misty.CreateActionAsync("follow-object-mystic1", "FOLLOW-OBJECT:book,50,2;", true);
                    //_ = Misty.StartActionAsync("follow-object-mystic1", true);

                    _ = Misty.SpeakAsync($"Sorry, I didn't understand that. Say hey misty and Try something else !", true, "RepeatPhraseRetry");
                }
                await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
            }
            
            if (robotInteractionEvent.Step == RobotInteractionStep.Dialog && robotInteractionEvent.DialogState?.Step == DialogActionStep.CompletedSpeaking)
            {
                _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
            }
        }
    }
}
