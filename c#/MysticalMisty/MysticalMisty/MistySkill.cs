using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MistyRobotics.Common;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Logger;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using MysticModesManagement;
using SkillTools.AssetTools;

namespace MysticalMisty
{
	/// <summary>
	/// Misty skill to start mode manager
	/// Placed in separate UWP app to enable easier integration with Unit Test framework
	/// </summary>
	public sealed class MistySkill : IMistySkill
	{
		private IRobotMessenger _misty;
		private static ModeManager _modeManager;

		public INativeRobotSkill Skill { get; private set; } = new NativeRobotSkill("Mystical-Misty", "72c5785f-6e80-44ec-bbfb-681812bba7fe")
		{
			TimeoutInSeconds = -1,
			StartupRules = { NativeStartupRule.Manual, NativeStartupRule.Startup }
		};

		public void LoadRobotConnection(IRobotMessenger robotInterface)
		{
			_misty = robotInterface;
		}

		public async void OnStart(object sender, IDictionary<string, object> parameters)
		{
            try
			{
				//TODO test this with other events
				_misty.UnregisterAllEvents(null);
				//give enough time for the events to unregister
				await Task.Delay(1000);

				//example log intercept - uncomment to get events on every skill message
				// Can be very busy and should only be used for debugging, can slow down skill!!
				//_misty.RegisterForSDKLogEvents(HandleLogEvent); 

				//Load assets needed for skill from MysticalMistySkill/Assets/SkillAssets folder
				//  if you put a lot of files in there, this may take a while if they don't exist on Misty yet				
				string loadMessage = "Loading any missing skill assets onto robot from MysticalMistySkill\\Assets\\SkillAssets";
				Debug.WriteLine(loadMessage);
				_misty.SkillLogger?.Log(loadMessage);
				AssetWrapper assetWrapper = new AssetWrapper(_misty);
				await assetWrapper.LoadAssets(false);

				_modeManager = await ModeManager.Start(parameters, _misty, assetWrapper);
				if (_modeManager == null)
				{
					string msg = "Failed to initialize and start Mode Manager.";
					Debug.WriteLine(msg);
					_misty.SkillLogger?.LogInfo(msg);
				}
			}
			catch (Exception ex)
            {
				string errorMessage = "Failed to initialize and start Mode Manager.";
				Debug.WriteLine(errorMessage);
				_misty.SkillLogger?.LogError(errorMessage, ex);
            }
		}

		public void OnCancel(object sender, IDictionary<string, object> parameters)
		{
			//Keep me here as I am used in an example mocked test
			_ = _misty.ChangeLEDAsync(0, 0, 255);

			string msg = "Cancelled, shutting down.";
			Debug.WriteLine(msg);
			_misty.SkillLogger?.LogInfo(msg);
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
			string msg = "Timeout, shutting down.";
			Debug.WriteLine(msg);
			_misty.SkillLogger?.LogInfo(msg);
		}

		/// <summary>
		/// Example handler for a log message listener event
		/// Uncomment event subscriber to use [_misty.RegisterForSDKLogEvents(HandleLogEvent)]
		/// Should only be used for debugging!
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="message"></param>
		private void HandleLogEvent(object sender, LogMessage message)
		{
			Debug.WriteLine($"SDK Log Message: {message.Message}");
		}

		#region IDisposable Support

		private static bool _isDisposed = false;

		private static void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					_modeManager?.Dispose();
				}
				
				_modeManager = null;
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