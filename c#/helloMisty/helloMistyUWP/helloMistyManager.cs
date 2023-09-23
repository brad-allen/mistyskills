using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FunnyBone;
using MistyRobotics.Common;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Logger;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using SkillTools.AssetTools;
using SkillTools.DataStorage;
using TimeManager;
using Weather.OpenWeather;

namespace helloMistyUWP
{
	public class HelloMistyManager : IDisposable
	{
		private IRobotMessenger _misty;
		private AssetWrapper _assetWrapper;
		private ISDKLogger _logger;
		private string _currentIp;
		private int _ipChecks = 0;
		private Timer _displayedTextTimer;
		private string _password;
		private Random _random = new Random();
		private EnglishTimeManager _timeManager;
		private IDictionary<string, object> _parameters = new Dictionary<string, object>();
		private FunnyBoneAPI _funnyBoneAPI { get; set; } = new FunnyBoneAPI();
		private EnglishWeatherManager _weatherManager;
		private IList<string> _responses1 = new List<string>();
		private bool _followFace;

		private readonly string DisplayedTextLayer = "DisplayedTextLayer";

		public async Task<bool> Start(IRobotMessenger misty, IDictionary<string, object> parameters)
		{
			try
			{
				_misty = misty;
				_parameters = parameters;
				_logger = _misty.SkillLogger;
				_assetWrapper = new AssetWrapper(_misty);
				_timeManager = new EnglishTimeManager(_parameters);

				_ = _misty.SpeakAsync("Hey there! Hold on, I'm starting up!", true, "Starting");

				_assetWrapper.ShowSystemImage(SystemImage.DefaultContent);
				if (_parameters.TryGetValue("OpenWeatherKey", out object code))
				{
					if (!_parameters.TryGetValue("Country", out object countryCode))
					{
						countryCode = "US";
					}

					if (!_parameters.TryGetValue("City", out object cityCode))
					{
						cityCode = "Boulder";
					}
					_weatherManager = new EnglishWeatherManager(_misty, (string)code, null, (string)countryCode, (string)cityCode);
				}
				
				//clean up old events
				_misty.UnregisterAllEvents(null);
				
				await _misty.EnableCameraServiceAsync();
				//await _misty.EnableAvStreamingServiceAsync();
				await _misty.EnableAudioServiceAsync();
				
				//Let blinking eye layer be overwritten so blinks don't rewrite over text
				await _misty.SetImageDisplaySettingsAsync(null, new MistyRobotics.SDK.ImageSettings
				{
					PlaceOnTop = false,
					Visible = true
				});

				//Create Text layer (big eyes will overlap with text)
				await _misty.SetTextDisplaySettingsAsync(DisplayedTextLayer,
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
				
				//Load assets needed for skill from helloMisty/Assets/SkillAssets folder
				//  if you put a lot of files in there, this may take a while if they don't exist on Misty yet				
				await _misty.DisplayTextAsync("Loading assets", DisplayedTextLayer);

				string loadMessage = "Loading any missing skill assets onto robot from helloMisty\\Assets\\SkillAssets";
				Debug.WriteLine(loadMessage);
				_misty.SkillLogger?.Log(loadMessage);
				AssetWrapper assetWrapper = new AssetWrapper(_misty);
				await assetWrapper.LoadAssets(false);
				
				_misty.GetDeviceInformation(DeviceInformationHandler);

				await SetInteractionSettings();

				//Give time to speak in case everything else goes real fast
				await Task.Delay(2000);
				_assetWrapper.PlaySystemSound(SystemSound.Sleepy);
				await _misty.StartKeyPhraseRecognitionVoskAsync(true, 5000, 3000);
				
				//Create and start an action that uses the alpha follow-face functionality
				await _misty.CreateActionAsync("hello-misty.action.follow-face", "FOLLOW-FACE;", true);
				_misty.StartAction("hello-misty.action.follow-face", true, null);
				//Start Face Rec to track and add that data to the event
				_misty.StartFaceRecognition(null);
				_followFace = true;

				_misty.Speak("Okay, ready to go. Use my bumpers or cap touch, or say, Hey Misty, what's the time, weather, or day, or ask me to dance, or tell a joke.", true, "Started", null);
				//Okay, events should handle it from here
				
				return true;
			}
			catch (Exception ex)
			{
				string errorMessage = "Failed to initialize and start Mode Manager.";
				Debug.WriteLine(errorMessage);
				_misty.SkillLogger?.LogError(errorMessage, ex);
				return false;
			}
		}

		public async void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
		{
			if (robotInteractionEvent.Step != RobotInteractionStep.Dialog &&
				   robotInteractionEvent.Step != RobotInteractionStep.BumperPressed &&
				   robotInteractionEvent.Step != RobotInteractionStep.CapTouched)
			{
				return;
			}

			switch(robotInteractionEvent.Step)
			{
				case RobotInteractionStep.BumperPressed:

					_followFace = !_followFace;
					if (_followFace)
					{
						_misty.StartFaceRecognition(null);
						_misty.StartAction("hello-misty.action.follow-face", true, null);
					}
					else
					{
						_misty.StopFaceRecognition(null);
					}
					
					if (robotInteractionEvent.BumperState.FrontLeft == TouchSensorOption.Contacted)
					{
						TimeObject to = _timeManager.GetTimeObject();
						if (to != null && !string.IsNullOrWhiteSpace(to.SpokenTime))
						{
							_misty.Speak("The time is " + to.SpokenTime + ".", true, "time", null);
						}
					}
					else if (robotInteractionEvent.BumperState.FrontRight == TouchSensorOption.Contacted)
					{
						await GetJoke();
					}
					else if (robotInteractionEvent.BumperState.BackLeft == TouchSensorOption.Contacted)
					{
						TimeObject to = _timeManager.GetTimeObject();
						if (to != null && !string.IsNullOrWhiteSpace(to.SpokenTime))
						{
							_misty.Speak("The day is " + to.SpokenDay + ".", true, "day", null);
						}
					}
					else if (robotInteractionEvent.BumperState.BackRight == TouchSensorOption.Contacted)
					{
						await GetWeather();
					}
					return;
				case RobotInteractionStep.CapTouched:
					//TODO put musical notes to play in here?
					if (robotInteractionEvent.CapTouchState.Chin == TouchSensorOption.Contacted)
					{
						_misty.PlayAudio("s_Sleepy.wav", null, null);
						_assetWrapper.ShowSystemImage(SystemImage.SleepingZZZ);
					}
					else if (robotInteractionEvent.CapTouchState.Back == TouchSensorOption.Contacted)
					{
						_misty.PlayAudio("s_PhraseHello.wav", null, null);
						_assetWrapper.ShowSystemImage(SystemImage.Love);
					} 
					else if (robotInteractionEvent.CapTouchState.Front == TouchSensorOption.Contacted)
					{
						_misty.PlayAudio("s_PhraseEvilAhHa.wav", null, null);
						_assetWrapper.ShowSystemImage(SystemImage.Rage4);
					}
					else if (robotInteractionEvent.CapTouchState.Left == TouchSensorOption.Contacted)
					{
						_misty.PlayAudio("s_PhraseOopsy.wav", null, null);
						_assetWrapper.ShowSystemImage(SystemImage.Disgust);
					}
					else if (robotInteractionEvent.CapTouchState.Right == TouchSensorOption.Contacted)
					{
						_misty.PlayAudio("s_PhraseByeBye.wav", null, null);
						_assetWrapper.ShowSystemImage(SystemImage.EcstacyStarryEyed);
					}

					return;
				case RobotInteractionStep.CapReleased:					
					_misty.StopAudio(null);
					return;
			}
			
			if (robotInteractionEvent.Step == RobotInteractionStep.Dialog && (robotInteractionEvent.DialogState?.Step == DialogActionStep.CompletedSpeaking ||
					robotInteractionEvent.DialogState?.Step == DialogActionStep.CompletedPlayingAudio ||
					robotInteractionEvent.DialogState?.Step == DialogActionStep.StoppedSpeaking ||
					robotInteractionEvent.DialogState?.Step == DialogActionStep.StoppedPlayingAudio))
			{
				_ = _misty.StartKeyPhraseRecognitionVoskAsync(true, 5000, 3000);
			}
			else if (robotInteractionEvent.Step == RobotInteractionStep.Dialog && robotInteractionEvent.DialogState?.Step == DialogActionStep.FinalIntent)
			{
				if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
				{
					await _misty.StartKeyPhraseRecognitionVoskAsync(true, 5000, 3000);
					_ = _misty.SpeakAsync($"Sorry, I couldn't hear anything. Please make sure you speak clearly and there is limited noise. My bumpers do things or you can say, Hey Misty, and ask for the time, weather, a joke or something else!", true, "IdlePackageRetry");
					_assetWrapper.ShowSystemImage(SystemImage.Disoriented);
				}
				else
				{
					if (robotInteractionEvent.DialogState.Intent.Equals("time", StringComparison.OrdinalIgnoreCase))
					{
						TimeObject to = _timeManager.GetTimeObject();
						if (to != null && !string.IsNullOrWhiteSpace(to.SpokenTime))
						{
							_misty.Speak("The time is " + to.SpokenTime + ".", true, "time", null);
							_assetWrapper.ShowSystemImage(SystemImage.ContentRight);
						}
					}
					else if (robotInteractionEvent.DialogState.Intent.Equals("day", StringComparison.OrdinalIgnoreCase))
					{
						TimeObject to = _timeManager.GetTimeObject();
						if (to != null && !string.IsNullOrWhiteSpace(to.SpokenTime))
						{
							_misty.Speak("The day is " + to.SpokenDay + ".", true, "day", null);
							_assetWrapper.ShowSystemImage(SystemImage.ContentLeft);
						}
					}
					else if (robotInteractionEvent.DialogState.Intent.Equals("weather", StringComparison.OrdinalIgnoreCase))
					{
						await GetWeather();
						_assetWrapper.ShowSystemImage(SystemImage.Rage3);
					}
					else if (robotInteractionEvent.DialogState.Intent.Equals("dance", StringComparison.OrdinalIgnoreCase))
					{
						_ = _misty.SpeakAsync("You got it, I love to dance!", true, "Dance");
						_assetWrapper.ShowSystemImage(SystemImage.Amazement);
						_misty.ChangeLED(255, 0, 0, null);
						_misty.MoveArms(0, 45, 90, 90, null, AngularUnit.Degrees, null);
						_misty.MoveHead(-10, 0, 0, 80, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(255, 255, 0, null);
						_misty.MoveArms(45, 90, 90, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(255, 255, 255, null);
						_misty.MoveHead(-20, -5, 15, 80, null, AngularUnit.Degrees, null);
						_misty.MoveArms(-20, 5, -15, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(100, 255, 255, null);
						_misty.MoveArms(115, 115, 90, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_assetWrapper.ShowSystemImage(SystemImage.JoyGoofy2);
						_misty.ChangeLED(100, 100, 255, null);
						_misty.MoveHead(-20, 5, -15, 80, null, AngularUnit.Degrees, null);
						_misty.MoveArms(115, 90, 90, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(100, 255, 100, null);
						_misty.MoveArms(90, 45, 90, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(255, 100, 100, null);
						_misty.MoveArms(45, 0, 90, 90, null, AngularUnit.Degrees, null);
						_misty.MoveHead(-10, 0, 0, 80, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(0, 0, 0, null);
						_misty.MoveArms(0, 0, 90, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(500); _assetWrapper.ShowSystemImage(SystemImage.Amazement);
						_misty.ChangeLED(255, 0, 0, null);
						_misty.MoveArms(0, 45, 90, 90, null, AngularUnit.Degrees, null);
						_misty.MoveHead(-10, 0, 0, 80, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(255, 255, 0, null);
						_misty.MoveArms(45, 90, 90, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(255, 255, 255, null);
						_misty.MoveHead(-20, -5, 15, 80, null, AngularUnit.Degrees, null);
						_misty.MoveArms(-20, 5, -15, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(100, 255, 255, null);
						_misty.MoveArms(115, 115, 90, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_assetWrapper.ShowSystemImage(SystemImage.JoyGoofy2);
						_misty.ChangeLED(100, 100, 255, null);
						_misty.MoveHead(-20, 5, -15, 80, null, AngularUnit.Degrees, null);
						_misty.MoveArms(115, 90, 90, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(100, 255, 100, null);
						_misty.MoveArms(90, 45, 90, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(255, 100, 100, null);
						_misty.MoveArms(45, 0, 90, 90, null, AngularUnit.Degrees, null);
						_misty.MoveHead(-10, 0, 0, 80, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(0, 0, 0, null);
						_misty.MoveArms(0, 0, 90, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(500);
						_assetWrapper.ShowSystemImage(SystemImage.Amazement);
						_misty.ChangeLED(255, 0, 0, null);
						_misty.MoveArms(0, 45, 90, 90, null, AngularUnit.Degrees, null);
						_misty.MoveHead(-10, 0, 0, 80, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(255, 255, 0, null);
						_misty.MoveArms(45, 90, 90, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(255, 255, 255, null);
						_misty.MoveHead(-20, -5, 15, 80, null, AngularUnit.Degrees, null);
						_misty.MoveArms(-20, 5, -15, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(100, 255, 255, null);
						_misty.MoveArms(115, 115, 90, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_assetWrapper.ShowSystemImage(SystemImage.JoyGoofy2);
						_misty.ChangeLED(100, 100, 255, null);
						_misty.MoveHead(-20, 5, -15, 80, null, AngularUnit.Degrees, null);
						_misty.MoveArms(115, 90, 90, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(100, 255, 100, null);
						_misty.MoveArms(90, 45, 90, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(255, 100, 100, null);
						_misty.MoveArms(45, 0, 90, 90, null, AngularUnit.Degrees, null);
						_misty.MoveHead(-10, 0, 0, 80, null, AngularUnit.Degrees, null);
						await Task.Delay(300);
						_misty.ChangeLED(0, 0, 0, null);
						_misty.MoveArms(0, 0, 90, 90, null, AngularUnit.Degrees, null);
						await Task.Delay(500);
						await _misty.StartActionAsync("party", false);

						_assetWrapper.ShowSystemImage(SystemImage.Amazement);
					}
					else if (robotInteractionEvent.DialogState.Intent.Equals("joke", StringComparison.OrdinalIgnoreCase))
					{
						await GetJoke();
						_assetWrapper.ShowSystemImage(SystemImage.EcstacyHilarious);
					}
					//else if (robotInteractionEvent.DialogState.Intent.Equals("help", StringComparison.OrdinalIgnoreCase))
					//{
						//TODO?
					//}
					else
					{
						if (!ProcessVoiceCommand(robotInteractionEvent.DialogState.Text))
						{
							//finally a magic 8 ball-like response
							_assetWrapper.ShowSystemImage(SystemImage.Fear);

							_misty.SetTextDisplaySettings(DisplayedTextLayer,
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
								},
								null);
							
							await _misty.DisplayTextAsync(robotInteractionEvent.DialogState.Text, DisplayedTextLayer);
							_responses1.Add("I believe the answer to that is 42. Or I didn't hear the request correctly.");
							_responses1.Add("I'd say that signs point to Yes. As in yes, I am very confused about what you said.");
							_responses1.Add("I'd say that Signs seem to indicate No. As in no, I didn't understand you.");
							_responses1.Add("Can you ask again later when we are not in such a noisy place.");
							_responses1.Add("I am uncertain about what you are saying, but the fog is clearing, ask me again!");
							_responses1.Add("I say, it is certain. What you said. That is the answer. Whatever it was.");
							_responses1.Add("I say, without a doubt I didn't understand you!");
							_responses1.Add("I believe I better not tell you now.");
							_responses1.Add("The future is uncertain, concentrate and speak to me again.");
							_responses1.Add("Not sure what you said, but maybe.");

							int random1 = _random.Next(0, _responses1.Count);
							string response1 = _responses1[random1];
							_ = _misty.SpeakAsync(response1, true, "M8BPhrase");

							_displayedTextTimer = new Timer(DisplayedTextCallback, null, 5000, Timeout.Infinite);
						}
						else
						{
							_ = _misty.StartKeyPhraseRecognitionVoskAsync(true, 5000, 3000);
						}
					}
				}				
			}// items in this area are only processed on a final intent
		}

		/// <summary>
		/// Process the device info data when it comes back
		/// </summary>
		/// <param name="data"></param>
		private async void DeviceInformationHandler(IGetDeviceInformationResponse data)
		{
			if (_isDisposed)
			{
				return;
			}

			_ipChecks++;

			if (data.Status == ResponseStatus.Success)
			{
				_currentIp = data.Data.IPAddress;
				await _misty.DisplayTextAsync(_currentIp, DisplayedTextLayer);

				_ipChecks = 0; //reset for requestable option

				//TODO make configurable display timeout
				_displayedTextTimer = new Timer(DisplayedTextCallback, null, 30000, Timeout.Infinite);
			}
			else
			{
				switch (_ipChecks)
				{
					case 1:
						await _misty.DisplayTextAsync("Looking for I.P. address...", DisplayedTextLayer);
						await Task.Delay(5000); // wait for system to recover
						_misty.GetDeviceInformation(DeviceInformationHandler);
						break;
					case 2:
						await _misty.DisplayTextAsync("Where am I?", DisplayedTextLayer);
						await Task.Delay(5000); // wait for system to recover       
						_misty.GetDeviceInformation(DeviceInformationHandler);
						break;
					case 3:
						await _misty.DisplayTextAsync("One last try!", DisplayedTextLayer);
						await Task.Delay(5000); // wait for system to recover  
						_misty.GetDeviceInformation(DeviceInformationHandler);
						break;
					default:
						await _misty.DisplayTextAsync("Could not obtain I.P.", DisplayedTextLayer);
						_ipChecks = 0; //reset for requestable option
						_displayedTextTimer = new Timer(DisplayedTextCallback, null, 10000, Timeout.Infinite);
						break;
				}
			}
		}

		private async Task GetJoke()
		{
			try
			{
				string _theJoke = "";
				//if (_random.Next(0, 3) < 2)
				//{
					SingleJokeFormat sjf = await _funnyBoneAPI.GetDeveloperJoke();
					_theJoke = sjf.Joke;
				/*}
				else
				{
					//These can be over the top. Uncomment at your own risk.
					//and throw in a chuck norris joke once in a while
					ChuckNorrisJokeFormat cnjf = await _funnyBoneAPI.GetChuckNorrisJoke();
					_theJoke = cnjf.Value;
				}*/

				if (string.IsNullOrWhiteSpace(_theJoke))
				{
					_theJoke = "Couldn't find a joke, sorry. Please ensure I am connected to the internet and try again.";
				}

				await _misty.SpeakAsync(_theJoke, true, "FunnyBoneSaid");

			}
			catch (Exception ex)
			{
				_ = _misty.SpeakAsync("Sorry, I had trouble while performing a joke request.", true, "FunnyBoneSaid");
				_misty.SkillLogger.Log("Failed getting joke.", ex);
			}
		}

		private async Task GetWeather()
		{
			if (_weatherManager != null && _parameters.TryGetValue("OpenWeatherKey", out object code))
			{
				try
				{
					await _misty.SpeakAsync("Getting the weather.", true, "WeatherPhraseStart");

					_ = _misty.SpeakAsync(_weatherManager.GetWeatherString(), true, "WeatherPhraseSaid");

				}
				catch (Exception ex)
				{
					_ = _misty.SpeakAsync("Sorry, I had trouble getting the weather.", true, "WeatherPhraseSaid");
					_misty.SkillLogger.Log("Failed getting weather.", ex);
				}
			}
			else
			{
				_ = _misty.SpeakAsync("Sorry, you need to get an open weather key to use this. Check the readme!", true, "WeatherPhraseSaid");
				_misty.SkillLogger.Log("No open weather key sent in parameters.");
			}
		}

		public void DisplayedTextCallback(object timerData)
		{
			_misty.SetTextDisplaySettings(DisplayedTextLayer,
				new TextSettings
				{
					Visible = false
				},
				null);
		}

		private async Task SetInteractionSettings()
		{
			///Change the VAD beep to much shorter to avoid playing over speech
			//Audio file exists in SkillAssets
			await _misty.SetNotificationSettingsAsync(false, true, true, "short-beep.mp3");

			//Put in dialog mode to get dialog data in RobotInteractionCallback, 
			await _misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "hello-misty.state.do-nothing",
				Speak = "",
				Overwrite = true,
			});
			
			await _misty.CreateConversationAsync("hello-misty.conversation.empty", "hello-misty.state.do-nothing", true, true, null);
			await _misty.StartConversationAsync("hello-misty.conversation.empty");
			//Don't map states
			await _misty.SetContextAsync("hello-misty.context.en.options", false, new List<string>(), true);

			//Register and listen for Robot Interaction Event instead of VoiceRecord event in order to catch other events in one listener as well      
			IList<RobotInteractionValidation> validations = new List<RobotInteractionValidation>();
			//TODO Add filters to keep this less noisy, especially if you watch for face events!
			_misty.RegisterRobotInteractionEvent(RobotInteractionCallback, 0, true, "RobotInteractionEvent" + _random.Next(0, 1000), validations, null);
			//Start the event, using vision data
			await _misty.StartRobotInteractionEventAsync(true);
			//Reset tally light to only turn on when listening to avoid speaking hint confusion
			await _misty.SetTallyLightSettingsAsync(true, false, false);
		}

		private bool ProcessVoiceCommand(string text)
		{
			//Simple ASR text comparison
			text = text.ToLower();
			//TODO Hacky speech check / Allow more and allow degrees and colors...
			if (text.Contains("arm") || text.Contains("are"))
			{
				if (text.Contains("left") || text.Contains("off"))
				{
					_misty.MoveArm(_random.Next(-40, 91), MistyRobotics.Common.Types.RobotArm.Left, 70, null, MistyRobotics.Common.Types.AngularUnit.Degrees, null);
				}
				else if (text.Contains("right") || text.Contains("write"))
				{
					_misty.MoveArm(_random.Next(-40, 91), MistyRobotics.Common.Types.RobotArm.Right, 70, null, MistyRobotics.Common.Types.AngularUnit.Degrees, null);
				}
				else
				{
					_misty.MoveArms(_random.Next(-40, 91), _random.Next(-40, 91), 70, 70, null, MistyRobotics.Common.Types.AngularUnit.Degrees, null);
				}
				return true;
			}

			if (text.Contains("head") || text.Contains("neck"))
			{
				_misty.MoveHead(_random.Next(-40, 20), _random.Next(-30, 31), _random.Next(-70, 71), 70, null, MistyRobotics.Common.Types.AngularUnit.Degrees, null);
				return true;
			}

			if (text.Contains("yellow"))
			{
				_ = _misty.ChangeLEDAsync(255, 255, 0);
			}
			else if (text.Contains("red") || text.Contains("read"))
			{
				_ = _misty.ChangeLEDAsync(255, 0, 0);
			}
			else if (text.Contains("blue") || text.Contains("blew"))
			{
				_ = _misty.ChangeLEDAsync(0, 0, 255);
			}
			else if (text.Contains("green"))
			{
				_ = _misty.ChangeLEDAsync(0, 255, 0);
			}
			else if (text.Contains("off"))
			{
				_ = _misty.ChangeLEDAsync(0, 0, 0);
			}
			else if (text.Contains("white"))
			{
				_ = _misty.ChangeLEDAsync(255, 255, 255);
			}
			else
			{
				return false;
			}
			return true;			
		}
		
		#region IDisposable Support

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					if(_misty != null)
					{
						_ = _misty.StopActionAsync();
						_misty.UnregisterAllEvents(null);

						_misty.SetTextDisplaySettings(DisplayedTextLayer,
						new TextSettings
						{
							Deleted = true
						},
						null);

						_misty.SetNotificationSettings(true, null, null, null, null);
					}
				}

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
