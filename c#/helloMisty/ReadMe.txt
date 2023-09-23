Just so there is no confusion...

I am not a Misty or Furhat employee at this time, these are just examples of skills I am writing during my personal time.

I make no claims about these skills, do not commit to supporting them, don't know if they will work with your specific Misty or future versions of Misty, nor do I assume any responsibilities from using these skills.

This skill uses two different joke endpoints. 
I have tried to exclude sections that could be deemed innapprpriate, but I can't guarantee some of the jokes might not be to your liking. Feel free to comment out the code.

This skill also changes a few settings on Misty. See below!

-----------------------------------------------

This is an assortment of capability examples in one skill, showing different ways to use Misty's capabilities and handle the asynchronous nature of her responses, including a very basic example of using the alpha Misty conversation system.
This skill assumes you have Misty 2.0 updates.

With a known bug in the conversation system, there are work-arounds to train the context in the nlp system.

Have fun!

Unless the code specifies a Misty Robotics or Furhat license, you can assume the code in these repos are under an MIT or Apache license. 

-----------------------------------------------
ALERT!!!  UPDATES TO MISTY DEFAULT SETTINGS

Be aware that I upload and replace the default listening beep setting that Misty uses in this skill as I find this shorter beep allows her to hear better/sooner.

I also adjust the tally light settings so it is only on when listening.

```await _misty.SetNotificationSettingsAsync(false, true, true, "short-beep.mp3");```

I do attempt to change it back when the skill is cancelled, but if you exit the skill without cancelling or other unexpected things happen, it may not change.
You can change this line, or call it from Misty's API Explorer in the web tools, to update the listening notification settings. 
									
with parameters (revert to default, led enabled, key phrase enabled, key phrase "listening" audio file)
Setting the first parameter to true will set Misty to their default notification settings.

This skill also auto-uploads a few assets from the helloMisty/Assets/SkillAssets folder in this repo. See below for more info...

-----------------------------------------------

This skill starts up as a 'startup skill', so if you have the Meet Misty OOBE or any other startup skills, you may want to change that setting so it, or the other skill(s), only starts manually.

-----------------------------------------------
KNOWN BUGS AND WORKAROUNDS
A lot of this functionality is very Alpha, and there are a few bugs in the C# SDK implementation of it requiring some temporary workarounds.

	** TrainNlpContext - Training Context Bug Workaround			
			Contexts are used for Misty’s conversational speech. Conversational Misty is an alpha feature, and may change as needed in the future. 
			Currently, the API Explorer and Skills fail the command when you try to use their training functionality. 
			One way to work around this is to use a tool such as Postman, Advanced REST Client or other REST system to POST commands to the robot.

			For this skill here is an English context you need so Misty can understand what you want. Feel free to change it as needed!
			Right now, "help" is ignored as an option in the skill

			POST http://<misty-ip>/api/dialogs/train

			JSON payload

{
	"context": "hello-misty.context.en.options",
  "editable": true,
	"Intents": [{
		"name": "time",
  "entities": [],
		"samples": ["time", "what time", "hour", "minute", "what's the time", "what is time"]
	}, {
		"name": "day",
  "entities": [],
		"samples": ["day", "what day", "what's today", "what is today", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday", "weekend"]
	}, {
		"name": "weather",
  "entities": [],
		"samples": ["weather", "what is it like outside", "outside", "temperature"]
	}, {
		"name": "dance",
  "entities": [],
		"samples": ["dance", "boogie", "shake", "party", "go crazy", "have fun"]
	}, {
		"name": "joke",
  "entities": [],
		"samples": ["joke", "tell me a joke", "make me laugh", "something funny", "giggle", "sense of humor"]
	}, {
		"name": "help",
  "entities": [],
		"samples": ["what are", "options", "what am I", "I am confused", "not sure", "I don't know", "I do not know", "lost", "I need help"]
	}],
	"save": true,
	"overwrite": true
}
			
	** Other known issues:
		GetActions fails in C# SDK

-----------------------------------------------
STARTING PARAMETERS

You can pass in these parameters. On the Skill Management page, click on the gear next to the skill you want to run.
	Add the parameters there, in name and value columns, and start the skill.

 Weather Mode needs these items from OpenWeather if you want to use it. You can get a free key if you don't use it too much (https://openweathermap.org/appid).
	* OpenWeatherKey -- must be passed in to use this mode successfully
	* CountryCode -- defaults to 'US', you can always change it in your code
	* CityCode -- defaults to 'Boulder' - Boulder, CO, cuz that's where I am, you can always change it in your code

-----------------------------------------------

AssetWrapper and extra Misty Assets

* AssetWrapper is an example library to show one way to upload assets to Misty at the start of a skill.

* helloMisty/Assets/SkillAssets
	The items in this folder will be uploaded the first time the skill runs if the files do not exist.
	These assets are used in this skill.
	If you remove 'short-beep.mp3' from this folder, then Misty will be unable to make a sound before starting listening in this skill.
	
-----------------------------------------------
Say Hey Misty and then ask Misty...
 * the Time
 * the Day
 * the Weather
 * a Joke
 * to move her arms or her head
 * to change her chest color l.e.d.
 * to dance

 Misty's Bumpers will do the following:
 * front right - the Time
 * front left - a Joke
 * back right - the Weather
 * back left - the Day
 
 Cap touch will make sounds and change Misty's face.
-----------------------------------------------
-----------------------------------------------

THAR BE DRAGONS
Be careful. Deleting or moving files in some of these areas, or using certain functionality in the device manager, can cause you to lose saved functionality, mess up the onboard web tools, or even delete or bork yer robot, so tread carefully.

-----------------------------------------------
WINDOWS IOT FILES

 You can use a file browser to connect to the robot at:
 \\<MistyIp>\c$\Data\Misty

 It will ask for your username and password, the username is 'administrator' and the password should be on the bottom of your bot.
 
 Misty Logs are located at:
 \\<MistyIp>\c$\Data\Misty\Logs

 Skill Logs are located at:
 \\<MistyIp>\c$\Data\Misty\SDK\Logs
	* I think they delete after 7 days or so (on the next run).

-----------------------------------------------
DEBUGGING CODE

After deploying new code to debug, use Force Reload on the Skill Management page, or re-stop, wait for its cleanup to happen, and the start the skill to ensure a new version is loaded on the robot.

-----------------------------------------------
WIRED MISTY

To directly access Misty, you can use an ethernet cable and two (2) USB-to-ethernet adapters to connect directly from a computer to Misty. If you are a developer, this can also be a much faster way to push skills to Misty.

Connect the cable to your computer using one of the adapters and the other to Misty's USB port on her back, and go to the device manager's Connectivity -> Network Tab and look for the USB to Ethernet IP.

-----------------------------------------------
UPDATING WEB TOOLS
	
Updating the hosted web tools files on your bot at \\<RobotIP>\c$\Data\Misty\Web\SDK will cause the hosted web tools to use that page instead after a reboot (and clearing the browser cache or using an incognito browser).
You should make a copy of the working web tools if you are planning on making updates outside of a repo, to ensure you don't break your web tools. :(

-----------------------------------------------
DEVICE MANAGER

Going to \\<MistyIp>:8080 will give you the device manager (you'll need to log in - your passwoprd should be on the bottom of your Misty) where you can manage the device a little more directly. Be careful if you are here as you can uninstall Misty!
	* To use some of the features in the device manager (like performance tracing and app management) you need to reload the page and log in again at the ethernet cable's IP (not wifi). You can find that on the device manager's Connectivity -> Network Tab, look for the USB to Ethernet IP.