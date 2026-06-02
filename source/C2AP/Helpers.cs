using Archipelago.Core.Models;
using Archipelago.Core.Util;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using static C2AP.Models.Enums;
using Location = Archipelago.Core.Models.Location;
namespace C2AP
{
    public class Helpers
    {
        private static GameStatus lastNonZeroStatus = GameStatus.Spawning;
        public static bool lastInGameStatus = false;

        private static Timer checkEmulation = new Timer(100);

        private static uint previousTime = 0;
        private static bool isEmulationPaused = true;
        public static string OpenEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string jsonFile = reader.ReadToEnd();
                return jsonFile;
            }
        }

        public static void StartCheckEmulationPaused()
        {
            checkEmulation.Elapsed += (s, ev) =>
            {
                uint time = Memory.ReadUInt(Addresses.Timer);
                isEmulationPaused = time == previousTime;
                previousTime = time;
                //uint crashAddress = CrashObject.FindObjectAddress(0, 0);
                //if (crashAddress != 0 && crashAddress != CrashObject.cacheOffset)
                //{
                //    Log.Logger.Information($"crash state: {Memory.ReadUInt(crashAddress + 0x1C)}");
                //}
            };
            checkEmulation.Start(); 
        }
        
        public static bool IsEmulationPaused()
        {
            return isEmulationPaused;
        }
        public static bool IsGamePaused()
        {
            return Memory.ReadUInt(Addresses.PausedFlag) != 0;
        }

        public static int GetOptionValue(string optionName)
        {
            App.Client.Options.TryGetValue(optionName, out var optionValue);
            if (optionValue == null)
            {
                Log.Logger.Error($"{optionName} option null");
                return -1;
            }
            Log.Information($"{optionName} option : {optionValue}");
            return Convert.ToInt32(optionValue.ToString());
        }
        public static bool IsInGame()
        {
            //Log.Debug($"Text: {Addresses.StaticText}");
            //Log.Debug($"Text: {Memory.ReadString(Addresses.StaticTextAddress, 0x50)}");
            if (Addresses.StaticText.Contains(Memory.ReadString(Addresses.StaticTextAddress, 0x50)))
            {
                //Log.Debug($"Text: true");
                if (!lastInGameStatus)
                {
                    //Log.Information("Entered in-game state");
                    BaseHooks.Initialize();
                }
                lastInGameStatus = true;
                return true;
            }
            //Log.Debug($"Text: false");
            if (lastInGameStatus)
            {
                //Log.Information("Exited in-game state");
                BaseHooks.UnInitialize();
            }
            lastInGameStatus = false;
            return false;
        }
        
        public static bool IsInDemo()
        {
            return Memory.ReadUInt(Addresses.DemoPointer) != 0;
        }
        public static List<ILocation> BuildLocationList()
        {
            //int id = 10000;
            List<ILocation> locations = new List<ILocation>();
            uint address;
            int bit;
            string category;
            foreach (string locName in Addresses.LocationIdInApWorld.Keys)
            {
                Location loc;
                if (locName.Contains("Gem"))
                {
                    address = Addresses.GemLocationsAddress;
                    bit = Addresses.BitOfLocation[locName];
                    category = "Gem";
                }
                else if (locName.Contains("Crystal"))
                {
                    address = Addresses.CrystalLocationsAddress;
                    bit = Addresses.BitOfLocation[locName];
                    category = "Crystal";
                }
                else if (locName.Contains("Polar"))
                {
                    address = Addresses.PolarLivesAddress;
                    bit = Addresses.PolarLivesBit;
                    category = "Misc";
                }
                else if (locName.Contains("Secret"))
                {
                    address = Addresses.SecretExitsAddress;
                    bit = Addresses.BitOfLocation[locName];
                    category = "Secret Exit";
                }
                else if (locName.Contains("Exit"))
                {
                    address = Addresses.LevelExitsAddress;
                    bit = Addresses.levelNameToId[locName.Replace(" Exit", "")];
                    category = "Exit";
                }
                else //if (locName.Contains("Defeated"))
                {
                    address = Addresses.LevelExitsAddress;
                    bit = Addresses.levelNameToId[locName.Replace(" Defeated", "")];
                    category = "Boss Defeated";
                }
                

                address += (uint)(bit / 8);
                bit = bit % 8;

                loc = new Location
                {
                    Name = locName,
                    Address = address,
                    AddressBit = bit,
                    CheckType = LocationCheckType.Bit,
                    Category = category,
                    Id = Addresses.LocationIdInApWorld[locName],
                };
                locations.Add(loc);
            }

            //Adding these "fake" locations so that CheckGoalCondition() can be executed
            locations.Add(new Location
            {
                Name = "Normal Ending",
                Address = Addresses.LevelIdAddress,
                CheckType = LocationCheckType.Int,
                CheckValue = "10496" //0x2900 == normal ending level id
            });
            locations.Add(new Location
            {
                Name = "100% Ending",
                Address = Addresses.LevelIdAddress,
                CheckType = LocationCheckType.Int,
                CheckValue = "10240" //0x2800 == 100% ending level id
            });

            //if (FruitCheck.Bundles != null)
            //{
            //    foreach (FruitCheck.FruitBundle bundle in FruitCheck.Bundles)
            //    {
            //        locations.Add(new Location
            //        {
            //            Name = bundle.locationName,
            //            Id = bundle.locationId,
            //        });
            //    }
            //}
            

            return locations;
        }

        //public static void ClearHookMemory()
        //{
        //    Memory.WriteByteArray(0xf000, new byte[0x0fff]);
        //}
    }
}
