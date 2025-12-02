using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using CS2MenuManager;
using CS2MenuManager.API.Menu;
using System.Drawing;
using System.Numerics;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace LynxCheatTool;

public class LynxCheatTool : BasePlugin
{
    public override string ModuleName => "LynxCheatTool Aimbot";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "LynxHera";
    public override string ModuleDescription => "";

    private readonly Dictionary<ulong, bool> _aimbotEnabled = new();
    private readonly Dictionary<ulong, CCSPlayerController?> _currentTarget = new(); 
    private readonly Dictionary<ulong, bool> _wallhackEnabled = new(); 
    private readonly Dictionary<ulong, bool> _magicBulletEnabled = new(); 
    private readonly Dictionary<ulong, bool> _noRecoilEnabled = new(); 
    private readonly Dictionary<ulong, bool> _noFlashEnabled = new();
    private readonly Dictionary<ulong, FragIcons> _fragChangerSettings = new();  
    private readonly Dictionary<CCSPlayerController, PlayerGlowData> _playerGlowData = new(); 
    private readonly float _aimbotFov = 200f; 
    private readonly float _aimbotStickyFov = 360f; 
    private readonly float _aimbotSmooth = 1f;

    private class PlayerGlowData
    {
        public CDynamicProp? ModelRelay { get; set; }
        public CDynamicProp? ModelGlow { get; set; }
        public string? ModelName { get; set; }
    }

    [Flags]
    public enum FragIcons
    {
        None = 0,
        Headshot = 1 << 0,
        Blind = 1 << 1,
        Smoke = 1 << 2,
        Wallbang = 1 << 3,
        Noscope = 1 << 4,
        Airborne = 1 << 5,
        Dominated = 1 << 6,
        Revenge = 1 << 7,
        Wipe = 1 << 8,
        All = Headshot | Blind | Smoke | Wallbang | Noscope | Airborne | Dominated | Revenge | Wipe
    }

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Pre);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        Console.WriteLine("[LynxCheatTool] Plugin loaded!");
    }

    [ConsoleCommand("css_aimbot", "Aimbot WASD menu")]
    [RequiresPermissions("@css/root")]
    public void OnAimbotCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ShowAimbotWasdMenu(player);
    }

    [ConsoleCommand("css_magicbullet", "Magic Bullet menu")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void OnMagicBulletCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ShowMagicBulletWasdMenu(player);
    }

    [ConsoleCommand("css_cheats", "Main menu")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCheatsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        
        if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
           
            return; 
            
            
        }

        ShowMainMenu(player);
    }

    private void ShowMainMenu(CCSPlayerController player)
    {
        WasdMenu menu = new("LynxCheatTool Main Menu", this);

        menu.AddItem("🎯 Aimbot Menu", (p, o) => ShowAimbotWasdMenu(p));
        menu.AddItem("🧱 Wallhack Menu", (p, o) => ShowWallhackWasdMenu(p));
        menu.AddItem("💀 Magic Bullet Menu", (p, o) => ShowMagicBulletWasdMenu(p));
        menu.AddItem("🔫 No Recoil Menu", (p, o) => ShowNoRecoilWasdMenu(p));
        menu.AddItem("⚡ No Flash Menu", (p, o) => ShowNoFlashWasdMenu(p));
        menu.AddItem("🌟 Frag Changer Menu", (p, o) => ShowFragChangerWasdMenu(p));

        menu.Display(player, 30);
        menu.Display(player, 30);
    }

    [ConsoleCommand("css_fragchanger", "Frag Changer WASD menu")]
    [RequiresPermissions("@css/root")]
    public void OnFragChangerCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ShowFragChangerWasdMenu(player);
    }

    private void ShowAimbotWasdMenu(CCSPlayerController admin)
    {
        var allPlayers = Utilities.GetPlayers()
            .Where(p => p != null && p.IsValid)
            .OrderBy(p => p.PlayerName)
            .ToList();

        if (allPlayers.Count == 0)
        {
            admin.PrintToChat($" {ChatColors.Green}[LynxCheatTool]{ChatColors.Default} {ChatColors.Red}No players found on the server!{ChatColors.Default}");
            return;
        }

        WasdMenu menu = new("LynxCheatTool Aimbot Menu", this);

        menu.AddItem("👥 Toggle All", (p, o) => 
        {
            ToggleAimbotAll(p);
            ShowAimbotWasdMenu(p);
        });

        foreach (var targetPlayer in allPlayers)
        {
            var steamId = targetPlayer.SteamID;
            var isEnabled = _aimbotEnabled.TryGetValue(steamId, out var enabled) && enabled;
            
            var statusIcon = isEnabled ? "✓" : "✗";
            var teamName = targetPlayer.TeamNum == 2 ? "[T]" : targetPlayer.TeamNum == 3 ? "[CT]" : "[SPEC]";
            var displayName = $"{statusIcon} {teamName} {targetPlayer.PlayerName}";

            menu.AddItem(displayName, (p, o) =>
            {
                ToggleAimbot(p, targetPlayer);
                
                ShowAimbotWasdMenu(p);
            });
        }

        menu.Display(admin, 30);
    }

    private void ToggleAimbotAll(CCSPlayerController admin)
    {
        var allPlayers = Utilities.GetPlayers().Where(p => p != null && p.IsValid).ToList();
        bool anyEnabled = allPlayers.Any(p => _aimbotEnabled.TryGetValue(p.SteamID, out var e) && e);
        bool newState = !anyEnabled;

        foreach (var player in allPlayers)
        {
            _aimbotEnabled[player.SteamID] = newState;
        }

        string stateText = newState ? "Enabled" : "Disabled";
        admin.PrintToCenter($"All players Aimbot status changed to {stateText}");
    }

    private void ToggleAimbot(CCSPlayerController admin, CCSPlayerController targetPlayer)
    {
        var steamId = targetPlayer.SteamID;
        
        if (!_aimbotEnabled.ContainsKey(steamId))
            _aimbotEnabled[steamId] = false;

        _aimbotEnabled[steamId] = !_aimbotEnabled[steamId];

        if (_aimbotEnabled[steamId])
        {
            admin.PrintToCenter($"Aimbot enabled for {targetPlayer.PlayerName}");
            targetPlayer.PrintToChat($" {ChatColors.Green}[LynxCheatTool]{ChatColors.Default} {ChatColors.LightRed}Aimbot enabled!{ChatColors.Default} (Admin: {admin.PlayerName})");
        }
        else
        {
            admin.PrintToCenter($"Aimbot disabled for {targetPlayer.PlayerName}");
            targetPlayer.PrintToChat($" {ChatColors.Green}[LynxCheatTool]{ChatColors.Default} {ChatColors.Grey}Aimbot disabled.{ChatColors.Default}");
            
            if (_currentTarget.ContainsKey(steamId))
                _currentTarget[steamId] = null;
        }
    }

    [ConsoleCommand("css_wallhack", "Wallhack WASD menu")]
    [RequiresPermissions("@css/root")]
    public void OnWallhackCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ShowWallhackWasdMenu(player);
    }

    private void ShowWallhackWasdMenu(CCSPlayerController admin)
    {
        var allPlayers = Utilities.GetPlayers()
            .Where(p => p != null && p.IsValid)
            .OrderBy(p => p.PlayerName)
            .ToList();

        if (allPlayers.Count == 0)
        {
            admin.PrintToChat($" {ChatColors.Green}[LynxCheatTool]{ChatColors.Default} {ChatColors.Red}No players found on the server!{ChatColors.Default}");
            return;
        }

        WasdMenu menu = new("LynxCheatTool Wallhack Menu", this);

        menu.AddItem("👥 Toggle All", (p, o) => 
        {
            ToggleWallhackAll(p);
            ShowWallhackWasdMenu(p);
        });

        foreach (var targetPlayer in allPlayers)
        {
            var steamId = targetPlayer.SteamID;
            var isEnabled = _wallhackEnabled.TryGetValue(steamId, out var enabled) && enabled;
            
            var statusIcon = isEnabled ? "✓" : "✗";
            var teamName = targetPlayer.TeamNum == 2 ? "[T]" : targetPlayer.TeamNum == 3 ? "[CT]" : "[SPEC]";
            var displayName = $"{statusIcon} {teamName} {targetPlayer.PlayerName}";

            menu.AddItem(displayName, (p, o) =>
            {
                ToggleWallhack(p, targetPlayer);
                
                ShowWallhackWasdMenu(p);
            });
        }

        menu.Display(admin, 30);
    }

    private void ToggleWallhackAll(CCSPlayerController admin)
    {
        var allPlayers = Utilities.GetPlayers().Where(p => p != null && p.IsValid).ToList();
        bool anyEnabled = allPlayers.Any(p => _wallhackEnabled.TryGetValue(p.SteamID, out var e) && e);
        bool newState = !anyEnabled;

        foreach (var player in allPlayers)
        {
            _wallhackEnabled[player.SteamID] = newState;
            
            if (!newState)
            {
                RemoveGlowEntity(player);
            }
        }

        string stateText = newState ? "Enabled" : "Disabled";
        admin.PrintToCenter($"All players Wallhack {stateText}");
    }

    private void ToggleWallhack(CCSPlayerController admin, CCSPlayerController targetPlayer)
    {
        var steamId = targetPlayer.SteamID;
        
        if (!_wallhackEnabled.ContainsKey(steamId))
            _wallhackEnabled[steamId] = false;

        _wallhackEnabled[steamId] = !_wallhackEnabled[steamId];

        if (_wallhackEnabled[steamId])
        {
            admin.PrintToCenter($"Wallhack enabled for {targetPlayer.PlayerName}");
            targetPlayer.PrintToChat($" {ChatColors.Green}[LynxCheatTool]{ChatColors.Default} {ChatColors.LightPurple}Wallhack enabled!{ChatColors.Default} (Admin: {admin.PlayerName})");
        }
        else
        {
            admin.PrintToCenter($"Wallhack disabled for {targetPlayer.PlayerName}");
            targetPlayer.PrintToChat($" {ChatColors.Green}[LynxCheatTool]{ChatColors.Default} {ChatColors.Grey}Wallhack disabled.{ChatColors.Default}");
        }
    }

    private void ShowMagicBulletWasdMenu(CCSPlayerController admin)
    {
        var allPlayers = Utilities.GetPlayers()
            .Where(p => p != null && p.IsValid)
            .OrderBy(p => p.PlayerName)
            .ToList();

        if (allPlayers.Count == 0)
        {
            admin.PrintToChat($" {ChatColors.Green}[LynxCheatTool]{ChatColors.Default} {ChatColors.Red}No players found on the server!{ChatColors.Default}");
            return;
        }

        WasdMenu menu = new("LynxCheatTool Magic Bullet Menu", this);

        menu.AddItem("👥 Toggle All", (p, o) => 
        {
            ToggleMagicBulletAll(p);
            ShowMagicBulletWasdMenu(p);
        });

        foreach (var targetPlayer in allPlayers)
        {
            var steamId = targetPlayer.SteamID;
            var isEnabled = _magicBulletEnabled.TryGetValue(steamId, out var enabled) && enabled;
            
            var statusIcon = isEnabled ? "✓" : "✗";
            var teamName = targetPlayer.TeamNum == 2 ? "[T]" : targetPlayer.TeamNum == 3 ? "[CT]" : "[SPEC]";
            var displayName = $"{statusIcon} {teamName} {targetPlayer.PlayerName}";

            menu.AddItem(displayName, (p, o) =>
            {
                ToggleMagicBullet(p, targetPlayer);
                
                ShowMagicBulletWasdMenu(p);
            });
        }

        menu.Display(admin, 30);
    }

    [ConsoleCommand("css_norecoil", "No Recoil WASD menu")]
    [RequiresPermissions("@css/root")]
    public void OnNoRecoilCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ShowNoRecoilWasdMenu(player);
    }

    private void ShowNoRecoilWasdMenu(CCSPlayerController admin)
    {
        var allPlayers = Utilities.GetPlayers()
            .Where(p => p != null && p.IsValid)
            .OrderBy(p => p.PlayerName)
            .ToList();

        if (allPlayers.Count == 0)
        {
            admin.PrintToChat($" {ChatColors.Green}[LynxCheatTool]{ChatColors.Default} {ChatColors.Red}No players found on the server!{ChatColors.Default}");
            return;
        }

        WasdMenu menu = new("LynxCheatTool No Recoil Menu", this);

        menu.AddItem("👥 Toggle All", (p, o) => 
        {
            ToggleNoRecoilAll(p);
            ShowNoRecoilWasdMenu(p);
        });

        foreach (var targetPlayer in allPlayers)
        {
            var steamId = targetPlayer.SteamID;
            var isEnabled = _noRecoilEnabled.TryGetValue(steamId, out var enabled) && enabled;
            
            var statusIcon = isEnabled ? "✓" : "✗";
            var teamName = targetPlayer.TeamNum == 2 ? "[T]" : targetPlayer.TeamNum == 3 ? "[CT]" : "[SPEC]";
            var displayName = $"{statusIcon} {teamName} {targetPlayer.PlayerName}";

            menu.AddItem(displayName, (p, o) =>
            {
                ToggleNoRecoil(p, targetPlayer);
                ShowNoRecoilWasdMenu(p);
            });
        }

        menu.Display(admin, 30);
    }

    private void ToggleNoRecoilAll(CCSPlayerController admin)
    {
        var allPlayers = Utilities.GetPlayers().Where(p => p != null && p.IsValid).ToList();
        bool anyEnabled = allPlayers.Any(p => _noRecoilEnabled.TryGetValue(p.SteamID, out var e) && e);
        bool newState = !anyEnabled;

        foreach (var player in allPlayers)
        {
            _noRecoilEnabled[player.SteamID] = newState;
        }

        string stateText = newState ? "Enabled" : "Disabled";
        admin.PrintToCenter($"All players No Recoil {stateText}");
    }

    private void ToggleNoRecoil(CCSPlayerController admin, CCSPlayerController targetPlayer)
    {
        var steamId = targetPlayer.SteamID;
        
        if (!_noRecoilEnabled.ContainsKey(steamId))
            _noRecoilEnabled[steamId] = false;

        _noRecoilEnabled[steamId] = !_noRecoilEnabled[steamId];

        if (_noRecoilEnabled[steamId])
        {
            admin.PrintToCenter($"No Recoil enabled for {targetPlayer.PlayerName}");
            targetPlayer.PrintToChat($" {ChatColors.Green}[LynxCheatTool]{ChatColors.Default} {ChatColors.LightBlue}No Recoil enabled! Laser beam!{ChatColors.Default} (Admin: {admin.PlayerName})");
        }
        else
        {
            admin.PrintToCenter($"No Recoil disabled for {targetPlayer.PlayerName}");
            targetPlayer.PrintToChat($" {ChatColors.Green}[LynxCheatTool]{ChatColors.Default} {ChatColors.Grey}No Recoil disabled.{ChatColors.Default}");
        }
    }

    [ConsoleCommand("css_noflash", "No Flash WASD menu")]
    [RequiresPermissions("@css/root")]
    public void OnNoFlashCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        ShowNoFlashWasdMenu(player);
    }

    private void ShowNoFlashWasdMenu(CCSPlayerController admin)
    {
        var allPlayers = Utilities.GetPlayers()
            .Where(p => p != null && p.IsValid)
            .OrderBy(p => p.PlayerName)
            .ToList();

        if (allPlayers.Count == 0)
        {
            admin.PrintToChat($" {ChatColors.Green}[LynxCheatTool]{ChatColors.Default} {ChatColors.Red}No players found on the server!{ChatColors.Default}");
            return;
        }

        WasdMenu menu = new("LynxCheatTool No Flash Menu", this);

        menu.AddItem("👥 Toggle All", (p, o) => 
        {
            ToggleNoFlashAll(p);
            ShowNoFlashWasdMenu(p);
        });

        foreach (var targetPlayer in allPlayers)
        {
            var steamId = targetPlayer.SteamID;
            var isEnabled = _noFlashEnabled.TryGetValue(steamId, out var enabled) && enabled;
            
            var statusIcon = isEnabled ? "✓" : "✗";
            var teamName = targetPlayer.TeamNum == 2 ? "[T]" : targetPlayer.TeamNum == 3 ? "[CT]" : "[SPEC]";
            var displayName = $"{statusIcon} {teamName} {targetPlayer.PlayerName}";

            menu.AddItem(displayName, (p, o) =>
            {
                ToggleNoFlash(p, targetPlayer);
                ShowNoFlashWasdMenu(p);
            });
        }

        menu.Display(admin, 30);
    }

    private void ToggleNoFlashAll(CCSPlayerController admin)
    {
        var allPlayers = Utilities.GetPlayers().Where(p => p != null && p.IsValid).ToList();
        bool anyEnabled = allPlayers.Any(p => _noFlashEnabled.TryGetValue(p.SteamID, out var e) && e);
        bool newState = !anyEnabled;

        foreach (var player in allPlayers)
        {
            _noFlashEnabled[player.SteamID] = newState;
        }

        string stateText = newState ? "Enabled" : "Disabled";
        admin.PrintToCenter($"All players No Flash {stateText}");
    }

    private void ToggleNoFlash(CCSPlayerController admin, CCSPlayerController targetPlayer)
    {
        var steamId = targetPlayer.SteamID;
        
        if (!_noFlashEnabled.ContainsKey(steamId))
            _noFlashEnabled[steamId] = false;

        _noFlashEnabled[steamId] = !_noFlashEnabled[steamId];

        if (_noFlashEnabled[steamId])
        {
            admin.PrintToCenter($"No Flash enabled for {targetPlayer.PlayerName}");
            targetPlayer.PrintToChat($" {ChatColors.Green}[LynxCheatTool]{ChatColors.Default} {ChatColors.LightYellow}No Flash enabled! Sunglasses on! 😎{ChatColors.Default} (Admin: {admin.PlayerName})");
        }
        else
        {
            admin.PrintToCenter($"No Flash disabled for {targetPlayer.PlayerName}");
            targetPlayer.PrintToChat($" {ChatColors.Green}[LynxCheatTool]{ChatColors.Default} {ChatColors.Grey}No Flash disabled.{ChatColors.Default}");
        }
    }

    private void ToggleMagicBulletAll(CCSPlayerController admin)
    {
        var allPlayers = Utilities.GetPlayers().Where(p => p != null && p.IsValid).ToList();
        bool anyEnabled = allPlayers.Any(p => _magicBulletEnabled.TryGetValue(p.SteamID, out var e) && e);
        bool newState = !anyEnabled;

        foreach (var player in allPlayers)
        {
            _magicBulletEnabled[player.SteamID] = newState;
        }

        string stateText = newState ? "Enabled" : "Disabled";
        admin.PrintToCenter($"All players Magic Bullet {stateText}");
    }

    private void ToggleMagicBullet(CCSPlayerController admin, CCSPlayerController targetPlayer)
    {
        var steamId = targetPlayer.SteamID;
        
        if (!_magicBulletEnabled.ContainsKey(steamId))
            _magicBulletEnabled[steamId] = false;

        _magicBulletEnabled[steamId] = !_magicBulletEnabled[steamId];

        if (_magicBulletEnabled[steamId])
        {
            admin.PrintToCenter($"Magic Bullet enabled for {targetPlayer.PlayerName}");
            targetPlayer.PrintToChat($" {ChatColors.Green}[LynxCheatTool]{ChatColors.Default} {ChatColors.LightRed}Magic Bullet enabled! Tek atış!{ChatColors.Default} (Admin: {admin.PlayerName})");
        }
        else
        {
            admin.PrintToCenter($"Magic Bullet disabled for {targetPlayer.PlayerName}");
            targetPlayer.PrintToChat($" {ChatColors.Green}[LynxCheatTool]{ChatColors.Default} {ChatColors.Grey}Magic Bullet disabled.{ChatColors.Default}");
        }
    }

    private void ShowFragChangerWasdMenu(CCSPlayerController admin)
    {
        var allPlayers = Utilities.GetPlayers()
            .Where(p => p != null && p.IsValid)
            .OrderBy(p => p.PlayerName)
            .ToList();

        if (allPlayers.Count == 0)
        {
            admin.PrintToChat($" {ChatColors.Green}[LynxCheatTool]{ChatColors.Default} {ChatColors.Red}No players found on the server!{ChatColors.Default}");
            return;
        }

        WasdMenu menu = new("LynxCheatTool Frag Changer Menu", this);

        menu.AddItem("👥 Toggle All", (p, o) => 
        {
            ToggleFragChangerAll(p);
            ShowFragChangerWasdMenu(p);
        });

        foreach (var targetPlayer in allPlayers)
        {
            var steamId = targetPlayer.SteamID;
            var currentSettings = _fragChangerSettings.TryGetValue(steamId, out var settings) ? settings : FragIcons.None;
            var isEnabled = currentSettings != FragIcons.None;
            
            var statusIcon = isEnabled ? "✓" : "✗";
            var teamName = targetPlayer.TeamNum == 2 ? "[T]" : targetPlayer.TeamNum == 3 ? "[CT]" : "[SPEC]";
            var displayName = $"{statusIcon} {teamName} {targetPlayer.PlayerName} (Settings)";

            menu.AddItem(displayName, (p, o) =>
            {
                ShowFragChangerDetailMenu(p, targetPlayer);
            });
        }

        menu.Display(admin, 30);
    }

    private void ShowFragChangerDetailMenu(CCSPlayerController admin, CCSPlayerController targetPlayer)
    {
        WasdMenu menu = new($"Frag Settings: {targetPlayer.PlayerName}", this);
        var steamId = targetPlayer.SteamID;
        
        if (!_fragChangerSettings.ContainsKey(steamId))
            _fragChangerSettings[steamId] = FragIcons.None;

        var currentSettings = _fragChangerSettings[steamId];

        menu.AddItem("🔄 Toggle All", (p, o) =>
        {
            if (_fragChangerSettings[steamId] == FragIcons.All)
                _fragChangerSettings[steamId] = FragIcons.None;
            else
                _fragChangerSettings[steamId] = FragIcons.All;

            ShowFragChangerDetailMenu(p, targetPlayer);
        });

        void AddToggleItem(string name, FragIcons icon)
        {
            bool isActive = currentSettings.HasFlag(icon);
            string status = isActive ? "✓" : "✗";
            menu.AddItem($"{status} {name}", (p, o) =>
            {
                if (isActive)
                    _fragChangerSettings[steamId] &= ~icon;
                else
                    _fragChangerSettings[steamId] |= icon;
                
                ShowFragChangerDetailMenu(p, targetPlayer);
            });
        }

        AddToggleItem("Headshot", FragIcons.Headshot);
        AddToggleItem("Blind", FragIcons.Blind);
        AddToggleItem("Smoke", FragIcons.Smoke);
        AddToggleItem("Wallbang", FragIcons.Wallbang);
        AddToggleItem("Noscope", FragIcons.Noscope);
        AddToggleItem("AttackerAir", FragIcons.Airborne);
        AddToggleItem("Domination", FragIcons.Dominated);
        AddToggleItem("Revenge", FragIcons.Revenge);
        AddToggleItem("Wipe", FragIcons.Wipe);

        menu.AddItem("⬅️ Back", (p, o) => ShowFragChangerWasdMenu(p));

        menu.Display(admin, 30);
    }

    private void ToggleFragChangerAll(CCSPlayerController admin)
    {
        var allPlayers = Utilities.GetPlayers().Where(p => p != null && p.IsValid).ToList();
        bool anyEnabled = allPlayers.Any(p => _fragChangerSettings.TryGetValue(p.SteamID, out var s) && s != FragIcons.None);
        
        var newSettings = anyEnabled ? FragIcons.None : FragIcons.All;

        foreach (var player in allPlayers)
        {
            _fragChangerSettings[player.SteamID] = newSettings;
        }

        string stateText = newSettings == FragIcons.All ? "All Icons Enabled" : "All Icons Disabled";
        admin.PrintToCenter($"All players Icons changed status to {stateText}");
    }



    private void OnTick()
    {
        var players = Utilities.GetPlayers();
        
        foreach (var player in players)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            if (_aimbotEnabled.TryGetValue(player.SteamID, out var aimbotEnabled) && aimbotEnabled)
            {
                var playerPawn = player.PlayerPawn.Value;
                if (playerPawn != null)
                {
                    var target = FindNearestEnemy(player);
                    if (target != null)
                    {
                        AimAtTarget(player, target);
                    }
                }
            }

            if (_noRecoilEnabled.TryGetValue(player.SteamID, out var noRecoilEnabled) && noRecoilEnabled)
            {
                var playerPawn = player.PlayerPawn.Value;
                if (playerPawn != null)
                {
                    playerPawn.ShotsFired = 0;

                    if (playerPawn.AimPunchAngle != null)
                    {
                        playerPawn.AimPunchAngle.X = 0;
                        playerPawn.AimPunchAngle.Y = 0;
                        playerPawn.AimPunchAngle.Z = 0;
                    }

                    if (playerPawn.AimPunchAngleVel != null)
                    {
                        playerPawn.AimPunchAngleVel.X = 0;
                        playerPawn.AimPunchAngleVel.Y = 0;
                        playerPawn.AimPunchAngleVel.Z = 0;
                    }


                }
            }

            // No Flash işlemi
            if (_noFlashEnabled.TryGetValue(player.SteamID, out var noFlashEnabled) && noFlashEnabled)
            {
                var playerPawn = player.PlayerPawn.Value;
                if (playerPawn != null)
                {
                    playerPawn.FlashDuration = 0;
                }
            }

            UpdatePlayerGlow(player);
        }
    }

    private void UpdatePlayerGlow(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PawnIsAlive || player.PlayerPawn.Value == null)
            return;

        bool shouldHaveGlow = false;
        foreach (var observer in Utilities.GetPlayers())
        {
            if (observer == null || !observer.IsValid)
                continue;

            if (_wallhackEnabled.TryGetValue(observer.SteamID, out var enabled) && enabled)
            {
                if (player.TeamNum != observer.TeamNum && player.Slot != observer.Slot)
                {
                    shouldHaveGlow = true;
                    break;
                }
            }
        }

        if (!shouldHaveGlow)
        {
            if (_playerGlowData.TryGetValue(player, out var data))
            {
                data.ModelRelay?.Remove();
                data.ModelGlow?.Remove();
                _playerGlowData.Remove(player);
            }
            return;
        }

        if (!_playerGlowData.TryGetValue(player, out var glowData))
        {
            glowData = new PlayerGlowData();
            _playerGlowData[player] = glowData;
        }

        string modelName = player.PlayerPawn.Value.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState?.ModelName ?? "";
        
        if (glowData.ModelRelay != null && glowData.ModelRelay.IsValid && 
            glowData.ModelGlow != null && glowData.ModelGlow.IsValid)
        {
            if (!string.IsNullOrEmpty(glowData.ModelName) && !string.IsNullOrEmpty(modelName) && 
                glowData.ModelName != modelName)
            {
                glowData.ModelRelay?.Remove();
                glowData.ModelGlow?.Remove();
            }
            else
            {
                return; 
            }
        }

        if (string.IsNullOrEmpty(modelName))
            return;

        glowData.ModelRelay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (glowData.ModelRelay == null) return;

        glowData.ModelName = modelName;
        glowData.ModelRelay.DispatchSpawn();
        glowData.ModelRelay.SetModel(modelName);
        glowData.ModelRelay.Spawnflags = 256u;
        glowData.ModelRelay.RenderMode = RenderMode_t.kRenderNone;

        glowData.ModelGlow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (glowData.ModelGlow == null) return;

        glowData.ModelGlow.Render = Color.FromArgb(1, 0, 0, 0);
        glowData.ModelGlow.DispatchSpawn();
        glowData.ModelGlow.SetModel(modelName);
        glowData.ModelGlow.Spawnflags = 256u;

        glowData.ModelGlow.Glow.GlowColorOverride = Color.FromArgb(255, 255, 0, 0);
        glowData.ModelGlow.Glow.GlowRange = 5000;
        glowData.ModelGlow.Glow.GlowTeam = -1;
        glowData.ModelGlow.Glow.GlowType = 3;
        glowData.ModelGlow.Glow.GlowRangeMin = 100;

        glowData.ModelRelay.AcceptInput("FollowEntity", player.PlayerPawn.Value, glowData.ModelRelay, "!activator");
        glowData.ModelGlow.AcceptInput("FollowEntity", glowData.ModelRelay, glowData.ModelGlow, "!activator");
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (@event?.Userid != null)
        {
            RemoveGlowEntity(@event.Userid);
        }

        if (@event?.Attacker != null && @event.Attacker.IsValid)
        {
            var attackerSteamId = @event.Attacker.SteamID;

            if (_magicBulletEnabled.TryGetValue(attackerSteamId, out var mbEnabled) && mbEnabled)
            {
                @event.Headshot = true;
            }

            if (_fragChangerSettings.TryGetValue(attackerSteamId, out var settings) && settings != FragIcons.None)
            {
                if (settings.HasFlag(FragIcons.Headshot)) @event.Headshot = true;
                if (settings.HasFlag(FragIcons.Blind)) @event.Attackerblind = true;
                if (settings.HasFlag(FragIcons.Smoke)) @event.Thrusmoke = true;
                if (settings.HasFlag(FragIcons.Wallbang)) @event.Penetrated = 1;
                if (settings.HasFlag(FragIcons.Noscope)) @event.Noscope = true;
                if (settings.HasFlag(FragIcons.Dominated)) @event.Dominated = 1;
                if (settings.HasFlag(FragIcons.Revenge)) @event.Revenge = 1;
                if (settings.HasFlag(FragIcons.Wipe)) @event.Wipe = 1;
                if (settings.HasFlag(FragIcons.Airborne)) @event.Attackerinair = true;
            }
            
           
            if (mbEnabled || (settings != FragIcons.None))
            {
                 return HookResult.Changed;
            }
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (@event?.Userid != null)
        {
            RemoveGlowEntity(@event.Userid);
        }
        return HookResult.Continue;
    }

    private void RemoveGlowEntity(CCSPlayerController player)
    {
        if (_playerGlowData.TryGetValue(player, out var data))
        {
            data.ModelRelay?.Remove();
            data.ModelGlow?.Remove();
            _playerGlowData.Remove(player);
        }
    }

    private CCSPlayerController? FindNearestEnemy(CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn == null)
            return null;

        var playerPos = playerPawn.AbsOrigin;
        if (playerPos == null)
            return null;

        var playerEyeAngles = playerPawn.EyeAngles;
        if (playerEyeAngles == null)
            return null;

        var steamId = player.SteamID;

        if (_currentTarget.TryGetValue(steamId, out var currentTarget) && currentTarget != null)
        {
            if (currentTarget.IsValid && currentTarget.PawnIsAlive && 
                currentTarget.TeamNum != player.TeamNum)
            {
                var currentTargetPawn = currentTarget.PlayerPawn.Value;
                if (currentTargetPawn != null && currentTargetPawn.AbsOrigin != null)
                {
                    var angleToCurrentTarget = CalculateAngle(playerPos, currentTargetPawn.AbsOrigin, playerPawn);
                    var fovDiffCurrent = GetFovDifference(playerEyeAngles, angleToCurrentTarget);

                    if (fovDiffCurrent <= _aimbotStickyFov)
                    {
                        return currentTarget;
                    }
                }
            }
            
            _currentTarget[steamId] = null;
        }

        CCSPlayerController? nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        var players = Utilities.GetPlayers();
        foreach (var target in players)
        {
            if (target == null || !target.IsValid || !target.PawnIsAlive)
                continue;

            if (target.TeamNum == player.TeamNum)
                continue;

            if (target.Slot == player.Slot)
                continue;

            var targetPawn = target.PlayerPawn.Value;
            if (targetPawn == null)
                continue;

            var targetPos = targetPawn.AbsOrigin;
            if (targetPos == null)
                continue;

            var distance = Vector3.Distance(
                new Vector3(playerPos.X, playerPos.Y, playerPos.Z),
                new Vector3(targetPos.X, targetPos.Y, targetPos.Z)
            );

            var angleToTarget = CalculateAngle(playerPos, targetPos, playerPawn);
            var fovDiff = GetFovDifference(playerEyeAngles, angleToTarget);

            if (fovDiff > _aimbotFov)
                continue;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = target;
            }
        }

        if (nearestEnemy != null)
        {
            _currentTarget[steamId] = nearestEnemy;
        }

        return nearestEnemy;
    }

    private void AimAtTarget(CCSPlayerController player, CCSPlayerController target)
    {
        var playerPawn = player.PlayerPawn.Value;
        var targetPawn = target.PlayerPawn.Value;

        if (playerPawn == null || targetPawn == null)
            return;

        var playerPos = playerPawn.AbsOrigin;
        var targetPos = targetPawn.AbsOrigin;

        if (playerPos == null || targetPos == null)
            return;

        Vector targetHeadPos;
        
        bool isCrouched = (targetPawn.Flags & (uint)PlayerFlags.FL_DUCKING) != 0;
        float headHeight = isCrouched ? 46.0f : 64.0f; 
        
        var sceneNode = targetPawn.CBodyComponent?.SceneNode;
        if (sceneNode != null)
        {
            
            try
            {
                
                var targetAngles = targetPawn.EyeAngles;
                if (targetAngles != null)
                {
                    var yawRad = targetAngles.Y * (Math.PI / 180.0);
                    var forwardOffset = 3.0f; 
                    
                    targetHeadPos = new Vector(
                        targetPos.X + (float)(Math.Cos(yawRad) * forwardOffset),
                        targetPos.Y + (float)(Math.Sin(yawRad) * forwardOffset),
                        targetPos.Z + headHeight 
                    );
                }
                else
                {
                    targetHeadPos = new Vector(targetPos.X, targetPos.Y, targetPos.Z + headHeight);
                }
            }
            catch
            {
                targetHeadPos = new Vector(targetPos.X, targetPos.Y, targetPos.Z + headHeight);
            }
        }
        else
        {
            targetHeadPos = new Vector(targetPos.X, targetPos.Y, targetPos.Z + headHeight);
        }

        var targetAngle = CalculateAngle(playerPos, targetHeadPos, playerPawn);

        var currentAngle = playerPawn.EyeAngles;
        if (currentAngle == null)
            return;

        var newAngle = new QAngle(
            Lerp(currentAngle.X, targetAngle.X, _aimbotSmooth),
            Lerp(currentAngle.Y, targetAngle.Y, _aimbotSmooth),
            currentAngle.Z
        );

        newAngle.X = NormalizeAngle(newAngle.X);
        newAngle.Y = NormalizeAngle(newAngle.Y);

        
        var currentVelocity = playerPawn.AbsVelocity;
        playerPawn.Teleport(playerPos, newAngle, currentVelocity);
        
        player.PrintToCenter($"🎯 Locked: {target.PlayerName}");
    }

    private QAngle CalculateAngle(Vector from, Vector to, CCSPlayerPawn pawn)
    {
        var eyePos = pawn.AbsOrigin;
        var eyeHeight = 64.0f; 
        
        if (eyePos != null)
        {
            eyePos = new Vector(eyePos.X, eyePos.Y, eyePos.Z + eyeHeight);
        }
        else
        {
            eyePos = from;
        }

        var delta = new Vector3(
            to.X - eyePos.X,
            to.Y - eyePos.Y,
            to.Z - eyePos.Z
        );

        var hyp = Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
        
        var pitch = (float)(Math.Atan2(-delta.Z, hyp) * (180.0 / Math.PI));
        var yaw = (float)(Math.Atan2(delta.Y, delta.X) * (180.0 / Math.PI));

        return new QAngle(pitch, yaw, 0);
    }

    private float GetFovDifference(QAngle current, QAngle target)
    {
        var diffX = Math.Abs(NormalizeAngle(current.X - target.X));
        var diffY = Math.Abs(NormalizeAngle(current.Y - target.Y));
        
        return (float)Math.Sqrt(diffX * diffX + diffY * diffY);
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180)
            angle -= 360;
        while (angle < -180)
            angle += 360;
        return angle;
    }

    private float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        if (@event.Attacker == null || @event.Userid == null)
            return HookResult.Continue;

        var attacker = @event.Attacker;
        var victim = @event.Userid;

        if (attacker.IsValid && _magicBulletEnabled.TryGetValue(attacker.SteamID, out var enabled) && enabled)
        {
            if (victim.IsValid && victim.PawnIsAlive && victim.TeamNum != attacker.TeamNum)
            {
               
                if (victim.PlayerPawn.Value != null)
                {
                    victim.PlayerPawn.Value.Health = 0;
                    
                }
            }
        }

        return HookResult.Continue;
    }
}
