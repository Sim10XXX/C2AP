using Archipelago.Core;
using Archipelago.Core.AvaloniaGUI.Models;
using Archipelago.Core.AvaloniaGUI.ViewModels;
using Archipelago.Core.AvaloniaGUI.Views;
using Archipelago.Core.GameClients;
using Archipelago.Core.Models;
using Archipelago.Core.Traps;
using Archipelago.Core.Util;
using Archipelago.Core.Util.Hook;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.OpenGL;
using DynamicData.Kernel;
using Newtonsoft.Json;
using ReactiveUI;
using Serilog;
using Silk.NET.Core;
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

namespace C2AP;

public partial class App : Application
{
    public static MainWindowViewModel Context;
    public static ArchipelagoClient Client { get; set; }
    public static List<ILocation> GameLocations { get; set; }
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
        Context = new MainWindowViewModel("0.6.2");
        Context.ClientVersion = "v0.4.1";
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

        //CustomHook blockCircle = new CustomHook([
        //                "la $t0, 0x80069bb8",
        //                "lw $t1, 0($t0)",
        //                "la $t2, 0x20000000",
        //                "or $t1, $t1, $t2",
        //                "sw $t1, 0($t0)",
        //                ]);

        
        
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
                Log.Logger.Information("Available commands:");
                Log.Logger.Information("/syncCrashGameState - Syncs the game state with the current received items and completed locations.");
                Log.Logger.Information("/useQuietHints - Hints for found locations will not be displayed.");
                Log.Logger.Information("/useVerboseHints - Hints for found locations will be displayed.");
                Log.Logger.Information("/warps - Prints the current warp room destinations.");
                Log.Logger.Information("/warp <warp room number> - Prints the current warp room destination for the specified warp room.");
                Log.Logger.Information("/debug - Prints available debug commands.");
                break;
            case "debug":
                Log.Logger.Information("Debug commands:");
                Log.Logger.Information("/debug_receiveDeathLink [delay (ms)] - Simulates receiving a DeathLink with an optional delay.");
                Log.Logger.Information("/debug_snapshot <name> - Creates a snapshot of the game's current memory and saves it with the specified name.");
                Log.Logger.Information("/debug_itemState - Prints the current item state.");
                Log.Logger.Information("/debug_locationState - Prints the current location state.");
                Log.Logger.Information("These should be disabled if you are on a full release build: ");
                Log.Logger.Information("/debug_openWarpRoom - Grants full access to the warp room");
                Log.Logger.Information("/debug_sendGoal - Sends a goal completion to the server.");
                break;
            case "warps":
            case "warp":
            case "warproom":
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
            case "debug_itemstate":
                if (Client.ItemState == null) break;
                List<Item> items = Client.ItemState.ReceivedItems.OfType<Item>().ToList();
                if (items.Count == 0)
                {
                    Log.Logger.Information("No items have been received yet.");
                    break;
                }
                foreach (Item item in items)
                {
                    Log.Logger.Information($"{item.Name}");
                }
                break;
            case "debug_locationstate":
                if (Client.LocationState == null) break;
                List<Location> locations = Client.LocationState.CompletedLocations.OfType<Location>().ToList();
                if (locations.Count == 0)
                {
                    Log.Logger.Information("No locations have been completed yet.");
                    break;
                }
                foreach (Location location in locations)
                {
                    Log.Logger.Information($"{location.Name}");
                }
                break;
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
            Client.CancelMonitors();
            Client.Connected -= OnConnected;
            Client.Disconnected -= OnDisconnected;
            Client.ItemReceived -= ItemReceived;
            Client.MessageReceived -= Client_MessageReceived;
            Client.LocationCompleted -= Client_LocationCompleted;
            Client.CurrentSession.Locations.CheckedLocationsUpdated -= Locations_CheckedLocationsUpdated;
        }
        DuckstationClient? client = null;
        try
        {
            client = new DuckstationClient();
        }
        catch (ArgumentException ex)
        {
            Log.Logger.Warning("Duckstation not running, open Duckstation and launch the game before connecting!");
            return;
        }
        var DuckstationConnected = client.Connect();
        if (!DuckstationConnected)
        {
            Log.Logger.Warning("Duckstation not running, open Duckstation and launch the game before connecting!");
            return;
        }
        Client = new ArchipelagoClient(client);
        Client.ShouldSaveStateOnItemReceived = false;

        Memory.GlobalOffset = Memory.GetDuckstationOffset();

        //InputLock.Initialize();
        //InputLock.LockInput(InputFlag.Square);
        //Helpers.ClearHookMemory();
        

        

        Client.Connected += OnConnected;
        Client.Disconnected += OnDisconnected;

        await Client.Connect(e.Host, "Crash2", "");
        if (!Client.IsConnected)
        {
            Log.Logger.Error("Your host seems to be invalid.  Please confirm that you have entered it correctly.");
            return;
        }
        GameLocations = Helpers.BuildLocationList();
        Client.LocationCompleted += Client_LocationCompleted;
        Client.CurrentSession.Locations.CheckedLocationsUpdated += Locations_CheckedLocationsUpdated;
        Client.MessageReceived += Client_MessageReceived;
        Client.ItemReceived += ItemReceived;
        Client.EnableLocationsCondition = () => Helpers.IsInGame() && Helpers.IsConnectionValid();
        await Client.Login(e.Slot, !string.IsNullOrWhiteSpace(e.Password) ? e.Password : null);
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
            Client.MonitorLocations(GameLocations);
        }
        else
        {
            Log.Logger.Error("Not in game. Please wait until the game is running before connecting");
            Log.Logger.Error("Locations will not be monitored and no features will be available");
        }
       
        //BaseHooks.Initialize();
        //WarpRoomRandomizer.Initialize();
        //CrashDeathLink.Initialize(e.Slot);


        //InputLock.Initialize();

        //InputLock.LockInput(InputFlag.All);
        //InputLock.UnlockInput(InputFlag.All);

        //CrashEvent.Initialize();
        //Traps.Initialize();
        //CrashObjectMod.Initialize();
        //GimmickLock.Initialize();
        //Helpers.StartCheckEmulationPaused();
        //Helpers.StartCheckLifeCount();

        //Timer testTimer = new Timer(100);
        //testTimer.Elapsed += (s, ev) =>
        //{
        //    testValue++;
        //    Memory.Write(0xF2EC, (uint)testValue);
        //};
        //testTimer.Start();
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
        if (Client.LocationState == null) return;
        if (Client.ItemState == null) return;

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
    }

    public static void SyncGameState()
    {
        // Adds locationState and itemState to the current crashState
        if (Client.LocationState == null) return;
        if (Client.ItemState == null) return;

        List<Location> locations = Client.LocationState.CompletedLocations.OfType<Location>().ToList();
        uint maxLifeCount = 0;
        foreach (Location location in locations)
        {
            //Log.Information($"Location: {location.Name} (ID: {location.Id})");
            string? levelName = Addresses.levelNameToId.Keys.FirstOrDefault(location.Name.Contains);
            if (levelName != null)
            {
                Helpers.seenLevelIds.Add((uint)Addresses.levelNameToId[levelName]);
            }
            if (location.Id >= 10000)
            {
                ItemCheck.CompleteBundle(location.Id);
                continue;
            }
            if (location.Id >= Helpers.lifeCountBaseId)
            {
                uint lifeCount = (uint)location.Id - Helpers.lifeCountBaseId;
                if (lifeCount > maxLifeCount) maxLifeCount = lifeCount;
                continue;
            }
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

        List<Item> items = Client.ItemState.ReceivedItems.ToList();
        uint crystalCount = 0;
        uint clearGemCount = 0;
        List<int> coloredGems = new();
        foreach (Item item in items)
        {
            switch (item.Name)
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

        }
        UpdateCrashState();
    }

    private static void IncrementByte(uint address)
    {
        uint data = Memory.ReadByte(address);
        data++;
        if (data > 0xFF) 
            data = 0xFF;
        Memory.WriteByte(address, (byte) data);
    }

    private static void CheckGoalCondition()
    {
        if (Client.LocationState == null) return;
        if (Client.ItemState == null) return;
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
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                spans.Add(new TextSpan() { Text = part.Text, TextColor = new SolidColorBrush(Color.FromRgb(part.Color.R, part.Color.G, part.Color.B)) });
            });
        }
        lock (_lockObject)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
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
