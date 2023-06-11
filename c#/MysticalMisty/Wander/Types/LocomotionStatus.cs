namespace Wander.Types
{
	/// <summary>
	/// Current driving direction of the robot
	/// </summary>
#pragma warning disable 1591
	public enum LocomotionStatus
	{
		Unknown = 0,
		DrivingForward = 1,
		DrivingBackward = 2,
		DrivingForwardRight = 3,
		DrivingForwardLeft = 4,
		TurningRight = 5,
		TurningLeft = 6,
		Stopped = 7
	}
#pragma warning restore 1591
}