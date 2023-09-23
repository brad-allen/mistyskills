using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.SDK.Events;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Messengers;
using Wander.DriveManagers;
using Wander.Types;

namespace Wander
{
	/// <summary>
	/// TODO Revisit me, this is prolly overly complicated now with new available events
	/// </summary>
	public class DriveManager
	{
		private readonly IRobotMessenger _misty;
		private CurrentObstacleState _currentObstacleState;		
		private DriveHeartbeat _driveHeartbeat;		
		private BaseDrive _driveManager;
		private bool _debugMode = true;

		public DriveMode DriveMode { get; private set; } = DriveMode.Stopped;

		public DriveManager(IRobotMessenger misty)
		{
			_misty = misty;
			_currentObstacleState = new CurrentObstacleState();

			_misty.TransitionLED(255, 140, 0, 0, 0, 255, LEDTransition.Breathe, 1000, null);
		}

		public void StartDriving(DriveManagerParameters drivemanagerParameters)
		{
			ProcessParameters(drivemanagerParameters);

			_misty.ChangeLED(0, 0, 255, null);

			RegisterEvents();

			//Start listening for heartbeat ticks
			_driveHeartbeat.HeartbeatTick += HeartbeatCallback;
		}

		public async void Stop()
		{
			DriveMode = DriveMode.Stopped;
			_misty.UnregisterEvent("FrontRight", null);
			_misty.UnregisterEvent("FrontLeft", null);
			//_misty.UnregisterEvent("FrontCenter", null);
			_misty.UnregisterEvent("Back", null);
			_misty.UnregisterEvent("FLEdge", null);
			_misty.UnregisterEvent("FREdge", null);
			_misty.UnregisterEvent("Bumper", null);
			_misty.UnregisterEvent("DriveEncoder", null);
			if(_driveHeartbeat != null)
            {
				_driveHeartbeat.HeartbeatTick -= HeartbeatCallback;
			}
			await _misty.StopAsync();
			_misty.TransitionLED(0, 0, 255, 255, 0, 0, LEDTransition.TransitOnce, 2000, null);			
			await _misty.StopAsync(); //just in case...
		}

		public async void HeartbeatCallback(object sender, DateTime _lastHeartbeatTime)
		{
			try
            {
				_misty.SkillLogger.LogInfo($"FrontRightTOF = {_currentObstacleState.FrontRightTOF} - FrontLeftTOF = {_currentObstacleState.FrontLeftTOF} - Paused = {_driveHeartbeat.HeartbeatPaused}");
				if (!_misty.Wait(0)) { return; }

				switch (DriveMode)
				{
					case DriveMode.Careful:
						await _driveManager.Drive();
						break;
					case DriveMode.Wander:
						if (_driveHeartbeat.HeartbeatPaused)
						{
							return;
						}
						//Wander2 does a little more complex driving so turn off the hearbeat until this drive action is complete
						_driveHeartbeat.PauseHeartbeat();
						await _driveManager.Drive();
						_driveHeartbeat.ContinueHeartbeat();
						break;
				}
			}
			catch (Exception ex)
            {
				_misty.SkillLogger.LogError("Exception while processing heartbeart callback.", ex);
            }
				
		}

		private void ProcessParameters(DriveManagerParameters wanderParameters)
		{
			try
			{
				_debugMode = wanderParameters.DebugMode;
				if(_debugMode)
				{
					_misty.SkillLogger.LogLevel = SkillLogLevel.Verbose;
				}

				DriveMode = wanderParameters.DriveMode;
				switch (DriveMode)
				{
					case DriveMode.Wander:
						_driveManager = new WanderDrive(_misty, _currentObstacleState, _debugMode);
						_driveHeartbeat = new DriveHeartbeat(150);
						break;
					case DriveMode.Careful:
						_driveManager = new CarefulDrive(_misty, _currentObstacleState, _debugMode);
						_driveHeartbeat = new DriveHeartbeat(3000);
						break;
				}
			}
			catch (Exception ex)
			{
				_misty.SkillLogger.Log("Failed handling wander startup parameters", ex);
			}
		}
		
		private void RegisterEvents()
		{
			//Register Bump Sensors with a callback
			_misty.RegisterBumpSensorEvent(BumpCallback, 50, true, null, "Bumper", null);

			//Front Right Time of Flight
			List<TimeOfFlightValidation> tofFrontRightValidations = new List<TimeOfFlightValidation>();
			tofFrontRightValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.FrontRight });
			_misty.RegisterTimeOfFlightEvent(TOFFRRangeCallback, 0, true, tofFrontRightValidations, "FrontRight", null);
			
			//Front Left Time of Flight
			List<TimeOfFlightValidation> tofFrontLeftValidations = new List<TimeOfFlightValidation>();
			tofFrontLeftValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.FrontLeft });
			_misty.RegisterTimeOfFlightEvent(TOFFLRangeCallback, 0, true, tofFrontLeftValidations, "FrontLeft", null);

			//Front Center Time of Flight
		//	List<TimeOfFlightValidation> tofFrontCenterValidations = new List<TimeOfFlightValidation>();
		//	tofFrontCenterValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.FrontCenter });
		//	_misty.RegisterTimeOfFlightEvent(TOFCRangeCallback, 0, true, tofFrontCenterValidations, "FrontCenter", null);
			
			//Back Time of Flight
			List<TimeOfFlightValidation> tofBackValidations = new List<TimeOfFlightValidation>();
			tofBackValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.Back });
			_misty.RegisterTimeOfFlightEvent(TOFBRangeCallback, 0, true, tofBackValidations, "Back", null);
			
			//Setting debounce a little higher to avoid too much traffic
			//Firmware will do the actual stop for edge detection
			List<TimeOfFlightValidation> tofFrontRightEdgeValidations = new List<TimeOfFlightValidation>();
			tofFrontRightEdgeValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.DownwardFrontRight });
			_misty.RegisterTimeOfFlightEvent(FrontEdgeCallback, 1000, true, tofFrontRightEdgeValidations, "FREdge", null);

			List<TimeOfFlightValidation> tofFrontLeftEdgeValidations = new List<TimeOfFlightValidation>();
			tofFrontLeftEdgeValidations.Add(new TimeOfFlightValidation { Name = TimeOfFlightFilter.SensorName, Comparison = ComparisonOperator.Equal, ComparisonValue = TimeOfFlightPosition.DownwardFrontLeft });
			_misty.RegisterTimeOfFlightEvent(FrontEdgeCallback, 1000, true, tofFrontLeftEdgeValidations, "FLEdge", null);


			IList<DriveEncoderValidation> driveValidations = new List<DriveEncoderValidation>();
			_misty.RegisterDriveEncoderEvent(EncoderCallback, 250, true, driveValidations, "DriveEncoder", null);
		}


		private void EncoderCallback(IDriveEncoderEvent encoderEvent)
		{
			//Keep track of the encoders so you know where you are and can draw a map using this data and tof data
			
			
		}

		private bool TryGetAdjustedDistance(ITimeOfFlightEvent tofEvent, out double distance)
		{
			distance = 0;
			//   0 = valid range data
			// 101 = sigma fail - lower confidence but most likely good
			// 104 = Out of bounds - Distance returned is greater than distance we are confident about, but most likely good
			if (tofEvent.Status == 0 || tofEvent.Status == 101 || tofEvent.Status == 104)
			{
				distance = tofEvent.DistanceInMeters;
			}
			else if (tofEvent.Status == 102)
			{
				//102 generally indicates nothing substantial is in front of the robot so the TOF is returning the floor as a close distance
				//So ignore the disance returned and just set to 2 meters
				distance = 2;
			}
			else
			{
				//TOF returning uncertain data or really low confidence in distance, ignore value 
				return false;
			}
			return true;
		}

		//All of these (tof range, tof edge, bump) can be monitored for mapping and possibly drawn differently		
		public void TOFFLRangeCallback(ITimeOfFlightEvent tofEvent)
		{
			if(TryGetAdjustedDistance(tofEvent, out double distance))
			{
				_currentObstacleState.FrontLeftTOF = distance;
			}	
		}

		public void TOFFRRangeCallback(ITimeOfFlightEvent tofEvent)
		{
			if (TryGetAdjustedDistance(tofEvent, out double distance))
			{
				_currentObstacleState.FrontRightTOF = distance;
			}
		}

		public void TOFCRangeCallback(ITimeOfFlightEvent tofEvent)
		{
			if (TryGetAdjustedDistance(tofEvent, out double distance))
			{
				_currentObstacleState.FrontCenterTOF = distance;
			}
		}

		public void TOFBRangeCallback(ITimeOfFlightEvent tofEvent)
		{
			if (TryGetAdjustedDistance(tofEvent, out double distance))
			{
				_currentObstacleState.BackTOF = distance;
			}
		}

		public void BumpCallback(IBumpSensorEvent bumpEvent)
		{
			switch (bumpEvent.SensorPosition)
			{
				case BumpSensorPosition.FrontRight:
					if (bumpEvent.IsContacted)
					{
						_currentObstacleState.FrontRightBumpContacted = true;
					}
					else
					{
						_currentObstacleState.FrontRightBumpContacted = false;
					}
					break;
				case BumpSensorPosition.FrontLeft:
					if (bumpEvent.IsContacted)
					{
						_currentObstacleState.FrontLeftBumpContacted = true;
					}
					else
					{
						_currentObstacleState.FrontLeftBumpContacted = false;
					}
					break;
				case BumpSensorPosition.BackRight:
					if (bumpEvent.IsContacted)
					{
						_currentObstacleState.BackRightBumpContacted = true;
					}
					else
					{
						_currentObstacleState.BackRightBumpContacted = false;
					}
					break;
				case BumpSensorPosition.BackLeft:
					if (bumpEvent.IsContacted)
					{
						_currentObstacleState.BackLeftBumpContacted = true;
					}
					else
					{
						_currentObstacleState.BackLeftBumpContacted = false;
					}
					break;
			}
		}

		private void FrontEdgeCallback(ITimeOfFlightEvent edgeEvent)
		{
			switch (edgeEvent.SensorPosition)
			{
				case TimeOfFlightPosition.DownwardFrontRight:
					_currentObstacleState.FrontRightEdgeTOF = edgeEvent.DistanceInMeters;
 					break;
				case TimeOfFlightPosition.DownwardFrontLeft:
					_currentObstacleState.FrontLeftEdgeTOF = edgeEvent.DistanceInMeters;
					break;
			}
		}
	}
}
