using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Messengers;
using MysticCommon;

namespace MysticModesDemo
{
    public class StartModePackage : BaseModePackage
    {
        private bool _repeatTime;
        private const string ConversationName = "StartModePackage";
        
        public StartModePackage(IRobotMessenger misty) : base(misty) { }

        public override async Task<ResponsePacket> Start(IDictionary<string, object> parameters)
        {
            _repeatTime = true;

            //example conversations
            await CreateStates();
            await CreateActions();
            await MapStates();

            _ = Misty.RegisterVoiceRecordEvent(0, true, "Test-is-this-needed", null); //shouldn't be needed, but might be still - oops

            //Set the intents of this mode            
            
            //_ = Misty.SpeakAsync("When you are ready, say, Hey Misty, and tell me what you want to do.", true, "idleSpeech");

            Misty.StartConversation(ConversationName, null);

            return new ResponsePacket { Success = true };
        }

        public override async Task<ResponsePacket> Stop()
        {
            //Do cleanup here...
            _repeatTime = false;
            Misty.UnregisterEvent("Test-is-this-needed", null);
            //TODO make a disposable?
            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("start");
            samples.Add("restart");

            intent = new Intent
            {
                Name = "Start",
                Samples = samples
            };
            return true;
        }

        private async Task MapStates()
        {
            //mystic-greet-start
            await Misty.MapStateAsync(
                ConversationName,
                "mystic-greet-start",
                StateTrigger.AudioCompleted,
                TouchSensorFilter.None,
                ObjectDescriptionFilter.none,
                null,
                null,
                "mystic-greet-look-around",
                ConversationName,
                false,
                false,
                true);

            await Misty.MapStateAsync(
                ConversationName,
                "mystic-greet-start",
                StateTrigger.BumperPressed,
                TouchSensorFilter.Any,
                ObjectDescriptionFilter.none,
                null,
                null,
                "mystic-greet-see-face",
                ConversationName,
                false,
                false,
                true);

            //mystic-greet-look-around
            await Misty.MapStateAsync(
                ConversationName,
                "mystic-greet-look-around",
                StateTrigger.NewFaceSeen,
                TouchSensorFilter.None,
                ObjectDescriptionFilter.none,
                null,
                null,
                "mystic-greet-see-face",
                ConversationName,
                false,
                false,
                true);

            await Misty.MapStateAsync(
                ConversationName,
                "mystic-greet-look-around",
                StateTrigger.BumperPressed,
                TouchSensorFilter.Any,
                ObjectDescriptionFilter.none,
                null,
                null,
                "mystic-greet-interaction-1",
                ConversationName,
                false,
                false,
                true);

            await Misty.MapStateAsync(
                ConversationName,
                "mystic-greet-look-around",
                StateTrigger.CapReleased,
                TouchSensorFilter.Cap_Chin,
                ObjectDescriptionFilter.none,
                null,
                null,
                "mystic-greet-interaction-1",
                ConversationName,
                false,
                false,
                true);

            await Misty.MapStateAsync(
                ConversationName,
                "mystic-greet-look-around",
                StateTrigger.Timer,
                TouchSensorFilter.Any,
                ObjectDescriptionFilter.none,
                "45000",
                null,
                "mystic-greet-see-nothing",
                ConversationName,
                false,
                false,
                true);

            //mystic-greet-see-nothing

            await Misty.MapStateAsync(
                new MapStateParameters
                {
                    Conversation = ConversationName,
                    State = "mystic-greet-see-nothing",
                    NextState = "mystic-greet-see-face",
                    Trigger = StateTrigger.NewFaceSeen,
                    Overwrite = true,
                    IncludeFollowUp = false,
                    ObjectDescriptionFilter = ObjectDescriptionFilter.none,
                    TouchSensorFilter = TouchSensorFilter.None
                });


            await Misty.MapStateAsync(
                ConversationName,
                "mystic-greet-see-nothing",
                StateTrigger.BumperPressed,
                TouchSensorFilter.Any,
                ObjectDescriptionFilter.none,
                null,
                null,
                "mystic-greet-interaction-1",
                ConversationName,
                false,
                false,
                true);


            await Misty.MapStateAsync(
                ConversationName,
                "mystic-greet-see-nothing",
                StateTrigger.Timer,
                TouchSensorFilter.None,
                ObjectDescriptionFilter.none,
                "45000",
                null,
                "mystic-greet-still-see-nothing",
                ConversationName,
                false,
                false,
                true);


            //mystic-greet-interaction-1

            await Misty.MapStateAsync(
                ConversationName,
                "mystic-greet-interaction-1",
                StateTrigger.SpeechHeard,
                TouchSensorFilter.None,
                ObjectDescriptionFilter.none,
                "yes",
                null,
                "mystic-greet-wifi-1",
                ConversationName,
                false,
                false,
                true);

            await Misty.MapStateAsync(
                ConversationName,
                "mystic-greet-interaction-1",
                StateTrigger.SpeechHeard,
                TouchSensorFilter.None,
                ObjectDescriptionFilter.none,
                "no",
                null,
                "mystic-greet-interaction-1",
                ConversationName,
                false,
                false,
                true);

            await Misty.MapStateAsync(
               ConversationName,
               "mystic-greet-interaction-1",
               StateTrigger.SpeechHeard,
               TouchSensorFilter.None,
               ObjectDescriptionFilter.none,
               "unknown",
               null,
               "mystic-greet-interaction-1",
               ConversationName,
               true,
               false,
               true);
        }

        private async Task CreateStates()
        {
            //Add States

            await Misty.CreateStateAsync(
                "mystic-greet-start", //name
                "Wow! It's good to stretch my arms again!", //speak
                null, //followup
                null, //audio
                false, //listen
                new List<string>(), //contexts
                new List<string>(), //prespeech
                "hug", //startAction
                "walk-fast", //speakingAction
                null, //listening/post speech Action
                null, //processingAction
                null, //transitionAction
                null, //noMatchAction
                null, //noMatchSpeech
                null, //noMatchAudio
                null, //reentrySpeech
                0, //max retries
                "mystic-greet-look-around", //failoverState
                false, //retrain
                true, //overwrite
                new List<string>(), //filters
                null); //required context

            await Misty.CreateStateAsync(
               "mystic-greet-interaction-1", //name
               "All right! So, for the rest of this interaction, you can either say Yes or No to my questions, or you can push my right front bumper for Yes, or my left front bumper for No. Now remember, this is from my perspective, so if you are looking at my face, then my right is your left. Do you understand?", //speak
               null, //followup
               null, //audio
               true, //listen
               new List<string> { "yes-no" }, //contexts
               new List<string>(), //prespeech
               "surprise", //startAction
               "hi", //speakingAction
               null, //listening/post speech Action
               null, //processingAction
               null, //transitionAction
               "confused", //noMatchAction
               "I didn't quite catch that, can you try again?", //noMatchSpeech //this won't be called if there are more than just speech intents, need to map unknown and/or silence in that case
               null, //noMatchAudio
               "I didn't quite catch that, remember, you can either say Yes or No to my questions, or you can push my right front bumper for Yes, or my left front bumper for No. Do you understand?", //reentrySpeech
               3, //max retries
               "mystic-greet-fail", //failoverState
               false, //retrain
                true, //overwrite
                new List<string>(), //filters
                null); //required context
            /*
            await Misty.CreateStateAsync(
                "mystic-greet-start", //name
                "Wow! It's good to stretch my arms again!", //speak
                null, //followup
                null, //audio
                false, //listen
                new List<string>(), //contexts
                new List<string>(), //prespeech
                "hug", //startAction
                "walk-fast", //speakingAction
                null, //listening/post speech Action
                null, //processingAction
                null, //transitionAction
                null, //noMatchAction
                null, //noMatchSpeech
                null, //noMatchAudio
                null, //reentrySpeech
                0, //max retries
                "mystic-greet-look-around", //failoverState
                false, //retrain
                true, //overwrite
                new List<string>(), //filters
                null); //required context

            await Misty.CreateStateAsync(
               "mystic-greet-interaction-1", //name
               "All right! So, for the rest of this interaction, you can either say Yes or No to my questions, or you can push my right front bumper for Yes, or my left front bumper for No. Now remember, this is from my perspective, so if you are looking at my face, then my right is your left. Do you understand?", //speak
               null, //followup
               null, //audio
               true, //listen
               new List<string> { "yes-no" }, //contexts
               new List<string>(), //prespeech
               "surprise", //startAction
               "hi", //speakingAction
               null, //listening/post speech Action
               null, //processingAction
               null, //transitionAction
               "confused", //noMatchAction
               "I didn't quite catch that, can you try again?", //noMatchSpeech //this won't be called if there are more than just speech intents, need to map unknown and/or silence in that case
               null, //noMatchAudio
               "I didn't quite catch that, remember, you can either say Yes or No to my questions, or you can push my right front bumper for Yes, or my left front bumper for No. Do you understand?", //reentrySpeech
               3, //max retries
               "mystic-greet-fail", //failoverState
               false, //retrain
                true, //overwrite
                new List<string>(), //filters
                null); //required context
            */
        }

        private async Task CreateActions()
        {

            //Create extra actions needed for OOBX
            string oobeStretchScript = @"
HEAD:0,0,0,2000; PAUSE(2000);
HEAD:-35,0,0,1500; PAUSE(1200);
HEAD:10,0,0,2000; PAUSE(1800);
HEAD:0,0,0,500; PAUSE(600);
HEAD:0,-20,0,750; PAUSE(650);
HEAD:0,20,0,1500; PAUSE(1200);
HEAD:0,0,0,750; PAUSE(750);
HEAD:0,0,-60,1500; PAUSE(1500);
HEAD:0,0,60,3000; PAUSE(2500);
HEAD:0,0,0,1500; PAUSE(2000);";
            await Misty.CreateActionAsync("wake-up-stretch", oobeStretchScript, true);
        }


        public override void ProcessEvent(MysticEvent mysticEvent)
        {

        }
    }
}