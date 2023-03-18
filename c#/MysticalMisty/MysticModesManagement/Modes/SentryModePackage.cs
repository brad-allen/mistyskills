using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Events;
using MysticCommon;

namespace MysticModesManagement
{
    public sealed class DetectionSkillParameters
    {
        public EntryDirection EntryDirection { get; set; }
        public double OuterRangeYaw { get; set; } = 356;
        public double InnerRangeYaw { get; set; } = 0;
        
        /*
        public int PostGreetDelayMs { get; set; } = 2000;
        public int PostGoodbyeDelayMs { get; set; } = 2000;
        public double MinimumWaitingHeadPitch { get; set; } = 0;
        public double MinimumWaitingHeadYaw { get; set; } = 0;
        public double MaximumWaitingHeadPitch { get; set; } = 0;
        public double MaximumWaitingHeadYaw { get; set; } = 0;
        public int StartScreenSensitivity { get; set; } = 5;
        public int StartScreenPostGoodbyeDelayMs { get; set; } = 2000;
        public int StartScreenPostGreetDelayMs { get; set; } = 2000;
        public int TransitionToWaitingDelayMs { get; set; } = 2500;
        */
        public double BodyDetectDistanceMeters { get; set; } = 1.1;

        //public int FaceDetectDistanceCm { get; set; } = 180;

        public bool MonitorWalkAways { get; set; } = true;

        public bool UseTOFsInDetection { get; set; } = true;

        public int StepAsideMs { get; set; } = 1500;
        public double StepAsideDistanceMeters { get; set; } = 1.0;

    }

    public enum EntryDirection
    {
        Unknown,
        LeftToRight,
        RightToLeft,
        HeadOn
    }

    public enum EntryStatus
    {
        Unknown,
        Waiting,
        Greet,
        Goodbye,
        Lost,
        Screen,
        Blocked
    }

    public class SentryParameters
    {
        public static EntryDirection EntryDirection { get; set; }
        public static double OuterRangeYaw { get; set; } = 356;
        public static double InnerRangeYaw { get; set; } = 0;
        public static int PostGreetDelayMs { get; set; } = 2000;
        public static int PostGoodbyeDelayMs { get; set; } = 2000;
        public double MinimumWaitingHeadPitch { get; set; } = 0;
        public static double MinimumWaitingHeadYaw { get; set; } = 0;
        public static double MaximumWaitingHeadPitch { get; set; } = 0;
        public static double MaximumWaitingHeadYaw { get; set; } = 0;
        public static int StartScreenSensitivity { get; set; } = 5;
        public static int StartScreenPostGoodbyeDelayMs { get; set; } = 2000;
        public static int StartScreenPostGreetDelayMs { get; set; } = 2000;
        public static int TransitionToWaitingDelayMs { get; set; } = 2500;

        public static double BodyDetectDistanceMeters { get; set; } = 1.1;
        public static int FaceDetectDistanceCm { get; set; } = 180;

        public static bool MonitorWalkAways { get; set; } = true;

        public static bool UseTOFsInDetection { get; set; } = true;

        public int StepAsideMs { get; set; } = 1500;
        public static double StepAsideDistanceMeters { get; set; } = 1.0;

    }

    public sealed class RecentClosestObject
    {
        public double? DistanceInMeters { get; set; } = null;
        public double? CameraDistance { get; set; } = null;
        public DateTimeOffset Timestamp { get; set; }
    }

    public class SentryModePackage : BaseModePackage
    {
        private bool _repeatTime;
        public SentryModePackage(IRobotMessenger misty) : base(misty) { }
        private DetectionSkillParameters _sentryParameters = new DetectionSkillParameters();
        private string _currentEyeImage = "e_DefaultContent.jpg";

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            _repeatTime = true;
            _ = Misty.RegisterVoiceRecordEvent(0, true, "Test-is-this-needed", null); //shouldn't be needed, but might be still - oops

            //Set the intents of this modes
            //overlap contexts if want to add to the existing ones, filtered intents will remove those options from any current contexts after processing, retrain is experimental
            _ = Misty.SetContextAsync("AllModes", false, null, false);
            _ = Misty.SpeakAsync("Okay, when you are ready, say, Hey Misty, and tell me what you want to do.", true, "idleSpeech");

            _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 2500);
          
            /*
            Misty.RegisterBumpSensorEvent(BumpCallback, 0, false, null, "MoveBump", null);
            */

            List<ObjectValidation> objectValidations = new List<ObjectValidation>();
            objectValidations.Add(new ObjectValidation { Name = ObjectFilter.Description, Comparison = ComparisonOperator.Equal, ComparisonValue = "person" });

            Misty.RegisterObjectDetectionEvent(ObjectDetectionCallback, 500, true, objectValidations, "MoveObject", null);

            //Start object detection in case it has shut down for some reason
            Misty.StartObjectDetector(0.6, 0, 3, null);
            
            //Front Right Time of Flight
            List<TimeOfFlightValidation> tofFrontRightValidations = new List<TimeOfFlightValidation>();
            tofFrontRightValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.FrontRight });
            tofFrontRightValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.Status, Comparison = ComparisonOperator.LessThanOrEqual, ComparisonValue = 101 });
            tofFrontRightValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.DistanceInMeters, Comparison = ComparisonOperator.LessThanOrEqual, ComparisonValue = _sentryParameters.BodyDetectDistanceMeters });
            Misty.RegisterTimeOfFlightEvent(TOFDistanceCallback, 100, true, tofFrontRightValidations, "FrontRightDetect", null);

            //Front Left Time of Flight
            List<TimeOfFlightValidation> tofFrontLeftValidations = new List<TimeOfFlightValidation>();
            tofFrontLeftValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.FrontLeft });
            tofFrontLeftValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.Status, Comparison = ComparisonOperator.LessThanOrEqual, ComparisonValue = 101 });
            tofFrontLeftValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.DistanceInMeters, Comparison = ComparisonOperator.LessThanOrEqual, ComparisonValue = _sentryParameters.BodyDetectDistanceMeters });
            Misty.RegisterTimeOfFlightEvent(TOFDistanceCallback, 100, true, tofFrontLeftValidations, "FrontLeftDetect", null);

            _personDetected = false;
            _entryStatus = EntryStatus.Waiting;
            _seenCount = 0;
            _waitingOnTimer = false;

            switch (_sentryParameters.EntryDirection)
            {
                case EntryDirection.LeftToRight:
                    _currentEyeImage = "e_ContentLeft.jpg";
                    break;
                case EntryDirection.RightToLeft:
                    _currentEyeImage = "e_ContentRight.jpg";
                    break;
                default:
                    _currentEyeImage = "e_DefaultContent.jpg";
                    break;
            }

            Misty.DisplayImage(_currentEyeImage, null, false, null);

            return new ResponsePacket { Success = true };
        }

        public override async Task<ResponsePacket> Stop()
        {
            //Do cleanup here...
            _repeatTime = false;
            Misty.UnregisterEvent("Test-is-this-needed", null);

            Misty.UnregisterEvent("MoveObject", null);
            Misty.UnregisterEvent("MoveBump", null);
            Misty.UnregisterEvent("MovementAudioComplete", null);
            Misty.UnregisterEvent("MovementTTSComplete", null);

            //TODO make a disposable?
            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("sentry");
            samples.Add("guard");
            samples.Add("protect");
            samples.Add("vigilant");
            samples.Add("be careful");

            intent = new Intent
            {
                Name = "sentry",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        private void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
          switch(robotInteractionEvent.EventType)
            {
                case EventType.BumpSensor:
                    BumpCallback(robotInteractionEvent.BumperState);
                    break;
                case EventType.FaceRecognition:
                    break;
            }
            
        }

        private bool _personDetected = false;
        private object _detectedLock = new object();
        private Timer _isWaitingCheckTimer;
        private Timer _walkAwayTimer;
        private EntryStatus _entryStatus = EntryStatus.Waiting;
        private object _lockWaiting = new object();
        private int _seenCount;
        private bool _waitingOnTimer;
        private RecentClosestObject _recentClosestObject = new RecentClosestObject();        
        private bool _personStillHereMonitor = false;

        private void BumpCallback(InteractionBumperState bumpEvent)
        {
            lock (_detectedLock)
            {
                if (!_personDetected && (bumpEvent.BackLeft == TouchSensorOption.Contacted ||
                    bumpEvent.BackRight == TouchSensorOption.Contacted ||
                    bumpEvent.FrontLeft == TouchSensorOption.Contacted ||
                    bumpEvent.FrontRight == TouchSensorOption.Contacted) )
                {
                    _personDetected = true;

                    //Greet!
                }
            }
        }

        private void TOFDistanceCallback(ITimeOfFlightEvent tofEvent)
        {
            if ((tofEvent.Status == 0 &&  //Deal with tof 101 false triggers
                tofEvent.DistanceInMeters <= _sentryParameters.BodyDetectDistanceMeters) ||
                (tofEvent.Status == 101 && tofEvent.DistanceInMeters >= 0.138 &&  //Deal with tof 101 false triggers
                tofEvent.DistanceInMeters <= _sentryParameters.BodyDetectDistanceMeters))

            {
                _recentClosestObject.Timestamp = DateTime.Now;
                _recentClosestObject.DistanceInMeters = tofEvent.DistanceInMeters;
            }
        }

        private async void ObjectDetectionCallback(IObjectDetectionEvent objectEvent)
        {
            try
            {
                if (_personDetected)
                {
                    return;
                }

                if (_sentryParameters.UseTOFsInDetection)
                {
                    if (_recentClosestObject.DistanceInMeters == null ||
                    _recentClosestObject.Timestamp < DateTime.Now.AddSeconds(-2.5) ||
                    _recentClosestObject.DistanceInMeters > _sentryParameters.BodyDetectDistanceMeters)
                    {
                        return;
                    }
                }

                //TODO Update data and test this with a timed callback to not worry aboout debounce etc?
                lock (_detectedLock)
                {
                    if (!_personDetected)
                    {
                        if (_personStillHereMonitor)
                        {
                            if (_entryStatus != EntryStatus.Blocked)
                            {
                                _entryStatus = EntryStatus.Blocked;
                                return;
                            }
                            else
                            {
                                //keeps doing this till they go away
                                return;
                            }
                        }
                        else if (_entryStatus == EntryStatus.Blocked)
                        {
                            _entryStatus = EntryStatus.Waiting;
                        }

                        if (_sentryParameters.EntryDirection == EntryDirection.HeadOn)
                        {
                            if (_entryStatus == EntryStatus.Waiting)
                            {
                                //greet
                                _entryStatus = EntryStatus.Greet;

                                //TODO Greet them and get thier attention!!!!!
                                // set intents
                                //Walking up

                                lock (_lockWaiting)
                                {
                                    if (!_waitingOnTimer)
                                    {
                                        _waitingOnTimer = true;
                                        _isWaitingCheckTimer = new Timer(WaitingCallback, null, SentryParameters.StartScreenPostGreetDelayMs, Timeout.Infinite);
                                    }
                                }
                                Misty.SkillLogger.Log($"Greeting person.");
                                Misty.Speak("Greet, Test Walk up", true, "test-wu", null);
                                return;
                            }
                            else if (_entryStatus == EntryStatus.Goodbye || _entryStatus == EntryStatus.Greet)
                            {
                                _seenCount++;
                            }
                            else
                            {
                                _seenCount = 0;
                            }
                        }

                        if (_entryStatus == EntryStatus.Waiting && _sentryParameters.EntryDirection != EntryDirection.HeadOn)
                        {
                            _seenCount = 0;

                            if (!_personDetected && _sentryParameters.EntryDirection == EntryDirection.LeftToRight ?
                                        objectEvent.Yaw >= -_sentryParameters.OuterRangeYaw && objectEvent.Yaw <= -_sentryParameters.InnerRangeYaw :
                                        objectEvent.Yaw >= _sentryParameters.InnerRangeYaw && objectEvent.Yaw <= _sentryParameters.OuterRangeYaw)
                            {
                                _entryStatus = EntryStatus.Greet;
                                
                                if (_personDetected)
                                {
                                    return;
                                }

                                //TODO Greet and set intents

                                lock (_lockWaiting)
                                {
                                    if (!_waitingOnTimer)
                                    {
                                        _waitingOnTimer = true;
                                        _isWaitingCheckTimer = new Timer(WaitingCallback, null, SentryParameters.StartScreenPostGreetDelayMs, Timeout.Infinite);
                                    }
                                }
                                Misty.SkillLogger.Log($"Greeting person.");
                                Misty.Speak("Greet, Test A", true, "test-a", null);
                            }
                            else if (!_personDetected && _sentryParameters.EntryDirection == EntryDirection.LeftToRight ?
                                         objectEvent.Yaw >= _sentryParameters.InnerRangeYaw && objectEvent.Yaw <= _sentryParameters.OuterRangeYaw :
                                        objectEvent.Yaw >= -_sentryParameters.OuterRangeYaw && objectEvent.Yaw <= -_sentryParameters.InnerRangeYaw)
                            {
                                _entryStatus = EntryStatus.Goodbye;


                                if (_personDetected)
                                {
                                    return;
                                }

                                //TODO Say goodbye and set intents

                                lock (_lockWaiting)
                                {
                                    if (!_waitingOnTimer)
                                    {
                                        _waitingOnTimer = true;
                                        _isWaitingCheckTimer = new Timer(WaitingCallback, null, SentryParameters.StartScreenPostGoodbyeDelayMs, Timeout.Infinite);
                                    }
                                }
                                Misty.Speak("Goodbye, Test C", true, "test-c", null);
                                Misty.SkillLogger.Log($"Saying goodbye.");
                            }
                        }
                        //How long till we address them
                        else if (_entryStatus == EntryStatus.Greet || _entryStatus == EntryStatus.Goodbye)
                        {
                            _seenCount++;
                        }
                        else
                        {
                            _seenCount = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
            }
        }

        private void WaitingCallback(object waitTimer)
        {
            try
            {
                lock (_detectedLock)
                {
                    if (_personDetected)
                    {
                        return;
                    }

                    lock (_lockWaiting)
                    {
                        _isWaitingCheckTimer?.Dispose();
                    }

                    if ((_entryStatus == EntryStatus.Greet || _entryStatus == EntryStatus.Goodbye) && _seenCount >= SentryParameters.StartScreenSensitivity)
                    {
                        if (_sentryParameters.MonitorWalkAways)
                        {
                            _walkAwayTimer = new Timer(WalkAwayCheckCallback, null, 500, 500);
                            _personStillHereMonitor = true;
                        }
                        _waitingOnTimer = true;
                        _personDetected = true;
                        _entryStatus = EntryStatus.Screen;
                        _seenCount = 0;

                        //OKAY, DO SOMETHING NOW THAT THEY ARE WAITING AROUND!
                        Misty.Speak("Hanging out?, Test D", true, "test-d", null);

                    }
                    else if (!_personDetected)
                    {
                        _seenCount = 0;
                        _entryStatus = EntryStatus.Waiting;
                        _waitingOnTimer = false;

                        //TODO Look around for someone or ?
                    }
                }
            }
            finally
            {
            }
        }

        private void WalkAwayCheckCallback(object timerData)
        {
            if (_recentClosestObject.DistanceInMeters == null ||
                    _recentClosestObject.Timestamp < DateTime.Now.AddMilliseconds(_sentryParameters.StepAsideMs >= -500 ? -_sentryParameters.StepAsideMs : -500) ||
                    _recentClosestObject.DistanceInMeters > _sentryParameters.StepAsideDistanceMeters)
            {
                _personStillHereMonitor = false;
                _walkAwayTimer?.Dispose();
            }
        }
    }
}