using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Messengers;

namespace ConversationHelpers
{
    class OneLineConversation
    {
		private readonly IRobotMessenger _misty;
		public string ConversationName { get; private set; } = "OneLineConversation";
		private readonly string _say = "";
		private readonly IList<string> _contexts = new List<string>();

		public OneLineConversation(IRobotMessenger misty, string say, List<string> contexts)
        {
			_misty = misty;
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
