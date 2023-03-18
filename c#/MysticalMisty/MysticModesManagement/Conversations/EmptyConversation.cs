using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Messengers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MysticModesManagement.Conversations
{
    class EmptyConversation
    {
		IRobotMessenger _misty;
		public string ConversationName { get; private set; } = "EmptyConversation";
		ConversationHelper _conversationHelper;

		public EmptyConversation(IRobotMessenger misty)
        {
			_misty = misty;
			_conversationHelper = new ConversationHelper(_misty);
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
