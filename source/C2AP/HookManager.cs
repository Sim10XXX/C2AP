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
    }
}
