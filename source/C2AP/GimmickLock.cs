using Archipelago.Core.Util;
using Serilog;
using System;
using System.Timers;
//using static C2AP.CrashEvent;

namespace C2AP
{
    internal class GimmickLock
    {
        private static Timer checkGimmickTimer = new(500);

        private static CustomHook overwritePolarEventHook = new([
            $"la $t1, 0x{Addresses.LastEventId + Addresses.CacheOffset:X}",
            "sw $a2, 0($t1)",
            //"addiu $t0, $zero, 0x4800", // event x
            //"beq $a2 $t0, 0x9", // branch to overw  
            "addiu $t0, $zero, 0x3f00", // event 63
            "beq $a2 $t0, 0x7", // branch to overw  
            "addiu $t0, $zero, 0x2300", // event 35
            "beq $a2 $t0, 0x5", // branch to overwrite
            "addiu $t0, $zero, 0x3D00", // event 61
            "beq $a2 $t0, 0x3", // branch to overwrite
            "nop",
            "beq $zero, $zero, 0x2", // branch to exit
            "nop",
            // overwrite
            "addiu $a2, $zero, 0x2600", // use event 38
            // exit
            ]);

        private static CustomHook overwriteJetpackEventHook = new([
            $"la $t1, 0x{Addresses.LastEventId + Addresses.CacheOffset:X}",
            "sw $a2, 0($t1)",
            //"addiu $t0, $zero, 0x", // event 
            //"beq $a2 $t0, 0x9", // branch to overw  
            //"addiu $t0, $zero, 0x0900", // event 9
            //"beq $a2 $t0, 0x7", // branch to overw  
            "addiu $t0, $zero, 0x2C00", // event 44
            "beq $a2 $t0, 0x5", // branch to overwrite
            "addiu $t0, $zero, 0x3f00", // event 63
            "beq $a2 $t0, 0x3", // branch to overwrite
            "nop",
            "beq $zero, $zero, 0x2", // branch to exit
            "nop",
            // overwrite
            "addiu $a2, $zero, 0x1e00", // use event 30
            // exit
            ]);
        private static CustomHook overwriteCortexEventHook = new([
           $"la $t1, 0x{Addresses.LastEventId + Addresses.CacheOffset:X}",
            "sw $a2, 0($t1)",
            "addiu $t0, $zero, 0x0900", // event 63
            "beq $a2 $t0, 0x3", // branch to overwrite
            "nop",
            "beq $zero, $zero, 0x2", // branch to exit
            "nop",
            // overwrite
            "addiu $a2, $zero, 0xc00", // use event 12
            // exit
            ]);

        private static uint lastLevelId = 0;
        public static void Initialize()
        {
            //App.Client.Options.TryGetValue("gimmick_lock", out var gimmickLock);
            //if (gimmickLock == null)
            //{
            //    Log.Logger.Error($"option null");
            //    return;
            //}
            //Log.Information($"option : {gimmickLock}");
            //if (Convert.ToInt32(gimmickLock.ToString()) != 1) return;

            checkGimmickTimer.Elapsed += (s, ev) => CheckGimmick();
            checkGimmickTimer.Start();
            if (BaseHooks.ApItemsHook == null) return;
            overwritePolarEventHook.InsertHook(0x1CD48, BaseHooks.ApItemsHook._hookSize + BaseHooks.ApItemsHook._freeAddress + CrashEvent.sendEvent._hookSize + Traps.trapsHookSize + 0xC);
            overwritePolarEventHook.RemoveHook();
            //overwriteJetpackEventHook.InsertHook(0x1CD48, BaseHooks.ApItemsHook._hookSize + BaseHooks.ApItemsHook._freeAddress + CrashEvent.sendEvent._hookSize + Traps.trapsHookSize + 0xC);
        }

        private static void CheckGimmick()
        {
            if (!App.Client.IsConnected) return;
            if (BaseHooks.ApItemsHook == null) return;
            uint crashAddress = CrashObject.FindObjectAddress(0, 0);
            if (crashAddress == 0 || crashAddress == CrashObject.cacheOffset)
            {
                return;
            }
            uint levelId = Memory.ReadUInt(Addresses.LevelIdAddress);
            uint state = Memory.ReadUInt(crashAddress + 0x1C);
            switch (levelId)
            {
                // Levels with Polar
                case 0x1D00:
                case 0x2200:
                case 0x1700:
                case 0x2500:
                    if (!(state == 76 || (state >= 105 && state <= 110))) break;
                    if (App.crashState.Polar == true) break;
                    CrashEvent.EnqueueEvent(CrashEvent.EventType.LandCrash); //, 21, [100], 0
                    if (levelId != lastLevelId)
                    {
                        Log.Information("inserting overwritePolarEventHook");
                        overwritePolarEventHook.InsertHook(0x1CD48, BaseHooks.ApItemsHook._hookSize + BaseHooks.ApItemsHook._freeAddress + CrashEvent.sendEvent._hookSize + Traps.trapsHookSize + 0xC);
                    }
                    lastLevelId = levelId;
                    break;

                // Levels with Jetpack
                case 0x1200:
                case 0x1A00:
                case 0x0700:
                    // break; //
                    if (!(state >= 78 && state <= 87)) break;
                    //if (state == 77) break;
                    if (App.crashState.Jetpack == true) break;
                    CrashEvent.EnqueueEvent(CrashEvent.EventType.TakeOffJetpack);
                    //if (levelId != lastLevelId)
                    if (Memory.ReadUInt(BaseHooks.ApItemsHook._hookSize + BaseHooks.ApItemsHook._freeAddress + CrashEvent.sendEvent._hookSize + Traps.trapsHookSize + 0xC) == 0)
                    {
                        if (levelId == 0x0700)
                        {
                            Log.Information("inserting overwriteCortexEventHook");
                            overwriteCortexEventHook.InsertHook(0x1CD48, BaseHooks.ApItemsHook._hookSize + BaseHooks.ApItemsHook._freeAddress + CrashEvent.sendEvent._hookSize + Traps.trapsHookSize + 0xC);
                        }
                        else
                        {
                            Log.Information("inserting overwriteJetpackEventHook");
                            overwriteJetpackEventHook.InsertHook(0x1CD48, BaseHooks.ApItemsHook._hookSize + BaseHooks.ApItemsHook._freeAddress + CrashEvent.sendEvent._hookSize + Traps.trapsHookSize + 0xC);
                        }
                        //
                    }
                    lastLevelId = levelId;
                    break;

                // Levels with Jetboard
                case 0x1900:
                case 0x2000:
                case 0x2100:
                    lastLevelId = levelId;
                    break;

                // Levels with Fireflies
                case 0x0C00:
                case 0x2700:
                    lastLevelId = levelId;
                    break;
                default:
                    if (overwritePolarEventHook._freeAddress != 0)
                    {
                        overwritePolarEventHook.RemoveHook();
                    }
                    if (overwriteJetpackEventHook._freeAddress != 0)
                    {
                        overwriteJetpackEventHook.RemoveHook();
                    }
                    if (overwriteCortexEventHook._freeAddress != 0)
                    {
                        overwriteCortexEventHook.RemoveHook();
                    }
                    lastLevelId = levelId;
                    break;
            }
        }
    }
}
