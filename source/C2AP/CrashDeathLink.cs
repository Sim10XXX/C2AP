using Archipelago.Core.Util;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
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

        public static void Initialize(string name)
        {
            if (Helpers.GetOptionValue("death_link") != 1) return;

            deathLinkService = App.Client.EnableDeathLink();
            deathLinkService.EnableDeathLink();
            deathLinkService.OnDeathLinkReceived += OnDeathLinkReceived;
            playerName = name;
            checkDeathTimer.Elapsed += (s, ev) => CheckDeath();
            checkDeathTimer.Start();
        }

        public static void OnDeathLinkReceived(DeathLink deathLink)
        {
            
            Log.Logger.Information("DeathLink received, killing Crash");
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
            uint time = Memory.ReadUInt(Addresses.Timer);


            uint levelId = Memory.ReadUInt(Addresses.LevelIdAddress);
            //Log.Logger.Information($"Level ID: {levelId:X}");
            if (levelId != previousLevelId)
            {
                //Log.Logger.Information($"Level changed: {previousLevelId:X} -> {levelId:X}");
                if (levelId == 0x3B00) // ID of game over screen
                {
                    Log.Logger.Information("Sending DeathLink");
                    deathLinkService.SendDeathLink(new DeathLink(playerName));
                }
            }
            previousLevelId = levelId;


            uint crashAddress = CrashObject.FindObjectAddress(0, 0);
            if (crashAddress == 0 || crashAddress == CrashObject.cacheOffset)
            {
                previousLives = 0;
                return;
            }
            uint lives = Memory.ReadByte(crashAddress + Addresses.LivesOffset);
            if (time < previousTime)
            {
                // if time went backwards, reset lives (this could mean a save state was loaded)
                Log.Logger.Information("Backwards Time");
                previousLives = lives;
            }
            previousTime = time;
            
            if (lives == previousLives - 1)
            {
                if (Traps.storedLives == 1) return;

                if (pardonDeath <= 0)
                {
                    Log.Logger.Information("Sending DeathLink");
                    deathLinkService.SendDeathLink(new DeathLink(playerName));
                }
                else pardonDeath = 0;
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

    }
}
