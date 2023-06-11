using Wander.Types;

namespace Wander
{
	public class DriveManagerParameters
    {
		public DriveMode DriveMode { get; set; } = DriveMode.Stopped;
		public bool DebugMode { get; set; } = false;
	}
}