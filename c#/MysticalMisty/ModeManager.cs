using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Logger;
using MistyRobotics.SDK.Messengers;
using MysticCommon;
using MysticModesDemo;

namespace MysticalMisty
{
    /**
	    Idle = 0,
	    Sentry = 1,
	    Settings = 2,
	    Repeat = 3,
	    Backward = 4,
	    WiseWords = 5,
	    ConnectToNetwork = 6,
	    Weather = 7,
	    Wander = 8,
	    VoiceCommand = 9,
	    FindObject = 10,
	    FindPerson = 11
			
	    --> 
	    In idle mode be ready for bumper and cap touch, and voice control with hey misty 
		    - she stays silent and doesn't mode
		    - back bumpers at same time is go to settings mode
		    - everything else owned by modes

	    In sentry mode be ready for bumper and cap touch, and voice control with hey misty or by checking in and asking
		    - she watches and follows folks with eyes and talks to them if they hang out

	    Settings mode bumpers are:
		    - front go up and down list
		    - head changes setting mode
		    - chin to save mode

	    Repeat
		    say what you say

	    Backward
		    say what you say backward

	    RubberDuckey
		    say what you say with things like... 
			    are you sure that ""
			    "" sounds like a wonderful idea, have you thought it through
				    yes/ no interaction later
    
        Magic Eight Ball

	    ConnectToNetwork
		    walk through network connection steps

	    Weather
		    weather for config place

	    Wander 
		    run wander skill - fix it too

	    VoiceCommand
		    control some misty aspects
			    change led, move arm, drive forward, etc

	    Find Object
		    wander with object detection

	    Find Person
		    wander with person detection

	    ad infinitum

		**/


    public class ModeManager : IDisposable
    {
        private static IRobotMessenger _misty;
        private static ModeManager _modeManager;        
        private static ISDKLogger _logger;
        private static IDictionary<MysticMode, IModePackage> _modePackages = new Dictionary<MysticMode, IModePackage>();
        private static IDictionary<string, object> _parameters = new Dictionary<string, object>();
        private static MysticMode _currentMode = MysticMode.Uninitialized;
        private static IModePackage _currentPackage;

        private static SemaphoreSlim _modeLock = new SemaphoreSlim(1, 1);

        //Move to Setting Manager class...
        private static MistySettings _savedMistySettings = new MistySettings();
        private static MistySettings _unsavedMistySettings = new MistySettings();

        private ModeManager() {}

        public static async Task<ModeManager> Initialize(IDictionary<string, object> parameters, IRobotMessenger misty)
        {
            try
            {
                await _modeLock.WaitAsync();

                if (_modeManager != null)
                {
                    _logger.LogInfo("ModeManager reinitialized.");
                    return _modeManager;
                }

                _modeManager = new ModeManager();
                _parameters = parameters;
                _misty = misty;

                //Let them know we are gonna check for wifi 

                //TODO Wait for wifi connection and data for a bit


                StartModePackage smp = new StartModePackage(_misty);
                UnimplementedModePackage unp = new UnimplementedModePackage(_misty);

                //Add modes
                _modePackages.Add(MysticMode.Start, smp);
                _modePackages.Add(MysticMode.Idle, new IdleModePackage(_misty));
                _modePackages.Add(MysticMode.Repeat, new RepeatModePackage(_misty));
                _modePackages.Add(MysticMode.Backward, new BackwardModePackage(_misty));
                _modePackages.Add(MysticMode.RubberDuckey, new RubberDuckeyModePackage(_misty));
                _modePackages.Add(MysticMode.MagicEightBall, new MagicEightBallModePackage(_misty));
                _modePackages.Add(MysticMode.Sentry, new SentryModePackage(_misty));
                
                _modePackages.Add(MysticMode.VoiceCommand, unp); //a few commands as an example, change led, move arm, etc

                _modePackages.Add(MysticMode.Settings, unp); //manual change through modes - bumper

                _modePackages.Add(MysticMode.ConnectToNetwork, unp); //run this at startup if no wifi
                _modePackages.Add(MysticMode.Weather, unp);
                _modePackages.Add(MysticMode.Wander, unp);
                
                _modePackages.Add(MysticMode.FindObject, unp);
                _modePackages.Add(MysticMode.FindPerson, unp);

                _misty.RegisterRobotInteractionEvent(RobotInteractionCallback, 0, true, "RobotInteractionEvent", null, null);
                _misty.StartRobotInteractionEvent(true, null);

                _logger.LogVerbose("Mode Manager initialized.");

                IList<Intent> intents = new List<Intent>();
                foreach (KeyValuePair<MysticMode, IModePackage> package in _modePackages)
                {
                    if (package.Value.TryGetIntentTrigger(out Intent intent))
                    {
                        intents.Remove(intent);
                        intents.Add(intent);
                    }
                }

                await _misty.TrainNLPEngineAsync(
                 new Context
                 {
                     Name = "AllModes",
                     Intents = intents
                 },
                 true, true);

                //TODO Allow changing of default start mode
                await SwitchMode(MysticMode.Start);
                await smp.Start(_parameters);
                return _modeManager;
            }
            catch (Exception ex)
            {
                _misty.SkillLogger.LogError("Exception while initializing Mode Manager.", ex);
                return null;
            }
            finally
            {
                _modeLock.Release();
            }
        }

        public MysticMode GetMode()
        {
            return _currentMode;
        }

        public static async Task<ResponsePacket> SwitchMode(MysticMode newMode)
        {
            try
            {
                await _modeLock.WaitAsync();

                _logger.LogInfo($"{ _currentMode} stopping.");
                await _currentPackage.Stop();
                _logger.LogInfo($"{ _currentMode} stopped.");

                if (_modePackages.TryGetValue(newMode, out IModePackage modePackage))
                {
                    _currentPackage = modePackage;
                    _currentMode = newMode;
                    _logger.LogInfo($"{ _currentMode} starting.");
                    _ = _currentPackage.Start(_parameters);

                    return new ResponsePacket
                    {
                        Success = true
                    };
                }

                if (_modePackages.TryGetValue(MysticMode.Idle, out IModePackage idlePackage))
                {
                    _currentPackage = idlePackage;
                    _currentMode = MysticMode.Idle;

                    _logger.LogWarning("Mode does not exist, starting Idle.");
                    _ = _currentPackage.Start(_parameters);

                    return new ResponsePacket
                    {
                        Success = false,
                        ErrorDetails = new ErrorDetails
                        {
                            ResponseMessage = "Mode does not exist, switching to Idle.",
                            ErrorLevel = SkillLogLevel.Warning
                        }
                    };
                }

                _logger.LogError("Mode does not exist, failed to switch to Idle.");

                return new ResponsePacket
                {
                    Success = false,
                    ErrorDetails = new ErrorDetails
                    {
                        ResponseMessage = "Mode does not exist, failed to switch to Idle.",
                        ErrorLevel = SkillLogLevel.Error
                    }
                };
            }
            catch (Exception ex)
            {
                _misty.SkillLogger.LogError("Exception while setting mode.", ex);
                return new ResponsePacket
                {
                    Success = false,
                    ErrorDetails = new ErrorDetails
                    {
                        ResponseException = ex,
                        ResponseMessage = "Exception while setting mode.",
                        ErrorLevel = SkillLogLevel.Error
                    }
                };
            }
            finally
            {
                _modeLock.Release();
            }
        }
/*
        private static async Task<ResponsePacket> StartInteraction()
        {
            try
            {
                await _modeLock.WaitAsync();
                _misty.StartConversation(ConversationName, null);
                return new ResponsePacket
                {
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _misty.SkillLogger.LogError("Exception while setting mode.", ex);
                return new ResponsePacket
                {
                    Success = false,
                    ErrorDetails = new ErrorDetails
                    {
                        ResponseException = ex,
                        ResponseMessage = "Exception while setting mode.",
                        ErrorLevel = SkillLogLevel.Error
                    }
                };
            }
            finally
            {
                _modeLock.Release();
            }
        }

        private static async Task<ResponsePacket> StopInteraction()
        {
            try
            {
                await _modeLock.WaitAsync();
                _misty.StopConversation(null);
                return new ResponsePacket
                {
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _misty.SkillLogger.LogError("Exception while setting mode.", ex);
                return new ResponsePacket
                {
                    Success = false,
                    ErrorDetails = new ErrorDetails
                    {
                        ResponseException = ex,
                        ResponseMessage = "Exception while setting mode.",
                        ErrorLevel = SkillLogLevel.Error
                    }
                };
            }
            finally
            {
                _modeLock.Release();
            }
        }*/


        private static void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            if(_currentMode != MysticMode.Uninitialized && _currentPackage != null)
            {
                _currentPackage.ProcessEvent(new MysticEvent { RobotInteractionEvent = robotInteractionEvent });
            }
        }

        #region IDisposable Support

        private bool _isDisposed = false;

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _currentMode = MysticMode.Uninitialized;
                    _modePackages.Clear();
                }

                _currentPackage = null;
                _modePackages = null;
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}