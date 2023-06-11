using System;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Events;

namespace MysticCommon
{
    public interface IModePackage
    {
        event EventHandler<PackageData> CallSwitchMode;
        event EventHandler<PackageData> DataEventPackage;

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