using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Logger;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using MysticCommon;

namespace MysticModesManagement
{
    public class CommonContexts
    {
        private static IRobotMessenger _misty;

        public async void Initialize(IDictionary<string, object> parameters, IRobotMessenger misty)
        {
            try
            {
                _misty = misty; 
            }
            catch (Exception ex)
            {
                _misty.SkillLogger.LogError("Exception while initializing Mode Manager.", ex);
            }
            finally
            {
            }
        }


        public async Task CreateYesNoContext()
        {

            IList<Intent> intents = new List<Intent>();
            Intent yesIntent = new Intent();
            List<string> yesSamples = new List<string>();
            yesSamples.Add("yes");
            yesSamples.Add("yeah");
            yesSamples.Add("sure");
            yesSamples.Add("yep");
            yesSamples.Add("see");
            yesSamples.Add("sea");
            yesSamples.Add("affirmative");

            yesIntent.Name = "yes";
            yesIntent.Samples = yesSamples;
            intents.Add(yesIntent);

            Intent noIntent = new Intent();
            List<string> noSamples = new List<string>();
            noSamples.Clear();
            noSamples.Add("no");
            noSamples.Add("nah");
            noSamples.Add("nope");
            noSamples.Add("nada");
            noSamples.Add("negative");

            noIntent.Name = "no";
            noIntent.Samples = noSamples;
            intents.Add(noIntent);
            await _misty.TrainNLPEngineAsync(
                new Context
                {
                    Name = "YesNo",
                    Intents = intents
                },
                true, true);
        }
    }

    public class ModeManager : IDisposable
    {
        private static IRobotMessenger _misty;
        private static ModeManager _modeManager;        
        private static ISDKLogger _logger;
        private static IDictionary<MysticMode, IModePackage> _modePackages = new Dictionary<MysticMode, IModePackage>();
        private static IDictionary<string, object> _parameters = new Dictionary<string, object>();
        private static MysticMode _currentMode = MysticMode.Uninitialized;
        private static IModePackage _currentPackage;
        private static Timer _ipDisplayTimer;
        private static CommonContexts _commonContexts;
        private static IDictionary<string, MysticMode> _intentModes = new Dictionary<string, MysticMode>(StringComparer.OrdinalIgnoreCase);

        private static SemaphoreSlim _modeLock = new SemaphoreSlim(1, 1);
        private static SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

        //Move to Setting Manager class...
        private static MistySettings _savedMistySettings = new MistySettings();
        private static MistySettings _unsavedMistySettings = new MistySettings();

        private ModeManager() { }
        private static int _ipChecks = 0;
        private static Context _modeContext = new Context();

        public static async Task<ModeManager> Initialize(IDictionary<string, object> parameters, IRobotMessenger misty)
        {
            try
            {
                await _initLock.WaitAsync();
                _parameters = parameters;
                _misty = misty;
                _logger = _misty.SkillLogger;

                if (_modeManager != null)
                {
                    _logger.LogInfo("ModeManager reinitialized.");
                    return _modeManager;
                }

                _commonContexts = new CommonContexts();
                _commonContexts.Initialize(_parameters, _misty);
                _modeManager = new ModeManager();

                //TODO Break this out
                StartModePackage smp = new StartModePackage(_misty);
                UnimplementedModePackage unp = new UnimplementedModePackage(_misty);

                _modePackages.Add(MysticMode.Start, smp);
                _modePackages.Add(MysticMode.Idle, new IdleModePackage(_misty));
                _modePackages.Add(MysticMode.Repeat, new RepeatModePackage(_misty));
                _modePackages.Add(MysticMode.Backward, new BackwardModePackage(_misty));
                _modePackages.Add(MysticMode.RubberDuckey, new RubberDuckeyModePackage(_misty));
                _modePackages.Add(MysticMode.MagicEightBall, new MagicEightBallModePackage(_misty));

                _modePackages.Add(MysticMode.VoiceCommand, new VoiceCommandModePackage(_misty));
                _modePackages.Add(MysticMode.Weather, new WeatherModePackage(_misty)); 
                _modePackages.Add(MysticMode.Sentry, new SentryModePackage(_misty));
                _modePackages.Add(MysticMode.Wander, new WanderModePackage(_misty));
                _modePackages.Add(MysticMode.Settings, new SettingsModePackage(_misty));

                //TODO SOME DAY
                //_modePackages.Add(MysticMode.ConnectToNetwork, unp);
                //_modePackages.Add(MysticMode.FindObject, unp);
                //_modePackages.Add(MysticMode.FindPerson, unp);

                //Filter or this may get noisy?
                /*IList<RobotInteractionValidation> riv = new List<RobotInteractionValidation>
                {
                    new RobotInteractionValidation
                    {
                         Name = RobotInteractionFilter.,
                           
                    },
                    new RobotInteractionValidation
                    {

                    }
                };*/

                //_misty.RegisterRobotInteractionEvent(RobotInteractionCallback, 0, true, "RobotInteractionEvent", null, null);
                //_misty.StartRobotInteractionEvent(true, null);

                //Let packages manage events

                _logger.LogVerbose("Mode Manager initialized.");

                await _commonContexts.CreateYesNoContext();

                IList<Intent> intents = new List<Intent>();
                foreach (KeyValuePair<MysticMode, IModePackage> package in _modePackages)
                {
                    if (package.Value.TryGetIntentTrigger(out Intent intent))
                    {
                        intents.Remove(intent);
                        intents.Add(intent);

                        _intentModes.Add(intent.Name, package.Key);
                    }
             
                    package.Value.CallSwitchMode += SwitchModeRequestReceived;
                }

                _modeContext = new Context
                {
                    Name = "AllModes",
                    Intents = intents,
                    Editable = true
                };

                var test = await _misty.TrainNLPEngineAsync(_modeContext, true, true);
                _misty.TrainNLPEngine(_modeContext, true, true, null);


                //Check for wifi
                _misty.GetDeviceInformation(DeviceInformationHandler);

                await SwitchMode(MysticMode.Start);
               // await smp.Start(_parameters);
                return _modeManager;
            }
            catch (Exception ex)
            {
                _misty.SkillLogger.LogError("Exception while initializing Mode Manager.", ex);
                return null;
            }
            finally
            {
                _initLock.Release();
            }
        }

        private static async void DeviceInformationHandler(IGetDeviceInformationResponse data)
        {
            _ipChecks++;
            
            //Let eye layer be overwritten so blinks don't rewrite over text
            await _misty.SetImageDisplaySettingsAsync(null, new MistyRobotics.SDK.ImageSettings
            {
                PlaceOnTop = false,
                Visible = true
            });

            //Create Text layer under eyes (some big eyes will overlap a little)
            await _misty.SetTextDisplaySettingsAsync("IpLayer",
                new TextSettings
                {
                    Height = 60,
                    HorizontalAlignment = ImageHorizontalAlignment.Center,
                    VerticalAlignment = ImageVerticalAlignment.Bottom,
                    Weight = 70,
                    Visible = true,
                    PlaceOnTop = true,
                    Red = 255,
                    Green = 255,
                    Blue = 255,
                    Style = ImageStyle.Normal,
                    Size = 40
                }
            );

            if (data.Status == ResponseStatus.Success)
            {
                await _misty.DisplayTextAsync(data.Data.IPAddress, "IpLayer");
                _ipChecks = 0; //reset for requestable option

                //Display for X secs / make config and requestable
                _ipDisplayTimer = new Timer(IpDisplayedCallback, null, 20000, Timeout.Infinite);
            }
            else
            {
                switch (_ipChecks)
                {
                    case 1:
                        await _misty.DisplayTextAsync("Looking for IP address...", "IpLayer");
                        await Task.Delay(5000); // wait for system to recover
                        _misty.GetDeviceInformation(DeviceInformationHandler);
                        break;
                    case 2:
                        await _misty.DisplayTextAsync("Where am I?", "IpLayer");
                        await Task.Delay(10000); // wait for system to recover       
                        _misty.GetDeviceInformation(DeviceInformationHandler);
                        break;
                    case 3:
                        await _misty.DisplayTextAsync("One last try!", "IpLayer");
                        await Task.Delay(10000); // wait for system to recover  
                        _misty.GetDeviceInformation(DeviceInformationHandler);
                        break;
                    default:
                        await _misty.DisplayTextAsync("Could not obtain IP.", "IpLayer");
                        _ipChecks = 0; //reset for requestable option
                        _ipDisplayTimer = new Timer(IpDisplayedCallback, null, 10000, Timeout.Infinite);
                        break;
                }
            }
        }

        public static void IpDisplayedCallback(object timerData)
        {
            _misty.SetTextDisplaySettings("IpLayer",
                new TextSettings
                {
                    Deleted = true
                },
                null);
        }

        public MysticMode GetMode()
        {
            return _currentMode;
        }

        public static void SwitchModeRequestReceived(object sender, PackageData e)
        {
            string data = e.DataName;//.ToLower().Trim();
            if (_intentModes.ContainsKey(data))
            {
                _ = SwitchMode(_intentModes[data]);
            }
            else if (_modePackages.TryGetValue(MysticMode.Idle, out IModePackage idlePackage))
            {
                _currentPackage = idlePackage;
                _currentMode = MysticMode.Idle;
                _logger.LogInfo($"{ _currentMode} starting.");
                _ = _currentPackage.Start(new PackageData(_currentMode, _currentMode.ToString())
                {
                    ModeContext = _modeContext,
                    Parameters = _parameters
                });
            }
        }

        public static async Task<ResponsePacket> SwitchMode(MysticMode newMode)
        {
            try
            {
                await _modeLock.WaitAsync();

                if(_currentPackage != null)
                {
                    _logger.LogInfo($"{ _currentMode} stopping.");
                    await _currentPackage.Stop();
                    await _misty.StopConversationAsync();
                    await _misty.StopKeyPhraseRecognitionAsync();
                    _logger.LogInfo($"{ _currentMode} stopped.");
                }

                if (_modePackages.TryGetValue(newMode, out IModePackage modePackage))
                {
                    _currentPackage = modePackage;
                    _currentMode = newMode;
                    _logger.LogInfo($"{ _currentMode} starting.");
                    _ = _currentPackage.Start(new PackageData(_currentMode, _currentMode.ToString())
                    {
                        ModeContext = _modeContext,
                        Parameters = _parameters
                    });

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
                    _ = _currentPackage.Start(new PackageData(_currentMode, _currentMode.ToString())
                    {
                        ModeContext = _modeContext,
                        Parameters = _parameters
                    });

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

        /*
        private static void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            if(_currentMode != MysticMode.Uninitialized && _currentPackage != null)
            {
                _currentPackage.ProcessEvent(new MysticEvent { RobotInteractionEvent = robotInteractionEvent });
            }
        }*/

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
                    _misty.SetTextDisplaySettings("IpLayer",
                       new TextSettings
                       {
                           Deleted = true
                       },
                       null);
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