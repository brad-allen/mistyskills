using MistyRobotics.Common.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MysticCommon
{
    public interface IModePackage
    {
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
        Task<ResponsePacket> Start(IDictionary<string, object> parameters);

        /// <summary>
        /// Stop the mode
        /// </summary>
        /// <returns></returns>
        Task<ResponsePacket> Stop();

        /// <summary>
        /// Handle events that come in as needed if they are not handled by the conversation mappings
        /// </summary>
        /// <param name="mysticEvent"></param>
        void ProcessEvent(MysticEvent mysticEvent);
    }
}