using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MistyRobotics.Common.Data;
using MistyRobotics.Common.Types;
using MistyRobotics.SDK;
using MistyRobotics.SDK.Events;
using MistyRobotics.SDK.Messengers;
using MysticCommon;
using SkillTools.AssetTools;

namespace MysticModesManagement
{
    public class ModeCommon
    {
        private static PackageData _packageData;
        
        private static ModeCommon _modeCommon;
        private static IRobotMessenger _misty;
        public const string EqualizerLayerName = "equalizer-layer";
        public const string WarningLayerName = "warning-layer";

        private ModeCommon(IRobotMessenger misty)
        {
            _misty = misty;
        }

        public static ModeCommon LoadCommonOptions(IRobotMessenger misty)
        {
            if(_modeCommon == null)
            {
                _modeCommon = new ModeCommon(misty);
            }
            return _modeCommon;
        }

        public async Task<bool> ShowWarningLayer()
        {
            await _misty.SetTextDisplaySettingsAsync(WarningLayerName,
                   new TextSettings
                   {
                       Height = 80,
                       HorizontalAlignment = ImageHorizontalAlignment.Center,
                       VerticalAlignment = ImageVerticalAlignment.Bottom,
                       Weight = 60,
                       Visible = true,
                       PlaceOnTop = true,
                       Red = 255,
                       Green = 94,
                       Blue = 14,
                       Style = ImageStyle.Normal,
                       Size = 20
                   }
                );
            return true;
        }

        public async Task<bool> DeleteWarningLayer()
        {
            await _misty.SetTextDisplaySettingsAsync(WarningLayerName,
                   new TextSettings
                   {
                       Deleted = true
                   }
                );

            return true;
        }

        public async Task<bool> HideWarningLayer()
        {
            await _misty.SetTextDisplaySettingsAsync(WarningLayerName,
                   new TextSettings
                   {
                       Visible = false
                   }
                );

            return true;
        }

        public async Task<bool> WriteToWarningLayer(string text)
        {
            await ShowWarningLayer();
            await _misty.DisplayTextAsync(text, WarningLayerName);
            return true;
        }

        public async Task SetEqualizerSpeech(IRobotInteractionEvent robotInteractionEvent)
        {
            //Example of base mode adding some functionality
            //In this case, adding the equaliazer to show when Misty is speaking
            if (robotInteractionEvent.DialogState?.Step == DialogActionStep.StartedSpeaking)
            {
                await _misty.SetImageDisplaySettingsAsync(EqualizerLayerName, new ImageSettings
                {
                    Visible = true
                });
                _ = _misty.DisplayImageAsync("equalizer2.gif", EqualizerLayerName, false);
            }
            else if (robotInteractionEvent.DialogState?.Step == DialogActionStep.CompletedSpeaking)
            {
                await _misty.SetImageDisplaySettingsAsync(EqualizerLayerName, new ImageSettings
                {
                    Visible = false
                });
            }
        }

        public async Task<bool> ShowEqualizerLayer()
        {
            await _misty.SetImageDisplaySettingsAsync(EqualizerLayerName, new ImageSettings
            {
                Height = 40,
                VerticalAlignment = MistyRobotics.Common.Types.ImageVerticalAlignment.Bottom,
                HorizontalAlignment = MistyRobotics.Common.Types.ImageHorizontalAlignment.Center,
                PlaceOnTop = false,
                Visible = true
            });
            return true;
        }

        public async Task<bool> DeleteEqualizerLayer()
        {
            await _misty.SetImageDisplaySettingsAsync(EqualizerLayerName, new ImageSettings
            {
                Deleted = false
            });
            return true;
        }

        public async Task<bool> HideEqualizerLayer()
        {
            await _misty.SetImageDisplaySettingsAsync(EqualizerLayerName, new ImageSettings
            {
                Visible = false
            });
            return true;
        }
    }
}