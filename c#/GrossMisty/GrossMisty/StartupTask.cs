using MistyRobotics.SDK.Messengers;
using Windows.ApplicationModel.Background;

namespace GrossMisty
{
    public sealed class StartupTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            RobotMessenger.LoadAndPrepareSkill(taskInstance, new GrossMisty());
        }
    }
}