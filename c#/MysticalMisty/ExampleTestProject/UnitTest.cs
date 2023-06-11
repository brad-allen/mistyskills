using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MysticalMisty;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using Moq;

namespace ExampleTestProject
{
	/// <summary>
	/// Example and experiment of how to connect and run with Mocks, but this isn't a real test / TODO Add some tests
	/// Uses a number of test system references to make it work, make sure they are installed as well...
	///   Microsoft.NET.Test.Sdk, Moq, MSTest.TestAdapter, MSTest.TestFramework, NUnit3TestAdapter, TestPlatform.Universal
	/// To run, make other project configs AnyCPU and make this project x86, open test explorer, and run
	/// Switch projects back to ARM to deploy to robot
	/// </summary>
	[TestClass]
	public class UnitTest
	{
		[TestMethod]
		public void TestOnCancel()
		{
			int ledChangedCounter = 0;
			//Mock the robot messenger
			Mock<IRobotMessenger> robotMessengerMock = new Mock<IRobotMessenger>();

			//Make a new skill
			MistySkill moqExampleSkill = new MistySkill();

			//Create a robot command response to use in the mock
			IRobotCommandResponse successfulActionResponse = new RobotCommandResponse
			{
				ResponseType = MessageType.ActionResponse,
				Status = ResponseStatus.Success
			};

			//Setup mock to ensure Change LED Async has been called the appropriate amount of times
			robotMessengerMock.Setup
			(
				//mocking ChangeLEDAsync to take any 3 ints
				x => x.ChangeLEDAsync
				(
					It.IsAny<uint>(),
					It.IsAny<uint>(),
					It.IsAny<uint>()
				)
			//running that mocked command increments the counter and returns the "successful" response
			).Returns(Task.Factory.StartNew(() =>
			{
				++ledChangedCounter;
				return successfulActionResponse;
			}).AsAsyncOperation());

			//Load mock messenger into skill
			moqExampleSkill.LoadRobotConnection(robotMessengerMock.Object);

			//OnCancel will trigger the skill's cancellation and in that method it changes the LED
			moqExampleSkill.OnCancel(this, null);

			//Test value
			Assert.IsTrue(ledChangedCounter == 1);
		}
	}
}