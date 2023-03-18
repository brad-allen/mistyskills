using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Messengers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MysticModesManagement.Conversations
{
    class OneLineConversation
    {
		IRobotMessenger _misty;
		public string ConversationName { get; private set; } = "OneLineConversation";
		ConversationHelper _conversationHelper;
		private string _say = "";
		private IList<string> _contexts = new List<string>();

		public OneLineConversation(IRobotMessenger misty, string say, List<string> contexts)
        {
			_misty = misty;
			_conversationHelper = new ConversationHelper(_misty);
			_say = say;
			_contexts = contexts;
		}

        public async Task<bool> Initialize()
        {
			await _misty.CreateStateAsync(new CreateStateParameters
			{
				Name = "one-liner-will-change",
				Contexts = _contexts,
				Speak = _say,
				Overwrite = true,
			});

			await _misty.CreateConversationAsync(ConversationName, "one-liner-will-change", true, true, null);

			
			//Don't map states

			return true;
		}

    }
}
