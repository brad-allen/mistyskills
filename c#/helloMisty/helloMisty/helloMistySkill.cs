using System;
using System.Collections.Generic;
using System.Diagnostics;
using helloMistyUWP;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Messengers;

namespace helloMisty
{
	/// <summary>
	/// Misty skill to start HelloMistyManager
	/// HelloMistyManager placed in separate UWP app to enable easier integration with Unit Test framework and UWP framework capabilities
	/// </summary>
	public sealed class HelloMistySkill : IMistySkill
	{
		private IRobotMessenger _misty;
		private HelloMistyManager _helloMistyManager;
		private IDictionary<string, object> _parameters = new Dictionary<string, object>();

		public INativeRobotSkill Skill { get; private set; } = new NativeRobotSkill("hello-misty", "e02d3aaa-ada3-47d6-ae52-60babca5630c")
		{
			//no timeout
			TimeoutInSeconds = -1,
			//run at startup and manually
			//if other skills are set to run at startup, 
			// or as an OOBE, this will run at the same time and they may interfere with each other
			StartupRules = { NativeStartupRule.Manual, NativeStartupRule.Startup },
			//Give dispose time to cleanup before Misty starts ignoring commands from this skill
			AllowedCleanupTimeInMs = 2000			 
		};

		public void LoadRobotConnection(IRobotMessenger robotInterface)
		{
			_misty = robotInterface;
		}

		public async void OnStart(object sender, IDictionary<string, object> parameters)
		{
			try
			{
				_parameters = parameters;
				_helloMistyManager = new HelloMistyManager();
				if (await _helloMistyManager.Start(_misty, _parameters))
				{
					//let it run....
					//the events will keep it alive
					string message = "Running skill.";
					Debug.WriteLine(message);
					_misty.SkillLogger?.LogInfo(message);
				}
				else
				{
					_misty.Speak("Failed to start hello misty manager. Sorry.", true, "failed1", null);
					string errorMessage = "Failed to start helloMistyManager.";
					Debug.WriteLine(errorMessage);
					_misty.SkillLogger?.LogError(errorMessage);
					Dispose();
				}
			}
			catch (Exception ex)
			{
				_misty.Speak("Exception starting hello misty manager. Sorry.", true, "failed1", null);
				string errorMessage = "Failed to initialize and start helloMistyManager.";
				Debug.WriteLine(errorMessage);
				_misty.SkillLogger?.LogError(errorMessage, ex);
				Dispose();
			}
		}
		
		public void OnCancel(object sender, IDictionary<string, object> parameters)
		{
			_misty.Speak("Goodbye!", true, "goodbye", null);
			string msg = "Shutting down.";
			Debug.WriteLine(msg);
			_misty.SkillLogger?.LogInfo(msg);
			Dispose();
		}

		public void OnPause(object sender, IDictionary<string, object> parameters)
		{
			OnCancel(sender, parameters);
		}

		public void OnResume(object sender, IDictionary<string, object> parameters)
		{
			OnStart(sender, parameters);
		}

		public void OnTimeout(object sender, IDictionary<string, object> parameters)
		{
			OnCancel(sender, parameters);
		}
		
		#region IDisposable Support

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					_helloMistyManager.Dispose();
				}
				
				_isDisposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}

		#endregion
	}
}