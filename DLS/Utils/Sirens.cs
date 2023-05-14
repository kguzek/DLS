using System.Collections.Generic;
using System.Linq;
using Rage;
using Rage.Native;

namespace DLS.Utils
{
    internal static class Sirens
    {
        private static readonly SoundSettings DefaultSoundSettings = new SoundSettings()
        {
            Tone1 = "VEHICLES_HORNS_SIREN_1",
            Tone2 = "VEHICLES_HORNS_SIREN_2",
            Tone3 = "VEHICLES_HORNS_POLICE_WARNING",
            Tone4 = "VEHICLES_HORNS_AMBULANCE_WARNING",
            Horn = "SIRENS_AIRHORN"
        };

        private static readonly IEnumerable<uint> additionalSahpHashes = GetAdditionalSahpHashes();

        private static IEnumerable<uint> GetAdditionalSahpHashes()
        {
            List<string> modelNames = Settings.ReadKeyArray("Settings", "SahpSirenModels").ToList();
            IEnumerable<uint> hashes = modelNames.Select(modelName => Game.GetHashKey(modelName));
            return hashes;
        }

        public static void Update(ActiveVehicle activeVeh, bool dls = true)
        {
            string soundName;
            SoundSettings soundSettings = DefaultSoundSettings;
            if (dls)
            {
                DLSModel dlsModel = activeVeh.Vehicle.GetDLS();
                if (dlsModel != null)
                {
                    // Use the model's sound config if it's set
                    soundSettings = dlsModel.SoundSettings;
                }
            } else
            {
                uint modelHash = activeVeh.Vehicle.Model.Hash;
                if (additionalSahpHashes.Contains(modelHash))
                {
                    // Replace sound siren names with police bike sirens
                    soundSettings.Tone1 = "sirens_slow_dir4";
                    soundSettings.Tone2 = "fast_9oghrv1";
                }
            }

            switch (activeVeh.SirenStage)
            {
                case SirenStage.Off:
                    Sound.NewSoundID(activeVeh);
                    return;
                case SirenStage.One:
                    soundName = soundSettings.Tone1;
                    break;
                case SirenStage.Two:
                    soundName = soundSettings.Tone2;
                    break;
                case SirenStage.Warning:
                    soundName = soundSettings.Tone3;
                    break;
                case SirenStage.Warning2:
                    soundName = soundSettings.Tone4;
                    break;
                case SirenStage.Horn:
                    soundName = soundSettings.Horn;
                    break;
                default:
                    return;
            }

            NativeFunction.Natives.PLAY_SOUND_FROM_ENTITY(Sound.NewSoundID(activeVeh), soundName, activeVeh.Vehicle, 0, 0, 0);
        }

        public static void MoveUpStage(ActiveVehicle activeVeh, bool isDLS = false, bool isMan = false)
        {
            if (isDLS)
            {
                activeVeh.SirenStage = GetNextStage(activeVeh.SirenStage);
            }
            else
            {
                switch (activeVeh.SirenStage)
                {
                    case SirenStage.Off:
                        activeVeh.SirenStage = SirenStage.One;
                        break;
                    case SirenStage.One:
                        activeVeh.SirenStage = SirenStage.Two;
                        break;
                    case SirenStage.Two:
                        activeVeh.SirenStage = SirenStage.Warning;
                        break;
                    case SirenStage.Warning:
                        activeVeh.SirenStage = SirenStage.Warning2;
                        break;
                    case SirenStage.Warning2:
                        activeVeh.SirenStage = SirenStage.Off;
                        break;
                }
            }
            Update(activeVeh, isDLS);
        }

        public static SirenStage GetNextStage(SirenStage sirenStage)
        {
            if (sirenStage != SirenStage.Warning2)
            {
                return sirenStage + 1;
            }
            else
            {
                return SirenStage.One;
            }
        }
        public static SirenStage GetNextStage(SirenStage sirenStage, DLSModel vehDLS)
        {
            return vehDLS.AvailableSirenStages.NextSirenStage(sirenStage, false);
        }
    }
}