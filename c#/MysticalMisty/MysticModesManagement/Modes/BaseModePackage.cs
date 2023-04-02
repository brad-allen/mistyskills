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
        protected IRobotMessenger Misty { get; set; }
        public virtual event EventHandler<PackageData> CallSwitchMode;
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

        public virtual void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent) {}

    }
}