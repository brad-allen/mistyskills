using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Messengers;
using MysticCommon;
using SkillTools.Web;

namespace ConversationHelpers
{
    public class ConversationHelper
    {
		private readonly IRobotMessenger _misty;
		private readonly string _hackIp;

		public ConversationHelper(IRobotMessenger misty, string hackIp)
        {
			_misty = misty;
			_hackIp = hackIp;
		}

		public async Task CreateYesNoContext()
		{
			IList<Intent> intents = new List<Intent>();
			Intent yesIntent = new Intent();
			List<string> yesSamples = new List<string>();
			yesSamples.Add("yes");
			yesSamples.Add("yeah");
			yesSamples.Add("okay");
			yesSamples.Add("sure");
			yesSamples.Add("yep");
			yesSamples.Add("see");
			yesSamples.Add("sea");
			yesSamples.Add("affirmative");

			yesIntent.Name = MysticConstants.YesIntentString;
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
			noSamples.Add("nine");

			noIntent.Name = MysticConstants.NoIntentString;
			noIntent.Samples = noSamples;
			intents.Add(noIntent);

			await SkillWorkarounds.TrainNLPHack(new Context
			{
				Name = MysticConstants.YesNoContextString,
				Intents = intents
			},
			_hackIp, true, true);

			/*TODO after fix, use this...
			 * await _misty.TrainNLPEngineAsync(
				new Context
				{
					Name = MysticConstants.YesNoContextString,
					Intents = intents
				},
				true, true);*/
		}

		public async Task CreateGoodBadContext()
		{
			IList<Intent> intents = new List<Intent>();
			Intent goodIntent = new Intent();
			List<string> goodSamples = new List<string>();
			goodSamples.Add("good");
			goodSamples.Add("happy");
			goodSamples.Add("I'm good");
			goodSamples.Add("awesome");
			goodSamples.Add("super");
			goodSamples.Add("great");
			goodSamples.Add("grate");
			goodSamples.Add("eight");

			goodIntent.Name = MysticConstants.GoodIntentString;
			goodIntent.Samples = goodSamples;
			intents.Add(goodIntent);

			Intent badIntent = new Intent();
			List<string> badSamples = new List<string>();
			badSamples.Clear();
			badSamples.Add("bad");
			badSamples.Add("poor");
			badSamples.Add("sick");
			badSamples.Add("sad");
			badSamples.Add("meh");
			badSamples.Add("blah");

			badIntent.Name = MysticConstants.BadIntentString;
			badIntent.Samples = badSamples;
			intents.Add(badIntent);

			await SkillWorkarounds.TrainNLPHack(new Context
			{
				Name = MysticConstants.GoodBadContextString,
				Intents = intents
			},
			_hackIp, true, true);

			/*TODO after fix, use this...
			 * await _misty.TrainNLPEngineAsync(
				new Context
				{
					Name = MysticConstants.GoodBadContextString,
					Intents = intents
				},
				true, true);*/
		}

		public async Task MapSpeakingOnlyStates(string conversation, string currentState, string nextState)
		{
			await _misty.MapStateAsync(new MapStateParameters
			{
				Conversation = conversation,
				State = currentState,
				Trigger = StateTrigger.AudioCompleted,
				NextState = nextState,
				Overwrite = true
			});
		}

		public async Task MapTrueFalseStates(string conversation, string currentState, string trueState, string falseState, string trueIntent, string falseIntent, string unknownState = null, string timeoutState = null, int timeoutMs = 0, bool useBumpers = true, bool useReentryOnFailure = false)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(trueState) && !string.IsNullOrWhiteSpace(falseState))
				{
					await _misty.MapStateAsync(new MapStateParameters
					{
						Conversation = conversation,
						State = currentState,
						Trigger = StateTrigger.SpeechHeard,
						TriggerFilter = trueIntent,
						NextState = trueState,
						Overwrite = true,
					});

					await _misty.MapStateAsync(new MapStateParameters
					{
						Conversation = conversation,
						State = currentState,
						Trigger = StateTrigger.SpeechHeard,
						TriggerFilter = falseIntent,
						NextState = falseState,
						Overwrite = true
					});

					if (useBumpers)
                    {
						await _misty.MapStateAsync(new MapStateParameters
						{
							Conversation = conversation,
							State = currentState,
							Trigger = StateTrigger.BumperPressed,
							TouchSensorFilter = TouchSensorFilter.Bumper_FrontRight,
							NextState = trueState,
							Overwrite = true
						});
						await _misty.MapStateAsync(new MapStateParameters
						{
							Conversation = conversation,
							State = currentState,
							Trigger = StateTrigger.BumperPressed,
							TouchSensorFilter = TouchSensorFilter.Bumper_FrontLeft,
							NextState = falseState,
							Overwrite = true
						});
					}

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

						await _misty.MapStateAsync(new MapStateParameters
						{
							Conversation = conversation,
							State = currentState,
							Trigger = StateTrigger.SpeechHeard,
							TriggerFilter = "",
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
				_misty.SkillLogger.LogError("Failed to map true/false state.", ex);
			}
		}

		public async Task MapHumanInteractionStates(string conversation, string currentState, string newFaceSeenState, string touchSensorState = null, string timeoutState = null, int timeoutMs = 0, string scruffSensorState = null)
		{
			try
			{
				if (!string.IsNullOrWhiteSpace(newFaceSeenState))
				{
					await _misty.MapStateAsync(new MapStateParameters
					{
						Conversation = conversation,
						State = currentState,
						Trigger = StateTrigger.NewFaceSeen,
						TriggerFilter = "",
						NextState = newFaceSeenState,
						Overwrite = true
					});
				}

				if (!string.IsNullOrWhiteSpace(touchSensorState))
				{
					if(string.IsNullOrWhiteSpace(scruffSensorState))
                    {
						scruffSensorState = touchSensorState;

					}

					await _misty.MapStateAsync(new MapStateParameters
					{
						Conversation = conversation,
						State = currentState,
						Trigger = StateTrigger.BumperPressed,
						TouchSensorFilter = TouchSensorFilter.Any,
						NextState = touchSensorState,
						Overwrite = true
					});

					await _misty.MapStateAsync(new MapStateParameters
					{
						Conversation = conversation,
						State = currentState,
						Trigger = StateTrigger.CapTouched,
						TouchSensorFilter = TouchSensorFilter.Any,
						NextState = touchSensorState,
						Overwrite = true
					});

					await _misty.MapStateAsync(new MapStateParameters
					{
						Conversation = conversation,
						State = currentState,
						Trigger = StateTrigger.CapTouched,
						TouchSensorFilter = TouchSensorFilter.Cap_Scruff,
						NextState = scruffSensorState,
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
				_misty.SkillLogger.LogError("Failed to map human interaction states.", ex);
			}
		}
	}
}
