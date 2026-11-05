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
        private static bool isActive = false;
        public static void Initialize()
        {
            if (Helpers.GetOptionValue("ability_lock") != 1) return;
            isActive = true;
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
            if (!isActive) return;
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

// other wumpa: 
Normal: Slide || Crawl

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

 


//// Snow Biz
// Box Gem:

both: Slam

normal: Slide || GroundSpin || CrouchJump + HighJumpSpin
normal: Slide + SlideJump || CrouchJump

normal: GroundSpin || JumpSpin || CrouchJump + HighJumpSpin
lunatic: GroundSpin || JumpSpin || CrouchJump + HighJumpSpin || Slide

Lunatic: Slam + ( GroundSpin || JumpSpin || CrouchJump + HighJumpSpin || Slide )
Normal: Slam + ( CrouchJump + HighJumpSpin || ( Slide || GroundSpin ) + ( Slide + SlideJump || CrouchJump ) + ( GroundSpin || JumpSpin ) ) 

// Snow Biz Red Gem Path Around Arrow Crate Wumpa:
Both: Sam

//// Air Crash
// Box Gem:

both: Slam

// Air Crash Bonus Big Jump Part 1-3
// Air Crash Bonus Big Jump Life

Lunatic: Slam || Slide + SlideJump
Normal: Slam

//// Bear It


//// Crash Crush (lunatic only rn)
// Box Gem:

both: Slam

// ALL:
lunatic: nothing
normal: Crawl || Slide

// Crash Crush Bonus Below Block Wumpa:

both: Crawl || Slide

//// The Eel Deal
// Box Gem:
both: Slam

// The Eel Deal Bonus End Wumpa:
lunatic: nothing
normal: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin || Slam


//// Komodo Bros:
both: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin

//// Plant Food
// Yellow Gem:
lunatic: nothing
normal: Slide + SlideJump || JumpSpin

// Box Gem:
lunatic: Slam || Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin
normal: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin

// Plant Food Bonus Above Small Platform Wumpa
lunatic: Slam || Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin
normal: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin

// Plant Food Bonus Platform Hops Part 1-3 Wumpa
lunatic: Slam || Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin
normal: ( Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin ) + ( Slam || Slide + SlideJump || CrouchJump )


//// Sewer or Later
// Exit (Crystal + Exit + Box Gem + Certain Wumpa):
lunatic: nothing
normal: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin

lunatic: nothing
normal: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin


// Box Gem:

lunatic: Slide || Crawl || CrouchJump + HighJumpSpin
normal: Slide || Crawl

lunatic: Slide || CrouchJump + HighJumpSpin
normal: Slide

both: Slide

both: Slam
__
lunatic: Slide + Slam
normal: Slide + Slam

// Sewer or Later Bonus Under Metal Boxes 1 Wumpa:

both: Slide || Crawl

// Sewer or Later Bonus Under Metal Boxes 2 Wumpa:

lunatic: Slide || CrouchJump + HighJumpSpin
normal: Slide

// Sewer or Later Bonus Ending Wumpa:

lunatic: Slide || CrouchJump + HighJumpSpin
normal: Slide

// Sewer or Later Left Split Wumpa:
lunatic: nothing
normal: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin

// Sewer or Later Ending Wumpa:
lunatic: nothing
normal: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin

// Sewer or Later Crystal:
// Sewer or Later Regular Exit:
lunatic: nothing
normal: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin

//// Bear Down
///



//// Road to Ruin
// Secret Entrance Skip:
lunatic: nothing
normal: Slide + SlideJump + HighJumpSpin

// Box Gem:
lunatic: nothing
normal: Slide + SlideJump + HighJumpSpin || RoadToRuinSecretEntrance

// Road to Ruin Secret Entrance Cube Stash Wumpa:
lunatic: nothing
normal: Slide + SlideJump + HighJumpSpin || RoadToRuinSecretEntrance

// Road to Ruin Death Route Wumpa Triangle Part 1-2 Wumpa:
// Road to Ruin Death Route Backwards Wumpa:
// Road to Ruin Death Route Gem Part 1-2 Wumpa:
// Death Route Clear Gem:
lunatic: nothing
normal: JumpSpin || CrouchJump + HighJumpSpin || Slide + SlideJump + HighJumpSpin



//// Hangin' Out
// Everything onwards:
lunatic: nothing
normal: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin

// Hangin' Out Second Wire Mesh Part 1-3 Wumpa:
// Hangin' Out Second Drop Wumpa:
// Hangin' Out Secret Drop Wumpa:
// Hangin' Out Before Secret Exit Part 1-5 Life
// Hangin' Out Secret Warp Wumpa:
// Hangin' Out Secret Exit:
// Hangin' Out Bonus Start Wumpa:
// Hangin' Out Crystal
// Hangin' Out Regular Exit:
lunatic: nothing
normal: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin


// Get Past Bonus Boxes:
lunatic: Slam || CrouchJump + ( JumpSpin || GroundSpin || HighJumpSpin ) || Slide
normal: CrouchJump + ( JumpSpin || GroundSpin || HighJumpSpin ) || Slide

// Box Gem:

lunatic: nothing
normal: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin

lunatic: Slam || Slide
normal: Slide || Slam + ( GroundSpin || JumpSpin || CrouchJump + HighJumpSpin )

Lunatic: Slam || Slide
Normal: Slide || Slam + ( GroundSpin || JumpSpin || CrouchJump + HighJumpSpin )

// Hangin' Out Bonus Arrow Crates Wumpa:
lunatic: Slam || CrouchJump + ( JumpSpin || GroundSpin || HighJumpSpin ) || Slide
normal: CrouchJump + ( JumpSpin || GroundSpin || HighJumpSpin ) || Slide


//// Cold Hard Crash
// Roadblock:
lunatic: Slam || Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin
normal: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin

lunatic: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin
normal: Slide || GroundSpin || JumpSpin

// Box Gem:

lunatic: Slam || Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin
normal: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin

lunatic: nothing
normal: Slide + SlideJump || CrouchJump

lunatic: nothing
normal: JumpSpin || CrouchJump || Slide + SlideJump || Slam


// Cold Hard Crash Death Route Above Nitro Wumpa:
// Cold Hard Crash Death Route Wumpa Triangle Part 1-2 Wumpa:
// Cold Hard Crash Death Route Behind The Wall Wumpa:
// Cold Hard Crash Death Route Outward Square Wumpa:
// Cold Hard Crash Death Route Arrow Crate Wumpa:
// Cold Hard Crash Death Route Jump Over Nitro Wumpa:
// Cold Hard Crash Death Route Between Two Nitros Wumpa:
// Cold Hard Crash Death Route Nitro Zigzag Part 1-2 Wumpa:
// Cold Hard Crash Death Route Drop Down Wumpa:
// Cold Hard Crash Death Route Drop Down "Impossible" Wumpas Wumpa:
// Cold Hard Crash Drop Down Part 1-3 Wumpa:
// Cold Hard Crash Drop Down Life:
// Cold Hard Crash Clear Gem (Death Route):
// Cold Hard Crash Crystal:
// Cold Hard Crash Regular Exit:
lunatic: Slam || Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin
normal: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin

// Cold Hard Crash Behind The Wall Part 1-6 Wumpa:
lunatic: ( Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin ) + ( Slam || Slide + SlideJump || CrouchJump )
normal: ( Slide || GroundSpin || JumpSpin ) + ( Slide + SlideJump || CrouchJump )

// Cold Hard Crash In The Wall Part 1-2 Wumpa:
// Cold Hard Crash Before Crusher Chain Wumpa:
// Cold Hard Crash In Between Crushers Life:

lunatic: Slide || GroundSpin || JumpSpin || CrouchJump + HighJumpSpin
normal: Slide || GroundSpin || JumpSpin








// fix the ability lock for snow go + something else in discord i think
/debug_unlock 
//Crawl,
//Slide,
//GroundSpin,
//SlideSpin,
//JumpSpin,
//HighJumpSpin,
//Slam,
//CrouchJump,
//SlideJump*/