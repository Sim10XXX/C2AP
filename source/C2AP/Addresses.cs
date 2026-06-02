using System.Collections.Generic;

namespace C2AP
{
    public static class Addresses
    {
        public const uint CacheOffset = 0x80000000;
        public const uint CrystalsReceivedAddress = 0x0000EA00; //64 bits
        public const uint GemsReceivedAddress = 0x0000EA10; //64 bits

        public const uint ColoredGemReceivedAddress = 0x0000EA17;
        public const int RedGemReceivedBit = 2;
        public const int GreenGemReceivedBit = 3;
        public const int PurpleGemReceivedBit = 4;
        public const int BlueGemReceivedBit = 5;
        public const int YellowGemReceivedBit = 6;

        public const uint GemLocationsWithReceivedColoredGemsAddress = 0x0000EA20; //64 bits
        public const uint ColoredGemOffset = 0x7;
        public const byte ColoredGemMask = 0b01111100;
        public const byte ColoredGemMaskNegated = 0b10000011;

        public const uint CrystalLocationsAddress = 0x0006D03C; //64 bits

        //public const uint WasSwappedAddress = 0x0000EA1C; //32 bit boolean
        //public const uint CrystalLocationsSwapAddress = 0x0000EA20; // 64 bits

        public const uint GemLocationsAddress = 0x0006CEC0;

        public const uint InputsAddress = 0x00069BB8; //32 bits, each bit is an input

        public const uint LevelIdAddress = 0x6ce08; //32 bits (hopefully) // global 0

        public const uint LevelExitsAddress = 0x6ce74; //64 bits
        public const uint SecretExitsAddress = 0x6ce79;

        public const uint LivesGlobalAddress = 0x6ce69; //8 bits
        public const uint WumpaGlobalAddress = 0x6ce71; //8 bits
        // TODO check for serial code (i.e. memcard filename) in order to detect region?
        public const uint StaticTextAddress = 0x5ce1c;
        public const string StaticText = "Library Programs (c) 1993-1997 Sony Computer Entertainment Inc., All Rights Reserved";

        public const uint PolarLivesAddress = 0x6d060;
        public const int PolarLivesBit = 5;

        public const uint FruitCollectedListStart = 0x0000EF30;
        public const uint FruitCollectedListEnd = 0x0000EA30;
        //crash object
        public const uint LivesOffset = 0x145;
        public const uint WumpaOffset = 0x141;

        // CHeOC (cortex head obs) gool file static data item offset
        public static readonly uint[] MontyHallWarpRoomInfoStaticDataOffset = {
            0x3AC, 0x418, 0x440, 0x468, 0x490, 0x4B8
        };
        // hardcoded monty hall spawn index remap table
        public const uint MontyHallSpawnIndexList = 0x005BBAC;

        public const uint JumptableWarpLevelOverrideLevelD = 0x001037C;//0x001037C;
        public const uint JumptableWarpLevelOverrideBreak = 0x001FE08;//0x8001FBC0;
        public const uint WarpLevelOverrideTotallyBear = 0x001FD30;//0x8001FAE8;
        public const uint WarpLevelOverrideRooWin = 0x001FD48;//0x8001FB00;
        public const uint WarpLevelOverrideRooNotWin = 0x001FD5C;//0x8001FB14;
        public const uint WarpLevelOverrideKimodoWinAgain = 0x001FD7C;//0x8001FB34;
        public const uint WarpLevelOverrideKimodoWin = 0x001FD90;//0x8001FB48;
        public const uint WarpLevelOverrideKimodoNotWinAndRooWinQuit = 0x001FD98;//0x8001FB50;
        public const uint WarpLevelOverrideTazWin = 0x001FDB0;//0x8001FB68;
        public const uint WarpLevelOverrideTazNotWin = 0x001FDC4;//0x8001FB7C;
        public const uint WarpLevelOverrideNGinWinAgain = 0x001FDE4;//0x8001FB9C;
        public const uint WarpLevelOverrideNGinWin = 0x001FDF8;//0x8001FBB0;
        public const uint WarpLevelOverrideTazWinQuitAndNGinNotWin = 0x001FE00;//0x8001FBB8;
        public const uint WarpLevelOverrideCortex = 0x001FE04;//0x8001FBBC;

        public const uint PbakId = 0x005f620; //0x8005f388;
        public const uint DemoPointer = 0x0005f670;

        public const uint CurrentEntityFlagList = 0x0006b45c;
        public const uint ContinuePointFlagList = 0x00060c2c;
        public const uint ContinuePoint2FlagList = 0x00061c8c;

        public const uint SendEventFlag = 0x0000E9FC; // used to call send event just once
        public const uint EventArgv = 0x0000E9D0; // enough space for 11 arguments
        public const uint SendEventFunction = 0x0001CD48;

        public const uint LastEventId = 0x0000E9CC;

        public const uint Timer = 0x0006cdfc;
        public const uint PausedFlag = 0x0005f418;
        public const uint SecretEntranceFlags = 0x0006d060;

        public static Dictionary<string, int> BitOfLocation = new Dictionary<string, int>
        {
            //crystals
            {"Sewer or Later Crystal", 10 },
            {"Night Fight Crystal", 12 },
            {"Hangin' Out Crystal", 13 },
            {"Snow Go Crystal", 14 },
            {"Ruination Crystal", 15 },
            {"Piston it Away Crystal", 16 },
            {"Snow Biz Crystal", 17 },
            {"Rock It Crystal", 18 },
            {"Cold Hard Crash Crystal",  19},
            {"Diggin' It Crystal", 21 },
            {"Road to Ruin Crystal", 22 },
            {"Un-Bearable Crystal", 23 },
            {"Crash Dash Crystal", 24 },
            {"Hang Eight Crystal", 25 },
            {"Pack Attack Crystal", 26  },
            {"Crash Crush Crystal", 27 },
            {"Bear It Crystal", 29 },
            {"Turtle Woods Crystal", 30 },
            {"The Pits Crystal", 31  },
            {"Air Crash Crystal", 32 },
            {"Plant Food Crystal", 33 },
            {"Bear Down Crystal", 34  },
            {"The Eel Deal Crystal", 35 },
            {"Bee-Having Crystal", 36 },
            {"Spaced Out Crystal", 38 },

            //gems
            {"Hang Eight Clear Gem (Timer)", 1 },
            {"Air Crash Clear Gem (Death Route)", 2 },
            {"Sewer or Later Clear Gem (Yellow Gem Path)", 3 },
            {"Road to Ruin Clear Gem (Death Route)", 4 },
            {"Piston it Away Clear Gem (Death Route)", 5 },
            {"Night Fight Clear Gem (Death Route)", 6 },
            {"Spaced Out Clear Gem (All Colored Gems Path)", 7 },
            {"Diggin' It Clear Gem (Death Route)", 8 },
            {"Cold Hard Crash Clear Gem (Death Route)", 9 },
            {"Sewer or Later Clear Gem (Box Gem)", 10 },
            {"Night Fight Clear Gem (Box Gem)", 12 },
            {"Hangin' Out Clear Gem (Box Gem)", 13 },
            {"Snow Go Clear Gem (Box Gem)", 14 },
            {"Ruination Clear Gem (Box Gem)", 15 },
            {"Piston it Away Clear Gem (Box Gem)", 16 },
            {"Snow Biz Clear Gem (Box Gem)", 17 },
            {"Rock It Clear Gem (Box Gem)", 18 },
            {"Cold Hard Crash Clear Gem (Box Gem)", 19 },
            {"Diggin' It Clear Gem (Box Gem)", 21 },
            {"Road to Ruin Clear Gem (Box Gem)", 22 },
            {"Un-Bearable Clear Gem (Box Gem)", 23 },
            {"Crash Dash Clear Gem (Box Gem)", 24 },
            {"Hang Eight Clear Gem (Box Gem)", 25 },
            {"Pack Attack Clear Gem (Box Gem)", 26 },
            {"Crash Crush Clear Gem (Box Gem)", 27 },
            {"Bear It Clear Gem (Box Gem)", 29 },
            {"Turtle Woods Clear Gem (Box Gem)", 30 },
            {"The Pits Clear Gem (Box Gem)", 31 },

            {"Air Crash Clear Gem (Box Gem)", 32 },
            {"Plant Food Clear Gem (Box Gem)", 33 },
            {"Bear Down Clear Gem (Box Gem)", 34 },
            {"The Eel Deal Clear Gem (Box Gem)", 35 },
            {"Bee-Having Clear Gem (Box Gem)", 36 },
            {"Totally Bear Clear Gem (Box Gem)", 37 },
            {"Spaced Out Clear Gem (Box Gem)", 38 },
            {"Totally Fly Clear Gem (Box Gem)", 39 },
            {"Ruination Clear Gem (Green Gem Path)", 57 },
            {"Snow Go Red Gem", 58 },
            {"The Eel Deal Green Gem", 59 },
            {"Bee-Having Purple Gem", 60 },
            {"Turtle Woods Blue Gem", 61 },
            {"Plant Food Yellow Gem", 62 },

            //secret exits
            { "Air Crash Secret Exit", 3 },
            { "Hangin' Out Secret Exit", 7 },
            { "Diggin' It Secret Exit", 5 },
            { "Un-Bearable Secret Exit", 6 },
            { "Bear Down Secret Exit", 4 },
        };
        // warp 1: 30, 14, 25, 31, 24 

        public static Dictionary<string, int> LocationIdInApWorld = new Dictionary<string, int> //copy pasted from AP world
        {
            { "Turtle Woods Crystal", 1 },
            { "Turtle Woods Clear Gem (Box Gem)", 2 },
            { "Turtle Woods Blue Gem", 3 },

            { "Snow Go Crystal", 4 },
            { "Snow Go Clear Gem (Box Gem)", 5 },
            { "Snow Go Red Gem", 6 },

            { "Hang Eight Crystal", 7 },
            { "Hang Eight Clear Gem (Box Gem)", 8 },
            { "Hang Eight Clear Gem (Timer)", 9 },

            { "The Pits Crystal", 10 },
            { "The Pits Clear Gem (Box Gem)", 11 },

            { "Crash Dash Crystal", 12 },
            { "Crash Dash Clear Gem (Box Gem)", 13 },

            { "Ripper Roo Defeated", 14 },

            { "Snow Biz Crystal", 15 },
            { "Snow Biz Clear Gem (Box Gem)", 16 },

            { "Air Crash Crystal", 17 },
            { "Air Crash Clear Gem (Box Gem)", 18 },
            { "Air Crash Clear Gem (Death Route)", 19 },

            { "Bear It Crystal", 20 },
            { "Bear It Clear Gem (Box Gem)", 21 },

            { "Crash Crush Crystal", 22 },
            { "Crash Crush Clear Gem (Box Gem)", 23 },

            { "The Eel Deal Crystal", 24 },
            { "The Eel Deal Clear Gem (Box Gem)", 25 },
            { "The Eel Deal Green Gem", 26 },

            { "Komodo Brothers Defeated", 27 },

            { "Plant Food Crystal", 28 },
            { "Plant Food Clear Gem (Box Gem)", 29 },
            { "Plant Food Yellow Gem", 30 },

            { "Sewer or Later Crystal", 31 },
            { "Sewer or Later Clear Gem (Box Gem)", 32 },
            { "Sewer or Later Clear Gem (Yellow Gem Path)", 33 },

            { "Bear Down Crystal", 34 },
            { "Bear Down Clear Gem (Box Gem)", 35 },

            { "Road to Ruin Crystal", 36 },
            { "Road to Ruin Clear Gem (Box Gem)", 37 },
            { "Road to Ruin Clear Gem (Death Route)", 38 },

            { "Un-Bearable Crystal", 39 },
            { "Un-Bearable Clear Gem (Box Gem)", 40 },

            { "Tiny Tiger Defeated", 41 },

            { "Hangin' Out Crystal", 42 },
            { "Hangin' Out Clear Gem (Box Gem)", 43 },

            { "Diggin' It Crystal", 44 },
            { "Diggin' It Clear Gem (Box Gem)", 45 },
            { "Diggin' It Clear Gem (Death Route)", 46 },

            { "Cold Hard Crash Crystal", 47 },
            { "Cold Hard Crash Clear Gem (Box Gem)", 48 },
            { "Cold Hard Crash Clear Gem (Death Route)", 49 },

            { "Ruination Crystal", 50 },
            { "Ruination Clear Gem (Box Gem)", 51 },
            { "Ruination Clear Gem (Green Gem Path)", 52 },

            { "Bee-Having Crystal", 53 },
            { "Bee-Having Clear Gem (Box Gem)", 54 },
            { "Bee-Having Purple Gem", 55 },

            { "Dr. N. Gin Defeated", 56 },

            { "Piston it Away Crystal", 57 },
            { "Piston it Away Clear Gem (Box Gem)", 58 },
            { "Piston it Away Clear Gem (Death Route)", 59 },

            { "Rock It Crystal", 60 },
            { "Rock It Clear Gem (Box Gem)", 61 },

            { "Night Fight Crystal", 62 },
            { "Night Fight Clear Gem (Box Gem)", 63 },
            { "Night Fight Clear Gem (Death Route)", 64 },

            { "Pack Attack Crystal", 65 },
            { "Pack Attack Clear Gem (Box Gem)", 66 },

            { "Spaced Out Crystal", 67 },
            { "Spaced Out Clear Gem (Box Gem)", 68 },
            { "Spaced Out Clear Gem (All Colored Gems Path)", 69 },

            // { "Dr. Neo Cortex Defeated", 70 },

            { "Totally Bear Clear Gem (Box Gem)", 71 },
            { "Totally Fly Clear Gem (Box Gem)", 72 },

          
            { "Turtle Woods Exit", 73 },
            { "Snow Go Exit", 74 },
            { "Hang Eight Exit", 75 },
            { "The Pits Exit", 76 },
            { "Crash Dash Exit", 77 },
            { "Snow Biz Exit", 79 },
            { "Air Crash Exit", 80 },
            { "Bear It Exit", 81 },
            { "Crash Crush Exit", 82 },
            { "The Eel Deal Exit", 83 },
            { "Plant Food Exit", 85 },
            { "Sewer or Later Exit", 86 },
            { "Bear Down Exit", 87 },
            { "Road to Ruin Exit", 88 },
            { "Un-Bearable Exit", 89 },
            { "Hangin' Out Exit", 91 },
            { "Diggin' It Exit", 92 },
            { "Cold Hard Crash Exit", 93 },
            { "Ruination Exit", 94 },
            { "Bee-Having Exit", 95 },
            { "Piston it Away Exit", 97 },
            { "Rock It Exit", 98 },
            { "Night Fight Exit", 99 },
            { "Pack Attack Exit", 100 },
            { "Spaced Out Exit", 101 },
            { "Totally Bear Exit", 102 },
            { "Totally Fly Exit", 103 },

            // Secret exits
            { "Air Crash Secret Exit", 104 },
            { "Hangin' Out Secret Exit", 105 },
            { "Diggin' It Secret Exit", 106 },
            { "Un-Bearable Secret Exit", 107 },
            { "Bear Down Secret Exit", 108 },

            // Extra

            { "Polar Lives Secret", 109 }

        };

        public static Dictionary<string, int> levelNameToId = new Dictionary<string, int>
        {
            { "Turtle Woods", 0x1E },
            { "Snow Go", 0x0E },
            { "Hang Eight", 0x19 },
            { "The Pits", 0x1F },
            { "Crash Dash", 0x18 },
            { "Ripper Roo", 0x06 },
            { "Snow Biz", 0x11 },
            { "Air Crash", 0x20 },
            { "Bear It", 0x1D },
            { "Crash Crush", 0x1B },
            { "The Eel Deal", 0x23 },
            { "Komodo Brothers", 0x08 },
            { "Plant Food", 0x21 },
            { "Sewer or Later", 0x0A },
            { "Bear Down", 0x22 },
            { "Road to Ruin", 0x16 },
            { "Un-Bearable", 0x17 },
            { "Tiny Tiger", 0x03 },
            { "Hangin' Out", 0x0D },
            { "Diggin' It", 0x15 },
            { "Cold Hard Crash", 0x13 },
            { "Ruination", 0x0F },
            { "Bee-Having", 0x24 },
            { "Dr. N. Gin", 0x09 },
            { "Piston it Away", 0x10 },
            { "Rock It", 0x12 },
            { "Night Fight", 0x0C },
            { "Pack Attack", 0x1A },
            { "Spaced Out", 0x26 },
            { "Dr. Neo Cortex", 0x07 },
            { "Totally Bear", 0x25 },
            { "Totally Fly", 0x27 },
        };

    }


}
