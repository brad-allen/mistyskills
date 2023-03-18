using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;

namespace MysticModesManagement
{
    public class BaseModePackage : IModePackage
    {
        protected IRobotMessenger Misty;
        public virtual event EventHandler<PackageData> CallSwitchMode;
        protected PackageData PackageData;

        public BaseModePackage(IRobotMessenger misty)
        {
            Misty = misty;
        }

        public virtual async Task<ResponsePacket> Start(PackageData packageData)
        {
            return await Task.FromResult(new ResponsePacket { Success = false });
        }

        public virtual async Task<ResponsePacket> Stop()
        {
            return await Task.FromResult(new ResponsePacket { Success = false });
        }

        public virtual bool TryGetIntentTrigger(out Intent intent)
        {
            intent = null;
            return false;
        }

        protected async void CreateContext(string name, IList<Intent> intents)
        {
            await Misty.TrainNLPEngineAsync(
               new Context
               {
                   Name = name,
                   Intents = intents,
                   Editable = true
               },
               true, true);
        }

        protected async void DeleteContext(string name)
        {
            await Misty.DeleteNLPContextAsync(name);
        }

        protected virtual void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {

        }

        protected async Task<bool> PrepareModeConversation()
        {
            //reset key phrase
            await Misty.StopKeyPhraseRecognitionAsync();
            
            //Must register voice record event or else data will not get processed and sent, or added to RobotInteractionEvent data
            _ = Misty.RegisterVoiceRecordEvent(0, true, "StartVREvent", null); //shouldn't be needed, but might be still - oops
            //Register and listen for Robot Interaction Event instead of VoiceRecord event in oprder to catch other events in one listener as well
            //just a personal pref...
            //TODO Add filters to keep this less noisy
            Misty.RegisterRobotInteractionEvent(RobotInteractionCallback, 0, true, "RobotInteractionEvent", null, null);
            //Start the event, not using vision data
            Misty.StartRobotInteractionEvent(false, null);
            //Reset tally light to only turn on when listening to avoid speaking hint confusion
            await Misty.SetTallyLightSettingsAsync(true, false, false);
            return true;
        }
        
        protected async Task BreakdownMode()
        {
            Misty.UnregisterEvent("StartVREvent", null);
            Misty.UnregisterEvent("RobotInteractionEvent", null);
            //Set tally light settings back to default
            await Misty.SetTallyLightSettingsAsync(true, true, true);
        }
    }
}