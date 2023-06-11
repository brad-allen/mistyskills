using System;
using System.Threading.Tasks;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Messengers;

namespace ConversationHelpers
{
    public class EmptyConversation
    {
		private readonly IRobotMessenger _misty;
		public string ConversationName { get; private set; } = "EmptyConversation";

		public EmptyConversation(IRobotMessenger misty)
        {
			_misty = misty;
		}

        public async Task<bool> Initialize()
        {
			await _misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "do-nothing",
				Speak = "",
				Overwrite = true,
			});

			await _misty.CreateConversationAsync(ConversationName, "do-nothing", true, true, null);

			//Don't map states

			return true;
		}

    }
}
