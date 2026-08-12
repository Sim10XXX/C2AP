using Archipelago.Core.Models;
using Archipelago.Core.Util;
using DynamicData;
using Serilog;
using Silk.NET.GLFW;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
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
        private static Timer checkLifeCount = new Timer(1000);
        private static Timer checkConnectionIntegrity = new Timer(1000);
        private static CustomHook connectionHook = new CustomHook([
            "nop",
        ]);

        private static uint previousTime = 0;
        private static bool isEmulationPaused = true;

        private static ushort seed = 0;
        private static bool connectionValid = false;
        private static bool shouldCheckConnection = false;

        public static bool shouldSyncProgress = false; // for use in connection integrity

        public const uint lifeCountBaseId = 1000;

        private static string slotName = "";

        public static void StartCheckLifeCount()
        {
            if (checkLifeCount.Enabled) return;
            //GetOptionValue();
            //App.Client.Options.TryGetValue("life_count_checks", out var optionValue);
            //if (optionValue == null)
            //{
            //    Log.Logger.Error($"life_count_checks option null");
            //    return;
            //}
            //Log.Information($"life_count_checks option : {optionValue}");
            //return Convert.ToInt32(optionValue.ToString());
            List<uint> checks = GetSlotDataList("life_count_checks").ConvertAll(x => (uint)x);
            //Log.Information($"life_count_checks option : {string.Join(", ", checks)}");
            if (checks.Count == 0)
            {
                //Log.Information($"No life count checks configured, skipping life count check");
                return;
            }
            App.crashState.LifeCountChecks = checks.ToArray();

            checkLifeCount.Elapsed += (s, ev) =>
            {
                uint crashAddress = CrashObject.FindObjectAddress(0, 0);
                if (crashAddress != 0 && crashAddress != CrashObject.cacheOffset)
                {
                    uint lifeCount = Memory.ReadByte(crashAddress + Addresses.LivesOffset);
                    //Log.Information($"Life count: {lifeCount}");
                    if (lifeCount > App.crashState.MaxLifeCount)
                    {
                        for (uint i = 0; i < App.crashState.LifeCountChecks.Length; i++)
                        {
                            uint lifeCountCheck = App.crashState.LifeCountChecks[i];
                            if (lifeCountCheck > App.crashState.MaxLifeCount)
                            {
                                if (lifeCountCheck > lifeCount) break;
                                App.Client.SendLocation(new Location
                                {
                                    Name = $"Collect {lifeCountCheck} Lives",
                                    Id = (int) (lifeCountBaseId + lifeCountCheck),
                                });
                                //Log.Information($"Sent life count check for {lifeCountCheck} lives");
                            }
                        }
                        App.crashState.MaxLifeCount = lifeCount;
                    }
                }
            };
            checkLifeCount.Start();
        }

        public static void StartCheckEmulationPaused()
        {
            if (checkEmulation.Enabled) return;

            previousTime = Memory.ReadUInt(Addresses.Timer);
            checkEmulation.Elapsed += (s, ev) =>
            {
                uint time = Memory.ReadUInt(Addresses.Timer);
                isEmulationPaused = time == previousTime;
                previousTime = time;
                //Log.Information($"Time: {time}, Previous Time: {previousTime}");
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

        public static void StartCheckConnectionIntegrity()
        {
            //Log.Information($"Seed: {GetSlotData("seed")}");
            seed = unchecked((ushort)GetSlotData("seed"));
            //seed += Addresses.CacheOffset;
            HookManager.AddHook(connectionHook, 0x15A20);
            HookManager.ReplaceAsm(connectionHook,[
                $"la $t0, 0x{Addresses.ConnectionCheck + Addresses.CacheOffset:X}",
                //$"la $t1, 0x{seed:X}",
                $"addiu $t1, $zero, 0x{seed:X}",
                "sw $t1, 0($t0)",
            ]);
            uint currentSeed = Memory.ReadUInt(Addresses.ConnectionCheck);
            if (currentSeed != 0 && currentSeed != seed)
            {
                Log.Error("Connected into a game from a different session");
            }

            //checkConnectionIntegrity.Elapsed += (s, ev) =>
            //{
            //    IsConnectionValid();
            //};
            //checkConnectionIntegrity.Start();
            shouldCheckConnection = true;
        }

        public static bool IsConnectionValid()
        {
            if (!shouldCheckConnection)
            {
                //Log.Warning($"Connection check skipped, connectionvalid: {connectionValid}");
                return connectionValid;
            }
            if (isEmulationPaused)
            {
                //Log.Warning("Emulation is paused, connection check skipped");
                return false;
            }
            uint currentSeed = Memory.ReadUInt(Addresses.ConnectionCheck);
            if (currentSeed == 0)
            {
                if (connectionValid)
                {
                    shouldCheckConnection = false;
                    shouldSyncProgress = true;
                    
                    if (IsInGame())
                    {
                        Log.Error("Connection interrupted due to loading of save state");
                    }
                    else
                    {
                        Log.Error("Connection interrupted due to console reset");
                    }
                    connectionValid = false;
                    return connectionValid;
                }
                Log.Error("Connection check failed, make sure DuckStation's execution mode is set to 'Interpreter'");
                return connectionValid;
            }
            if (currentSeed != seed)
            {
                shouldCheckConnection = false;
                shouldSyncProgress = true;
                if (connectionValid)
                {
                    Log.Error("Loaded into a save state from a different session");
                    connectionValid = false;
                    return connectionValid;
                }
                Log.Error($"Seed mismatch detected: {currentSeed} != {seed}");
                return connectionValid;
            }
            connectionValid = true;
            //Log.Information("Connection check passed");
            return connectionValid;
        }

        public static int GetOptionValue(string optionName)
        {
            App.Client.Options.TryGetValue(optionName, out var optionValue);
            if (optionValue == null)
            {
                Log.Logger.Error($"{optionName} option null");
                return -1;
            }
            //Log.Information($"{optionName} option : {optionValue}");
            return Convert.ToInt32(optionValue.ToString());
        }

        public static int GetSlotData(string slotName)
        {
            App.SlotData.TryGetValue(slotName, out var slotValue); //2,000,000,000
            if (slotValue == null)
            {
                Log.Logger.Error($"{slotName} slot null");
                return -1;
            }

            //Log.Information($"{slotName} option : {slotValue}");
            
            // prevent overflow when converting to int, since the seed is going to be a very large number
            return Convert.ToInt32(slotValue.ToString().Substring(0,Math.Min(9, slotValue.ToString().Length)));
        }

        public static List<int> GetSlotDataList(string slotName)
        {
            App.SlotData.TryGetValue(slotName, out var slotValue);
            if (slotValue == null)
            {
                Log.Logger.Error($"{slotName} slot null");
                return [];
            }
            var value = slotValue.ToString();
            if (value == null)
            {
                return [];
            }
            
            var valueList = value.Trim('[', ']').Split(',');
            List<int> resultList = [];
            if (valueList.Length == 1)
            {
                //Log.Information($"valuelist: {valueList[0]}");
                if (valueList[0] == "")
                {
                    return [];
                }
            }            
            foreach (var item in valueList)
            {
                resultList.Add(Convert.ToInt32(item.Trim().Trim('"')));
                //Log.Information($"Adding : {item.Trim().Trim('"')}");
            }
            return resultList;
        }

        public static bool IsInGame()
        {
            //Log.Debug($"Text: {Addresses.StaticText}");
            //Log.Debug($"Text: {Memory.ReadString(Addresses.StaticTextAddress, 0x50)}");

            bool check1 = Addresses.StaticText.Contains(Memory.ReadString(Addresses.StaticTextAddress, 0x50));
            //bool check2 = !IsInDemo();
            //bool check3 = Memory.ReadUInt(Addresses.LevelIdAddress) != 0x3C00; // is not on the title screen
            if (check1)
            {
                //Log.Debug($"Text: true, level: {Memory.ReadUInt(Addresses.LevelIdAddress):X}");
                if (!lastInGameStatus)
                {
                    lastInGameStatus = true;
                    Log.Information("Entered in-game state");
                    //BaseHooks.Initialize();
                    InitializeAll(slotName);
                }
                uint levelId = Memory.ReadUInt(Addresses.LevelIdAddress);
                if (levelId != 0x3C00)
                {
                    if (!IsInDemo())
                    {
                        if (shouldSyncProgress)
                        {
                            App.UpdateCrashState();
                            Log.Information("Updating crash state");
                            if (!shouldCheckConnection)
                            {
                                shouldCheckConnection = true;
                                Log.Information("Resuming connection check");
                            }
                        }
                    }
                }
                else
                {
                    shouldSyncProgress = true;
                }
                

                return true;
            }
            //Log.Debug($"Text: false");
            if (lastInGameStatus)
            {
                Log.Error("Exited in-game state (console was reset)");
                Log.Error("Please restart the client since most features will not work correctly");
                shouldSyncProgress = true;
                //BaseHooks.UnInitialize();
            }
            lastInGameStatus = false;
            Log.Warning("Not in game");
            return false;
        }
        
        public static bool IsInDemo()
        {
            return Memory.ReadUInt(Addresses.DemoPointer) != 0;
        }

        public static void InitializeAll(string slot)
        {
            HookManager.ClearHooks();
            HookManager.ClearHooksFromPreviousConnection();
            BaseHooks.Initialize();
            WarpRoomRandomizer.Initialize();
            CrashDeathLink.Initialize(slot);
            slotName = slot;

            InputLock.Initialize();

            InputLock.LockInput(InputFlag.All);
            InputLock.UnlockInput(InputFlag.All);

            CrashEvent.Initialize();
            Traps.Initialize();
            CrashObjectMod.Initialize();
            GimmickLock.Initialize();
            Helpers.StartCheckEmulationPaused();
            Helpers.StartCheckLifeCount();
            Helpers.StartCheckConnectionIntegrity();

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
