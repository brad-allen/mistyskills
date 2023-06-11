using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Commands;
using MysticCommon;
using SkillTools.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConversationHelpers
{
    /// <summary>
    /// Hacks using endpoints instead of skill commands for some buggy alpha commands until those are fixed
    /// </summary>
    public class SkillWorkarounds
	{	
		public static async Task<WebMessengerData> TrainNLPHack(Context context, string hackIp, bool save = true, bool overwrite = true)
		{
			IList<TrainIntent> trainIntents = new List<TrainIntent>();
			foreach (Intent intent in context.Intents)
			{
				TrainIntent ti = new TrainIntent(intent.Name, intent.Samples.ToArray());
				trainIntents.Add(ti);
			}

			TrainNLPData tmlpData = new TrainNLPData
			{
				Context = context.Name,
				Intents = trainIntents.ToArray(),
				Save = save,
				Overwrite = overwrite,

			};
			string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(tmlpData);

			WebMessenger wm = new WebMessenger();
			WebMessengerData wmd = await wm.PostRequest("http://" + hackIp + "/api/dialogs/train", jsonData, "application/json");

			return wmd;
		}

		/*
		 * 
		 * 
		 * {
		  "Context": "all-modes",
		  "Intents": [
			{
			  "name": "Start",
			  "Samples": [
				"start",
				"restart"
			  ],
			  "Entities": []
			},
			{
			  "name": "Idle",
			  "Samples": [
				"idle",
				"be quiet",
				"silence",
				"silent",
				"shut up",
				"stop"
			  ],
			  "Entities": []
			},
			{
			  "name": "Repeat",
			  "Samples": [
				"repeat",
				"copy",
				"after me"
			  ],
			  "Entities": []
			},
			{
			  "name": "Backward",
			  "Samples": [
				"backward",
				"back ward",
				"reverse"
			  ],
			  "Entities": []
			},
			{
			  "name": "RubberDuckey",
			  "Samples": [
				"rubber duckey",
				"duckey",
				"duck"
			  ],
			  "Entities": []
			},
			{
			  "name": "MagicEightBall",
			  "Samples": [
				"magic eight ball",
				"eight",
				"magic"
			  ],
			  "Entities": []
			},
			{
			  "name": "VoiceCommand",
			  "Samples": [
				"voice command",
				"command",
				"voice control",
				"voice",
				"do what I say",
				"follow my lead",
				"follow the leader"
			  ],
			  "Entities": []
			},
			{
			  "name": "Weather",
			  "Samples": [
				"weather",
				"whether",
				"temperature",
				"cold outside",
				"hot outside"
			  ],
			  "Entities": []
			},
			{
			  "name": "sentry",
			  "Samples": [
				"sentry",
				"guard",
				"protect",
				"vigilant",
				"be careful"
			  ],
			  "Entities": []
			},
			{
			  "name": "Wander",
			  "Samples": [
				"wander",
				"walk around",
				"go away"
			  ],
			  "Entities": []
			},
			{
			  "name": "Settings",
			  "Samples": [
				"settings",
				"control panel"
			  ],
			  "Entities": []
			}
		  ],
		  "Save": true,
		  "Overwrite": true
		}

		 * required format
		 * 
		 * 
				{
		  "Context": "testy",
		  "Intents": [
			{
			  "name": "yes",
			  "Samples": [
				"yes",
				"yeah",
				"okay"
			  ],
			  "Entities": []
			},
			{
			  "name": "no",
			  "Samples": [
				"nope",
				"nada",
				""
			  ],
			  "Entities": []
		}
		  ],
		  "Save": false,
		  "Overwrite": false
		}
		*/

	}
}