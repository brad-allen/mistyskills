using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConversationHelpers;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;

namespace MysticModesManagement
{
    public class BaseModePackage : IModePackage
    {
        protected IRobotMessenger Misty { get; set; }
        public virtual event EventHandler<PackageData> CallSwitchMode;
        public virtual event EventHandler<PackageData> DataEventPackage;
        protected PackageData PackageData;

        public BaseModePackage(IRobotMessenger misty)
        {
            Misty = misty;
        }

        public virtual async Task<ResponsePacket> Start(PackageData packageData)
        {
            PackageData = packageData;
            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public virtual async Task<ResponsePacket> Stop()
        {
            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public virtual bool TryGetIntentTrigger(out Intent intent)
        {
            intent = null;
            return false;
        }

        /*protected async void CreateContext(string name, IList<Intent> intents)
        {
            Context newContext = new Context
            {
                Name = name,
                Intents = intents,
                Editable = true
            };

            var testHack = await SkillWorkarounds.TrainNLPHack(newContext, _hackIp, true, true);

            await Misty.TrainNLPEngineAsync(newContext, true, true);
        }*/

        /*protected async void DeleteContext(string name)
        {
            await Misty.DeleteNLPContextAsync(name);
        }*/

        public virtual void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent) {}

    }
}