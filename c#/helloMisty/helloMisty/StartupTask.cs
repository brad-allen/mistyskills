using MistyRobotics.SDK.Messengers;
using Windows.ApplicationModel.Background;

namespace helloMisty
{
    public sealed class StartupTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            RobotMessenger.LoadAndPrepareSkill(taskInstance, new HelloMistySkill());
        }
    }
}