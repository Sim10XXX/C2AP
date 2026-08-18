using Archipelago.Core.Util;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace C2AP
{
    internal class WarpRoomRandomizer
    {
        private static CustomHook? WarpLevelOverridesLeveldHook;

        private static readonly int[] OriginalMontyHallDestinations = [
            0x18, 0x1F, 0x19, 0x0E, 0x1E,
            0x23, 0x1B, 0x1D, 0x20, 0x11,
            0x17, 0x16, 0x22, 0x0A, 0x21,
            0x24, 0x0F, 0x13, 0x15, 0x0D,
            0x26, 0x1A, 0x0C, 0x12, 0x10,
            0x27, 0x25, 0x116, 0x10E, 0x120
        ];

        private static readonly int[] OriginalMontyHallSpawnList = {
            0x19, 0x0E, 0x1E, 0x00, 0x18, 0x1F,
            0x1D, 0x20, 0x11, 0x23, 0x1B,
            0x22, 0x0A, 0x21, 0x17, 0x16,
            0x13, 0x15, 0x0D, 0x24, 0x0F,
            0x0C, 0x12, 0x10, 0x26, 0x1A,
            0x2D, 0x2B, 0x2C, 0x2F, 0x2E
        };

        public static int[] MontyHallDestinations = new int[OriginalMontyHallDestinations.Length];
        public static int[] MontyHallSpawnList = new int[OriginalMontyHallSpawnList.Length];

        public static int GameLevelIdToApWorldSecretLevelId(int id)
        {
            return id switch
            {
                0x2B => 0x10E,// snow go secret entrance
                0x2C => 0x120,// air crash secret entrance
                0x2D => 0x116,// road to ruin secret entrance
                0x2E => 0x25,// totally bear
                0x2F => 0x27,// totally fly
                _ => id,
            };
        }

        public static int ApWorldSecretLevelIdToGameLevelId(int id)
        {
            return id switch
            {
                0x10E => 0x2B,// snow go secret entrance
                0x120 => 0x2C,// air crash secret entrance
                0x116 => 0x2D,// road to ruin secret entrance
                0x25 => 0x2E,// totally bear
                0x27 => 0x2F,// totally fly
                _ => id,
            };
        }

        public static void Initialize()
        {
            //Log.Information("Initializing warp room randomizer");
            App.Client.Options.TryGetValue("randomize_warp_destinations", out var option_randomize_warp_room);
            if (option_randomize_warp_room == null)
                return;
            int randomize_warp_room = Convert.ToInt32(option_randomize_warp_room.ToString());
            //Log.Information($"Randomize warp room destinations option: {randomize_warp_room}");
            if (randomize_warp_room != 0)
            {
                App.SlotData.TryGetValue("warp_room_destinations", out var data_warp_room_destinations);
                App.SlotData.TryGetValue("secret_warp_room_entrances", out var data_secret_warp_room_entrances);
                //Log.Information($"Warp room destinations from slot data: {data_warp_room_destinations}");
                //Log.Information($"Secret warp room entrances from slot data: {data_secret_warp_room_entrances}");
                if (data_warp_room_destinations != null && data_secret_warp_room_entrances != null)
                {
                    List<int> new_montyhall_spawn_list = new();
                    foreach (string val in data_warp_room_destinations.ToString().Split(","))
                    {
                        new_montyhall_spawn_list.Add(Convert.ToInt32(Regex.Replace(val, @"\D", "")));
                    }
                    List<int> new_montyhall_secret_entrances = new();
                    foreach (string val in data_secret_warp_room_entrances.ToString().Split(","))
                    {
                        new_montyhall_secret_entrances.Add(Convert.ToInt32(Regex.Replace(val, @"\D", "")));
                    }
                    // remap to patch spawn list
                    Dictionary<int, int> montyhall_remap = new();
                    // set up remap and modify destinations list
                    for (int i = 0; i < OriginalMontyHallDestinations.Length; ++i)
                    {
                        montyhall_remap.Add(OriginalMontyHallDestinations[i], new_montyhall_spawn_list[i]);
                        MontyHallDestinations[i] = new_montyhall_spawn_list[i] & 0x3F;
                    }
                    // now modify spawn list
                    for (int i = 0; i < OriginalMontyHallSpawnList.Length; ++i)
                    {
                        int corrected_spawn_index = GameLevelIdToApWorldSecretLevelId(OriginalMontyHallSpawnList[i]);
                        if (!montyhall_remap.ContainsKey(corrected_spawn_index))
                            continue;
                        MontyHallSpawnList[i] = ApWorldSecretLevelIdToGameLevelId(montyhall_remap[corrected_spawn_index]);
                    }

                    // now patch memory
                    byte[] bytes = new byte[MontyHallSpawnList.Length * 4];
                    Buffer.BlockCopy(MontyHallSpawnList, 0, bytes, 0, bytes.Length);
                    Memory.Write(Addresses.MontyHallSpawnIndexList, bytes);

                    //if (WarpLevelOverridesLeveldHook != null)
                    //{
                    //    WarpLevelOverridesLeveldHook.RemoveHook();
                    //}
                    //// make it so the exit level override for totally fly is whatever level unlocks totally fly
                    //WarpLevelOverridesLeveldHook = new CustomHook([
                    //    $"ori $v1, $zero, 0x{new_montyhall_secret_entrances[0]:X}"
                    //]);
                    //Log.Error("WarpLevelOverridesLeveldHook and Inputlock currently use overlapping free address");
                    //WarpLevelOverridesLeveldHook.InsertHookInJumptable(Addresses.JumptableWarpLevelOverrideLevelD, Addresses.JumptableWarpLevelOverrideBreak, 0xf020);

                    Memory.WriteByte(Addresses.WarpLevelOverrideTotallyBear, (byte)new_montyhall_secret_entrances[1]); // needs to be whatever level unlocks totally bear
                    Memory.WriteByte(Addresses.WarpLevelOverrideRooWin, (byte)montyhall_remap[0x11]);
                    Memory.WriteByte(Addresses.WarpLevelOverrideRooNotWin, (byte)montyhall_remap[0x1e]);
                    Memory.WriteByte(Addresses.WarpLevelOverrideKimodoWinAgain, (byte)montyhall_remap[0x21]);
                    Memory.WriteByte(Addresses.WarpLevelOverrideKimodoWin, (byte)montyhall_remap[0x21]);
                    Memory.WriteByte(Addresses.WarpLevelOverrideKimodoNotWinAndRooWinQuit, (byte)montyhall_remap[0x11]);
                    Memory.WriteByte(Addresses.WarpLevelOverrideTazWin, (byte)montyhall_remap[0xd]);
                    Memory.WriteByte(Addresses.WarpLevelOverrideTazNotWin, (byte)montyhall_remap[0x21]);
                    Memory.WriteByte(Addresses.WarpLevelOverrideNGinWinAgain, (byte)montyhall_remap[0x10]);
                    Memory.WriteByte(Addresses.WarpLevelOverrideNGinWin, (byte)montyhall_remap[0x10]);
                    Memory.WriteByte(Addresses.WarpLevelOverrideTazWinQuitAndNGinNotWin, (byte)montyhall_remap[0xd]);
                    Memory.WriteByte(Addresses.WarpLevelOverrideCortex, (byte)montyhall_remap[0x10]);
                }
            }
        }

        public static void UnInitialize()
        {
            if (WarpLevelOverridesLeveldHook != null)
                WarpLevelOverridesLeveldHook.RemoveHook();
        }

        public static void PrintMontyHallDestinations()
        {
            // Warp 6 is not currently randomized, so no need to print it
            for (int i = 1; i <= 5; i++)
            {
                PrintMontyHallDestinations(i);
            }
        }
        public static void PrintMontyHallDestinations(int warpRoom)
        {
            string outstring = string.Empty;
            string tempstring = string.Empty;
            if (MontyHallDestinations.All<int>(x => x == 0))
            {
                Log.Information("The warp room is not randomized.");
                return;
            }
            
            Log.Information($"Warp {warpRoom}:");
            for (int i = (warpRoom - 1) * 5; i < warpRoom * 5 && i < MontyHallDestinations.Length; ++i)
            {

                tempstring = Addresses.IdToLevelName.GetValueOrDefault(OriginalMontyHallDestinations[i], "Unknown") + " -> ";
                if (Helpers.seenLevelIds.Contains((uint)MontyHallDestinations[i]))
                    tempstring += Addresses.IdToLevelName.GetValueOrDefault(MontyHallDestinations[i], "Unknown");
                else
                    tempstring += "???";
                if (i % 5 != 0)
                    tempstring += "\n";
                outstring = tempstring + outstring;                
            }
            Log.Information(outstring);
        }
    }
}
