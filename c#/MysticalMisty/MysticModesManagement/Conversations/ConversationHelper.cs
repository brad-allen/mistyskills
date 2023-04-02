using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Messengers;
using SkillTools.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MysticModesManagement.Conversations
{
    public class ConversationHelper
    {
		private IRobotMessenger _misty;

		public ConversationHelper(IRobotMessenger misty)
        {
			_misty = misty;
		}

		public async Task MapAudioPassThroughStates(string conversation, string currentState, string nextState)
		{
			await _misty.MapStateAsync(new MapStateParameters
			{
				Conversation = conversation,
				State = currentState,
				Trigger = StateTrigger.AudioCompleted,
				TriggerFilter = "",
				NextState = nextState,
				Overwrite = true
			});
		}

		public async Task MapYesNoStates(string conversation, string currentState, string yesState, string noState, string unknownState = null, string timeoutState = null, int timeoutMs = 0, bool useReentryOnFailure = false)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(yesState) && !string.IsNullOrWhiteSpace(noState))
				{
					await _misty.MapStateAsync(new MapStateParameters
					{
						Conversation = conversation,
						State = currentState,
						Trigger = StateTrigger.SpeechHeard,
						TriggerFilter = "yes",
						NextState = yesState,
						Overwrite = true,
					});

					await _misty.MapStateAsync(new MapStateParameters
					{
						Conversation = conversation,
						State = currentState,
						Trigger = StateTrigger.BumperPressed,
						TouchSensorFilter = TouchSensorFilter.Bumper_FrontRight,
						NextState = yesState,
						Overwrite = true
					});

					await _misty.MapStateAsync(new MapStateParameters
					{
						Conversation = conversation,
						State = currentState,
						Trigger = StateTrigger.SpeechHeard,
						TriggerFilter = "no",
						NextState = noState,
						Overwrite = true
					});

					await _misty.MapStateAsync(new MapStateParameters
					{
						Conversation = conversation,
						State = currentState,
						Trigger = StateTrigger.BumperPressed,
						TouchSensorFilter = TouchSensorFilter.Bumper_FrontLeft,
						NextState = noState,
						Overwrite = true
					});

					if (!string.IsNullOrWhiteSpace(unknownState))
					{
						await _misty.MapStateAsync(new MapStateParameters
						{
							Conversation = conversation,
							State = currentState,
							Trigger = StateTrigger.SpeechHeard,
							TriggerFilter = "unknown",
							NextState = unknownState,
							ReEntry = useReentryOnFailure,
							Overwrite = true,
						});
					}
				}

				if (!string.IsNullOrWhiteSpace(timeoutState) && timeoutMs > 0)
				{
					await _misty.MapStateAsync(new MapStateParameters
					{
						Conversation = conversation,
						State = currentState,
						Trigger = StateTrigger.Timer,
						TriggerFilter = timeoutMs.ToString(),
						NextState = timeoutState,
						ReEntry = useReentryOnFailure,
						Overwrite = true
					});
				}
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogError("Failed to map person finding states.", ex);
			}
		}

		public async Task MapPersonFindingStates(string conversation, string currentState, string faceSeenState, string touchState = null, string timeoutState = null, int timeoutMs = 0)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(faceSeenState))
				{
					await _misty.MapStateAsync(new MapStateParameters
					{
						Conversation = conversation,
						State = currentState,
						Trigger = StateTrigger.NewFaceSeen,
						TriggerFilter = "",
						NextState = faceSeenState,
						Overwrite = true
					});
				}

				if (!string.IsNullOrWhiteSpace(touchState))
				{
					await _misty.MapStateAsync(new MapStateParameters
					{
						Conversation = conversation,
						State = currentState,
						Trigger = StateTrigger.BumperPressed,
						TouchSensorFilter = TouchSensorFilter.Any,
						NextState = touchState,
						Overwrite = true
					});

					await _misty.MapStateAsync(new MapStateParameters
					{
						Conversation = conversation,
						State = currentState,
						Trigger = StateTrigger.CapTouched,
						TouchSensorFilter = TouchSensorFilter.Any,
						NextState = touchState,
						Overwrite = true
					});
				}

				if (!string.IsNullOrWhiteSpace(timeoutState) && timeoutMs > 0)
				{
					await _misty.MapStateAsync(new MapStateParameters
					{
						Conversation = conversation,
						State = currentState,
						Trigger = StateTrigger.Timer,
						TriggerFilter = timeoutMs.ToString(),
						NextState = timeoutState,
						Overwrite = true
					});
				}
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.LogError("Failed to map person finding states.", ex);
			}
		}

		public async Task<WebMessengerData> TrainNLPHack(Context context, string ip, bool save = true, bool overwrite = true)
        {
			IList<TrainIntent> trainIntents = new List<TrainIntent>();
			foreach(Intent intent in context.Intents)
            {
				TrainIntent ti = new TrainIntent(intent.Name, intent.Samples.ToArray());
				trainIntents.Add(ti);
			}
			
			TrainNLPData tmlpData = new TrainNLPData
			{
				Context = context.Name,
				Intents = trainIntents.ToArray(),
				Save = save,
				Overwrite = overwrite,
				
			};
			string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(tmlpData);

			WebMessenger wm = new WebMessenger();
			WebMessengerData wmd = await wm.PostRequest("http://"+ip+ "/api/dialogs/train", jsonData, "application/json");
			return wmd;
		}

		/*
		 * 
		 * 
		 * {
		  "Context": "all-modes",
		  "Intents": [
			{
			  "name": "Start",
			  "Samples": [
				"start",
				"restart"
			  ],
			  "Entities": []
			},
			{
			  "name": "Idle",
			  "Samples": [
				"idle",
				"be quiet",
				"silence",
				"silent",
				"shut up",
				"stop"
			  ],
			  "Entities": []
			},
			{
			  "name": "Repeat",
			  "Samples": [
				"repeat",
				"copy",
				"after me"
			  ],
			  "Entities": []
			},
			{
			  "name": "Backward",
			  "Samples": [
				"backward",
				"back ward",
				"reverse"
			  ],
			  "Entities": []
			},
			{
			  "name": "RubberDuckey",
			  "Samples": [
				"rubber duckey",
				"duckey",
				"duck"
			  ],
			  "Entities": []
			},
			{
			  "name": "MagicEightBall",
			  "Samples": [
				"magic eight ball",
				"eight",
				"magic"
			  ],
			  "Entities": []
			},
			{
			  "name": "VoiceCommand",
			  "Samples": [
				"voice command",
				"command",
				"voice control",
				"voice",
				"do what I say",
				"follow my lead",
				"follow the leader"
			  ],
			  "Entities": []
			},
			{
			  "name": "Weather",
			  "Samples": [
				"weather",
				"whether",
				"temperature",
				"cold outside",
				"hot outside"
			  ],
			  "Entities": []
			},
			{
			  "name": "sentry",
			  "Samples": [
				"sentry",
				"guard",
				"protect",
				"vigilant",
				"be careful"
			  ],
			  "Entities": []
			},
			{
			  "name": "Wander",
			  "Samples": [
				"wander",
				"walk around",
				"go away"
			  ],
			  "Entities": []
			},
			{
			  "name": "Settings",
			  "Samples": [
				"settings",
				"control panel"
			  ],
			  "Entities": []
			}
		  ],
		  "Save": true,
		  "Overwrite": true
		}

		 * required format
		 * 
		 * 
				{
		  "Context": "testy",
		  "Intents": [
			{
			  "name": "yes",
			  "Samples": [
				"yes",
				"yeah",
				"okay"
			  ],
			  "Entities": []
			},
			{
			  "name": "no",
			  "Samples": [
				"nope",
				"nada",
				""
			  ],
			  "Entities": []
		}
		  ],
		  "Save": false,
		  "Overwrite": false
		}
		*/

	}

	public class TrainIntent
	{
		public TrainIntent(string _name, string[] samples)
        {
			name = _name;
			Samples = samples;
        }

		public string name { get; set; }
		public string[] Samples { get; set; } = new string[0];
		public object[] Entities { get; set; } = new object[0];
	}

	public class TrainNLPData
    {
		public string Context { get; set; }
		public TrainIntent[] Intents { get; set; } = new TrainIntent[0];
		public bool Save { get; set; }
		public bool Overwrite { get; set; }
	}
}
