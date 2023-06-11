using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConversationHelpers;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Logger;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using MysticCommon;
using SkillTools.AssetTools;
using SkillTools.DataStorage;

namespace MysticModesManagement
{
    public class ModeManager : IDisposable
    {
        private static IRobotMessenger _misty;
        private static ISDKLogger _logger;
        private static IDictionary<MysticMode, IModePackage> _modePackages = new Dictionary<MysticMode, IModePackage>();
        private static IDictionary<string, object> _parameters = new Dictionary<string, object>();
        private static MysticMode _currentMode = MysticMode.Uninitialized;
        private static IModePackage _currentPackage;
        private static ISkillStorage _simpleStorage;
        private static IDictionary<string, object> _skillData = new Dictionary<string, object>();
        private static IDictionary<string, MysticMode> _intentModes = new Dictionary<string, MysticMode>(StringComparer.OrdinalIgnoreCase);

        private readonly static SemaphoreSlim _modeLock = new SemaphoreSlim(1, 1);
        private readonly static SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private readonly static SemaphoreSlim _dataLock = new SemaphoreSlim(1, 1);
        private static Timer _ipDisplayTimer;
        private static int _ipChecks = 0;
        private static string _password;
        private static Context _modeContext = new Context();
        private static ConversationHelper _conversationHelper;
        private static string _currentIp;
        private static string _hackIp;
        private static AssetWrapper _assetWrapper;
        private static ModeManager _modeManager;

        private ModeManager(IDictionary<string, object> parameters, IRobotMessenger misty, AssetWrapper assetWrapper)
        {
            _assetWrapper = assetWrapper;
            _parameters = parameters;
            _misty = misty;
            _logger = _misty.SkillLogger;
        }

        public static async Task<ModeManager> Start(IDictionary<string, object> parameters, IRobotMessenger misty, AssetWrapper assetWrapper)
        {
            try
            {
                await _initLock.WaitAsync();
                if (_modeManager == null)
                {
                    _modeManager = new ModeManager(parameters, misty, assetWrapper);
                }

                await Cleanup();

                await _misty.EnableCameraServiceAsync();
                //await _misty.EnableAvStreamingServiceAsync();
                await _misty.EnableAudioServiceAsync();

                if (_parameters.TryGetValue("HackIp", out object hackIp))
                {
                    try
                    {
                        _hackIp = (string)hackIp;
                    }
                    catch (Exception ex)
                    {
                        _logger.Log("Couldn't parse hack ip", ex);
                    }
                }
                else
                {
                    //If you see this, I checked it in :|
                    _logger.Log("Hack ip not sent in - defaulting to hardcoded IP.");

                    //TEMP!!!
                    //169.254.143.186 - 'Groovy'
                    //169.254.133.55 - 'Zoinks'
                    _hackIp = "169.254.143.186";                    
                }

                //Check for wifi and handle in callback method instead of awaiting
                _misty.GetDeviceInformation(DeviceInformationHandler);

                //load helper to help build some conversations with common mappings
                _conversationHelper = new ConversationHelper(_misty, _hackIp);

                await PrepareDialogSystem();

                //TODO Allow separate skills to act as packages for dynamic mode loading instead of hardcoding them in
                //  and use DataEventPackage parameters for sharing global data across skills
                StartModePackage smp = new StartModePackage(_misty);
                UnimplementedModePackage unp = new UnimplementedModePackage(_misty);

                //TODO Revisit the TryGetIntentTrigger() methods to create better intent phrases than these examples

                _modePackages.Add(MysticMode.Start, smp);
                _modePackages.Add(MysticMode.Idle, new IdleModePackage(_misty, _assetWrapper));
                _modePackages.Add(MysticMode.Repeat, new RepeatModePackage(_misty));
                _modePackages.Add(MysticMode.Backward, new BackwardModePackage(_misty));
                _modePackages.Add(MysticMode.RubberDuckey, new RubberDuckeyModePackage(_misty));
                _modePackages.Add(MysticMode.MagicEightBall, new MagicEightBallModePackage(_misty));
                _modePackages.Add(MysticMode.Wander, new WanderModePackage(_misty));
                _modePackages.Add(MysticMode.Weather, new WeatherModePackage(_misty));
                _modePackages.Add(MysticMode.VoiceCommand, new VoiceCommandModePackage(_misty));
                _modePackages.Add(MysticMode.Settings, new SettingsModePackage(_misty, _hackIp));
                _modePackages.Add(MysticMode.FollowPerson, new FollowPersonModePackage(_misty));
                _modePackages.Add(MysticMode.Conversation, new ConversationModePackage(_misty, _conversationHelper, _assetWrapper));
                _modePackages.Add(MysticMode.FunnyBone, new FunnyBoneModePackage(_misty));
                _modePackages.Add(MysticMode.Reverse, new WordReverseModePackage(_misty));
                //_modePackages.Add(MysticMode.TrackObject, new TrackObjectModePackage(_misty));

                await _conversationHelper.CreateYesNoContext();

                IList<Intent> intents = new List<Intent>();
                foreach (KeyValuePair<MysticMode, IModePackage> package in _modePackages)
                {
                    if (package.Value.TryGetIntentTrigger(out Intent intent))
                    {
                        intents.Remove(intent);
                        intents.Add(intent);

                        _intentModes.Add(intent.Name.ToLower(), package.Key);
                    }

                    package.Value.CallSwitchMode += SwitchModeRequestReceived;
                    //TODO Manage other common data being sent up and dd to parameters passed around
                    package.Value.DataEventPackage += HandleDataPackage;
                }

                _modeContext = new Context
                {
                    Name = "all-modes",
                    Intents = intents,
                    Editable = true
                };
                
                await _misty.DeleteNLPContextAsync("all-modes");

                //Argh, bug in SDK train? it is Alpha after all
                //TODO Get rid of hack
                var testTrain = await _misty.TrainNLPEngineAsync(_modeContext, true, true);
                var testHack = await SkillWorkarounds.TrainNLPHack(_modeContext, _hackIp, true, true);

                if (testHack.HttpCode != 200)
                {
                    _ = _misty.SpeakAsync($"Could not reach Misty at I.P. {_hackIp}. Conversation mode may not work!", true, "hack-msg");
                    await Task.Delay(10000);
                }

                //Example using simple data store code
                //Load up simple storage DBs based upon if password passed in
                if (_parameters.TryGetValue("password", out object password))
                {
                    try
                    {
                        _password = (string)password;
                        _simpleStorage = await EncryptedStorage.GetDatabase("MysticMistyDataEnc", _password);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log("Couldn't obtain encrypted database. Using unencrypted data store.", ex);
                        _simpleStorage = SkillStorage.GetDatabase("MysticMistyData");
                    }
                }
                else
                {
                    _password = "no-password";
                    _logger.Log("No password in startup parameters.");
                    _simpleStorage = SkillStorage.GetDatabase("MysticMistyData");
                }

                //TODO Store long term items here                 
                if (_simpleStorage != null)
                {
                    int startupCount = 0;
                    //Load the data from on bot simple storage
                    _skillData = await _simpleStorage.LoadDataAsync();

                    //See if field already exists
                    if (_skillData.TryGetValue("StartupCount", out object startupCountObject))
                    {
                        //if so, convert to proper type to increment
                        startupCount = Convert.ToInt32(startupCountObject);
                        startupCount++;
                    }
                    else
                    {
                        //if not, first time!
                        startupCount = 1;
                    }

                    //Update count in dictionary
                    _skillData.Remove("StartupCount");
                    _skillData.Add("StartupCount", startupCount);

                    //Save back to file
                    await _simpleStorage.SaveDataAsync(_skillData);
                }

                _logger.LogVerbose("Mode Manager initialized.");
                await SwitchMode(MysticMode.Start);
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

        private static async Task PrepareDialogSystem()
        {
            ///Change the VAD beep to much shorter to avoid playing over speech
            //Audio file exists in SkillAssets
            await _misty.SetNotificationSettingsAsync(false, true, true, "short-beep.mp3");            
            
            //TODO Test
            //_misty.RegisterVoiceRecordEvent(0, true, "StartVREvent", null); //shouldn't be needed, but might be still - oops
            //_misty.RegisterAudioPlayCompleteEvent(0, true, "AudioPlayCompleteEvent", null); //shouldn't be needed, but might be still - oops

            //Register and listen for Robot Interaction Event instead of VoiceRecord event in order to catch other events in one listener as well      
            IList<RobotInteractionValidation> validations = new List<RobotInteractionValidation>();
            //TODO Add filters to keep this less noisy
            //Issue with things not unregistering correctly in skill? Try unique names
            Random rand = new Random();
            _misty.RegisterRobotInteractionEvent(RobotInteractionCallback, 0, true, "RobotInteractionEvent"+ rand.Next(0, 1000000), validations, null);
            //Start the event, using vision data
            await _misty.StartRobotInteractionEventAsync(true);
            //Reset tally light to only turn on when listening to avoid speaking hint confusion
            await _misty.SetTallyLightSettingsAsync(true, false, false);
        }

protected static void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent) 
{
if (robotInteractionEvent.Step == RobotInteractionStep.CapTouched ||
      robotInteractionEvent.Step == RobotInteractionStep.BumperPressed)
{
    if (robotInteractionEvent.CapTouchState.Scruff == TouchSensorOption.Contacted &&
        robotInteractionEvent.BumperState.BackLeft == TouchSensorOption.Contacted &&
        robotInteractionEvent.BumperState.BackRight == TouchSensorOption.Contacted)
    {
        _ =  _misty.SpeakAsync("Magic cancel sequence activated, cancelling skill!", true, "end-it");
        _misty.CancelRunningSkill("72c5785f-6e80-44ec-bbfb-681812bba7fe", null);
        return;
    }
}

if (_currentPackage != null && !_isDisposed)
{
    //Check for cancellation magic               
    //Pass it on to the currently running package
    _currentPackage.RobotInteractionCallback(robotInteractionEvent);
}
}

/// <summary>
/// Process the device info data when it comes back
/// </summary>
/// <param name="data"></param>
private static async void DeviceInformationHandler(IGetDeviceInformationResponse data)
{
if(_isDisposed)
{
    return;
}

_ipChecks++;

//Let eye layer be overwritten so blinks don't rewrite over text
await _misty.SetImageDisplaySettingsAsync(null, new MistyRobotics.SDK.ImageSettings
{
    PlaceOnTop = false,
    Visible = true
});

//Create Text layer (some big eyes will overlap a little)
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
    _currentIp = data.Data.IPAddress;
    await _misty.DisplayTextAsync(_currentIp, "IpLayer");

    _ipChecks = 0; //reset for requestable option

    //Display for X secs / make config and requestable
    _ipDisplayTimer = new Timer(IpDisplayedCallback, null, 15000, Timeout.Infinite);
}
else
{
    switch (_ipChecks)
    {
        case 1:
            await _misty.DisplayTextAsync("Looking for I.P. address...", "IpLayer");
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
            await _misty.DisplayTextAsync("Could not obtain I.P.", "IpLayer");
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

public static async void HandleDataPackage(object sender, PackageData e)
{
await _dataLock.WaitAsync();
try
{
    string dataname = e.DataName.ToLower().Trim();
    IDictionary<string, object> data = e.Parameters;
    _misty.SkillLogger.Log($"ModeManager received '{dataname}' message from running mode.");

    //Adding to parameters passed across modes
    _parameters.Remove(dataname);
    _parameters.Add(dataname, data);

    //Save to storage example
    _skillData = await _simpleStorage.LoadDataAsync();
    _skillData.Remove(dataname);
    _skillData.Add(dataname, data);
    await _simpleStorage.SaveDataAsync(_skillData);
}
catch (Exception ex)
{
    _logger.LogError("Failed to process mode data package.", ex);
}
finally
{
    _dataLock.Release();
}
}

public static async void SwitchModeRequestReceived(object sender, PackageData e)
{
string data = e.DataName.ToLower().Trim();
if (_intentModes.ContainsKey(data))
{
    await SwitchMode(_intentModes[data]);
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

    //await _misty.StopKeyPhraseRecognitionAsync();
   // await _misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 4000);
}
}

public static async Task<ResponsePacket> SwitchMode(MysticMode newMode)
{
try
{
    await _modeLock.WaitAsync();
    bool loading = false;
    if(_currentPackage != null)
    {
        _logger.LogInfo($"{ _currentMode} stopping.");
        await _currentPackage.Stop();

        _logger.LogInfo($"{ _currentMode} stopped.");
    }
    else
    {
        loading = true;
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

        if(!loading)
        {
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

            await _misty.DisplayTextAsync($"New Mode:{_currentMode.ToString()}", "IpLayer");
            _ipDisplayTimer = new Timer(IpDisplayedCallback, null, 5000, Timeout.Infinite);
        }


        return new ResponsePacket { Success = true };
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

private static async Task Cleanup()
{
if (_misty != null)
{
    _misty.UnregisterAllEvents(null);
    await _misty.StopConversationAsync();
    await _misty.StopKeyPhraseRecognitionAsync();
    await _misty.StopRobotInteractionEventAsync();
    await _misty.SetDisplaySettingsAsync(true);
}
}

#region IDisposable Support

private static bool _isDisposed = false;

private  void Dispose(bool disposing)
{
if (!_isDisposed)
{
    if (disposing)
    {
        _ = Cleanup();
        _ = _misty.SetTallyLightSettingsAsync(true, true, true);
        _ = _misty.SetNotificationSettingsAsync(true, null, null, null);
        _currentMode = MysticMode.Uninitialized;
        _modePackages.Clear();
        _ipDisplayTimer?.Dispose();
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