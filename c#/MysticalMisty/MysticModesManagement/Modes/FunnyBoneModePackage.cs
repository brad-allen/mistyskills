using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FunnyBone;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;

namespace MysticModesManagement
{
    public class FunnyBoneModePackage : BaseAllModesPackage
    {
        public override event EventHandler<PackageData> CallSwitchMode;
        public FunnyBoneModePackage(IRobotMessenger misty) : base(misty) { }
        public FunnyBoneAPI _funnyBone = new FunnyBoneAPI();
        private Random _random = new Random();

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            try
            {
                await base.Start(packageData);
                string _theJoke = "";
                if (_random.Next(0,3) < 2)
                {
                    SingleJokeFormat sjf = await _funnyBone.GetDeveloperJoke();
                    _theJoke = sjf.Joke;
                }
                else
                {
                    //ands throw in a chuck norris joke once in a while
                    ChuckNorrisJokeFormat cnjf = await _funnyBone.GetChuckNorrisJoke();
                    _theJoke = cnjf.Value;
                }


                if(string.IsNullOrWhiteSpace(_theJoke))
                {
                    _theJoke = "Couldn't find a joke, sorry. Please ensure I am connected to the internet and try again.";
                }

                await Misty.SpeakAsync(_theJoke, true, "FunnyBoneSaid");

            }
            catch (Exception ex)
            {
                _ = Misty.SpeakAsync("Sorry, I had trouble while performing a joke request.", true, "FunnyBoneSaid");
                Misty.SkillLogger.Log("Failed getting joke.", ex);
            }

            return new ResponsePacket { Success = true };
        }

        public override async Task<ResponsePacket> Stop()
        {
            return await Task.FromResult(new ResponsePacket { Success = true });
        }

        public override bool TryGetIntentTrigger(out Intent intent)
        {
            List<string> samples = new List<string>();
            samples.Add("funny bone");
            samples.Add("joke");
            samples.Add("make me laugh");
            samples.Add("laugh");
            samples.Add("tell me a joke");
            
            intent = new Intent
            {
                Name = "funnybone",
                Samples = samples,
                Entities = new List<Entity>()
            };
            return true;
        }

        public override void RobotInteractionCallback(IRobotInteractionEvent robotInteractionEvent)
        {
            if ((robotInteractionEvent.Step == RobotInteractionStep.CapTouched && robotInteractionEvent.CapTouchState.Scruff == TouchSensorOption.Contacted) ||
                (robotInteractionEvent.Step == RobotInteractionStep.Dialog &&
                robotInteractionEvent.DialogState.Step == DialogActionStep.CompletedSpeaking &&
                (string)robotInteractionEvent.DialogState.Data == "FunnyBoneSaid"))
            {

                PackageData pd = new PackageData(MysticMode.FunnyBone, "Idle")
                {
                    ModeContext = PackageData.ModeContext,
                    Parameters = PackageData.Parameters
                };

                CallSwitchMode?.Invoke(this, pd);
            }
        }
    }
}