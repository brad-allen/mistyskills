using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;
using Weather.OpenWeather;

namespace MysticModesManagement
{
    public class WeatherModePackage : BaseAllModesPackage
    {
        public override event EventHandler<PackageData> CallSwitchMode;
        public WeatherModePackage(IRobotMessenger misty) : base(misty) {}
        public EnglishWeatherManager _weatherManager;

        public override async Task<ResponsePacket> Start(PackageData packageData)
        {
            await base.Start(packageData);

            if(packageData.Parameters.TryGetValue("OpenWeatherKey", out object code))
            {
                try
                {
                    _ = Misty.SpeakAsync("Getting the weather.", true, "WeatherPhraseStart");

                    if(!packageData.Parameters.TryGetValue("Country", out object countryCode))
                    {
                        countryCode = "US";
                    }

                    if (!packageData.Parameters.TryGetValue("City", out object cityCode))
                    {
                        cityCode = "Boulder";
                    }

                    _weatherManager = new EnglishWeatherManager(Misty, (string)code, null, (string)countryCode, (string)cityCode);

                    //Fixes around async still, for now, give it a moment to get weather...
                    await Task.Delay(2500);

                    _ = Misty.SpeakAsync(_weatherManager.GetWeatherString(), true, "WeatherPhraseSaid");
                }
                catch (Exception ex)
                {
                    _ = Misty.SpeakAsync("Sorry, I couldn't get the weather with your key.", true, "WeatherPhraseSaid");
                    Misty.SkillLogger.Log("Failed getting open weather info with key", ex);
                }

            }
            else
            {
                _ = Misty.SpeakAsync("Sorry, I need an open weather key to tell you the weather.", true, "WeatherPhraseSaid");
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
            samples.Add("weather");
            samples.Add("whether");
            samples.Add("ether");
            samples.Add("the temperature");
            samples.Add("cold outside");
            samples.Add("hot outside");

            intent = new Intent
            {
                Name = "weather",
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
                (string)robotInteractionEvent.DialogState.Data == "WeatherPhraseSaid"))
            {

                PackageData pd = new PackageData(MysticMode.Weather, "Idle")
                {
                    ModeContext = PackageData.ModeContext,
                    Parameters = PackageData.Parameters
                };

                CallSwitchMode?.Invoke(this, pd);
                return;
            }
        }
    }
}