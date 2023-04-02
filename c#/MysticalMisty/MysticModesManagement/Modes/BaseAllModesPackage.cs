using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;
using MysticModesManagement.Conversations;

namespace MysticModesManagement
{
    public class BaseAllModesPackage : BaseModePackage
    {
        protected AllModesConversation AllModesConversation;

        public BaseAllModesPackage(IRobotMessenger misty) : base(misty) 
        {
            AllModesConversation = new AllModesConversation(Misty);
        }

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            await base.Start(packageData);

            //Start empty conversation for now to get the intents and contexts returned in the RobotInteractionEvent
            await AllModesConversation.Initialize();
            await Misty.StartConversationAsync(AllModesConversation.ConversationName);

            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override async Task<ResponsePacket> Stop()
        {
            return await Task.FromResult(new ResponsePacket { Success = false });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
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

        public override void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {

        }

       
    }
}