using Archipelago.Core.Models;
using Archipelago.Core.Util;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace C2AP
{
    internal class FruitCheck
    {
        public class FruitBundle
        {
            public SortedSet<uint> collectedFruits = new SortedSet<uint>();
            public SortedSet<uint> requiredFruits = new SortedSet<uint>();
            public int requiredFruitCount;
            public int locationId;
            public string locationName = "";
        }
        //private struct FruitBundle
        //{
        //    public SortedSet<uint> collectedFruits;
        //    public int requiredFruitCount;
        //    public int locationId;
        //}

        private static Dictionary<uint, int> ?FruitIdToBundle;

        public static List<FruitBundle> ?Bundles;

        private static Dictionary<uint, (int start, int end)> ?LevelIdToBundle;

        private static Dictionary<int, int> ?LocationIdToBundle;

        private static Timer checkFruitTimer = new Timer();

        private static uint lastLevelId = 0;
        public static void Initialize()
        {
            if (FruitIdToBundle == null || Bundles == null)
            {
                int fruit_sanity = Helpers.GetOptionValue("fruit_sanity");
                if (fruit_sanity == 0 || fruit_sanity == -1) return;
                try
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var resourceName = "C2AP.fruitbundles.txt";

                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        //if (reader == null) return;
                        FruitIdToBundle = new Dictionary<uint, int>();
                        LocationIdToBundle = new Dictionary<int, int>();
                        Bundles = new List<FruitBundle>();
                        LevelIdToBundle = new Dictionary<uint, (int start, int end)>();

                        int bundleLocationIdOffset = 10000;
                        if (fruit_sanity == 2)
                        {
                            bundleLocationIdOffset = 20000;
                        }
                        string line;
                        uint id = 0;
                        uint levelid = 0;
                        int totalBundles = 0;
                        int currentBundle = -1;
                        string levelname = "";
                        string bundlename = "";
                        int start = 0;
                        FruitBundle bundle = new();
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line[0] == '#')
                            {
                                if (line.Contains("level:"))
                                {
                                    levelname = line.Replace("#level: ", "");
                                }
                                else
                                {
                                    if (bundlename != "")
                                    {                   //level_name + " " + bundle_name + " bundle (" + str(wumpa_count) + " wumpas)"
                                        if (fruit_sanity == 1)
                                        {
                                            bundle.locationName = $"{levelname} {bundlename}  bundle ({bundle.requiredFruitCount} wumpas)";
                                        }
                                        else // == 2
                                        {
                                            bundle.locationName = $"{levelname} {bundlename} Wumpa #";
                                        }
                                        
                                    }
                                    bundlename = line.Replace("#", "");
                                }
                                continue;
                            }
                            string[] split = line.Split('-');
                            if (split.Length == 1)
                            {
                                if (levelid != 0)
                                {
                                    LevelIdToBundle[levelid] = (start, totalBundles);
                                }
                                start = totalBundles;
                                levelid = Convert.ToUInt32(split[0], 16);
                                currentBundle = -1;
                            }
                            else
                            {
                                if (fruit_sanity == 2 || Convert.ToInt32(split[0], 16) != currentBundle)
                                {
                                    currentBundle = Convert.ToInt32(split[0], 16);
                                    Bundles.Add(new FruitBundle());
                                    bundle = Bundles.Last();
                                    bundle.locationId = bundleLocationIdOffset + totalBundles;
                                    LocationIdToBundle[bundle.locationId] = totalBundles;
                                    totalBundles++;

                                }
                                id = Convert.ToUInt32(split[1], 16);
                                id = id << 8;
                                id += levelid;
                                FruitIdToBundle[id] = totalBundles - 1;
                                bundle.requiredFruits.Add(id);
                                bundle.requiredFruitCount++;
                            }
                        }
                        //using (FileStream fs = File.Create("ctestfile-wumpabundles.txt"))
                        //{
                        //    FruitIdToBundle.Keys.ToList().ForEach(key =>
                        //    {
                        //        uint levelid = key & 0xFF;
                        //        uint fruitid = key >> 8;
                        //        //string line = $"{key:X} - level: {levelid:X}, fruit: {fruitid:X}, bundle: {FruitIdToBundle[key]}\n";
                        //        string line = $"{levelid:X}-{fruitid:X}:{Bundles[FruitIdToBundle[key]].locationId}\n";
                        //        fs.Write(Encoding.UTF8.GetBytes(line));
                        //    });

                        //}
                        LevelIdToBundle[levelid] = (start, totalBundles);
                        Log.Logger.Debug($"Loaded {totalBundles} fruit bundles");
                    }
                }
                catch (IOException e)
                {
                    Log.Logger.Error($"An error occurred: {e.Message}");
                }
            }
            checkFruitTimer.Interval = 1400; // ms - adjust to desired tick rate
            checkFruitTimer.AutoReset = true;
            checkFruitTimer.Elapsed += (s, ev) =>
            {
                uint levelid = Memory.ReadByte(Addresses.LevelIdAddress + 1);
                
                if (levelid != lastLevelId)
                {
                    uint crashAddress = CrashObject.FindObjectAddress(0, 0);
                    if (crashAddress != 0 && crashAddress != CrashObject.cacheOffset)
                    {
                        
                        SetDeadFlags(levelid);
                        Log.Debug($"setting dead flags for new level {levelid:X}, old level {lastLevelId:X}");
                        lastLevelId = levelid;
                    }
                } 
                ScanCollectedFruitList();
            };
            checkFruitTimer.Enabled = true;
        }
        public static bool IsInitialized()
        {
            return FruitIdToBundle != null && Bundles != null;
        }
        public static void ScanCollectedFruitList()
        {
            //Memory.Write(Addresses.CurrentEntityFlagList + 0x1E * 4, 2);
            //Memory.Write(Addresses.CurrentEntityFlagList + 0x21 * 4, 2);
            //Memory.Write(Addresses.CurrentEntityFlagList + 0x23 * 4, 2);
            //Memory.Write(Addresses.CurrentEntityFlagList + 0x25 * 4, 2);

            if (FruitIdToBundle == null) return;
            if (Bundles == null) return;

            uint len = Memory.ReadUInt(Addresses.FruitCollectedListStart);
            if (len == 0) return;

            uint levelId = Memory.ReadByte(Addresses.LevelIdAddress+1);
            //Log.Logger.Information("scanning");
            Memory.ReadByteArray(Addresses.FruitCollectedListStart - len, (int)len);
            //Memory.
            uint id;
            for (uint i = 0; i < len; i += 4)
            {
                //Log.Logger.Information("scanning1");
                id = Memory.ReadUInt(Addresses.FruitCollectedListStart - len + i);
                //id = id << 8;
                id += levelId;
                CheckId(id);
            }

            //clear out the list
            //Log.Logger.Information("clearing");
            Memory.WriteByteArray(Addresses.FruitCollectedListStart - len, new byte[(int)len+4]);

            //check if fruit was added during the scan
            while (true)
            {
                len += 4;
                id = Memory.ReadUInt(Addresses.FruitCollectedListStart - len);
                if (id == 0) break;
                Memory.Write(Addresses.FruitCollectedListStart - len, new byte[4]);
                id += levelId;
                CheckId(id);
            }
        }

        private static void CheckId(uint id)
        {
            if (Helpers.IsInDemo()) return;
            if (!FruitIdToBundle.TryGetValue(id, out int value))
            {
                Log.Logger.Warning($"Unknown fruit id: {id:X}");
                return;
            }
            //Log.Logger.Information("scanning3");
            SetDeadFlagForFruitId(id);
            FruitBundle bundle = Bundles[value];
            if (bundle.collectedFruits.Add(id))
            {
                if (bundle.collectedFruits.Count == bundle.requiredFruitCount)
                {
                    App.Client.SendLocation(new Location {
                        Name = bundle.locationName,
                        Id = bundle.locationId,
                        //Category = "Fruit Bundle",
                    });
                    Log.Logger.Debug($"sending {bundle.locationId}");
                }
            }
            //Log.Logger.Information("scanning6");
        }

        private static void SetDeadFlags(uint levelid)
        {
            Log.Debug($"setting dead flags for level {levelid:X}");
            if (LevelIdToBundle == null) return;
            if (Bundles == null) return;

            if (!LevelIdToBundle.TryGetValue(levelid, out (int start, int end) indices)) return;
            
            //(int start, int end) indices = LevelIdToBundle[levelid];
            Log.Debug($"start: {indices.start}, end: {indices.end}");
            for (int i = indices.start; i < indices.end; i++)
            {
                FruitBundle bundle = Bundles[i];
                foreach (uint fruitid in bundle.collectedFruits)
                {
                    SetDeadFlagForFruitId(fruitid);
                }
            }
            Log.Debug($"finished setting dead flags for level {levelid:X}");
        }
        private static void SetDeadFlagForFruitId(uint fruitid)
        {
            uint id = fruitid >> 8;
            Memory.Write(Addresses.CurrentEntityFlagList + id * 4, 2);
            Memory.Write(Addresses.ContinuePointFlagList + id * 4, 2);
            Memory.Write(Addresses.ContinuePoint2FlagList + id * 4, 2);
            Log.Debug($"setting dead flags for fruit {id:X}");
            Log.Debug($"Address{Addresses.CurrentEntityFlagList + id * 4:X}");
        }

        public static void CompleteBundle(int locationId)
        {
            if (LocationIdToBundle == null) return;
            if (Bundles == null) return;
            if (!LocationIdToBundle.TryGetValue(locationId, out int bundleIndex))
            {
                Log.Logger.Warning($"Unknown bundle location id: {locationId}");
                return;
            }
            FruitBundle bundle = Bundles[bundleIndex];
            foreach (uint fruitid in bundle.requiredFruits)
            {
                bundle.collectedFruits.Add(fruitid);
            }
        }
        public static List<uint> DebugScanFruitList()
        {
            List<uint> list = new List<uint>();
            //if (FruitIdToBundle == null) return list;
            //if (Bundles == null) return list;

            uint len = Memory.ReadUInt(Addresses.FruitCollectedListStart);
            if (len == 0) return list;

            uint levelId = Memory.ReadByte(Addresses.LevelIdAddress + 1);
            //Log.Logger.Information("scanning");
            Memory.ReadByteArray(Addresses.FruitCollectedListStart - len, (int)len);
            //Memory.
            uint id;
            for (uint i = 0; i < len; i += 4)
            {
                //Log.Logger.Information("scanning1");
                id = Memory.ReadUInt(Addresses.FruitCollectedListStart - len + i);
                id = id >> 8;
                //id += levelId;
                //CheckId(id);
                list.Add(id);
            }

            //clear out the list
            //Log.Logger.Information("clearing");
            Memory.WriteByteArray(Addresses.FruitCollectedListStart - len, new byte[(int)len + 4]);

            //check if fruit was added during the scan
            while (true)
            {
                len += 4;
                id = Memory.ReadUInt(Addresses.FruitCollectedListStart - len);
                if (id == 0) break;
                Memory.Write(Addresses.FruitCollectedListStart - len, new byte[4]);
                //id += levelId;
                id = id >> 8;
                list.Add(id);
            }
            return list;
        }
    }
}