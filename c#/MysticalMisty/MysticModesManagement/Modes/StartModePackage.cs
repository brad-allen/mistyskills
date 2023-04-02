using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Commands;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MistyRobotics.SDK.Responses;
using MysticCommon;
using MysticModesManagement.Conversations;


namespace MysticModesManagement
{
    public class StartModePackage : BaseAllModesPackage
    {
        public override event EventHandler<PackageData> CallSwitchMode;

        public StartModePackage(IRobotMessenger misty) : base(misty) {}

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            await base.Start(packageData);
            await CreateActions();
            Misty.StartAction(MysticWake, false, null);
            await Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
            Misty.Speak("Okay, when you are ready, say, Hey Misty, and tell me what you want to do.", true, "sayheyMisty", null);
            return new ResponsePacket { Success = true };
        }

        public override async Task<ResponsePacket> Stop()
        {
            //await BreakdownMode();
            return await Task.FromResult(new ResponsePacket { Success = true });   
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("start");
            samples.Add("begin");

            intent = new Intent
            {
                Name = "Start",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        protected const string MysticWake = "mystic-wake";

        private async Task CreateActions()
        {
            //TODO Move this to common area

            //To get a list of the action commands misty knows // see list at bottom of page // alpha functionality!!
            //IGetActionCommandsResponse actionCommands = await Misty.GetActionCommandsAsync();

            //Create action using action commands
            await Misty.CreateActionAsync(MysticWake,
 @"HEAD:0,0,0,1000;
ARMS:-55,0,500;
LED-PATTERN:255,0,0,0,0,255,1000,breathe;
PAUSE:500;
ARMS:0,-55,500;
PAUSE:500;
ARMS:-55,0,500;
PAUSE:500;
ARMS:0,-55,500;
HEAD:-20,5,20,500;
LED-PATTERN:255,0,0,255,0,0,1000,breathe;
PAUSE:500;
ARMS:-55,90,500;
HEAD:-20,5,-20,1200;
PAUSE:1200;
HEAD:-20,0,0,750;
ARMS:90,90,750;
PAUSE:750;
ARMS:-55,-55,500;
HEAD:-100,0,0,500;
LED-PATTERN:255,0,0,0,0,255,1000,transitonce;
", true);
        }

        public override void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            //Process
            try
            {
                //Note, to get Dialog events, you must be in a conversation at this time, otherwise use the voicecommand callbacks
                if (robotInteractionEvent.Step == RobotInteractionStep.Dialog && robotInteractionEvent.DialogState?.Step == MistyRobotics.Common.Types.DialogActionStep.FinalIntent)
                {
                    if (string.IsNullOrWhiteSpace(robotInteractionEvent.DialogState.Text))
                    {
                        _ = Misty.SpeakAsync($"I didn't hear anything. Say, Hey Misty, and try again!", true, "StartPackageRetry");
                        _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
                    }
                    else if (robotInteractionEvent.DialogState.Intent.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                    {
                        _ = Misty.SpeakAsync($"Sorry! I didn't understand that request. Did you say {robotInteractionEvent.DialogState.Text}? Say hey misty and try again.", true, "StartPackageRetry2");
                        _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
                    }
                    else if (robotInteractionEvent.DialogState.Contexts.Contains("all-modes") && !robotInteractionEvent.DialogState.Intent.Equals("start", StringComparison.OrdinalIgnoreCase))
                    {
                        PackageData pd = new PackageData(MysticMode.Start, robotInteractionEvent.DialogState.Intent)
                        {
                            ModeContext = PackageData.ModeContext,
                            Parameters = PackageData.Parameters
                        };

                        CallSwitchMode?.Invoke(this, pd);
                    }
                    else
                    {
                        _ = Misty.SpeakAsync($"Sorry! I didn't understand that request. Did you say {robotInteractionEvent.DialogState.Text}? Say hey misty and try again.", true, "StartPackageRetry2");
                        _ = Misty.StartKeyPhraseRecognitionVoskAsync(true, 20000, 4000);
                    }
                }
                else if (robotInteractionEvent.Step == RobotInteractionStep.BumperPressed)
                {
                    _ = Misty.SpeakAsync($"You pressed a bumper.", true, "BumperPress");
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
            }            
        }
    }
}

/*
 * 
 * {
 "result": [
  {
   "description": "Moves the arms.",
   "fieldHint": "left-degrees,right-degrees,duration(ms)",
   "name": "ARMS"
  },
  {
   "description": "Moves the head.",
   "fieldHint": "pitch-degrees,roll-degrees,yaw-degrees,duration(ms)",
   "name": "HEAD"
  },
  {
   "description": "Displays an image on the default image layer.",
   "fieldHint": "filename(string),alpha(double)",
   "name": "IMAGE"
  },
  {
   "description": "Pauses the script.",
   "fieldHint": "duration(ms)",
   "name": "PAUSE"
  },
  {
   "description": "Runs a color pattern on the LED.",
   "fieldHint": "red(byte),green(byte),blue(byte)red2(byte),green2(byte),blue2(byte),type(breathe,blink,transitonce),time(ms)",
   "name": "LED-PATTERN"
  },
  {
   "description": "Changes the volume.",
   "fieldHint": "volume(0-100)",
   "name": "VOLUME"
  },
  {
   "description": "Changes the LED.",
   "fieldHint": "red(byte),green(byte),blue(byte)",
   "name": "LED"
  },
  {
   "description": "Turns the light on and off.",
   "fieldHint": "on,off,true,false(string)",
   "name": "LIGHT"
  },
  {
   "description": "Publishes an event through websockets as 'UserSkillData.'",
   "fieldHint": "message(string),optional-data(string)",
   "name": "PUBLISH"
  },
  {
   "description": "Takes a picture and displays it on the screen.",
   "fieldHint": "save-as-image-name(string)",
   "name": "PICTURE"
  },
  {
   "description": "Plays an audio file.",
   "fieldHint": "filename, volume(0-100 or leave empty to keep current volume)",
   "name": "AUDIO"
  },
  {
   "description": "Stops a playing an audio file, but not TTS.",
   "fieldHint": "",
   "name": "STOP-AUDIO"
  },
  {
   "description": "Pauses a playing audio file, but not TTS.",
   "fieldHint": "",
   "name": "PAUSE-AUDIO"
  },
  {
   "description": "Drives the specified arc.",
   "fieldHint": "heading(degrees),radius(meters),time(ms),reverse(true,false)",
   "name": "ARC"
  },
  {
   "description": "Turns to the specified heading.",
   "fieldHint": "heading(degrees),time(ms),reverse(true,false)",
   "name": "TURN-HEADING"
  },
  {
   "description": "Start recording audio.",
   "fieldHint": "filename(string)",
   "name": "RECORD"
  },
  {
   "description": "Stop recording audio.",
   "fieldHint": "",
   "name": "STOP-RECORDING"
  },
  {
   "description": "Drives to the heading.",
   "fieldHint": "heading(degrees),distance(meters),time(ms),reverse(true,false)",
   "name": "HEADING"
  },
  {
   "description": "Changes the voice.",
   "fieldHint": "voice-name(string)",
   "name": "SET-VOICE"
  },
  {
   "description": "Changes the speech rate.",
   "fieldHint": "rate(double - default 1.0)",
   "name": "SET-SPEECH-RATE"
  },
  {
   "description": "Changes the pitch.",
   "fieldHint": "pitch(double - default 1.0)",
   "name": "SET-SPEECH-PITCH"
  },
  {
   "description": "Speaks with the current settings, but does not start a new state.",
   "fieldHint": "text-to-say(string)",
   "name": "SPEAK"
  },
  {
   "description": "Speaks and listens with the current settings.",
   "fieldHint": "text-to-say(string)",
   "name": "SPEAK-AND-LISTEN"
  },
  {
   "description": "Speaks with the current settings and awaits response until done. Does not start a new state. Max of 60 second wait.",
   "fieldHint": "text-to-say(string)",
   "name": "SPEAK-AND-WAIT"
  },
  {
   "description": "Stops onboard TTS, but not audio files.",
   "fieldHint": "",
   "name": "STOP-SPEAKING"
  },
  {
   "description": "Stops following faces and objects.",
   "fieldHint": "",
   "name": "STOP-FOLLOW"
  },
  {
   "description": "Starts following faces.",
   "fieldHint": "face(optional)",
   "name": "FOLLOW-FACE"
  },
  {
   "description": "Starts following objects.",
   "fieldHint": "object(string),minimum confidence(double 0-1.0),tracker history(int)",
   "name": "FOLLOW-OBJECT"
  },
  {
   "description": "Starts a skill.",
   "fieldHint": "skillid",
   "name": "START-SKILL"
  },
  {
   "description": "Stops a skill.",
   "fieldHint": "skillid",
   "name": "STOP-SKILL"
  }
 ],
 "status": "Success"
}
 */