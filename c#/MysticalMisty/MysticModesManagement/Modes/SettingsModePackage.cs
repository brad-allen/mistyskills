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
using MistyRobotics.SDK.Messengers;
using MysticCommon;
using SkillTools.AssetTools;

namespace MysticModesManagement
{
    public class SettingsModePackage : BaseAllModesPackage
    {
        public SettingsModePackage(IRobotMessenger misty, string hackIp) : base(misty) 
        {
            _hackIp = hackIp;
        }
        public override event EventHandler<PackageData> CallSwitchMode;
        public override event EventHandler<PackageData> DataEventPackage;

        private AssetWrapper _assetWrapper;
        private MysticSetting _mysticSetting =  MysticSetting.Voice;
        private const string DefaultVoice = "en-us-x-sfg-local";
        private MysticSettings _currentMysticSettings = new MysticSettings();

        private MysticSettings _displayedMysticSettings = new MysticSettings();
        
        private Dictionary<int, string> _languageCounter = new Dictionary<int, string>();
        private int _currentLanguageCounter;

        private Dictionary<int, string> _voiceCounter = new Dictionary<int, string>();
        private int _currentVoiceCounter;

        private bool _saved = false;
        private bool _initialized = false;
        private readonly SemaphoreSlim _slim = new SemaphoreSlim(1,1);
        private readonly SemaphoreSlim _eventSlim = new SemaphoreSlim(1, 1);
        private string _hackIp;//needed?
        //TODO This should really share the data across sessions instead of resetting it all

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            try
            {
                await _slim.WaitAsync();
                await base.Start(packageData);
                _assetWrapper = new AssetWrapper(Misty);

                await Misty.SetDisplaySettingsAsync(true);

                await Misty.SetTextDisplaySettingsAsync(null,
                        new TextSettings
                        {
                            HorizontalAlignment = ImageHorizontalAlignment.Center,
                            VerticalAlignment = ImageVerticalAlignment.Bottom,
                            Weight = 20,
                            Visible = true,
                            PlaceOnTop = true,
                            Red = 0,
                            Green = 255,
                            Blue = 255,
                            Style = ImageStyle.Normal,
                            Size = 15
                        }
                    );

                await Misty.ConfigureDialogAsync(new ConfigureDialogParameters
                {
                    AsrService = SpeechRecognitionEngine.Vosk,
                    NlpEngine = NaturalLanguageProcessingEngine.MaxEntropy,
                    TtsService = TextToSpeechEngine.Misty
                });

                bool hazSettings = _currentMysticSettings != null && _saved && _currentMysticSettings.VoiceSpeed > 0;
                if (hazSettings)
                {
                    await Misty.UpdateDialogSettingsAsync(new UpdateDialogSettingsParameters
                    {
                        Pitch = _currentMysticSettings.VoicePitch,
                        Voice = _currentMysticSettings.Voice ?? DefaultVoice,
                        SpeechRate = _currentMysticSettings.VoiceSpeed,
                        UserLanguage = _currentMysticSettings.Language ?? "en",
                        MaxSpeechLengthMs = 10000,
                        SilenceTimeoutMs = 5000,
                        MinimumConfidence = 0.6
                    });
                }
                else
                {
                    await Misty.UpdateDialogSettingsAsync(new UpdateDialogSettingsParameters
                    {
                        Pitch = 1.0,
                        SpeechRate = 1.0,
                        Voice = DefaultVoice,
                        UserLanguage = "en",
                        MaxSpeechLengthMs = 10000,
                        SilenceTimeoutMs = 5000,
                        MinimumConfidence = 0.6
                    });
                }

                if(!_initialized)
                {
                    int i = 0;
                    _languageCounter.Clear();
                    foreach (string item in MysticLanguages.AllLanguages.Keys)
                    {
                        _languageCounter.Add(i, item);

                        if (!hazSettings && item.Equals("en", StringComparison.OrdinalIgnoreCase) ||
                            hazSettings && item.Equals(_currentMysticSettings.Language, StringComparison.OrdinalIgnoreCase))
                        {
                            _currentLanguageCounter = i;
                        }
                        i++;
                    }

                    int v = 0;
                    _voiceCounter.Clear();
                    foreach (string item in MysticVoices.AllVoices)
                    {
                        _voiceCounter.Add(v, item);

                        if (!hazSettings && item.Equals(DefaultVoice, StringComparison.OrdinalIgnoreCase) ||
                            hazSettings && item.Equals(_currentMysticSettings.Voice, StringComparison.OrdinalIgnoreCase))
                        {
                            _currentVoiceCounter = v;
                        }
                        v++;
                    }
                }
                
                _initialized = true;
                await DisplaySettings(false);

                //await Misty.StopKeyPhraseRecognitionAsync()
                await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                _ = Misty.SpeakAsync($"Hi! Change my settings through my touch sensors! My Front bumpers scroll. The Top of my head saves your setting. And my Chin switches settings. You can also Say, Hey Misty, and tell me to switch modes!", _currentMysticSettings.Language, _currentMysticSettings.VoicePitch, _currentMysticSettings.VoiceSpeed, _currentMysticSettings.Voice, true, "VoiceCommand");
              

                return new ResponsePacket { Success = true };
            }
            catch (Exception ex)
            {
                Misty.SkillLogger.Log("Failed to start settings mode.", ex);
                return new ResponsePacket { Success = false };
            }
            finally
            {
                _slim.Release();
            }
        }

        private async Task DisplaySettings(bool saved)
        {
            string settingString;
            if (saved)
            {
                settingString = $"-- Saved Settings --{Environment.NewLine}";
            }
            else
            {
                settingString = $"-- Settings --{Environment.NewLine}";
            }
            switch (_mysticSetting)
            {
                case MysticSetting.Language:
                    settingString += $"Language{Environment.NewLine}";
                    settingString += $"{_displayedMysticSettings.Language}";
                    break;
                case MysticSetting.Verbosity: //TODO, doesn't affect skill yet
                    settingString += $"Verbosity{Environment.NewLine}";
                    settingString += $"{_displayedMysticSettings.SpeechVerbosity}";
                    break;
                case MysticSetting.Mood: //TODO, doesn't affect skill yet
                    settingString += $"Mood{Environment.NewLine}";
                    settingString += $"{_displayedMysticSettings.Mood}";
                    break;
                case MysticSetting.Voice:
                    settingString += $"Voice{Environment.NewLine}";
                    settingString += $"{_displayedMysticSettings.Voice}";
                    break;
                case MysticSetting.VoicePitch:
                    settingString += $"Voice Pitch{Environment.NewLine}";
                    settingString += $"{_displayedMysticSettings.VoicePitch}";
                    break;
                case MysticSetting.VoiceSpeed:
                    settingString += $"Voice Speed{Environment.NewLine}";
                    settingString += $"{_displayedMysticSettings.VoiceSpeed}";
                    break;
                default:
                    settingString += $"Unknown or empty Setting";
                    break;
            }

            await Misty.DisplayTextAsync(settingString, null);
        }

        public override async Task<ResponsePacket> Stop()
        {
            await base.Stop();
            await Misty.SetTextDisplaySettingsAsync(null,
                new TextSettings
                {
                    Deleted = true
                }
            );

            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("settings");
            samples.Add("setting");
            samples.Add("sending");
            samples.Add("set");
            samples.Add("soybeans");
            samples.Add("soy beans");
            samples.Add("configuration");
            samples.Add("config");
            samples.Add("change setting");
            samples.Add("update setting");

            intent = new Intent
            {
                Name = "Settings",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        public override async void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            if(robotInteractionEvent.Step != RobotInteractionStep.BumperPressed &&
                robotInteractionEvent.Step != RobotInteractionStep.CapTouched
                && robotInteractionEvent.DialogState?.Step != MistyRobotics.Common.Types.DialogActionStep.FinalIntent)
            { 
                return;
            }

            await _eventSlim.WaitAsync();

            bool saved = false;
            try
            {
                if (robotInteractionEvent.Step == RobotInteractionStep.BumperPressed)
                {
                    if (robotInteractionEvent.BumperState.BackLeft == TouchSensorOption.Contacted)
                    {
                        //unsure yet - this is Misty's back left, not as viewed from the front
                        _assetWrapper.PlaySystemSound(SystemSound.Joy);
                        _assetWrapper.ShowSystemImage(SystemImage.Joy);
                    }
                    else if (robotInteractionEvent.BumperState.BackRight == TouchSensorOption.Contacted)
                    {
                        //unsure yet
                        _assetWrapper.PlaySystemSound(SystemSound.Love);
                        _assetWrapper.ShowSystemImage(SystemImage.Love);                        
                    }
                    else if (robotInteractionEvent.BumperState.FrontRight == TouchSensorOption.Contacted)
                    {
                        //whatever setting you are on - make the number go down and display
                        switch (_mysticSetting)
                        {
                            case MysticSetting.Verbosity:
                                switch(_displayedMysticSettings.SpeechVerbosity)
                                {
                                    case SpeechVerbosity.Silent:
                                        //do nothing
                                        break;
                                    case SpeechVerbosity.Shy:
                                        _displayedMysticSettings.SpeechVerbosity = SpeechVerbosity.Silent;
                                        break;
                                    case SpeechVerbosity.Average:
                                        _displayedMysticSettings.SpeechVerbosity = SpeechVerbosity.Shy;
                                        break;
                                    case SpeechVerbosity.Talkative:
                                        _displayedMysticSettings.SpeechVerbosity = SpeechVerbosity.Average;
                                        break;
                                }
                                break;

                            case MysticSetting.Mood:
                                switch (_displayedMysticSettings.Mood)
                                {
                                    case MysticMood.Angry:
                                        //do nothing
                                        break;
                                    case MysticMood.Sad:
                                        _displayedMysticSettings.Mood = MysticMood.Angry;
                                        break;


                                    case MysticMood.Sarcastic:
                                        _displayedMysticSettings.Mood = MysticMood.Sad;
                                        break;

                                    case MysticMood.Professional:
                                        _displayedMysticSettings.Mood = MysticMood.Sarcastic;
                                        break;

                                    case MysticMood.Encouraging:
                                        _displayedMysticSettings.Mood = MysticMood.Professional;
                                        break;

                                    case MysticMood.Happy:
                                        _displayedMysticSettings.Mood = MysticMood.Encouraging;
                                        break;

                                    case MysticMood.Funny:
                                        _displayedMysticSettings.Mood = MysticMood.Happy;
                                        break;

                                    case MysticMood.Love:
                                        _displayedMysticSettings.Mood = MysticMood.Funny;
                                        break;
                                }
                                break;

                            case MysticSetting.VoicePitch:
                                if(_displayedMysticSettings.VoicePitch - 0.1 > -5 && _displayedMysticSettings.VoicePitch - 0.1 < 5)
                                {
                                    _displayedMysticSettings.VoicePitch = _displayedMysticSettings.VoicePitch - 0.1f;
                                }
                                break;
                            case MysticSetting.VoiceSpeed:
                                if (_displayedMysticSettings.VoiceSpeed - 0.1 > -5 && _displayedMysticSettings.VoiceSpeed - 0.1 < 5)
                                {
                                    _displayedMysticSettings.VoiceSpeed = _displayedMysticSettings.VoiceSpeed - 0.1f;
                                }
                                break;
                            case MysticSetting.Voice:
                                if (_currentVoiceCounter == 0)
                                {
                                    //nothin
                                }
                                else
                                {
                                    _voiceCounter.TryGetValue(--_currentVoiceCounter, out string newVoice);
                                    _displayedMysticSettings.Voice = newVoice;
                                }
                                break;

                            case MysticSetting.Language:
                                if(_currentLanguageCounter == 0)
                                {
                                    //nothin
                                }
                                else
                                {
                                    _languageCounter.TryGetValue(--_currentLanguageCounter, out string newLanguage);
                                    _displayedMysticSettings.Language = newLanguage;
                                }
                                break;
                        }

                    }
                    else if (robotInteractionEvent.BumperState.FrontLeft == TouchSensorOption.Contacted)
                    {
                        //whatever setting you are on - make the number go up and display
                        //whatever setting you are on - make the number go down and display
                        switch (_mysticSetting)
                        {
                            case MysticSetting.Verbosity:
                                switch (_displayedMysticSettings.SpeechVerbosity)
                                {
                                    case SpeechVerbosity.Silent:
                                        _displayedMysticSettings.SpeechVerbosity = SpeechVerbosity.Shy;
                                        break;
                                    case SpeechVerbosity.Shy:
                                        _displayedMysticSettings.SpeechVerbosity = SpeechVerbosity.Average;
                                        break;
                                    case SpeechVerbosity.Average:
                                        _displayedMysticSettings.SpeechVerbosity = SpeechVerbosity.Talkative;
                                        break;
                                    case SpeechVerbosity.Talkative:
                                        //nothing
                                        break;
                                }
                                break;

                            case MysticSetting.Mood:
                                switch (_displayedMysticSettings.Mood)
                                {
                                    case MysticMood.Angry:
                                        _displayedMysticSettings.Mood = MysticMood.Sad;
                                        break;
                                    case MysticMood.Sad:
                                        _displayedMysticSettings.Mood = MysticMood.Sarcastic;
                                        break;


                                    case MysticMood.Sarcastic:
                                        _displayedMysticSettings.Mood = MysticMood.Professional;
                                        break;

                                    case MysticMood.Professional:
                                        _displayedMysticSettings.Mood = MysticMood.Encouraging;
                                        break;

                                    case MysticMood.Encouraging:
                                        _displayedMysticSettings.Mood = MysticMood.Happy;
                                        break;

                                    case MysticMood.Happy:
                                        _displayedMysticSettings.Mood = MysticMood.Funny;
                                        break;

                                    case MysticMood.Funny:
                                        _displayedMysticSettings.Mood = MysticMood.Love;
                                        break;

                                    case MysticMood.Love:
                                        //do nothing
                                        break;
                                }
                                break;

                            case MysticSetting.VoicePitch:
                                if (_displayedMysticSettings.VoicePitch + 0.1 > -5 && _displayedMysticSettings.VoicePitch + 0.1 < 5)
                                {
                                    _displayedMysticSettings.VoicePitch = _displayedMysticSettings.VoicePitch + 0.1f;
                                }
                                break;
                            case MysticSetting.VoiceSpeed:
                                if (_displayedMysticSettings.VoiceSpeed + 0.1 > -5 && _displayedMysticSettings.VoiceSpeed + 0.1 < 5)
                                {
                                    _displayedMysticSettings.VoiceSpeed = _displayedMysticSettings.VoiceSpeed + 0.1f;
                                }
                                break;
                            case MysticSetting.Voice:
                                if (_currentVoiceCounter >= MysticVoices.AllVoices.Count)
                                {
                                    //nothin
                                }
                                else
                                {
                                    _voiceCounter.TryGetValue(++_currentVoiceCounter, out string newVoice);
                                    _displayedMysticSettings.Voice = newVoice;
                                }
                                break;

                            case MysticSetting.Language:
                                if (_currentLanguageCounter >= MysticLanguages.AllLanguages.Count)
                                {
                                    //nothin
                                }
                                else
                                {
                                    _languageCounter.TryGetValue(++_currentLanguageCounter, out string newLanguage);
                                    _displayedMysticSettings.Language = newLanguage;
                                }
                                break;
                        }

                    }
                }
                if (robotInteractionEvent.Step == RobotInteractionStep.CapTouched)
                {
                    if (robotInteractionEvent.CapTouchState.Scruff == TouchSensorOption.Contacted)
                    {
                        //go back to start mode
                        PackageData pd = new PackageData(MysticMode.Settings, "start")
                        {
                            ModeContext = PackageData.ModeContext,
                            Parameters = PackageData.Parameters
                        };

                        CallSwitchMode?.Invoke(this, pd);
                        return;

                    }
                    else if (robotInteractionEvent.CapTouchState.Chin == TouchSensorOption.Contacted)
                    {
                        //switch setting
                        switch(_mysticSetting)
                        {
                            case MysticSetting.Language:
                                _mysticSetting = MysticSetting.Mood;
                                break;
                            case MysticSetting.Mood:
                                _mysticSetting = MysticSetting.Verbosity;
                                break;
                            case MysticSetting.Verbosity:
                                _mysticSetting = MysticSetting.Voice;
                                break;
                            case MysticSetting.Voice:
                                _mysticSetting = MysticSetting.VoicePitch;
                                break;
                            case MysticSetting.VoicePitch:
                                _mysticSetting = MysticSetting.VoiceSpeed;
                                break;
                            case MysticSetting.VoiceSpeed:
                                _mysticSetting = MysticSetting.Language;
                                break;
                        }

                    }
                    else if (robotInteractionEvent.CapTouchState.Front == TouchSensorOption.Contacted)
                    {
                        await UpdateSettings();
                        saved = true;
                    }
                    else if (robotInteractionEvent.CapTouchState.Back == TouchSensorOption.Contacted)
                    {
                        await UpdateSettings();
                        saved = true;
                    }
                    else if (robotInteractionEvent.CapTouchState.Left == TouchSensorOption.Contacted)
                    {
                        await UpdateSettings();
                        saved = true;
                    }
                    else if (robotInteractionEvent.CapTouchState.Right == TouchSensorOption.Contacted)
                    {
                        await UpdateSettings();
                        saved = true;
                    }
                }

                if (robotInteractionEvent.DialogState?.Step == DialogActionStep.FinalIntent)
                {
                    if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
                    {
                        _ = Misty.SpeakAsync($"I didn't hear anything. Say, Hey Misty, and speak to me or use my touch sensors!", _currentMysticSettings.Language, _currentMysticSettings.VoicePitch, _currentMysticSettings.VoiceSpeed, _currentMysticSettings.Voice, true, "WanderPhraseError");
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
                    else
                    {
                        _ = Misty.SpeakAsync($"Sorry! I didn't understand that request. Did you say {robotInteractionEvent.DialogState.Text}?", _currentMysticSettings.Language, _currentMysticSettings.VoicePitch, _currentMysticSettings.VoiceSpeed, _currentMysticSettings.Voice, true, "SsettingsPackageRetry2");
                        
                    }
                    return;
                }
            }
            catch
            {

            }
            finally
            {
                _eventSlim.Release();
                //await Misty.StopKeyPhraseRecognitionAsync()
                await Misty.StartKeyPhraseRecognitionVoskAsync(true, 10000, 5000);
                _ = DisplaySettings(saved);
            }
        }

        private async Task UpdateSettings()
        {
            _currentMysticSettings.Mood = _displayedMysticSettings.Mood;
            _currentMysticSettings.Language = _displayedMysticSettings.Language;
            _currentMysticSettings.SpeechVerbosity= _displayedMysticSettings.SpeechVerbosity;
            _currentMysticSettings.Voice = _displayedMysticSettings.Voice;
            _currentMysticSettings.VoicePitch = _displayedMysticSettings.VoicePitch;
            _currentMysticSettings.VoiceSpeed = _displayedMysticSettings.VoiceSpeed;

            await Misty.UpdateDialogSettingsAsync(new UpdateDialogSettingsParameters
            {
                Pitch = _currentMysticSettings.VoicePitch,
                Voice = _currentMysticSettings.Voice,
                SpeechRate = _currentMysticSettings.VoiceSpeed,
                UserLanguage = _currentMysticSettings.Language,
                MaxSpeechLengthMs = 10000,
                SilenceTimeoutMs = 5000,
                MinimumConfidence = 0.6
            });

            //And update the general voice parameters
            await Misty.SpeakAsync("Wow! This is my new voice!", _currentMysticSettings.Language, _currentMysticSettings.VoicePitch, _currentMysticSettings.VoiceSpeed, _currentMysticSettings.Voice, true, "id");

            //send this up to the parent to share across systems  //TODO revisit 

            PackageData.Parameters.Remove("mystic-settings");  
            PackageData.Parameters.Add("mystic-settings", _currentMysticSettings);

            PackageData pd = new PackageData(MysticMode.Settings, "voice-settings")
            {
                ModeContext = PackageData.ModeContext,
                Parameters = PackageData.Parameters
            };
            DataEventPackage?.Invoke(this, pd);
            _saved = true;            
        }
    }
}