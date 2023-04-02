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

namespace MysticalMistySkill
{
	internal class MistySkill : IMistySkill
	{
		private IRobotMessenger _misty;
		private ISDKLogger _logger;

		private static ModeManager _modeManager;

		public INativeRobotSkill Skill { get; private set; } = new NativeRobotSkill("Mystical-Misty", "72c5785f-6e80-44ec-bbfb-681812bba7fe")
		{
			TimeoutInSeconds = -1,
			AllowedCleanupTimeInMs = 2000,
			StartupRules = { NativeStartupRule.Manual, NativeStartupRule.Startup }
		};

		public void LoadRobotConnection(IRobotMessenger robotInterface)
		{
			_misty = robotInterface;
			_logger = _misty.SkillLogger;
		}

		private async Task<bool> Initialize(IDictionary<string, object> parameters)
        {
			_modeManager = await ModeManager.Initialize(parameters, _misty);
			return false;
        }

		public async void OnStart(object sender, IDictionary<string, object> parameters)
		{
            try
            {
				if (!await Initialize(parameters))
				{
					_logger.LogError("Failed to initialize.");

				}
			}
			catch (Exception ex)
            {
				_logger.LogError("Failed in skill OnStart", ex);
            }
		}

		public void OnCancel(object sender, IDictionary<string, object> parameters)
		{
			_logger.LogInfo("Cancelled, shutting down.");
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
			_logger.LogInfo("Timeout, shutting down.");
			Dispose();
		}

		#region IDisposable Support

		private bool _isDisposed = false;

		private void Dispose(bool disposing)
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