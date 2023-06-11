
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;
using MistyRobotics.Common.Types;
using ConversationHelpers;
using MistyRobotics.SDK.Commands;
using SkillTools.AssetTools;

namespace MysticModesManagement
{
    public class ConversationModePackage : BaseAllModesPackage
    {
        public override event EventHandler<PackageData> CallSwitchMode;
		private readonly ConversationHelper _conversationHelper;
		private const string ConversationName = "MysticExample1";
		private readonly AssetWrapper _assetWrapper;
		private ModeCommon _modeCommon;

		public ConversationModePackage(IRobotMessenger misty, ConversationHelper conversationHelper, AssetWrapper assetWrapper) : base(misty) 
		{
			_modeCommon = ModeCommon.LoadCommonOptions(misty);			
			_conversationHelper = conversationHelper;
			_assetWrapper = assetWrapper;
		}

		public override async Task<ResponsePacket> Start(PackageData packageData)
        {
			await base.Start(packageData);
			await _modeCommon.ShowWarningLayer();
			await _modeCommon.ShowEqualizerLayer();

			await Misty.StopConversationAsync(); //stop existing ones

			_ = Misty.SpeakAsync("Loading a conversation example.", true, "loading-conversation");

			await Misty.StartRobotInteractionEventAsync(true);
			await _conversationHelper.CreateYesNoContext();
			await _conversationHelper.CreateGoodBadContext();

			await CreateMysticConversation();

			Misty.RegisterVoiceRecordEvent(0, true, "StartVREvent", null); //TODO shouldn't be needed, but might be still - oops
			//Misty.RegisterAudioPlayCompleteEvent(0, true, "AudioPlayCompleteEvent", null); //TODO shouldn't be needed, but might be still - oops
			await Misty.StartFaceRecognitionAsync();

			//TODO check conversation, it sets these?
			//await Misty.SetContextAsync(MysticConstants.YesNoContextString, false, new List<string>(), false);
			//await Misty.SetContextAsync(MysticConstants.GoodBadContextString, true, new List<string>(), false);
			Misty.StartConversation(ConversationName, null);

            return new ResponsePacket { Success = true };
        }


		private async Task CreateMysticConversation()
		{
			//example action creation
			await Misty.CreateActionAsync("start.mystic",
@"LED-PATTERN:0,255,0,0,0,255,1000,breathe;
HEAD:0,0,0,1000;
ARMS:-80,-80,1000;
PAUSE:500;
ARMS:30,-80,500;
LED-PATTERN:255,0,0,0,0,255,transitonce,1500;
PAUSE:1500;
ARMS:0,0,1000;
LED-PATTERN:0,255,0,0,0,255,transitonce,1500;
PAUSE:1500;
ARMS:-80,-80,1000;
PAUSE:500;
ARMS:0,-55,500;
HEAD:-20,5,20,500;
LED:0,255,0;
", true);


			await Misty.CreateActionAsync("shake-right-arm.mystic",
@"ARMS:0,110,1000;
PAUSE:1000;
ARMS:0,45,400
PAUSE:400;
ARMS:0,110,400;
PAUSE:400;
ARMS:0,45,400;
PAUSE:400;
ARMS:0,110,400;
PAUSE:400;
ARMS:0,45,400;
PAUSE:400;
ARMS:0,110,400;
PAUSE:400;
ARMS:0,45,400;
PAUSE:400;
ARMS:0,110,400;
", true);

			await Misty.CreateActionAsync("shake-left-arm.mystic",
@"ARMS:110,0,1000;
PAUSE:1000;
ARMS:45,0,400;
PAUSE:400;
ARMS:110,0,400;
PAUSE:400;
ARMS:45,0,400;
PAUSE:400;
ARMS:110,0,400;
PAUSE:400;
ARMS:45,0,400;
PAUSE:400;
ARMS:110,0,400;
PAUSE:400;
ARMS:45,0,400;
PAUSE:400;
ARMS:110,0,400;
", true);

			//most states use onboard actions which should already exist on your Misty
			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "error.mystic.en",
				StartAction = "fear",
				Speak = "I have hit an exception and I may not be able to recover from it. I have a bad feeling about this...",
				SpeakingAction = "dizzy",
				Overwrite = true
			});

			//TODO mode needs to listen for end of conversation
			//Conversation
			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "start.mystic.en",
				StartAction = "check-surroundings-slow",
				Speak = "All right, here we go!",
				SpeakingAction = "look-right-then-left",
				FailoverState = "error.mystic.en",
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "start2.mystic.en",
				StartAction = "check-surroundings-fast",
				Speak = "Time to wake up! If you are around, touch my bumpers or stand where I can see you in good light!",
				SpeakingAction = "look-up-left",
				FailoverState = "error.mystic.en",
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "start-timeout.mystic.en",
				StartAction = "check-surroundings-slow",
				Speak = "No one seems to be here, going back to default mode. Next time, touch my bumper or stand where I can see you in good light!",
				SpeakingAction = "look-right-then-left",
				Overwrite = true
			});//Don't map this one to anything and the conversation will end

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "scruff-end.mystic.en",
				Speak = "You touched my scruff, going back to default mode.",
				Overwrite = true
			});//Don't map this one to anything and the conversation will end

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "person-found.mystic.en",
				StartAction = "hi",
				Speak = "Hello {{face||there!}} There you are! How are you?",
				SpeakingAction = "hug",
				FailoverState = "error.mystic.en",
				ListeningAction = "listen",
				ProcessingAction = "thinking",
				Listen = true,
				Contexts = new List<string> { "good-bad" },
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "unknown-are-you-ok.mystic.en",
				StartAction = "confused",
				Speak = "Sorry, I didn't hear you correctly. If you are done, touch my scruff.",
				SpeakingAction = "surprise",
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "press-right-bumper.mystic.en",
				Speak = "Press my right bumper to say yes.",
				SpeakingAction = "shake-right-arm.mystic",
				Overwrite = true
			});


			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "press-left-bumper.mystic.en",
				Speak = "Press my left bumper to say no.",
				SpeakingAction = "shake-left-arm.mystic",
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "are-you-okay2.mystic.en",
				Speak = "Are you doing well?",
				SpeakingAction = "yes",
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "timeout-are-you-ok.mystic.en",
				StartAction = "confused",
				Speak = "Sorry, I didn't hear you . If you are done, touch my scruff.",
				SpeakingAction = "surprise",
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "doing-poor.mystic.en",
				TransitionAction = "love",
				Speak = "Sorry to hear!",
				SpeakingAction = "sad",
				FailoverState = "error.mystic.en",
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "bumper-check-success.mystic.en",
				TransitionAction = "cute",
				Speak = "Yay, your bumpers work!",
				SpeakingAction = "check-it-out",
				FailoverState = "error.mystic.en",
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "doing-good.mystic.en",
				Speak = "Great to hear!",
				SpeakingAction = "cheers",
				TransitionAction = "hug",
				FailoverState = "error.mystic.en",
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "try-bumpers-et-al.mystic.en",
				Speak = "Touch my bumpers and head to make me make sounds! When you are done, touch my chin.",
				SpeakingAction = "oh-wow",
				FailoverState = "error.mystic.en",
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "done-playing.mystic.en",
				SpeakingAction = "hi",
				Speak = "Okay, we are done. Was that fun?",
				ListeningAction = "listen",
				FailoverState = "error.mystic.en",
				ProcessingAction = "correct",
				Listen = true,
				Contexts = new List<string> { "yes-no" },
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "not-fun.mystic.en",
				Speak = "Boo hoo!",
				SpeakingAction = "sad",
				FailoverState = "error.mystic.en",
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "was-fun.mystic.en",
				Speak = "Yay!",
				SpeakingAction = "head-up-down-nod",
				FailoverState = "error.mystic.en",
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "fun-timeout.mystic.en",
				StartAction = "confused",
				Speak = "Sorry, I didn't hear you.",
				SpeakingAction = "surprise",
				FailoverState = "error.mystic.en",
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "unknown-fun.mystic.en",
				StartAction = "confused",
				Speak = "unknown",
				SpeakingAction = "surprise",
				FailoverState = "error.mystic.en",
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "end.mystic.en",
				StartAction = "walk-fast",
				Speak = "This was just a simple example of making a conversation. Have fun!",
				SpeakingAction = "walk-fast",
				FailoverState = "error.mystic.en",
				Overwrite = true
			});

			await Misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "end2.mystic.en",
				Speak = "Bye!",
				SpeakingAction = "party",
				FailoverState = "error.mystic.en",
				Overwrite = true
			});


			//Create and map conversation
			await Misty.CreateConversationAsync(ConversationName, "start.mystic.en", true, true, null);

			await _conversationHelper.MapSpeakingOnlyStates(ConversationName, "start.mystic.en", "start2.mystic.en");

			await _conversationHelper.MapHumanInteractionStates
				(ConversationName, "start2.mystic.en", "person-found.mystic.en", "person-found.mystic.en", "start-timeout.mystic.en", 60000, "scruff-end.mystic.en");

			await _conversationHelper.MapTrueFalseStates
				(ConversationName, "person-found.mystic.en", "doing-good.mystic.en", "doing-poor.mystic.en", MysticConstants.GoodIntentString,
					MysticConstants.BadIntentString, "unknown-are-you-ok.mystic.en", "timeout-are-you-ok.mystic.en", 10000, true, false);


			await _conversationHelper.MapSpeakingOnlyStates(ConversationName, "timeout-are-you-ok.mystic.en", "press-right-bumper.mystic.en");
			await _conversationHelper.MapSpeakingOnlyStates(ConversationName, "unknown-are-you-ok.mystic.en", "press-right-bumper.mystic.en");
			await _conversationHelper.MapSpeakingOnlyStates(ConversationName, "press-right-bumper.mystic.en", "press-left-bumper.mystic.en");
			await _conversationHelper.MapSpeakingOnlyStates(ConversationName, "press-left-bumper.mystic.en", "are-you-okay2");

			await _conversationHelper.MapTrueFalseStates
				(ConversationName, "are-you-okay2", "doing-good.mystic.en", "doing-poor.mystic.en", MysticConstants.YesIntentString,
					MysticConstants.NoIntentString, "unknown-are-you-ok.mystic.en", "timeout-are-you-ok.mystic.en", 10000, true, false);

			/*

			//Map bumpers
			await Misty.MapStateAsync(new MapStateParameters
			{
				Conversation = ConversationName,
				State = "press-left-bumper.mystic.en",
				Trigger = StateTrigger.BumperPressed,
				TouchSensorFilter = TouchSensorFilter.Any,
				NextState = "bumper-check-success.mystic.en",
				Overwrite = true
			});

			await Misty.MapStateAsync(new MapStateParameters
			{
				Conversation = ConversationName,
				State = "timeout-are-you-ok.mystic.en",
				Trigger = StateTrigger.BumperPressed,
				TouchSensorFilter = TouchSensorFilter.Any,
				NextState = "bumper-check-success.mystic.en",
				Overwrite = true
			});

			await Misty.MapStateAsync(new MapStateParameters
			{
				Conversation = ConversationName,
				State = "timeout-are-you-ok.mystic.en",
				Trigger = StateTrigger.Timer,
				TriggerFilter = "20000",
				NextState = "error.mystic.en",
				Overwrite = true
			});

			await Misty.MapStateAsync(new MapStateParameters
			{
				Conversation = ConversationName,
				State = "unknown-are-you-ok.mystic.en",
				Trigger = StateTrigger.Timer,
				TriggerFilter = "20000",
				NextState = "error.mystic.en",
				Overwrite = true
			});*/

			//await _conversationHelper.MapSpeakingOnlyStates(ConversationName, "bumper-check-success.mystic.en", "try-bumpers-et-al.mystic.en");
			await _conversationHelper.MapSpeakingOnlyStates(ConversationName, "doing-good.mystic.en", "try-bumpers-et-al.mystic.en");
			await _conversationHelper.MapSpeakingOnlyStates(ConversationName, "doing-poor.mystic.en", "try-bumpers-et-al.mystic.en");

			await Misty.MapStateAsync(new MapStateParameters
			{
				Conversation = ConversationName,
				State = "try-bumpers-et-al.mystic.en",
				Trigger = StateTrigger.CapTouched,
				TouchSensorFilter = TouchSensorFilter.Cap_Chin,
				NextState = "done-playing.mystic.en",
				Overwrite = true
			});


			await _conversationHelper.MapTrueFalseStates
				(ConversationName, "done-playing.mystic.en", "was-fun.mystic.en", "not-fun.mystic.en", MysticConstants.YesIntentString,
					MysticConstants.NoIntentString, "unknown-fun.mystic.en", "fun-timeout.mystic.en", 10000, true, false);

			await _conversationHelper.MapSpeakingOnlyStates(ConversationName, "was-fun.mystic.en", "end.mystic.en");
			await _conversationHelper.MapSpeakingOnlyStates(ConversationName, "not-fun.mystic.en", "end.mystic.en");
			await _conversationHelper.MapSpeakingOnlyStates(ConversationName, "unknown-fun.mystic.en", "end.mystic.en");
			await _conversationHelper.MapSpeakingOnlyStates(ConversationName, "fun-timeout.mystic.en", "end.mystic.en");

			//map "end.mystic.en" so it 'leaves' state  (see RobotInteractionCallback) and causes event to go to idle
			await _conversationHelper.MapSpeakingOnlyStates(ConversationName, "end.mystic.en", "end2.mystic.en");

			//Don't map end so the conversation ends
		}
		
		public override async Task<ResponsePacket> Stop()
		{
			await base.Stop();
			await _modeCommon.DeleteWarningLayer();
			return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("conversation");
            samples.Add("talk");
			samples.Add("let's talk");
			samples.Add("talk to me");
			samples.Add("top");

            intent = new Intent
            {
                Name = "Conversation",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        public override void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
		{
			_ = _modeCommon.SetEqualizerSpeech(robotInteractionEvent);
			if (robotInteractionEvent.DialogState?.Step == DialogActionStep.FinalIntent)
			{
				_ = _modeCommon.WriteToWarningLayer(robotInteractionEvent.DialogState.Text);
			}
			else if (robotInteractionEvent.Step == RobotInteractionStep.StartingState)
			{
				_ = _modeCommon.HideWarningLayer();
			}

			if ((robotInteractionEvent.Step == RobotInteractionStep.CapTouched && robotInteractionEvent.CapTouchState.Scruff == TouchSensorOption.Contacted) ||
				(robotInteractionEvent.Step == RobotInteractionStep.LeavingState && robotInteractionEvent.State == "end.mystic.en"))				
            {
				_ = Misty.StopConversationAsync();
				_assetWrapper.ShowSystemImage(SystemImage.DefaultContent);

				PackageData pd = new PackageData(MysticMode.Conversation, "idle")
				{
					ModeContext = PackageData.ModeContext,
					Parameters = PackageData.Parameters
				};

				CallSwitchMode?.Invoke(this, pd);
				return;
			}

			if ((robotInteractionEvent.Step == RobotInteractionStep.CapTouched ||
				robotInteractionEvent.Step == RobotInteractionStep.BumperPressed) && 
				robotInteractionEvent.State == "try-bumpers-et-al.mystic.en")
            {
				if (robotInteractionEvent.Step == RobotInteractionStep.BumperPressed)
				{
					if(robotInteractionEvent.BumperState.BackLeft == TouchSensorOption.Contacted)
                    {
						_assetWrapper.PlaySystemSound(SystemSound.PhraseOwwww);
						Misty.StartAction("cry-slow", true, null);
					}
					else if (robotInteractionEvent.BumperState.BackRight == TouchSensorOption.Contacted)
					{
						_assetWrapper.PlaySystemSound(SystemSound.PhraseOopsy);
						Misty.StartAction("cute", true, null);
					}
					else if (robotInteractionEvent.BumperState.FrontLeft == TouchSensorOption.Contacted)
					{
						_assetWrapper.PlaySystemSound(SystemSound.PhraseEvilAhHa);
						Misty.StartAction("angry", true, null);
					}
					else if (robotInteractionEvent.BumperState.FrontRight == TouchSensorOption.Contacted)
					{
						_assetWrapper.PlaySystemSound(SystemSound.PhraseHello);
						Misty.StartAction("hug", true, null);
					}
				}
				else if (robotInteractionEvent.Step == RobotInteractionStep.CapTouched)
                {
					 if (robotInteractionEvent.CapTouchState.Back == TouchSensorOption.Contacted)
					{
						_assetWrapper.PlaySystemSound(SystemSound.PhraseNoNoNo);
						Misty.StartAction("mad", true, null);
					}
					else if (robotInteractionEvent.CapTouchState.Front == TouchSensorOption.Contacted)
					{
						_assetWrapper.PlaySystemSound(SystemSound.PhraseUhOh);
						Misty.StartAction("sad", true, null);
					}
					else if (robotInteractionEvent.CapTouchState.Right == TouchSensorOption.Contacted)
					{
						_assetWrapper.PlaySystemSound(SystemSound.Sleepy2);
						Misty.StartAction("sleep", true, null);
					}
					else if (robotInteractionEvent.CapTouchState.Left == TouchSensorOption.Contacted)
					{
						_assetWrapper.PlaySystemSound(SystemSound.Distraction);
						Misty.StartAction("surprise", true, null);
					}
				}
			}
		}
    }
}