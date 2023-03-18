using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Messengers;
using MysticCommon;

namespace MysticModesDemo
{
    public abstract class BaseModePackage : IModePackage
    {
        protected IRobotMessenger Misty;

        public BaseModePackage(IRobotMessenger misty)
        {
            Misty = misty;
        }

        public abstract Task<ResponsePacket> Start(IDictionary<string, object> parameters);

        public abstract Task<ResponsePacket> Stop();

        public abstract bool TryGetIntentTrigger(out Intent intent);

        public abstract void ProcessEvent(MysticEvent mysticEvent);

        protected async void CreateContext(string name, IList<Intent> intents)
        {
            await Misty.TrainNLPEngineAsync(
               new Context
               {
                   Name = name,
                   Intents = intents
               },
               true, true);
        }

        protected async void DeleteContext(string name)
        {
            await Misty.DeleteNLPContextAsync(name);
        }

    }
}