
HEY! This isn't done and still has some bugs.
-----------------------------------------------

Just so there is no confusion...
I am not a Misty or Furhat employee at this time, these are just examples of skills I am writing during my personal time.
I make no claims about these skills, do not commit to supporting them, don't know if they will work with your specific Misty or future versions of Misty, nor do I assume any responsibilities from using these skills.

-----------------------------------------------

This is an assortment of capability examples in one skill, showing different ways to use Misty's capabilities and handle the asynchronous nature of her responses, including a very basic example of using the alpha Misty conversation system.
This skill assumes you have Misty 2.0 updates.

Right now this is a work in progress, full of issues and bugs, so initial versions are prolly going to work inconsistently.
With a known bug in the conversation system, there are work-arounds to train the all-modes context in the nlp system.
This skill also changes a few settings on Misty. See below!

Feel free to use these as base templates and examples for your skills, and improve this example infrastructure as you see fit.
Please give back to the community with your examples and skills!
Have fun!

Unless the code specifies a Misty Robotics or Furhat license, you can assume the code in these repos are under an MIT or Apache license. 

-----------------------------------------------
ALERT!!!  UPDATES TO MISTY DEFAULT SETTINGS

Be aware that I upload and replace the default listening beep setting that Misty uses in this skill as I find this shorter beep allows her to hear better/sooner.
I do attempt to change it back when the skill is cancelled, but if you exit the skill without cancelling ot other unexpected things happen, it may not change.

You can change this line, or call it from Misty's API Explorer in the web tools, to update the listening notification settings. 
									
```await _misty.SetNotificationSettingsAsync(false, true, true, "short-beep.mp3");```
	
with parameters (revert to default, led enabled, key phrase enabled, key phrase "listening" audio file)
Setting the first parameter to true will set Misty to their default notification settings.

Also, if you change the voice settings in this skill through the settings mode, they will stay that way after the skill has ended.
You can always reset them on the Misty Tools Site.

This skill also auto-uploads a few assets from the MysticalMistySkill/Assets/SkillAssets folder in this repo. See below for more info...

-------------------------------------
Framework:
* MysticalMystySkill
	Startup Task and entry point into skill. Code that is called from the robot to connect to and interact with the skill.

* MysticalMisty
	IMistySkill interface implementation. The actual start and management of the skill. 
	In the default Misty templates this is in the same location as the Startup Task, but I've pulled it into its own UWP app here for easier integration with the Unit Test example.

* MysticCommon
	Common data packages and helpers used across mystic misty projects.

* MysticTools
	A bunch of different things to make Misty more fun, not all are used in modes yet. 
	Some of these have been provided in earlier community examples.
		* EmailTools (sending emails)
		* FunnyBone (jokes)
		* Conversations Helpers and other SDK Hacks and workarounds for known conversation bugs
		* SkillTools (asset, web and simple storage)
		* TimeManager (tells time in english)
		* SMSServiceManager (sms msgs) //currently just twilio integration
		* Open Weather interface (tells weather in english when proper creds are passed in)

* MysticModesManagement
	A number of really simple example capabilities you can start by talking to Misty and doing what she says.
		I find Misty can hear better if you are not charging her while using this skill.
		I am mainly using the new alpha dialog interaction event system and responding to them differently in each example mode, instead of registering for each independent event type.
		I am using an all-modes context for switching modes, but most of my non-conversation examples that use speech (voice commands, etc) just use a text compare for now (TODO).
		If you visit each mode's TryGetIntentTrigger() method, you can build out better, and personalized, intents for you and your Misty.
		NOTE that the train nlp command isn't working on the C# SDK so to train this you will need to hack it or create a separate endpoint call... more later.

* ExampleTestProject
	An example of connecting to and mocking a Misty skill for automated testing.
	TODO No good tests yet, but framework is there to build upon.

* Wander
	A very simple driving example where Misty compares her two front time of flights and her back tof and drives to avoid things.
	She does better in bright light and larger rooms.
	
-----------------------------------------------
KNOWN BUGS AND WORKAROUNDS
A lot of this functionality is very Alpha, and there are a few bugs in the C# SDK implementation of it requiring some temporary workarounds.

	** TrainNlpContext seems to fail in the C# SDK
			The C# command does not work
			* So one hack is to add it manually through REST calls
				BUT to do this from the skill, it seems access depends on network settings 
				** I had to connect through a cable, get the IP of that cable and use that IP as a passed in parameter [HackIp] to send the request - see below... **
			* The other is to copy the file to TBD
				
	** GetActions fails in C# SDK

-----------------------------------------------
KNOWN ODDITIES

	** Voice settings are still distinct for the Dialog system, versus normal Misty speaking, so to change voice settings for the different ways Misty speaks, the system updates the 'dialog' settings AND the 'default voice' settings
		-- this is done in the SettingsModePackage example, but be aware when you are coding these systems on your own if you notice Misty changing voices while speaking.

-----------------------------------------------
STARTING PARAMETERS

You can pass in these parameters. On the Skill Management page, click on the gear next to the skill you want to run.
	Add the parameters there in name and value columns.

* HackIp
	You MUST pass this in if you aren't manually adding the context and other items until the C# interface bugs are fixed
		depending on your network settings, you may need to connect to a wire and use that IP, as there can be security issues for wifi connections - to get that information you can use the Device Manager to get the network IP (see below)

 Weather Mode needs these items from OpenWeather if you want to use it. You can get a free key if you don't use it too much (https://openweathermap.org/appid).
	* OpenWeatherKey -- must be passed in to use this mode successfully
	* CountryCode -- defaults to 'US', you can always it in your code
	* CityCode -- defaults to 'Boulder' - Boulder, CO, cuz that's where I am, you can always change it in your code

-----------------------------------------------

AssetWrapper and extra Misty Assets

In the MysticalMistySkill there are three folders under Assets.

* SkillAssets
	These will be uploaded the first time the skill runs if the files do not exist. These assets are used in this skill.
	If you remove short-beep from this folder, then Misty will be unable to make a sound before starting listening.
	
* MistyAssets
	Audio files that have come on different versions of Misty robots over the years. 
		If you don't have them and want to upload them, you can do it manually or move them to the SkillAssets folder, make sure they are "included" in the project, build and run the skill. May take a while if there are a lot of files that don't already exist, delaying the start of the skill.

* EasterEggs
	Different assets I have used in skills to represent speaking animation.
		If you don't have them and want to upload them, you can do it manually or move them to the SkillAssets folder, make sure they are "included" in the project build and run the skill.

-----------------------------------------------
* Current Modes *

Idle: Waiting around for a voice command to tell it to do something.

Start: Mode it starts the skill with. Says hi and waits around for a voice command to tell it to do something.

Backward: Listens to what you say and says it backward. Says it backward by letter.

Word Reverse: Listens to what you say and says it backward by word.

Conversation: A very simple example conversation.

Follow Person: Follows a face. If you press the different bumpers, it'll start and stop following or speaking.

FunnyBone: Will get and say a random developer or chuck norris joke. Reguires an internet connection.

Magic Eight Ball: Ask Misty a yes or no question for an "answer".

Repeat: Listens to what you say and says it back to you.

Rubber Ducky: Wraps your question or thought in other phrases. Pretty much same as Magic Eight ball :|

Settings: Change the voice, and voice pitch and speed. Press the Chin to change the setting, press the front bumpers to scroll through the lists and press the top of the head to save the setting.

Voice Command: Really simple text response reading to move the arms, head and change the LED.

Wander: Uses the time of flights to attempt to navigate a room. Does better in bright light, larger rooms, and less table and chair legs.

Weather: Pass in an OpenWeatherKey (free at https://openweathermap.org/appid) and your CountryCode and CityCode to get the weather when you ask for it.


* For most/all all of these you can say Hey Misty and ask for another mode, or touch Misty's scruff to get her to go back to the Idle or Start mode.

-----------------------------------------------
-----------------------------------------------

THAR BE DRAGONS
Be careful. Deleting or moving files in some of these areas, or using certain functionality in the device manager, can cause you to lose saved functionality, mess up the onboard web tools, or even delete or bork yer robot, so tread carefully.

 Other good Misty coding tricks to know

 You can use a file browser to connect to the robot at:
 \\<MistyIp>\c$\Data\Misty

 It will ask for your username and password, the username is 'administrator' and the password should be on the bottom of your bot.
 
 Misty Logs are located at:
 \\<MistyIp>\c$\Data\Misty\Logs

 Skill Logs are located at:
 \\<MistyIp>\c$\Data\Misty\SDK\Logs
	* I think they delete after 7 days or so (on the next run).

Dialog information is saved in files in specific folders under the Misty folder. Instead of calling Misty with a command, you can also copy files from one Misty to another to allow that Misty to handle the instance.

After pushing new code to debug, use Force Reload on the Skill Management page, or re-stop, wait for its cleanup to happen, and the start the skill to ensure new version is loaded on the robot.

-----------------------------------------------
WIRED MISTY

To directly access Misty, you can use an ethernet cable and two USB-to-ethernet adapters to connect directly from a computer to Misty. If you are a developer, this can also be a much faster way to push skills to Misty.

Plug in the cable to your computer and Misty's USB port on her back, and go to the device manager's Connectivity -> Network Tab and look for the USB to Ethernet IP.

-----------------------------------------------
UPDATING WEB TOOLS
	
Updating the hosted web tools files on your bot at \\<RobotIP>\c$\Data\Misty\Web\SDK will cause the hosted web tools to use that page instead after a reboot (and clearing the browser cache or using an incognito browser).

-----------------------------------------------
DEVICE MANAGER

Going to \\<MistyIp>:8080 will give you the device manager (you'll need to log in - your passwoprd should be on the bottom of your Misty) where you can manage the device a little more directly. Be careful if you are here as you can uninstall Misty!
	* To use some of the features in the device manager (like performance tracing and app management) you need to reload the page and log in again at the ethernet cable's IP (not wifi). You can find that on the device manager's Connectivity -> Network Tab, look for the USB to Ethernet IP.