using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Messengers;

namespace ConversationHelpers
{
    public class AllModesConversation
    {
		private readonly IRobotMessenger _misty;
		public string ConversationName { get; private set; } = "AllModesConversation";

		public AllModesConversation(IRobotMessenger misty)
        {
			_misty = misty;
		}

        public async Task<bool> Initialize()
        {
			await _misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "all-modes-check",
				Contexts = new List<string> { { "all-modes" }  },
				Speak = "",
				Overwrite = true,
			});

			await _misty.CreateConversationAsync(ConversationName, "all-modes-check", true, true, null);

			//Set Context of modes so we can understand what they are asking for...
			await _misty.SetContextAsync("all-modes", false, null, false);

			return true;
		}
    }
}
