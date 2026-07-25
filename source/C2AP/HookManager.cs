using Archipelago.Core.Util;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2AP
{
    internal class HookManager
    {
        private static List<CustomHook> ActiveHooks = new();

        private const uint FreeSpaceStart = 0xf000;

        private const uint FreeSpaceEnd = 0xfffc;
        
        public static void AddHook(CustomHook hook, uint targetAddress)
        {
            foreach (CustomHook activeHook in ActiveHooks) 
            {
                if (activeHook._targetAddress == targetAddress)
                {
                    Log.Error($"A hook is already active at address 0x{targetAddress:X}");
                    Log.Error("Aborting hook insertion to prevent problems with removing hooks in the wrong order");
                    return;
                }
            }


            uint freeAddress;
            if (ActiveHooks.Count == 0)
            {
                freeAddress = FreeSpaceStart;
            }
            else
            {
                CustomHook lastHook = ActiveHooks[ActiveHooks.Count() - 1];
                freeAddress = (uint)(lastHook._freeAddress + lastHook._hookSize) + 0x4;
            }

            hook.InsertHook(targetAddress, freeAddress);
            if (freeAddress + hook._hookSize > FreeSpaceEnd)
            {
                Log.Error("Inserted hook exceeded free space limit, game might become unstable");
            }
            ActiveHooks.Add(hook);
        }

        public static void RemoveHook(CustomHook hook)
        {
            int index = ActiveHooks.IndexOf(hook);
            if (index == -1)
            {
                Log.Error("Hook not found in ActiveHooks list");
                return;
            }

            uint freeAddress;
            // Remove the specified hook
            hook.RemoveHook();
            ActiveHooks.RemoveAt(index);

            if (index == 0)
            {
                freeAddress = FreeSpaceStart;
            }
            else
            {
                CustomHook lastHook = ActiveHooks[index - 1];
                freeAddress = (uint)(lastHook._freeAddress + lastHook._hookSize) + 0x4;
            }

            // Shift all subsequent hooks down to fill the gap left by the removed hook
            for (; index < ActiveHooks.Count; index++)
            {
                ulong targetAddress = ActiveHooks[index]._targetAddress;
                ActiveHooks[index].RemoveHook();
                ActiveHooks[index].InsertHook(targetAddress, freeAddress);
                freeAddress = (uint)(ActiveHooks[index]._freeAddress + ActiveHooks[index]._hookSize) + 0x4;
            }
        }

        public static void RefreshHooks()
        {
            foreach (var hook in ActiveHooks)
            {
                ulong targetAddress = hook._targetAddress;
                ulong freeAddress = hook._freeAddress;
                hook.RemoveHook();
                hook.InsertHook(targetAddress, freeAddress);
            }
        }

        public static void ClearHooks()
        {
            foreach (var hook in ActiveHooks)
            {
                hook.RemoveHook();
            }
            ActiveHooks.Clear();
        }

        public static void ReplaceAsm(CustomHook hook, List<string> asm)
        {
            if (ActiveHooks.All(h => h != hook))
            {
                Log.Warning("ReplaceAsm: hook not found");
                return;
            }
            if (hook._targetAddress == 0 && hook._freeAddress == 0)
            {
                Log.Warning("can't run ReplaceAsm on uninserted hook");
                return;
            }
            uint targetAddress = (uint) hook._targetAddress;
            int targetInstructionSize = hook._targetInstructionSize;
            //ulong freeAddress = hook._freeAddress;

            RemoveHook(hook);
            hook._asm = asm;
            hook._bytes = CustomHook.ConvertAsm(asm);
            //Log.Information("replaceasm1");
            AddHook(hook, targetAddress);
            //Log.Information("replaceasm2");
        }

        public static void ClearHooksFromPreviousConnection()
        {
            if (ActiveHooks.Count != 0)
            {
                Log.Error("ClearHooksFromPreviousConnection should only be called when HookManager is unaware of any hooks");
                return;
            }
            byte[] bytes = Memory.ReadByteArray(FreeSpaceStart, (int)(FreeSpaceEnd - FreeSpaceStart));
            int previousZeroes = 0;
            int hookStartIndex = 0;
            if (bytes[0] == 0 &&
                    bytes[1] == 0 &&
                    bytes[2] == 0 &&
                    bytes[3] == 0)
            {
                //Nothing to clear
                return;
            }
            for (int i = 0; i < bytes.Length; i+=4) 
            {
                if (bytes[i] == 0 &&
                    bytes[i + 1] == 0 &&
                    bytes[i + 2] == 0 &&
                    bytes[i + 3] == 0)
                {
                    previousZeroes++;
                    if (previousZeroes >= 3)
                    {
                        Log.Information($"Stopping at {i + FreeSpaceStart:X}");
                        break;
                    }
                    if (previousZeroes == 1)
                    {
                        continue;
                    }
                    // 2 nop's in a row imply the end of a hook, so remove the hook

                    // The first part of the hook containes the original target instructions
                    byte[] first = bytes[hookStartIndex..(hookStartIndex + 8)];

                    // The jmpback is always the last instruction, and can be used to find the target address
                    byte[] jmpback = bytes[(i-8)..(i-4)];

                    // Reverse to get rid of little endian (to make it easier for me)
                    //jmpback.Reverse();
                    jmpback[3] &= 0x03; // Clear the top 6 bits, since they are not part of the address
                    uint address = BitConverter.ToUInt32(jmpback.ToArray(), 0);
                    address -= 2; // Go back 2 instructions, since the jmp goes after the original target instructions
                    address <<= 2; // Shift left to get the actual address (undoing MIPS encoding)

                    Log.Information($"Target of hook is at {address:X}");

                    // Write the original instructions back to the target address
                    Memory.WriteByteArray(address, first);

                    // Clear the hook from free space
                    Memory.WriteByteArray((ulong) hookStartIndex + FreeSpaceStart, new byte[i -hookStartIndex]);


                    hookStartIndex = i + 4;
                }
                else
                {
                    previousZeroes = 0;
                }
            }

        }
    }
}
