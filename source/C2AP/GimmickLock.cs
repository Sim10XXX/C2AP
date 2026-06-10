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
        private static CustomHook overwriteJetboardEventHook = new([
           $"la $t1, 0x{Addresses.LastEventId + Addresses.CacheOffset:X}",
            "sw $a2, 0($t1)",
            "addiu $t0, $zero, 0x4200", // event 66
            "beq $a2 $t0, 0x3", // branch to overwrite
            "nop",
            "beq $zero, $zero, 0x2", // branch to exit
            "nop",
            // overwrite
            "addiu $a2, $zero, 0x2100", // use event 33
            // exit
            ]);

        private static uint lastLevelId = 0;
        private static bool delay = false;
        public static void Initialize()
        {
            if (Helpers.GetOptionValue("gimmick_lock") != 1) return;

            if (Helpers.GetOptionValue("jetpack_lock_logic") == 0)
                App.crashState.Jetpack = true;
            if (Helpers.GetOptionValue("jetboard_lock_logic") == 0)
                App.crashState.Jetboard = true;
            if (Helpers.GetOptionValue("polar_lock_logic") == 0)
                App.crashState.Polar = true;
            if (Helpers.GetOptionValue("firefly_lock_logic") == 0)
                App.crashState.Fireflies = true;

            checkGimmickTimer.Elapsed += (s, ev) => CheckGimmick();
            checkGimmickTimer.Start();
            if (BaseHooks.ApItemsHook == null) return;
            //overwritePolarEventHook.InsertHook(0x1CD48, BaseHooks.ApItemsHook._hookSize + BaseHooks.ApItemsHook._freeAddress + CrashEvent.sendEvent._hookSize + Traps.trapsHookSize + 0xC);
            //overwritePolarEventHook.RemoveHook();
            //overwriteJetpackEventHook.InsertHook(0x1CD48, BaseHooks.ApItemsHook._hookSize + BaseHooks.ApItemsHook._freeAddress + CrashEvent.sendEvent._hookSize + Traps.trapsHookSize + 0xC);
        }

        private static void CheckGimmick()
        {
            if (!App.Client.IsConnected) return;
            if (BaseHooks.ApItemsHook == null) return;
            if (Helpers.IsEmulationPaused() || Helpers.IsGamePaused()) return;
            uint crashAddress = CrashObject.FindObjectAddress(0, 0);
            if (crashAddress == 0 || crashAddress == CrashObject.cacheOffset)
            {
                return;
            }
            uint levelId = Memory.ReadUInt(Addresses.LevelIdAddress);
            uint state = Memory.ReadUInt(crashAddress + 0x1C);


            //Memory.Write(crashAddress + 0xE0, Memory.ReadUInt(crashAddress + 0xDC));
            switch (levelId)
            {
                // Levels with Polar
                case 0x1D00:
                case 0x2200:
                case 0x1700:
                case 0x2500:
                    if (!(state == 76 || (state >= 105 && state <= 110))) break;
                    if (App.crashState.Polar == true) break;
                    CrashEvent.EnqueueEvent(CrashEvent.EventType.TakeOffJetpack); //, 21, [100], 0
                    if (levelId != lastLevelId)
                    {
                        //Log.Information("inserting overwritePolarEventHook");
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
                    if (levelId != lastLevelId)
                    //if (Memory.ReadUInt(BaseHooks.ApItemsHook._hookSize + BaseHooks.ApItemsHook._freeAddress + CrashEvent.sendEvent._hookSize + Traps.trapsHookSize + 0xC) == 0)
                    {
                        if (levelId == 0x0700)
                        {
                            //Log.Information("inserting overwriteCortexEventHook");
                            overwriteCortexEventHook.InsertHook(0x1CD48, BaseHooks.ApItemsHook._hookSize + BaseHooks.ApItemsHook._freeAddress + CrashEvent.sendEvent._hookSize + Traps.trapsHookSize + 0xC);
                        }
                        else
                        {
                            //Log.Information("inserting overwriteJetpackEventHook");
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
                    if (App.crashState.Jetboard == true)
                    {
                        if (overwriteJetboardEventHook._freeAddress != 0)
                        {
                            overwriteJetboardEventHook.RemoveHook();
                        }
                        break;
                    }
                    if (levelId != lastLevelId)
                    {
                        // It is still possible to get on the jetboard even when it is at scale 0, so we need to overwrite that event
                        //Log.Information("inserting overwriteJetboardEventHook");
                        overwriteJetboardEventHook.InsertHook(0x1CD48, BaseHooks.ApItemsHook._hookSize + BaseHooks.ApItemsHook._freeAddress + CrashEvent.sendEvent._hookSize + Traps.trapsHookSize + 0xC);
                    }
                    uint jetboardAddress = CrashObject.FindObjectAddress(47, 2);
                    if (jetboardAddress != 0 && jetboardAddress != CrashObject.cacheOffset)
                    {
                        if (Memory.ReadUInt(jetboardAddress + 0x78) != 0)
                        {
                            //Log.Information("removing jetboard");
                            //Memory.Write(jetboardAddress + 0x64, 0x0fffffff);
                            Memory.Write(jetboardAddress + 0x78, 0);
                            Memory.Write(jetboardAddress + 0x7C, 0);
                            Memory.Write(jetboardAddress + 0x80, 0);
                        }
                    }
                    lastLevelId = levelId;
                    break;

                // Levels with Fireflies
                case 0x0C00:
                case 0x2700:
                    // Could exclude Night Fight Second Wumpa Chain Part 6 Wumpa #1
                    lastLevelId = levelId;
                    if (App.crashState.Fireflies == true) break;
                    //break;
                    //if (levelId != lastLevelId)
                    //{
                    //    // It is still possible to get on the jetboard even when it is at scale 0, so we need to overwrite that event
                    //    Log.Information("inserting overwriteJetboardEventHook");
                    //    overwriteJetboardEventHook.InsertHook(0x1CD48, BaseHooks.ApItemsHook._hookSize + BaseHooks.ApItemsHook._freeAddress + CrashEvent.sendEvent._hookSize + Traps.trapsHookSize + 0xC);
                    //}
                    uint fireflyAddress = CrashObject.FindObjectAddress(57, 1);
                    if (fireflyAddress != 0 && fireflyAddress != CrashObject.cacheOffset)
                    {
                        Memory.Write(fireflyAddress + 0x140, 0);
                        //Log.Information("firefly updated");
                    }
                    else
                    {
                        fireflyAddress = CrashObject.FindObjectAddress(57, 7);
                        if (fireflyAddress != 0 && fireflyAddress != CrashObject.cacheOffset)
                        {
                            //Memory.Write(fireflyAddress + 0x140, 0);
                            //Log.Information("firefly subtype 7 updated");
                            //Log.Information($"firefly address: {fireflyAddress:X}");
                            Memory.Write(fireflyAddress + 0x60, float.PositiveInfinity);
                            Memory.Write(fireflyAddress + 0x64, float.PositiveInfinity);
                            Memory.Write(fireflyAddress + 0x68, float.PositiveInfinity);
                        }
                    }
                    
                    break;
                default:
                    //case 0x0200:
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
                    if (overwriteJetboardEventHook._freeAddress != 0)
                    {
                        overwriteJetboardEventHook.RemoveHook();
                    }
                    if (levelId == 0x0200)
                    {
                        if (App.crashState.Polar == false)
                        {
                            uint bearAddress = CrashObject.FindObjectAddress(48, 8);
                            if (bearAddress != 0 && bearAddress != CrashObject.cacheOffset)
                            {
                                // remove the warp room bear
                                Memory.Write(bearAddress + 0x64, 0x0fffffff);
                            }
                        }
                        if (0 != Memory.ReadUInt(BaseHooks.ApItemsHook._hookSize + BaseHooks.ApItemsHook._freeAddress + CrashEvent.sendEvent._hookSize + Traps.trapsHookSize + 0xC))
                        {
                            overwritePolarEventHook.InsertHook(0x1CD48, BaseHooks.ApItemsHook._hookSize + BaseHooks.ApItemsHook._freeAddress + CrashEvent.sendEvent._hookSize + Traps.trapsHookSize + 0xC);
                            overwritePolarEventHook.RemoveHook();
                        }
                    }
                    lastLevelId = levelId;
                    break;
            }
            delay = !delay;
            if (delay && state == 1)
            {
                switch (levelId)
                {
                    case 0x1D00:
                    case 0x2200:
                    case 0x1700:
                    case 0x2500:
                        if (App.crashState.Polar == true) break;
                        CrashEvent.EnqueueUniqueEvent(CrashEvent.EventType.BasicEvent, 56, [0], 0);
                        break;
                    //case 0x1200:
                    //case 0x1A00:
                    //    if (App.crashState.Jetpack == true) break;
                    //    CrashEvent.EnqueueUniqueEvent(CrashEvent.EventType.BasicEvent, 21, [100], 0);
                    //    break;
                }
            }
        }
    }
}
