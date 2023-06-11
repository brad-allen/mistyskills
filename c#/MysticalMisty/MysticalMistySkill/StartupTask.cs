using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Messengers;
using MysticalMisty;
using Windows.ApplicationModel.Background;

namespace MysticalMistySkill
{
	/// <summary>
	/// Called when the skill connects to Misty
	/// </summary>
	public sealed class StartupTask : IBackgroundTask
	{
		public void Run(IBackgroundTaskInstance taskInstance)
		{
			RobotMessenger.LoadAndPrepareSkill(taskInstance, new MistySkill(), SkillLogLevel.Verbose, "Mystical-Misty:");
		}
	}
}