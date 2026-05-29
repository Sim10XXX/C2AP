using Archipelago.Core.Util;
using Avalonia;
using Serilog;
using Silk.NET.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Timers;

namespace C2AP
{
    internal class CrashEvent
    {
        public enum EventType
        {
            KillCrash,
            GiveLife,
            GiveWumpa,
            SyncGlobalValue,
            LockInput,
            UnlockInput,
            InterruptCrash,
            LandCrash,
            TakeOffJetpack,
            BasicEvent,
            //Event9,
            //Event58,
            //Event56,
            //Event34,
            //Event21,
            //Event0,
        }
        private class Event
        {
            public EventType Type;
            //public uint Priority;
            public uint EventId;
            public uint[] EventArgv = [];
            public uint ResultState;
        }

        private static Dictionary<EventType, uint> EventPriority = new Dictionary<EventType, uint>
        {
            { EventType.KillCrash, 0 },
            
            { EventType.TakeOffJetpack, 1  },
            { EventType.LockInput, 3 },
            { EventType.InterruptCrash, 4 },
            { EventType.BasicEvent, 5 },
            { EventType.LandCrash, 6  },
            { EventType.UnlockInput, 7 },
            { EventType.SyncGlobalValue, 9 },
            { EventType.GiveLife, 10 },
            { EventType.GiveWumpa, 10 },

        };
        public static CustomHook sendEvent = new CustomHook(["nop"]);
        private static PriorityQueue<Event, uint> eventQueue = new();

        private static Timer processEventTimer = new Timer(50);

        //private static Random rnd = new Random();
        public static void Initialize()
        {
            if (BaseHooks.ApItemsHook == null) return;
            if (BaseHooks.ApItemsHook._freeAddress == 0)
            {
                Log.Error("CrashEvent must be initialized after BaseHooks");
                return;
            }
            sendEvent.InsertHook(0x15A04, BaseHooks.ApItemsHook._hookSize + BaseHooks.ApItemsHook._freeAddress + 0x4);

            // Call a dummy event in order to have the correct value for CrashEvent.sendEvent._hookSize
            CallSendEvent(0, 0, 0, 0, []);

            // Set this flag to 0 so the dummy event isn't actually executed
            Memory.Write(Addresses.SendEventFlag, 0);

            Log.Information("initialized CrashEvent");
            
                //processEventTimer = new Timer(25);
            processEventTimer.Elapsed += (s, ev) =>
            {
                ProcessNextEvent();
            };
        }

        public static void EnqueueEvent(EventType eventType, uint eventId, uint[] eventArgv, uint resultState)
        {
            //Log.Logger.Information($"Enqueue event : {eventType}");
            
            EnqueueEvent(new Event { Type = eventType, EventId = eventId, EventArgv = eventArgv, ResultState = resultState });
        }
        public static void EnqueueEvent(EventType eventType)
        {
            EnqueueEvent(eventType, 0, [], 0);
        }
        private static void EnqueueEvent(Event ev)
        {
            eventQueue.Enqueue(ev, EventPriority[ev.Type]);
            processEventTimer.Start();
        }
        private static void ProcessNextEvent()
        {
            if (Memory.ReadUInt(Addresses.SendEventFlag) != 0) return;
            Event nextEvent = eventQueue.Dequeue();
            if (CallEvent(nextEvent))
            {
                if (eventQueue.Count == 0)
                {
                    processEventTimer.Stop();
                }
            }
            else
            {
                EnqueueEvent(nextEvent);
            }
        }
        private static bool CallEvent(Event ev)
        {
            uint crashAddress = CrashObject.FindObjectAddress(0, 0);
            if (crashAddress == 0 || crashAddress == CrashObject.cacheOffset) return false;
            //Log.Logger.Information($"Running event : {eventType}");
            uint state = Memory.ReadUInt(crashAddress + 0x1C);
            Log.Logger.Information($"crash state: {state}");
            switch (ev.Type)
            {
                case EventType.KillCrash:
                    /** relevant crash states:
                     * 4: walking
                     * 11: hanging still
                     * 12-14: various hanging actions
                     * 16, 18: crouch & crawl
                     * 24: slide/crouch jump
                     * 28: mid-air from taking damage
                     * 38: stuck riding platform
                     * 56, 64: damage animations
                     * 65: victory dance
                     * 66 - 70: various warp in/out animations
                     * 68: standing on lift
                     * 71: crash dance
                     * 76: something jetpack related
                     * 78 - 87: jetpack
                     * 90: burnt jetpack
                     * 94: taking off jetpack
                     * 96: entering jetboard
                     * 97: jetboard
                     * 98: jetboard boost
                     * 99: mid-air with jetboard
                     * 100: exiting jetboard
                     * 105 - 110: polar
                     * 116 - 119: digging states
                     * 117 - 118: underground
                     * 123, 124, 127: Ngin fight
                     */
                    
                    {
                        if (state == 69) 
                            return false;
                        uint levelId = Memory.ReadUInt(Addresses.LevelIdAddress);
                        if (state == 38 || (state >= 65 && state <= 68) || state == 70 || state == 100 || state == 105 || state == 117 || state == 118)
                        {

                            // these states need to be interrupted with event 39
                            // so if we are on a level where event 39 is unavailable, we must wait
                            // bear it, rock it, pack attack, cortex
                            
                            if (levelId == 0x1D00 || levelId == 0x1200 || levelId == 0x1A00 || levelId == 0x0700)
                            {
                                return false;
                            }
                            //EnqueueEvent(Event.LockInput);
                            EnqueueEvent(EventType.InterruptCrash);
                            //EnqueueEvent(EventType.BasicEvent, 70, [100], 51);
                            //EnqueueEvent(Event.UnlockInput);
                        }
                        //else
                        //{
                            //EnqueueEvent(Event.LockInput);
                            //EnqueueEvent(EventType.Event9);
                        if (levelId == 0x0700) 
                        {
                            // Event 70 crashes in Cortex
                            if (App.crashState.Jetpack == true)
                            {
                                EnqueueEvent(EventType.BasicEvent, 31, [100], 90);
                            }
                            else
                            {
                                // If crash doesn't have a jetpack in Cortex, he's as good as dead anyways,
                                // so we'll just use event 12 and not worry about the possibility of the event getting blocked
                                EnqueueEvent(EventType.BasicEvent, 12, [], 0);
                            }
                            
                        }
                        else
                        {
                            EnqueueEvent(EventType.BasicEvent, 70, [100], 51);
                        }
                        
                            //EnqueueEvent(Event.UnlockInput);
                        //}
                    }
                    return true;
                    //break;
                //case Event.GiveWumpa:
                //    CallSendEvent(0, crashAddress + CrashObject.cacheOffset, 36, 1, [1]);
                //    EnqueueEvent(Event.SyncGlobalValue);
                //    break;
                //case Event.GiveLife:
                //    CallSendEvent(0, crashAddress + CrashObject.cacheOffset, 17, 1, [1]);
                //    EnqueueEvent(Event.SyncGlobalValue);
                //    break;
                case EventType.LandCrash:
                    //EnqueueEvent(Event.LockInput);
                    //EnqueueEvent(EventType.Event56);
                    //EnqueueEvent(Event.UnlockInput);
                    
                    if (ev.EventId != 0)
                    {
                        Log.Logger.Information($"Event landcrash {ev.EventId}");
                        CallSendEvent(0, crashAddress + CrashObject.cacheOffset, ev.EventId, (uint) ev.EventArgv.Length, ev.EventArgv);
                    }
                    else
                    {
                        Log.Logger.Information($"Event landcrash 56");
                        CallSendEvent(0, crashAddress + CrashObject.cacheOffset, 56, 0, []);
                    }
                    if (state >= 30)
                        return false;
                    break;
                case EventType.TakeOffJetpack:
                    //EnqueueEvent(Event.Event58);
                    //EnqueueEvent(Event.LockInput);
                    //EnqueueEvent(Event.Event0);
                    Log.Logger.Information($"Event takeoffjetpack");
                    EnqueueEvent(EventType.BasicEvent, 34, [], 32);
                    EnqueueEvent(EventType.LandCrash, 21, [100], 0);
                    //EnqueueEvent(EventType.Event9);

                    //EnqueueEvent(Event.UnlockInput);
                    break;
                case EventType.SyncGlobalValue:
                    Memory.WriteByte(Addresses.LivesGlobalAddress, Memory.ReadByte(crashAddress + Addresses.LivesOffset));
                    Memory.WriteByte(Addresses.WumpaGlobalAddress, Memory.ReadByte(crashAddress + Addresses.WumpaOffset));
                    break;
                case EventType.LockInput:
                    InputLock.UnlockInput(InputFlag.All);
                    InputLock.LockInput(InputFlag.All);
                    CallSendEvent(0, crashAddress + CrashObject.cacheOffset, 0, 0, []);
                    break;
                case EventType.UnlockInput:
                    InputLock.LockInput(InputFlag.All);
                    InputLock.UnlockInput(InputFlag.All);
                    CallSendEvent(0, crashAddress + CrashObject.cacheOffset, 0, 0, []);
                    break;
                case EventType.BasicEvent:
                    CallSendEvent(0, crashAddress + CrashObject.cacheOffset, ev.EventId, (uint) ev.EventArgv.Length, ev.EventArgv);
                    if (ev.ResultState != 0)
                    {
                        if (state != ev.ResultState)
                        {
                            //if (ev.ResultState == 90 && state == 58) // both with & without jetpack should return true
                            //    break;
                            return false;
                        }
                    }
                    break;
                case EventType.InterruptCrash:
                    //EnqueueEvent(EventType.BasicEvent, 39, [], 50);
                    CallSendEvent(0, crashAddress + CrashObject.cacheOffset, 39, 0, []);
                    if (state != 50)
                    {
                        return false;
                    }
                    break;

            }
            return true;
        }
        public static void CallSendEvent(uint sender, uint receiver, uint eventId, uint eventArgc, uint[] eventArgv)
        {
            if (eventArgv.Length != eventArgc)
            {
                Log.Error($"CallSendEvent: Provided eventArgv has a different length ({eventArgv.Length}) than the provided eventArgc ({eventArgc})");
                return;
            }
            if (eventArgc > 11)
            {
                Log.Error($"CallSendEvent: Providing more than 11 args is not currently supported");
            }

            // make sure everything is at 8-bit offset
            if ((eventId & 0xFF) != 0)
                eventId = eventId << 8;
            
            for (uint i = 0; i < eventArgv.Length; i++)
            {
                if ((eventArgv[i] & 0xFF) != 0)
                    eventArgv[i] = eventArgv[i] << 8;
            }

            // write event args
            for (uint i = 0; i < eventArgc && i <= 11; i++)
            {
                Memory.Write(Addresses.EventArgv + 0x4 * i, eventArgv[i]);
            }

            sendEvent.ReplaceAsm([
               
                $"la $t1, 0x{Addresses.SendEventFlag + Addresses.CacheOffset:X}",
                "lw $t0, 0($t1)",
                "nop",
                "beq $t0, $zero, 0x1F", // branch to exit ///// 0x1B
                "nop",

                // allocate space on the stack
                "addiu $sp, $sp, 0xFFE0",

                // argv pointer is expected at 0x10 + $sp
                $"la $t0, 0x{Addresses.EventArgv + Addresses.CacheOffset:X}",
                "sw $t0, 0x10($sp)",

                // save the args of the function we are currently in, because I decided to hook into a function
                "sw $a0, 0x4($sp)",
                "sw $a1, 0x8($sp)",
                "sw $a2, 0xc($sp)",
                "sw $a3, 0x14($sp)",

                // the 2 operations done on $v0 (at the target location) need to be saved because the function call to Send Event will overwrite $v0
                "sw $v0, 0x18($sp)",

                // also save $ra because jal overwrites it
                "sw $ra, 0x1C($sp)",

                

                // setup args for "Send Event".  This assembly code can be optimized (instead of using la use addiu with $zero for args that don't use the upper 16 bits)
                $"la $a0, 0x{sender:X}",
                $"la $a1, 0x{receiver:X}",
                $"la $a2, 0x{eventId:X}",
                $"la $a3, 0x{eventArgc:X}",

                // zero the block flag for the receiver
                // $"sw $zero, 0xb0($a1)",

                $"jal 0x{Addresses.SendEventFunction:X}",
                "nop",

                // restore args
                "lw $a0, 0x4($sp)",
                "lw $a1, 0x8($sp)",
                "lw $a2, 0xc($sp)",
                "lw $a3, 0x14($sp)",

                // restore $v0
                "lw $v0, 0x18($sp)",

                // restore $ra
                "lw $ra, 0x1C($sp)",

                // restore sp
                "addiu $sp, $sp, 0x20",

                // set the sendEventFlag to 0 to run this just once
                $"la $t1, 0x{Addresses.SendEventFlag + Addresses.CacheOffset:X}", // $t1 may have been overwritten
                "sw $zero, 0($t1)",

                //exit
                ]);

            // update flag (used to only send the event once)
            Memory.Write(Addresses.SendEventFlag, 1);
        }
    }
}
