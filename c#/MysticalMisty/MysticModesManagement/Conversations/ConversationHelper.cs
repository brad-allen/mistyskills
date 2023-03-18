using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Messengers;
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

	}
}
