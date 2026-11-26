using Archipelago.Core.Util;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using DynamicData;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Timers;


namespace C2AP
{
    internal class CrashDeathLink
    {
        private static int pardonDeath;
        private static uint previousTime = 0;
        private static DeathLinkService? deathLinkService;
        private static uint previousLives = 0;
        private static string playerName = "";
        private static Timer checkDeathTimer = new Timer(250);
        private static uint previousLevelId = 0;
        private static uint amnesty = 0;
        private static uint deathCount = 0;
        private static uint previousState = 0;
        private static uint currentState = 0;

        public static void Initialize(string name)
        {
            if (Helpers.GetOptionValue("death_link") != 1) return;
            amnesty = (uint) Helpers.GetOptionValue("death_link_amnesty");
            deathLinkService = App.Client.EnableDeathLink();
            deathLinkService.EnableDeathLink();
            deathLinkService.OnDeathLinkReceived += OnDeathLinkReceived;
            playerName = name;
            checkDeathTimer.Elapsed += (s, ev) => CheckDeath();
            checkDeathTimer.Start();
        }

        public static void OnDeathLinkReceived(DeathLink deathLink)
        {
            
            Log.Logger.Information($"DeathLink received ({deathLink.Cause}), killing Crash");
            CrashEvent.EnqueueEvent(CrashEvent.EventType.KillCrash);

            // 10 second grace period where the first death doesn't send deathlink
            // Paused when the game or emu is paused
            // Must end after a set time in case the Kill event ran when Crash couldn't be killed, because then we would pardon the next real death
            pardonDeath = 10000 / (int) checkDeathTimer.Interval;
        }

        private static void CheckDeath()
        {
            if (!App.Client.IsConnected) return;
            if (deathLinkService == null) return;
            if (Helpers.IsInDemo()) return; 
            uint time = Memory.ReadUInt(Addresses.Timer);
            

            uint levelId = Memory.ReadUInt(Addresses.LevelIdAddress);
            //Log.Logger.Information($"Level ID: {levelId:X}");
            if (levelId != previousLevelId)
            {
                //Log.Logger.Information($"Level changed: {previousLevelId:X} -> {levelId:X}");
                if (levelId == 0x3B00) // ID of game over screen
                {
                    HandleDeath();
                }
            }
            previousLevelId = levelId;


            uint crashAddress = CrashObject.FindObjectAddress(0, 0);
            if (crashAddress == 0 || crashAddress == CrashObject.cacheOffset)
            {
                previousLives = 0;
                return;
            }
            uint state = Memory.ReadUInt(crashAddress + Addresses.StateOffset);
            if (state != currentState)
            {
                previousState = currentState;
                currentState = state;
            }
            uint lives = Memory.ReadByte(crashAddress + Addresses.LivesOffset);
            if (time < previousTime)
            {
                // if time went backwards, reset lives (this could mean a save state was loaded)
                //Log.Logger.Information("Backwards Time");
                previousLives = lives;
            }
            previousTime = time;
            
            if (lives == previousLives - 1)
            {
                if (Traps.storedLives >= 1) return;

                
                HandleDeath();
                
            }
            else
            {
                if (pardonDeath > 0) // if we are expecting a death (from receiving deathlink)
                {
                    if (!Helpers.IsEmulationPaused() && !Helpers.IsGamePaused())
                    {
                        // If the game and emulator are not paused, count down 
                        pardonDeath--;
                    }
                }
            }
            previousLives = lives;
        }

        private static void HandleDeath()
        {
            if (deathLinkService == null) return;
            if (pardonDeath > 0)
            {
                pardonDeath = 0;
                return;
            }
            deathCount++;
            Log.Information($"Prevstate: {previousState}, Currstate: {currentState}");
            if (amnesty != 0)
                Log.Logger.Information($"{amnesty - deathCount} death(s) left before sending a DeathLink");
            if (deathCount >= amnesty)
            {
                deathCount = 0;
                Log.Logger.Information("Sending DeathLink");
                string cause = previousState switch
                {
                    51 => "fell in a bottomless pit",
                    //53 => "died",
                    54 => "blew themselves up",
                    56 => "was electrocuted",
                    57 => "got caught in the blast",
                    58 => "was incinerated",
                    59 => "blown up by TNT",
                    60 => "was eaten whole",
                    61 => "tried to swim",
                    63 => "couldn't outrun the boulder",
                    64 => "was crushed into a pancake",
                    90 => "was turned to ash",
                    91 => "flew straight into an explosive",
                    93 => "was overloaded with volts",
                    98 => "couldn't catch up to Cortex",
                    101 => "was eaten off the jetboard",
                    102 => "jetboarded into an explosive",
                    103 => "was sucked into a whirlpool",
                    111 => "fell into cold water",
                    112 => "rode straight into an explosive",
                    113 => "tripped and fell off of Polar",
                    114 => "slammed into a pillar",
                    120 => "dug their own grave",
                    121 => "was smashed into the ground",
                    122 => "is allergic to bees",
                    _ => $"died in {Addresses.IdToLevelName[Memory.ReadByte(Addresses.LevelIdAddress + 1)]}"
                };
                //Log.Information($"{playerName} {cause}");
                deathLinkService.SendDeathLink(new DeathLink(playerName, $"{playerName} {cause}"));
            }
        }

    }
}
