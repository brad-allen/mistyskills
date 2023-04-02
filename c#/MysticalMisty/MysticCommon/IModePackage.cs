using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MysticCommon
{
    public class PackageData
    {
        public PackageData(MysticMode mode, string dataName)
        {
            Mode = mode;
            DataName = dataName;
        }
        public MysticMode Mode { get; private set; }
        public string DataName { get; private set; }
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public Context ModeContext { get; set; } = new Context();
    }

    public interface IModePackage
    {
        event EventHandler<PackageData> CallSwitchMode;

        void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent);

        /// <summary>
        /// The intent to use to choose this mode option
        /// </summary>
        /// <param name="intent"></param>
        /// <returns></returns>
        bool TryGetIntentTrigger(out Intent intent);

        /// <summary>
        /// Start the mode
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<ResponsePacket> Start(PackageData packageData);

        /// <summary>
        /// Stop the mode
        /// </summary>
        /// <returns></returns>
        Task<ResponsePacket> Stop();
    }
}