using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Messengers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MysticModesManagement.Conversations
{
    public class AllModesConversation
    {
		IRobotMessenger _misty;
		public string ConversationName { get; private set; } = "AllModesConversation";
		ConversationHelper _conversationHelper;

		public AllModesConversation(IRobotMessenger misty)
        {
			_misty = misty;
			_conversationHelper = new ConversationHelper(_misty);
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
			//Arggh! Bug?!
			var test = await _misty.SetContextAsync("all-modes", false, null, false);

			return true;
		}

    }
}
