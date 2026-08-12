using Archipelago.Core.Util;
using Archipelago.Core.Util.Hook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2AP
{
    internal class BaseHooks
    {
        public static CustomHook? ApItemsHook;
        //private static CustomHook? CenterLiftHook1;
        //private static CustomHook? CenterLiftHook2;
        //private static CustomHook? CenterLiftHook3;
        //private static CustomHook? CenterLiftHook4;

        public static void Initialize()
        {
            uint crystalAddressDelta = Addresses.CrystalLocationsAddress - Addresses.CrystalsReceivedAddress;
            uint gemAddressDelta = Addresses.GemLocationsAddress - Addresses.GemsReceivedAddress;
            uint offset = 0x80000000;

            //check if initialize is called multiple times in a row
            if (ApItemsHook != null)
            {
                if (ApItemsHook._freeAddress != 0)
                {
                    return;
                }
                //ApItemsHook.RemoveHook();
            }
            //make sure nothing is left over in the "free" addresses

            //Memory.WriteByteArray(0xf000, new byte[0xfff]);

            ApItemsHook = new CustomHook([
                "addiu $sp, $sp, 0xFFEC",
                "sw $t0, 0x4($sp)",
                "sw $t1, 0x8($sp)",
                "sw $t2, 0xc($sp)",
                //"sw $t3, 0x10($sp)",
                $"la $t0, 0x{Addresses.PausedFlag + offset:X}", //address of "paused"
                "lw $t1, 0($t0)",
                "nop", //load delay
                "beq $t1 $zero, colored_gem_check", //"bne $t0 $zero, 0x8", //"beq $t1 $zero, 0x8", //branch to colored gem check
                $"la $t0, 0x{Addresses.CrystalLocationsAddress + offset:X}",
                "beq $v0 $t0, eval_for_crystals", //branch to eval for crystals
                "addiu $t0, $t0, 0x4",
                "bne $v0 $t0, check_gems", //branch to check gems
                //eval for crystals
                "eval_for_crystals:",

                $"la $t1, 0x{crystalAddressDelta:X}",
                "subu $v0, $v0, $t1",
                "beq $zero $zero, exit", //branch to exit
                //check gems
                "check_gems:",

                $"la $t0, 0x{Addresses.GemLocationsAddress + offset:X}",
                "beq $v0 $t0, eval_for_gems", //branch to eval for gems
                "addiu $t0, $t0, 0x4",
                "bne $v0 $t0, exit", //branch to exit
                //eval for gems
                "eval_for_gems:",

                $"la $t1, 0x{gemAddressDelta:X}",
                "subu $v0, $v0, $t1",
                "beq $zero $zero, exit", //branch to exit
                //colored gem check
                "colored_gem_check:",

                $"la $t0, 0x{Addresses.GemLocationsAddress + 0x4 + offset:X}",
                "bne $v0 $t0, exit", //branch to exit
                $"la $t1, 0x{Addresses.LevelIdAddress + offset:X}",
                "lw $t1, 0($t1)",
                "addiu $t0, $zero, 0x1900", //Hang Eight
                "beq $t1, $t0, get_new_gem_address", //branch to get new gem address
                "addiu $t0, $zero, 0x1100", //Snow Biz
                "beq $t1, $t0, get_new_gem_address", //branch to get new gem address
                "addiu $t0, $zero, 0x0A00", //Sewer or Later
                "beq $t1, $t0, get_new_gem_address", //branch to get new gem address
                "addiu $t0, $zero, 0x0F00", //Ruination
                "beq $t1, $t0, get_new_gem_address", //branch to get new gem address
                "addiu $t0, $zero, 0x2600", //Spaced Out
                "beq $t1, $t0, get_new_gem_address", //branch to get new gem address
                "nop",
                "beq $zero $zero, exit", //branch to exit
                "nop",
                //get new gem address
                "get_new_gem_address:",

                $"la $v0, 0x{Addresses.GemLocationsWithReceivedColoredGemsAddress + 0x4 + offset:X}",
                //exit
                "exit:",

                $"lw $t1, 0x{CrashObject.subtypeOffset:X}($s0)", //subtype
                "addiu $t0, $zero, 0x10",// 0x10 for fruit, 0x5 for lives
                "beq $t1, $t0, Store_ID", // Store ID
                "addiu $t0, $zero, 0x5",
                "bne $t1, $t0, Real_Exit", // Real exit
                //Store ID
                "Store_ID:",

                $"lw $t1, 0x{CrashObject.stateOffset:X}($s0)", //State
                "addiu $t0, $zero, 0xd",
                "bne $t1, $t0, Real_Exit", //branch if state not 0xd to real exit
                //increment collected test by 4
                

                $"la $t0, 0x{Addresses.FruitCollectedListStart + offset:X}",
                "lw $t1, 0($t0)",
                "nop",
                "addiu $t1, $t1, 0x4",

                $"addiu $t2, $zero, 0x{Addresses.FruitCollectedListStart-Addresses.FruitCollectedListEnd:X}", //max length of list
                "beq $t1, $t2, Real_Exit", //goto real exit if list full

                $"lw $t2, 0x{CrashObject.entityIdOffset:X}($s0)", //ID
                "sw $t1, 0($t0)",

                "subu $t0, $t0, $t1",
                "sw $t2, 0($t0)", //store id

                "addiu $t0, $zero, 0x0",
                $"sw $t0, 0x{CrashObject.stateOffset:X}($s0)",
                //real exit
                "Real_Exit:",

                "lw $t0, 0x4($sp)",
                "lw $t1, 0x8($sp)",
                "lw $t2, 0xc($sp)",
                //"lw $t3, 0x10($sp)",
                "addiu $sp, $sp, 0x14",
            ]);
            //0x19, 0x11, 0x0A, 0x0F, 0x10

            //ApItemsHook.InsertHook(0x3A8C0, 0xf030);
            HookManager.AddHook(ApItemsHook, 0x3A8C0);

            

            //App.SyncGameState();

            ////exit
            //$"lw $t1, 0x{CrashObject.subtypeOffset:X}($s0)", //subtype
            //    "addiu $t0, $zero, 0x10",
            //    "bne $t1, $t0, 0xe", //real exit

            //    $"lw $t1, 0x{CrashObject.stateOffset:X}($s0)", //State
            //    "addiu $t0, $zero, 0xd",
            //    "bne $t1, $t0, 0xb", //branch if state not 0xd to real exit
            //    //increment collected test by 4
            //    $"lw $t2, 0x{CrashObject.entityIdOffset:X}($s0)", //ID

            //    $"la $t0, 0x{Addresses.FruitCollectedListStart:X}",
            //    "lw $t1, 0($t0)",
            //    "nop",
            //    "addiu $t1, $t1, 0x4",

            //    "sw $t1, 0($t0)",

            //    "subu $t0, $t0, $t1",
            //    "sw $t2, 0($t0)", //store id

            //    "addiu $t0, $zero, 0x0",
            //    $"sw $t0, 0x{CrashObject.stateOffset:X}($s0)",

            //    //real exit
            //uint address = CrashObject.FindObjectAddress(36, 8);
            //uint bytecodeAddress = CrashObject.GetGoolBytecodeAddressFromObject(address);

            //uint magicOffset = 0x180;

            //uint instructionNumber1 = 6465; //instruction number to hook for first hook
            //uint instructionNumber2 = 6488; //instruction number to hook for second hook

            

            ////uint CenterLiftHook1TargetAddress = bytecodeAddress + (instructionNumber1 * 4) - magicOffset;
            //uint CenterLiftHook2TargetAddress = bytecodeAddress + (instructionNumber2 * 4) - magicOffset;
            //uint CenterLiftHook3TargetAddress = bytecodeAddress + (instructionNumber1 * 4);
            //uint CenterLiftHook4TargetAddress = bytecodeAddress + (instructionNumber2 * 4);



            ////CustomHook.DecodeITypeInstruction(Memory.ReadUInt(CenterLiftHook1TargetAddress), out _, out _, out string rt, out _);

            ////CenterLiftHook1 = new CustomHook([
            ////    //$"la $t7, 0x{Addresses.CrystalsReceivedAddress + offset:X}",
            ////    $"la {rt}, 0x{Addresses.CrystalsReceivedAddress + offset - 0x234:X}"
            ////    //$"lw {rt}, 0($t7)"
            ////    ]);

            //CustomHook.DecodeITypeInstruction(Memory.ReadUInt(CenterLiftHook2TargetAddress), out _, out _, out string rt, out _);
            //CenterLiftHook2 = new CustomHook([
            //    //$"la $t7, 0x{Addresses.CrystalsReceivedAddress + offset + 0x4:X}",
            //    $"la {rt}, 0x{Addresses.CrystalsReceivedAddress + offset - 0x234:X}"
            //    //$"lw {rt}, 0($t7)"
            //    ]);

            //CustomHook.DecodeITypeInstruction(Memory.ReadUInt(CenterLiftHook3TargetAddress), out _, out _, out rt, out _);
            //CenterLiftHook3 = new CustomHook([
            //    //$"la $t7, 0x{Addresses.CrystalsReceivedAddress + offset + 0x4:X}",
            //    $"la {rt}, 0x{Addresses.CrystalsReceivedAddress + offset - 0x234:X}"
            //    //$"lw {rt}, 0($t7)"
            //    ]);

            //CustomHook.DecodeITypeInstruction(Memory.ReadUInt(CenterLiftHook4TargetAddress), out _, out _, out rt, out _);
            //CenterLiftHook4 = new CustomHook([
            //    //$"la $t7, 0x{Addresses.CrystalsReceivedAddress + offset + 0x4:X}",
            //    $"la {rt}, 0x{Addresses.CrystalsReceivedAddress + offset - 0x234:X}"
            //    //$"lw {rt}, 0($t7)"
            //    ]);

            ////CenterLiftHook1.InsertHook(CenterLiftHook1TargetAddress, PauseMenuItems._hookSize + PauseMenuItems._freeAddress + 0x4);
            //CenterLiftHook2.InsertHook(CenterLiftHook2TargetAddress, PauseMenuItems._hookSize + PauseMenuItems._freeAddress + 0x4);
            //CenterLiftHook3.InsertHook(CenterLiftHook3TargetAddress, CenterLiftHook2._freeAddress + CenterLiftHook2._hookSize + 0x4);
            //CenterLiftHook4.InsertHook(CenterLiftHook4TargetAddress, CenterLiftHook3._freeAddress + CenterLiftHook3._hookSize + 0x4);



            //147EB8
            //148038


            //PauseMenuItems = new CustomHook([
            //    "addiu $sp, $sp, 0xFFF0",
            //    "sw $t0, 0x4($sp)",
            //    "sw $t1, 0x8($sp)",
            //    "la $t0, 0x8005f418", //address of "paused"
            //    "lw $t1, 0($t0)",
            //    "nop", //load delay
            //    "beq $t1 $zero, 0x8", //"bne $t0 $zero, 0x8", //"beq $t1 $zero, 0x8", //branch to exit
            //    "la $t0, 0x8006d03c",
            //    "beq $v0 $t0, 0x2", //branch to eval
            //    "addiu $t0, $t0, 0x4",
            //    "bne $v0 $t0, 0x3", //branch to exit
            //    //eval
            //    $"la $t1, 0x{crystalAddressDelta:X}",
            //    "subu $v0, $v0, $t1",
            //    //exit
            //    "lw $t0, 0x4($sp)",
            //    "lw $t1, 0x8($sp)",
            //    "addiu $sp, $sp, 0x10",
            //]);
            //uint offset = 0x80000000;

            //PauseMenuItems = new CustomHook([
            //    "addiu $sp, $sp, 0xFFF0",
            //    "sw $t0, 0x4($sp)",
            //    "sw $t1, 0x8($sp)",
            //    "sw $t2, 0xC($sp)",

            //    "la $t0, 0x8005f418", //address of "paused"
            //    "lw $t0, 0($t0)",
            //    $"la $t2, 0x{Addresses.WasSwappedAddress + offset:X}",
            //    "lw $t1, 0($t2)",

            //    "beq $t0, $t1, 0x1D", //branch to exit
            //    $"la $t1, 0x{Addresses.CrystalLocationsAddress + offset:X}", //t1 holds the game's crystals address
            //    "sw $t0, 0($t2)", //set was swapped to paused state
            //    "beq $t0, $zero, 0x12", //branch to unpause
            //    //pause
            //    //save locations, overwrite with crystal items
            //    $"la $t0, 0x{Addresses.CrystalLocationsSwapAddress + offset:X}",
            //    "lw $t2, 0($t1)",
            //    "sw $t2, 0($t0)",
            //    "addiu $t0, $t0, 0x4",
            //    "addiu $t1, $t1, 0x4",
            //    "lw $t2, 0($t1)",
            //    "sw $t2, 0($t0)",

            //    $"la $t0, 0x{Addresses.CrystalsReceivedAddress + offset:X}",
            //    "addiu $t1, $t1, 0xFFFC", //subtract the 4 added earlier
            //    "lw $t2, 0($t0)",
            //    "sw $t2, 0($t1)",
            //    "addiu $t0, $t0, 0x4",
            //    "addiu $t1, $t1, 0x4",
            //    "lw $t2, 0($t0)",
            //    "sw $t2, 0($t1)",
            //    "beq $zero, $zero, 0x8", //branch to exit

            //    //unpause
            //    //restore locations
            //    $"la $t0, 0x{Addresses.CrystalLocationsSwapAddress + offset:X}",
            //    "lw $t2, 0($t0)",
            //    "sw $t2, 0($t1)",
            //    "addiu $t0, $t0, 0x4",
            //    "addiu $t1, $t1, 0x4",
            //    "lw $t2, 0($t0)",
            //    "sw $t2, 0($t1)",


            //    //exit
            //    "lw $t0, 0x4($sp)",
            //    "lw $t1, 0x8($sp)",
            //    "lw $t2, 0xC($sp)",
            //    "addiu $sp, $sp, 0x10",
            //    ]);

            //0x3A8C4 doesn't work

            //PauseMenuItems.InsertHook(0x4A8E0, 0xf030);
        }

        public static void UnInitialize()
        {
            if (ApItemsHook != null)
                HookManager.RemoveHook(ApItemsHook);
            WarpRoomRandomizer.UnInitialize();
        }
    }
}
