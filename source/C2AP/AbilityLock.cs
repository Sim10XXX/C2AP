using Archipelago.Core.Util;
using Avalonia.Controls;
using Avalonia.Platform;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;

namespace C2AP
{
    public enum Ability : uint
    {
        Crawl,
        Slide,
        GroundSpin,
        SlideSpin,
        JumpSpin,
        HighJumpSpin,
        Slam,
        CrouchJump,
        SlideJump

        //HighJump,
    }
    internal class AbilityLock
    {
        
        private static readonly Dictionary<Ability, Tuple<List<byte[]>, List<uint>>> AbilityToMod = new Dictionary<Ability, Tuple<List<byte[]>, List<uint>>>
        {
            { Ability.Crawl, new Tuple<List<byte[]>, List<uint>>([[0,0,0,0], [0,0,0,0]], [1521, 1592]) },
            { Ability.Slide, new Tuple<List<byte[]>, List<uint>>([[0x01,0x08,0xBE,0x16, 0x10, 0x44, 0x49, 0x35, 0x00, 0x40, 0x89, 0x31]], [1719]) },
            { Ability.GroundSpin, new Tuple<List<byte[]>, List<uint>>([[0,0,0,0], [0,0,0,0], [0,0,0,0]], [821, 1415, 2862]) },
            { Ability.SlideSpin, new Tuple<List<byte[]>, List<uint>>([[0,0,0,0]], [1788]) },
            { Ability.JumpSpin, new Tuple<List<byte[]>, List<uint>>([[0,0,0,0], [0,0,0,0]], [2062, 2324]) },
            { Ability.HighJumpSpin, new Tuple<List<byte[]>, List<uint>>([[0,0,0,0]], [2152]) },
            { Ability.Slam, new Tuple<List<byte[]>, List<uint>>([[0,0,0,0], [0,0,0,0]], [2078, 2340]) },
            { Ability.CrouchJump, new Tuple<List<byte[]>, List<uint>>([[0,0,0,0]], [1656]) },
            { Ability.SlideJump, new Tuple<List<byte[]>, List<uint>>([[0,0,0,0]], [1810]) }
        };

        private static readonly Dictionary<Ability, Tuple<List<byte[]>, List<uint>>> AbilityToOriginal = new Dictionary<Ability, Tuple<List<byte[]>, List<uint>>>
        {
            { Ability.Crawl, new Tuple<List<byte[]>, List<uint>>([[0x12,0xC0,0x57,0x36], [0x12,0xC0,0x67,0x37]], [1521, 1592]) },
            { Ability.Slide, new Tuple<List<byte[]>, List<uint>>([[0x00, 0xE8, 0x14, 0x10, 0x55, 0xF1, 0xE1, 0x00, 0x1F, 0xDE, 0x15, 0x02]], [1719]) },
            { Ability.GroundSpin, new Tuple<List<byte[]>, List<uint>>([[0x2C, 0x40, 0x49, 0x35], [0x2C, 0x40, 0x49, 0x35], [0x2C, 0x40, 0x49, 0x35]], [821, 1415, 2862]) },
            { Ability.SlideSpin, new Tuple<List<byte[]>, List<uint>>([[0x16, 0x40, 0x49, 0x35]], [1788]) },
            { Ability.JumpSpin, new Tuple<List<byte[]>, List<uint>>([[0x2D, 0x40, 0x49, 0x35], [0x2D, 0x40, 0x49, 0x35]], [2062, 2324]) },
            { Ability.HighJumpSpin, new Tuple<List<byte[]>, List<uint>>([[0x2D, 0x40, 0x49, 0x35]], [2152]) },
            { Ability.Slam, new Tuple<List<byte[]>, List<uint>>([[0x14, 0xD0, 0x57, 0x36], [0x14, 0xD0, 0x57, 0x36]], [2078, 2340]) },
            { Ability.CrouchJump, new Tuple<List<byte[]>, List<uint>>([[0x18, 0xC4, 0x57, 0x36]], [1656]) },
            { Ability.SlideJump, new Tuple<List<byte[]>, List<uint>>([[0x18, 0x44, 0x49, 0x35]], [1810]) }
        };

        private static Dictionary<Ability, CrashObjectMod?> AbilityLockMods = new();

        private static Timer LevelIdCheck = new Timer(1000);
        private static uint lastLevelId = 0;
        private static bool updateModLevelId = false;
        private static bool refreshing = false;
        public static void Initialize()
        {
            foreach (Ability ability in Enum.GetValues<Ability>())
            {
                AbilityLockMods[ability] = null;
            }
            RefreshAbilityLock();

            if (!LevelIdCheck.Enabled)
            {
                LevelIdCheck.Elapsed += (sender, e) =>
                {
                    uint currentLevelId = Memory.ReadUInt(Addresses.LevelIdAddress);
                    if (currentLevelId != lastLevelId)
                    {
                        lastLevelId = currentLevelId;
                        updateModLevelId = true;
                        RefreshAbilityLock();
                    }
                };
                LevelIdCheck.Start();
            }
        }

        public static void RefreshAbilityLock()
        {
            RefreshAbilityLock(App.crashState.UnlockedAbilities);
        }

        public static void RefreshAbilityLock(SortedSet<Ability> unlockedAbilities)
        {
            if (refreshing) return;
            refreshing = true; // try to prevent this method from being spammed

            List<uint> modlines;
            if (updateModLevelId)
            {
                foreach (Ability ability in Enum.GetValues<Ability>())
                {
                    //modlines = AbilityToMod[ability].Item2;
                    //for (int i = 0; i < modlines.Count; i++)
                    //{
                    //    if (IsLevelIdOffset(lastLevelId))
                    //    {
                    //        Log.Information($"Level {lastLevelId:X} has offset");
                    //        if (modlines[i] >= 1270)
                    //        {
                    //            modlines[i] += 2;
                    //        }
                    //        else if (modlines[i] >= 1102)
                    //        {
                    //            modlines[i] += 1;
                    //        }
                    //    }
                    //}

                    AbilityLockMods[ability]?.RemoveMod();
                    AbilityLockMods[ability] = null;
                }
                updateModLevelId = false;
            }

            foreach (Ability ability in Enum.GetValues<Ability>())
            {
                // Adjust the mod instruction lines if in a level with a slightly different WillC
                modlines = [..AbilityToMod[ability].Item2];
                for (int i = 0; i < modlines.Count; i++)
                {
                    if (IsLevelIdOffset(lastLevelId))
                    {
                        //Log.Information($"Level {lastLevelId:X} has offset");
                        if (modlines[i] >= 1270)
                        {
                            modlines[i] += 2;
                        }
                        else if (modlines[i] >= 1102)
                        {
                            modlines[i] += 1;
                        }
                    }
                }
                //Log.Logger.Information($"ModLines for ability {ability}: {string.Join(", ", modlines)}");
                if (unlockedAbilities.Contains(ability))
                {
                    if (AbilityLockMods[ability] != null)
                    {
                        // If the ability is unlocked but the locking mod is present
                        //Log.Information($"Removing mod for ability {ability}");
                        AbilityLockMods[ability]?.RemoveMod(AbilityToOriginal[ability].Item1, modlines);
                        AbilityLockMods[ability] = null;
                    }
                }
                else
                {
                    if (AbilityLockMods[ability] == null)
                    {
                        // If the ability is locked but the locking mod is not present
                        //Log.Information($"Adding mod for ability {ability}");
                        
                        AbilityLockMods[ability] = new CrashObjectMod(0, 0, AbilityToMod[ability].Item1, modlines);
                        AbilityLockMods[ability]?.SetLevelId((int)lastLevelId>>8);
                        //Log.Information($"Level: {lastLevelId:X}");
                    }
                }
            }

            //foreach (CrashObjectMod? mod in AbilityLockMods.Values)
            //{
                
            //}
            refreshing = false;
        }

        private static bool IsLevelIdOffset(uint levelId)
        {
            switch (levelId)
            {
                // plant food, rock it, ruination, hang eight, unbearable, crash crush, pack attack
                case 0x2100:
                case 0x1200:
                case 0x0F00:
                case 0x1900:
                case 0x1700:
                case 0x1B00:
                case 0x1A00:
                    return true;
            }
            return false;
        }
    }
}


/*
 
Logic

//// Turtle woods
// Box Gem:
Lunatic: Slam
Normal: Slam + Slide + SlideJump

//// Snow Go
// Red Gem Early:
both: Slam, Slide, SlideJump, HighJumpSpin
// Box Gem:
Slam || CrouchJump || Slide + SlideJump
Slam || Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin

Lunatic: Slam || Slide + SlideJump || CrouchJump + (HighJumpSpin || GroundSpin || JumpSpin)
Normal: Slide + SlideJump || GroundSpin + (Slam || CrouchJump)

//// Hang Eight
// Timer Gem:
Lunatic: nothing
Normal: Slide || JumpSpin || GroundSpin
// Bonus Part 1 Wumpa:
Both: Slide || Crawl
// Box Gem:
Lunatic: nothing
Normal: JumpSpin || GroundSpin || Slide + SlideJump || CrouchJump + HighJumpSpin

//// The Pits
// Box Gem:
Slam || Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin
Slam + Slide + Crawl
Both: Slam + Slide + Crawl
// Bonus Part 4 Wumpa:
Both: Slide + Crawl

//// Crash Dash
// Box Gem:

1 Lunatic: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin
1 Normal: JumpSpin || CrouchJump + HighJumpSpin || Slide + SlideJump + HighJumpSpin

2 Lunatic: Slide || GroundSpin || JumpSpin
2 Normal: GroundSpin || Slide + SlideSpin

Lunatic : Slide || GroundSpin || JumpSpin
Normal : (GroundSpin || Slide + SlideSpin) && (JumpSpin || CrouchJump + HighJumpSpin || Slide + SlideJump + HighJumpSpin)

 */


//Crawl,
//Slide,
//GroundSpin,
//SlideSpin,
//JumpSpin,
//HighJumpSpin,
//Slam,
//CrouchJump,
//SlideJump