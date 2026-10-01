using Archipelago.Core;
using Archipelago.Core.AvaloniaGUI.Models;
using Archipelago.Core.AvaloniaGUI.ViewModels;
using Archipelago.Core.AvaloniaGUI.Views;
//using Archipelago.Core.GameClients;
using Archipelago.Core.Helpers;
using Archipelago.Core.Models;
using Archipelago.Core.Traps;
using Archipelago.Core.Util;
using Archipelago.Core.Util.Hook;
using Archipelago.Core.Util.PlatformMemory;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.Remote.Protocol.Viewport;
using Newtonsoft.Json;
using ReactiveUI;
using Serilog;
//using Silk.NET.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Location = Archipelago.Core.Models.Location;
using Timer = System.Timers.Timer;
using Color = Avalonia.Media.Color;
using Silk.NET.Core;

namespace C2AP;

public partial class App : Application
{
    public static MainWindowViewModel Context;
    public static ArchipelagoClient Client { get; set; }
    public static List<ILocation> GameLocations { get; set; }
    public static Dictionary<long, ILocation> GameLocationsById { get; set; }
    public static Dictionary<string, object> SlotData { get; private set; } = new();
    private static readonly object _lockObject = new object();
    private static Dictionary<string, string> _hintsList { get; set; }
    private static bool _hasSubmittedGoal { get; set; }
    private static bool _useQuietHints { get; set; }

    private static uint _execCount;

    private static uint[] _execParam = [];

    public static uint testValue = 0;

    private static Timer testTimer = new Timer();
    public class CrashState
    {
        public uint Crystals;
        public uint ClearGems;

        public uint MaxLifeCount;
        public uint[] LifeCountChecks = [];

        public byte[] CrystalLocations = new byte[8];
        public byte[] GemLocations = new byte[8];
        public byte[] LevelExitLocations = new byte[8];

        public byte[] GemLocationsWithReceivedColoredGems = new byte[8];

        public bool RedGem;
        public bool GreenGem;
        public bool PurpleGem;
        public bool BlueGem;
        public bool YellowGem;

        // unlocked gimmicks
        public bool Polar;
        public bool Jetpack;
        public bool Jetboard;
        public bool Fireflies;

        public SortedSet<Ability> UnlockedAbilities = new();

    }

    public static CrashState crashState = new CrashState();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Start();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Context
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainWindow
            {
                DataContext = Context
            };
        }
        base.OnFrameworkInitializationCompleted();
    }

    public void Start()
    {
        Context = new MainWindowViewModel();
        Context.ClientVersion = "v0.5.0-pre1-1";
        Context.ConnectClicked += Context_ConnectClicked;
        Context.CommandReceived += (e, a) =>
        {
            if (string.IsNullOrWhiteSpace(a.Command)) return;
            Client?.SendMessage(a.Command);
            HandleCommand(a.Command);
        };
        Context.ConnectButtonEnabled = true;
        _hintsList = null;
        _hasSubmittedGoal = false;
        _useQuietHints = true;
        //Log.Logger.Information("Hello World");
        Log.Logger.Information("This Archipelago Client is compatible only with the Crash Bandicoot 2 Europe (PAL) Release");
        Log.Logger.Information("Trying to play with a different version will not work and may release all of your locations at the start.");
    }


    private void HandleCommand(string command)
    {
        command = command.TrimStart('/');
        string[] args = command.Split(' ');
        uint crashAddress;
        switch (args[0].ToLower())
        {
            //case "clearcrashgamestate":
            //    Log.Logger.Information("Clearing the game state.  Please reconnect to the server while in game to refresh received items.");
            //    Client.ForceReloadAllItems();
            //    break;
            case "synccrashgamestate":
                Log.Logger.Information("Syncing the game state.");
                SyncGameState();
                UpdateCrashState();
                Log.Logger.Information("Sync complete.");
                break;
            case "usequiethints":
                Log.Logger.Information("Hints for found locations will not be displayed.  Type 'useVerboseHints' to show them.");
                _useQuietHints = true;
                break;
            case "useverbosehints":
                Log.Logger.Information("Hints for found locations will be displayed.  Type 'useQuietHints' to show them.");
                _useQuietHints = false;
                break;
            case "help":
                Log.Logger.Information("\tAvailable commands:");
                Log.Logger.Information("/syncCrashGameState - Syncs the game state with the current received items and completed locations.");
                Log.Logger.Information("/useQuietHints - Hints for found locations will not be displayed.");
                Log.Logger.Information("/useVerboseHints - Hints for found locations will be displayed.");
                Log.Logger.Information("/warps - Prints the current warp room destinations.");
                Log.Logger.Information("/warp <warp room number> - Prints the current warp room destination for the specified warp room.");
                Log.Logger.Information("/debug - Prints available debug commands.");
                break;
            case "debug":
                Log.Logger.Information("\tDebug commands:");
                Log.Logger.Information("/debug_receiveDeathLink [delay (ms)] - Simulates receiving a DeathLink with an optional delay.");
                Log.Logger.Information("/debug_snapshot <name> - Creates a snapshot of the game's current memory and saves it with the specified name.");
                //Log.Logger.Information("/debug_itemState - Prints the current item state.");
                //Log.Logger.Information("/debug_locationState - Prints the current location state.");
                Log.Logger.Information("\tThese should be disabled if you are on a full release build: ");
                Log.Logger.Information("/debug_openWarpRoom - Grants full access to the warp room");
                Log.Logger.Information("/debug_sendGoal - Sends a goal completion to the server.");
                Log.Logger.Information("/debug_unlockAbilities - Unlocks all abilities");
                Log.Logger.Information("/debug_lockAbilities - Locks all abilities");
                Log.Logger.Information("/debug_unlock <ability name> - Unlocks the specified ability");
                break;
            case "warps":
            case "warp":
            case "warproom":
            case "montyhall":
                if (args.Length == 1)
                {
                    WarpRoomRandomizer.PrintMontyHallDestinations();
                    return;
                }
                if (args.Length != 2)
                {
                    Log.Logger.Warning("Usage: /warp <warp room number>");
                    return;
                }
                int warpRoom;
                if (!int.TryParse(args[1], out warpRoom))
                {
                    Log.Logger.Warning("Invalid warp room number. Please enter a number.");
                    return;
                }
                if (warpRoom < 1 || warpRoom > 6)
                {
                    Log.Logger.Warning("Invalid warp room number. Please enter a number between 1 and 6.");
                    return;
                }
                WarpRoomRandomizer.PrintMontyHallDestinations(warpRoom);

                break;
            case "goolr":
                // 2326: plant food, rock it, ruination, hang eight, unbearable, crash crush, pack attack
                // extra lines: 1102, 1270 (ish)
                uint goolAdd = CrashObject.GetGoolBytecodeAddressFromObject(CrashObject.FindObjectAddress(0, 0));
                //Log.Logger.Information($"gool: {goolAdd:X}");
                ulong offset1 = 0;
                if (args.Length == 1)
                {
                    break;
                }
                if (args.Length >= 2)
                {
                    ulong.TryParse(args[1], out offset1);
                }
                Log.Logger.Information($"{goolAdd + offset1 * 4:X} : {Memory.ReadInt(goolAdd + offset1 * 4):X}");
                break;
            case "gool":




                //// state 4: walking animation related
                // 838
                // 1616
                // 2522
                // 2878

                //// state 16:crouch
                //// 1489: return crashes the game
                // 1625: kinda bugs out
                // 1746: -

                //// state 17
                // 1525: prevents un-crouching
                // 1687: something with transitioning from crawl to crouch
                // 1754: prevents going from idle to crouch (you just slide instead)

                //// state 18
                // 1521: crawl
                // 1592: crawl during the crouch animation

                //// state 19: slide
                //// 1719: return causes the slide to crouch
                // 835: slide from idle (also prevents crouching)
                // 1425: slide from running
                // 2876: ??

                //// state 20: slam
                // 2078: slam (in some edge case state)
                // 2340: slam

                //// state 22:
                // 1788 slide spin

                //// state 23:
                // 803: jump from idle state
                // 2844: jump from landing state
                // 3096: idk

                //// state 24
                // 1656: crouch jump
                // 1810: slide jump

                //// state 42: something to do with landing
                // 2080
                // 2162
                // 2318
                // 2408
                // 2746

                //// state 43
                // 1954: no visible effect
                // 2184: -
                // 2316: -
                // 2798: -

                //// state 44
                // 821: standing spin
                // 1415: running spin
                // 2862: landing spin

                //// state 45: spin in mid air (these didn't work when in plant food)
                // 2062: prevents spinning when rising in a regular jump
                // 2152: prevents spinning during a crouch/slide jump
                // 2324: prevents spinning when falling in a regular jump

                //// state 47
                // 2949: gets rid of glitchy high jump with some weird side effects
                // 3002: nothing?
                uint goolAddress = CrashObject.GetGoolBytecodeAddressFromObject(CrashObject.FindObjectAddress(0, 0));
                Log.Logger.Information($"gool: {goolAddress:X}");
                ulong offset = 0;
                int val = 0;
                if (args.Length == 1)
                {
                    break;
                }
                if (args.Length >= 2)
                {
                    ulong.TryParse(args[1], out offset);
                }
                if (args.Length >= 3)
                {
                    if (args[2] == "ret")
                    {
                        val = 0x31894000;
                    }
                }
                Memory.Write(goolAddress + offset * 4, val);
                //Memory.Write(0x14a3e0 + 1658 * 4, Memory.ReadInt(0x14a3e0 + 1686 * 4));
                //Memory.Write(0x14a3e0 + 1659 * 4, Memory.ReadInt(0x14a3e0 + 1687 * 4));
                //Memory.Write(0x14a3e0 + 1660 * 4, Memory.ReadInt(0x14a3e0 + 1688 * 4));
                Log.Logger.Information($"wrote at {goolAddress + offset * 4:X}: {val:X}");
                break;
            case "debug_receivedeathlink":
                //break;
                int delay = 1;
                if (args.Length == 2)
                {
                    delay = int.Parse(args[1]);
                    if (delay <= 0)
                    {
                        delay = 1;
                    }
                }
                else if (args.Length >= 3)
                {
                    Log.Logger.Warning("Usage: /debug_receiveDeathLink [delay (ms)]");
                }
                //Memory.Write(Addresses.SecretEntranceFlags, 0);
                //break;
                testTimer.Elapsed += (s, ev) => CrashDeathLink.OnDeathLinkReceived(new("test"));
                testTimer.Interval = delay;
                testTimer.AutoReset = false;
                testTimer.Start();

                crashAddress = CrashObject.FindObjectAddress(0, 0);
                if (crashAddress == 0 || crashAddress == CrashObject.cacheOffset) break;
                uint state = Memory.ReadUInt(crashAddress + 0x1C);
                Log.Logger.Information($"crash state: {state}");
                //{
                Log.Logger.Information($"crash address: {crashAddress + CrashObject.cacheOffset:X}");

                break;
            //case "c":
            //    if (args.Length > 1) break;
            //    crashAddress = CrashObject.FindObjectAddress(0, 0);
            //    if (crashAddress == 0 || crashAddress == CrashObject.cacheOffset) break;
            //    Log.Logger.Information($"Running Event Id: {_execCount}");
            //    Log.Logger.Information($"crash state: {Memory.ReadUInt(crashAddress + 0x1C)}");
            //    CrashEvent.CallSendEvent(0, crashAddress + CrashObject.cacheOffset, _execCount << 8, (uint)_execParam.Length, _execParam);
            //    _execCount++;
            //    break;
            //case "debug_itemstate":
            //    if (Client.ItemState == null) break;
            //    List<Item> items = Client.ItemState.ReceivedItems.OfType<Item>().ToList();
            //    if (items.Count == 0)
            //    {
            //        Log.Logger.Information("No items have been received yet.");
            //        break;
            //    }
            //    foreach (Item item in items)
            //    {
            //        Log.Logger.Information($"{item.Name}");
            //    }
            //    break;
            //case "debug_locationstate":
            //    if (Client.LocationState == null) break;
            //    List<Location> locations = Client.LocationState.CompletedLocations.OfType<Location>().ToList();
            //    if (locations.Count == 0)
            //    {
            //        Log.Logger.Information("No locations have been completed yet.");
            //        break;
            //    }
            //    foreach (Location location in locations)
            //    {
            //        Log.Logger.Information($"{location.Name}");
            //    }
            //    break;
            case "debug_openwarproom":
                //break;
                // mark bosses as complete
                uint address;
                int[] bossBits = [
                    Addresses.levelNameToId["Dr. N. Gin"],
                    Addresses.levelNameToId["Ripper Roo"],
                    Addresses.levelNameToId["Komodo Brothers"],
                    Addresses.levelNameToId["Tiny Tiger"],

                ];
                for (int i = 0; i < bossBits.Length; i++)
                {
                    address = Addresses.LevelExitsAddress + (uint)bossBits[i] / 8;
                    int bit = bossBits[i] % 8;
                    Memory.WriteBit(address, bit, true);
                }
                // mark secret entrances as open
                Memory.Write(Addresses.SecretEntranceFlags, 0x1f);

                // set to 25 crystals
                crashState.Crystals = 25;
                UpdateCrashState();

                break;
            case "debug_snapshot":
                if (args.Length != 2)
                {
                    Log.Logger.Warning("Usage: /debug_snapshot <name>");
                    return;
                }
                string filename = $"memorysnapshot_{args[1]}.mem";
                Log.Logger.Information($"Creating memory snapshot at {filename}");
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
                using (FileStream fs = File.Create(filename))
                {
                    byte[] memoryDump = Memory.ReadByteArray(0, 0b1000000000000000000000);
                    fs.Write(memoryDump, 0, memoryDump.Length);
                }
                break;
            //case "debug_sendgoal":
            //    Client.SendGoalCompletion();
            //    break;

            //address = CrystalAddress + (uint)levelid / 8;
            //int bit = levelid % 8;
            //Memory.WriteBit(address, bit, true);
            case "debug_unlockall":
            case "debug_unlockabilities":
                Log.Information("Unlocking all abilities");
                crashState.UnlockedAbilities.Clear();
                foreach (Ability ability in Enum.GetValues(typeof(Ability)))
                {
                    crashState.UnlockedAbilities.Add(ability);
                }
                UpdateCrashState();
                break;
            case "debug_lockall":
            case "debug_lockabilities":
                Log.Information("Locking all abilities");
                crashState.UnlockedAbilities.Clear();
                UpdateCrashState();
                break;
            case "debug_unlock":
                if (args.Length != 2)
                {
                    Log.Logger.Warning("Usage: /debug_unlock <ability>");
                    return;
                }
                if (Enum.TryParse<Ability>(args[1], true, out Ability abilityToUnlock))
                {
                    Log.Information($"Unlocking ability: {abilityToUnlock}");
                    crashState.UnlockedAbilities.Add(abilityToUnlock);
                    UpdateCrashState();
                }
                else
                {
                    Log.Logger.Warning($"Invalid ability: {args[1]}");
                }
                break;
        }


        //if (args[0] == "debug_sendevent")
        //{
        //    return;

        //    List<uint> eventArgv = new();
        //    //Log.Logger.Information($"try exec");
        //    for (int i = 2; i < args.Length; i++)
        //    {
        //        //Log.Logger.Information($"adding: {Convert.ToUInt32(args[i]) << 8}");
        //        eventArgv.Add(Convert.ToUInt32(args[i]) << 8);
        //    }
        //    //Log.Logger.Information($"find crash");
        //    crashAddress = CrashObject.FindObjectAddress(0, 0);
        //    if (crashAddress != 0 && crashAddress != CrashObject.cacheOffset)
        //    {
        //        Log.Logger.Information($"crash address: {crashAddress + CrashObject.cacheOffset:X}");

        //        Log.Logger.Information($"crash state: {Memory.ReadUInt(crashAddress + 0x1C)}");
        //        CrashEvent.CallSendEvent(0, crashAddress + CrashObject.cacheOffset, Convert.ToUInt32(args[1]) << 8, (uint)eventArgv.Count, eventArgv.AsArray());

        //    }
        //}
    }
    private async void Context_ConnectClicked(object? sender, ConnectClickedEventArgs e)
    {

        if (Client != null)
        {
            Client.LocationManager.CancelMonitors();
            Client.Connected -= OnConnected;
            Client.Disconnected -= OnDisconnected;
            Client.ItemManager.ItemReceived -= ItemReceived;
            Client.MessageReceived -= Client_MessageReceived;
            Client.LocationManager.LocationCompleted -= Client_LocationCompleted;
            Client.CurrentSession.Locations.CheckedLocationsUpdated -= Locations_CheckedLocationsUpdated;
        }
        GameClient gameClient = new GameClient("duckstation-qt");
        //DuckstationClient? client = null;
        //try
        //{
        //    client = new DuckstationClient();
        //}
        //catch (ArgumentException ex)
        //{
        //    Log.Logger.Warning("Duckstation not running, open Duckstation and launch the game before connecting!");
        //    return;
        //}
        //var DuckstationConnected = client.Connect();
        //if (!DuckstationConnected)
        //{
        //    Log.Logger.Warning("Duckstation not running, open Duckstation and launch the game before connecting!");
        //    return;
        //}
        if (!gameClient.Connect())
        {
            Log.Logger.Warning("Duckstation not running, open Duckstation and launch the game before connecting!");
            return;
        }

        Client = new ArchipelagoClient(gameClient);
        //Client.ShouldSaveStateOnItemReceived = false;

        //Memory.GlobalOffset = Memory.GetDuckstationOffset();
        PlatformMemory.GlobalOffset = PlatformMemory.GetDuckstationOffset();

        //InputLock.Initialize();
        //InputLock.LockInput(InputFlag.Square);
        //Helpers.ClearHookMemory();




        Client.Connected += OnConnected;
        Client.Disconnected += OnDisconnected;

        await Client.Connect(e.Host, "Crash 2");
        if (!Client.IsConnected)
        {
            Log.Logger.Error("Your host seems to be invalid.  Please confirm that you have entered it correctly.");
            return;
        }
        GameLocations = Helpers.BuildLocationList();
        GameLocationsById = new Dictionary<long, ILocation>();
        foreach (ILocation location in GameLocations)
        {
            if (!GameLocationsById.TryAdd(location.Id, location))
            {
                Log.Logger.Warning($"Failed to add location with ID {location.Id}");
            }
            else
            {
                //Log.Logger.Information($"Location added with ID {location.Id}");
            }
        }
        //GameLocationsById = GameLocations.OfType<Location>().ToDictionary(loc => (long)loc.Id, loc => (ILocation)loc);
        await Client.Login(e.Slot, !string.IsNullOrWhiteSpace(e.Password) ? e.Password : null);

        Client.LocationManager.LocationCompleted += Client_LocationCompleted;
        Client.CurrentSession.Locations.CheckedLocationsUpdated += Locations_CheckedLocationsUpdated;
        Client.MessageReceived += Client_MessageReceived;
        Client.ItemManager.ItemReceived += ItemReceived;
        Client.LocationManager.EnableLocationsCondition = () => Helpers.IsInGame() && Helpers.IsConnectionValid();
        
        //if (Client.Options?.Count > 0)
        //{
        //    Client.MonitorLocations(GameLocations);
        //    Log.Logger.Information("Warnings and errors above are okay if this is your first time connecting to this multiworld server.");
        //}
        //else
        //{
        //    Log.Logger.Error("Failed to login.  Please check your host, name, and password.");
        //}

        if (Helpers.IsInGame())
        {
            SyncGameState();
            UpdateCrashState();
            //Helpers.InitializeAll(e.Slot);
            await Client.ReceiveReady();
            Client.LocationManager.MonitorLocationsAsync(Client.CurrentSession, GameLocations);
            //Client.MonitorLocations(GameLocations);
        }
        else
        {
            Log.Logger.Error("Not in game. Please wait until the game is running before connecting");
            Log.Logger.Error("Locations will not be monitored and no features will be available");
        }
    }


    private void UpdateGemLocationsChecked()
    {
        Log.Debug("UpdateGemLocationsChecked");
        byte[] gemFlags = Memory.ReadByteArray(Addresses.GemLocationsAddress, 8);
        for (int i = 0; i < gemFlags.Length; i++)
            Log.Debug($"gemflags {i}: {gemFlags[i]:X}");
        gemFlags[Addresses.ColoredGemOffset] &= Addresses.ColoredGemMaskNegated; //clear out colored gem bits
        for (int i = 0; i < gemFlags.Length; i++)
            Log.Debug($"gemflags {i}: {gemFlags[i]:X}");
        byte receivedColoredGemFlags = Memory.ReadByte(Addresses.ColoredGemReceivedAddress);
        Log.Debug($"receivedColoredGemFlags: {receivedColoredGemFlags:X}");
        receivedColoredGemFlags &= Addresses.ColoredGemMask; //clear out clear gem bits
        Log.Debug($"receivedColoredGemFlags: {receivedColoredGemFlags:X}");

        gemFlags[Addresses.ColoredGemOffset] |= receivedColoredGemFlags; //set colored gem bits from received items
        for (int i = 0; i < gemFlags.Length; i++)
            Log.Debug($"gemflags {i}: {gemFlags[i]:X}");
        Memory.WriteByteArray(Addresses.GemLocationsWithReceivedColoredGemsAddress, gemFlags);
        //SyncGameState();
    }

    private void Client_LocationCompleted(object? sender, LocationCompletedEventArgs e)
    {
        //if (Client.GameState == null) return;
        //UpdateGemLocationsChecked();
        //SyncGameState();
        //Log.Logger.Information($"location: {e.CompletedLocation.Name}");
        UpdateCrashState();
        CheckGoalCondition();
    }

    public static void UpdateCrashState()
    {
        Helpers.shouldSyncProgress = false;
        // Updates the game with the current crashState
        //if (Client.LocationState == null) return;
        //if (Client.ItemState == null) return;

        // First get the current locations from the game
        byte[] gemFlags = Memory.ReadByteArray(Addresses.GemLocationsAddress, 8);
        byte[] crystalFlags = Memory.ReadByteArray(Addresses.CrystalLocationsAddress, 8);
        byte[] levelExitFlags = Memory.ReadByteArray(Addresses.LevelExitsAddress, 8);
        for (int i = 0; i < 8; i++)
        {
            crashState.GemLocations[i] |= gemFlags[i];
            crashState.CrystalLocations[i] |= crystalFlags[i];
            crashState.LevelExitLocations[i] |= levelExitFlags[i];
        }


        uint crystalCount = crashState.Crystals;
        uint clearGemCount = crashState.ClearGems;

        //update center lift with current crystalCount
        if (CrashObjectMod.liftMod == null)
        {
            Log.Debug("Lift mod is not initialized!");
        }
        else
        {
            List<byte[]> mods =
            [
                CustomHook.ConvertAsm([$"addiu $a0, $zero, 0x{crystalCount:X}"]).ToArray(),
                CustomHook.ConvertAsm([$"addiu $v1, $zero, 0x{crystalCount:X}"]).ToArray(),
            ];

            List<uint> modInstructionLines = [6507 - CrashObjectMod.magicOffset / 4, 6507];
            CrashObjectMod.liftMod.EditMod(mods, modInstructionLines);
        }

        //set crystal item flags
        byte[] bytes = new byte[8];
        for (int i = 0; i < bytes.Length; i++)
        {
            for (int j = 1; j < 0xFF; j = j << 1)
            {
                if (crystalCount == 0) break;
                crystalCount--;
                bytes[i] |= (byte)j;
            }
            if (crystalCount == 0) break;
        }
        Memory.WriteByteArray(Addresses.CrystalsReceivedAddress, bytes);

        //set clear gem item flags
        bytes = new byte[8];
        for (int i = 0; i < bytes.Length; i++)
        {
            int bit = 1;
            for (int j = 0; j < 8; j++)
            {
                if (clearGemCount == 0) break;
                clearGemCount--;
                if (i == Addresses.ColoredGemOffset && j == Addresses.RedGemReceivedBit)
                {
                    j = Addresses.YellowGemReceivedBit + 0x1;
                }
                bytes[i] |= (byte)bit;
                bit = bit << 1;
            }
            if (clearGemCount == 0) break;
        }
        Memory.WriteByteArray(Addresses.GemsReceivedAddress, bytes);

        //set colored gem flags

        Memory.WriteBit(Addresses.ColoredGemReceivedAddress, Addresses.RedGemReceivedBit, crashState.RedGem);
        Memory.WriteBit(Addresses.ColoredGemReceivedAddress, Addresses.GreenGemReceivedBit, crashState.GreenGem);
        Memory.WriteBit(Addresses.ColoredGemReceivedAddress, Addresses.PurpleGemReceivedBit, crashState.PurpleGem);
        Memory.WriteBit(Addresses.ColoredGemReceivedAddress, Addresses.BlueGemReceivedBit, crashState.BlueGem);
        Memory.WriteBit(Addresses.ColoredGemReceivedAddress, Addresses.YellowGemReceivedBit, crashState.YellowGem);

        //set GemLocationsWithReceivedColoredGems
        //crashState.GemLocationsWithReceivedColoredGems = crashState.GemLocations;
        for (int i = 0; i < crashState.GemLocations.Length; i++)
        {
            crashState.GemLocationsWithReceivedColoredGems[i] = crashState.GemLocations[i];
        }
        crashState.GemLocationsWithReceivedColoredGems[Addresses.ColoredGemOffset] &= Addresses.ColoredGemMaskNegated; //clear out colored gem bits
        crashState.GemLocationsWithReceivedColoredGems[Addresses.ColoredGemOffset] |= (byte)(
            (crashState.RedGem ? (0x1 << Addresses.RedGemReceivedBit) : 0) |
            (crashState.GreenGem ? (0x1 << Addresses.GreenGemReceivedBit) : 0) |
            (crashState.PurpleGem ? (0x1 << Addresses.PurpleGemReceivedBit) : 0) |
            (crashState.BlueGem ? (0x1 << Addresses.BlueGemReceivedBit) : 0) |
            (crashState.YellowGem ? (0x1 << Addresses.YellowGemReceivedBit) : 0)
        );
        Memory.WriteByteArray(Addresses.GemLocationsWithReceivedColoredGemsAddress, crashState.GemLocationsWithReceivedColoredGems);

        //set the locations that should be already done

        Memory.WriteByteArray(Addresses.GemLocationsAddress, crashState.GemLocations);
        Memory.WriteByteArray(Addresses.CrystalLocationsAddress, crashState.CrystalLocations);
        Memory.WriteByteArray(Addresses.LevelExitsAddress, crashState.LevelExitLocations);

        AbilityLock.RefreshAbilityLock();
    }

    public static void SyncGameState()
    {
        // Adds locationState and itemState to the current crashState
        //if (Client.LocationState == null) return;
        //if (Client.ItemState == null) return;

        List<long> locationIds = Client.CurrentSession.Locations.AllLocationsChecked.ToList();

        //Client.LocationManager;
        //GameLocations
        //List<Location> locations = Client.CurrentSession.Locations.AllLocationsChecked.OfType<Location>().ToList();
        uint maxLifeCount = 0;
        foreach (long locationId in locationIds)
        {
            Location? location = (Location?)GameLocationsById.GetValueOrDefault(locationId);
            //Log.Information($"Checking location ID: {locationId}");
            string? levelName = null;
            if (location != null)
            { 
                levelName = Addresses.levelNameToId.Keys.FirstOrDefault(location.Name.Contains);
                //Log.Information($"Location: {location.Name} (ID: {location.Id})");
            }

             
            if (levelName != null)
            {
                Helpers.seenLevelIds.Add((uint)Addresses.levelNameToId[levelName]);
            }
            if (locationId >= 10000)
            {
                ItemCheck.ItemBundle? bundle = ItemCheck.CompleteBundle((int)locationId);
                if (bundle != null)
                {
                    levelName = Addresses.levelNameToId.Keys.FirstOrDefault(bundle.locationName.Contains);
                    if (levelName != null)
                    {
                        Helpers.seenLevelIds.Add((uint)Addresses.levelNameToId[levelName]);
                    }
                }
                continue;
            }
            if (locationId >= Helpers.lifeCountBaseId)
            {
                uint lifeCount = (uint)locationId - Helpers.lifeCountBaseId;
                if (lifeCount > maxLifeCount) maxLifeCount = lifeCount;
                continue;
            }

            if (location == null) continue;
            if (location.Address == 0/* || location.AddressBit == 0*/) continue;
            if (location.Address >= Addresses.GemLocationsAddress && location.Address < Addresses.GemLocationsAddress + 8)
            {
                crashState.GemLocations[location.Address - Addresses.GemLocationsAddress] |= (byte)(0x1 << location.AddressBit);
            }
            else if (location.Address >= Addresses.CrystalLocationsAddress && location.Address < Addresses.CrystalLocationsAddress + 8)
            {
                crashState.CrystalLocations[location.Address - Addresses.CrystalLocationsAddress] |= (byte)(0x1 << location.AddressBit);
            }
            else if (location.Address >= Addresses.LevelExitsAddress && location.Address < Addresses.LevelExitsAddress + 8)
            {
                uint levelId = ((uint)location.Address - Addresses.LevelExitsAddress) * 8 + (uint)location.AddressBit;
                //Log.Information($"Marking level exit complete for location {location.Name} with level id {levelId}");
                crashState.LevelExitLocations[location.Address - Addresses.LevelExitsAddress] |= (byte)(0x1 << location.AddressBit);

                // For any secret exit, open up its corresponding secret entrance
                switch (levelId)
                {
                    case 43: // Air Crash Secret Exit
                        Memory.WriteBit(Addresses.SecretEntranceFlags, 4, true); // Snow Go Secret Entrance
                        break;
                    case 44: // Bear Down Secret Exit
                        Memory.WriteBit(Addresses.SecretEntranceFlags, 3, true); // Bear Down Secret Entrance
                        break;
                    case 45: // Diggin' It Secret Exit
                        Memory.WriteBit(Addresses.SecretEntranceFlags, 0, true); // Road To Ruin Secret Entrance
                        break;
                    case 46: // Un-Bearable Secret Exit
                        Memory.WriteBit(Addresses.SecretEntranceFlags, 1, true); // Totally Bear Secret Entrance
                        break;
                    case 47: // Hangin' Out Secret Exit
                        Memory.WriteBit(Addresses.SecretEntranceFlags, 2, true); // Totally Fly Secret Entrance
                        break;
                }
            }
        }
        crashState.MaxLifeCount = maxLifeCount;
        crashState.UnlockedAbilities.Clear();
        List<ItemInfo> items = Client.CurrentSession.Items.AllItemsReceived.ToList();

        uint crystalCount = 0;
        uint clearGemCount = 0;
        List<int> coloredGems = new();
        foreach (ItemInfo item in items)
        {
            switch (item.ItemName)
            {
                case "Crystal":
                    crystalCount++;
                    break;
                case "Clear Gem":
                    clearGemCount++;
                    break;
                case "Red Gem":
                    crashState.RedGem = true;
                    break;
                case "Green Gem":
                    crashState.GreenGem = true;
                    break;
                case "Purple Gem":
                    crashState.PurpleGem = true;
                    break;
                case "Blue Gem":
                    crashState.BlueGem = true;
                    break;
                case "Yellow Gem":
                    crashState.YellowGem = true;
                    break;
                case "Jetpack":
                    crashState.Jetpack = true;
                    break;
                case "Jetboard":
                    crashState.Jetboard = true;
                    break;
                case "Polar":
                    crashState.Polar = true;
                    break;
                case "Fireflies":
                    crashState.Fireflies = true;
                    break;
                default: // Abilities
                    if (Enum.TryParse<Ability>(item.ItemName.Replace(" ", ""), true, out Ability ability))
                    {
                        crashState.UnlockedAbilities.Add(ability);
                    }
                    break;
            }
        }
        if (crashState.Crystals < crystalCount)
        {
            crashState.Crystals = crystalCount;
        }
        if (crashState.ClearGems < clearGemCount)
        {
            crashState.ClearGems = clearGemCount;
        }
    }
    private async void ItemReceived(object? o, ItemReceivedEventArgs args)
    {
        Log.Logger.Debug($"Item Received: {JsonConvert.SerializeObject(args.Item)}");
        uint crashAddress;
        switch (args.Item.Name)
        {
            case "Crystal":
                crashState.Crystals++;
                break;
            case "Clear Gem":
                crashState.ClearGems++;
                break;
            case "Red Gem":
                crashState.RedGem = true;
                break;
            case "Green Gem":
                crashState.GreenGem = true;
                break;
            case "Purple Gem":
                crashState.PurpleGem = true;
                break;
            case "Blue Gem":
                crashState.BlueGem = true;
                break;
            case "Yellow Gem":
                crashState.YellowGem = true;
                break;
            case "Jetpack":
                crashState.Jetpack = true;
                break;
            case "Jetboard":
                crashState.Jetboard = true;
                break;
            case "Polar":
                crashState.Polar = true;
                break;
            case "Fireflies":
                crashState.Fireflies = true;
                break;
            case "Life":
                //CrashFunction.EnqueueEvent(CrashFunction.Event.GiveLife);
                crashAddress = CrashObject.FindObjectAddress(0, 0);
                if (crashAddress != 0 && crashAddress != CrashObject.cacheOffset)
                {
                    if (Memory.ReadByte(crashAddress + Addresses.LivesOffset) >= 99)
                        return;
                    IncrementByte(crashAddress + Addresses.LivesOffset);
                }
                if (Memory.ReadByte(Addresses.LivesGlobalAddress) >= 99)
                    return;
                IncrementByte(Addresses.LivesGlobalAddress);
                return;
            //break;
            case "Wumpa Fruit":
                //CrashFunction.EnqueueEvent(CrashFunction.Event.GiveWumpa);
                crashAddress = CrashObject.FindObjectAddress(0, 0);
                if (crashAddress != 0 && crashAddress != CrashObject.cacheOffset)
                {
                    IncrementByte(crashAddress + Addresses.WumpaOffset);
                }
                IncrementByte(Addresses.WumpaGlobalAddress);
                return;
            case "Big Crash Trap":
                Traps.AddTrap(Traps.TrapType.BigCrash);
                return;
            case "Small Crash Trap":
                Traps.AddTrap(Traps.TrapType.SmallCrash);
                return;
            case "No Lives Trap":
                Traps.AddTrap(Traps.TrapType.NoLives);
                return;
            case "Jetpack Controls Trap":
                Traps.AddTrap(Traps.TrapType.JetpackControls);
                return;
            default: // Abilities
                if (Enum.TryParse<Ability>(args.Item.Name.Replace(" ", ""), true, out Ability ability))
                {
                    crashState.UnlockedAbilities.Add(ability);
                }
                break;

        }
        UpdateCrashState();
    }

    private static void IncrementByte(uint address)
    {
        uint data = Memory.ReadByte(address);
        data++;
        if (data > 0xFF)
            data = 0xFF;
        Memory.WriteByte(address, (byte)data);
    }

    private static void CheckGoalCondition()
    {
        //if (Client.LocationState == null) return;
        //if (Client.ItemState == null) return;
        if (_hasSubmittedGoal)
        {
            return;
        }
        byte levelid = Memory.ReadByte(Addresses.LevelIdAddress + 0x1);
        if (levelid == 0x29 || levelid == 0x28)
        {
            Timer sendGoal = new Timer();
            sendGoal.Interval = 10;
            sendGoal.AutoReset = false;
            sendGoal.Elapsed += (s, ev) =>
            {
                Client.SendGoalCompletion();
            };
            sendGoal.Enabled = true;
            //Client.SendGoalCompletion();
            _hasSubmittedGoal = true;
        }
    }
    private static async void RunLagTrap()
    {
        using (var lagTrap = new LagTrap(TimeSpan.FromSeconds(20)))
        {
            lagTrap.Start();
            await lagTrap.WaitForCompletionAsync();
        }
    }

    private static void LogItem(Item item)
    {
        // Not supported at this time.
        /*var messageToLog = new LogListItem(new List<TextSpan>()
            {
                new TextSpan(){Text = $"[{item.Id.ToString()}] -", TextColor = new SolidColorBrush(Color.FromRgb(255, 255, 255))},
                new TextSpan(){Text = $"{item.Name}", TextColor = new SolidColorBrush(Color.FromRgb(200, 255, 200))}
            });
        lock (_lockObject)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                Context.ItemList.Add(messageToLog);
            });
        }*/
    }

    private void Client_MessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        // If the player requests it, don't show "found" hints in the main client.
        if (e.Message.Parts.Any(x => x.Text == "[Hint]: ") && (!_useQuietHints || !e.Message.Parts.Any(x => x.Text.Trim() == "(found)")))
        {
            LogHint(e.Message);
        }
        if (!e.Message.Parts.Any(x => x.Text == "[Hint]: ") || !_useQuietHints || !e.Message.Parts.Any(x => x.Text.Trim() == "(found)"))
        {
            Log.Logger.Information(JsonConvert.SerializeObject(e.Message));
        }
    }
    private static void LogHint(LogMessage message)
    {
        var newMessage = message.Parts.Select(x => x.Text);

        foreach (var hint in Context.HintList)
        {
            IEnumerable<string> hintText = hint.TextSpans.Select(y => y.Text);
            if (newMessage.Count() != hintText.Count())
            {
                continue;
            }
            bool isMatch = true;
            for (int i = 0; i < hintText.Count(); i++)
            {
                if (newMessage.ElementAt(i) != hintText.ElementAt(i))
                {
                    isMatch = false;
                    break;
                }
            }
            if (isMatch)
            {
                return; //Hint already in list
            }
        }
        List<TextSpan> spans = new List<TextSpan>();
        foreach (var part in message.Parts)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                spans.Add(new TextSpan() { Text = part.Text, TextColor = new SolidColorBrush(Color.FromRgb(part.Color.R, part.Color.G, part.Color.B)) });
            });
        }
        lock (_lockObject)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                Context.HintList.Add(new LogListItem(spans));
            });
        }
    }
    private static void Locations_CheckedLocationsUpdated(System.Collections.ObjectModel.ReadOnlyCollection<long> newCheckedLocations)
    {
        //if (Client.GameState == null) return;
        CheckGoalCondition();

    }

    private static void OnConnected(object sender, EventArgs args)
    {
        int currentSlot = Client.CurrentSession.ConnectionInfo.Slot;
        Log.Logger.Information("Connected to Archipelago");
        Log.Logger.Information($"Playing {Client.CurrentSession.ConnectionInfo.Game} as {Client.CurrentSession.Players.GetPlayerName(currentSlot)}");

        var slotDataTask = App.Client.CurrentSession.DataStorage.GetSlotDataAsync(currentSlot);
        slotDataTask.Wait();
        SlotData = slotDataTask.Result;

        // There is a tradeoff here when creating new threads.  Separate timers allow for better control over when
        // memory reads and writes will happen, but they take away threads for other client tasks.
        // This solution is fine with the current item pool size but won't scale with gemsanity.
        // TODO: Test which of these can be combined without impacting the end result.

        //_loadGameTimer = new Timer();
        //_loadGameTimer.Elapsed += new ElapsedEventHandler(StartSpyroGame);
        //_loadGameTimer.Interval = 5000;
        //_loadGameTimer.Enabled = true;


        // Repopulate hint list.  There is likely a better way to do this using the Get network protocol
        // with keys=[$"hints_{team}_{slot}"].
        Client?.SendMessage("!hint");
        if (!ItemCheck.IsInitialized())
        {
            ItemCheck.Initialize();
        }

    }

    private static void OnDisconnected(object sender, EventArgs args)
    {
        Log.Logger.Information("Disconnected from Archipelago");
        // Avoid ongoing timers affecting a new game.
        _hintsList = null;
        _hasSubmittedGoal = false;
        _useQuietHints = true;
        Log.Logger.Information("This Archipelago Client is compatible only with the Crash Bandicoot 2 Europe (PAL) Release");
        Log.Logger.Information("Trying to play with a different version will not work and may release all of your locations at the start.");


    }
}
