using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MenuAPI;
using Newtonsoft.Json;
using CitizenFX.Core;
using static CitizenFX.Core.UI.Screen;
using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.ConfigManager;

namespace vMenuClient
{
    public class MainMenu : BaseScript
    {
        #region Variables
        //public static MenuPool Mp { get; } = new MenuPool();

        private bool firstTick = true;
        public static bool PreSetupComplete = false;

        public static int MenuToggleKey { get; private set; } = 244; // M by default (InteractionMenu)
        public static int NoClipKey { get; private set; } = 289; // F2 by default (ReplayStartStopRecordingSecondary)
        public static Menu Menu { get; private set; }

        public static PlayerOptions PlayerOptionsMenu { get; private set; }
        public static OnlinePlayers OnlinePlayersMenu { get; private set; }
        public static BannedPlayers BannedPlayersMenu { get; private set; }
        public static SavedVehicles SavedVehiclesMenu { get; private set; }
        public static VehicleOptions VehicleOptionsMenu { get; private set; }
        public static VehicleSpawner VehicleSpawnerMenu { get; private set; }
        public static PlayerAppearance PlayerAppearanceMenu { get; private set; }
        public static MpPedCustomization MpPedCustomizationMenu { get; private set; }
        public static TimeOptions TimeOptionsMenu { get; private set; }
        public static WeatherOptions WeatherOptionsMenu { get; private set; }
        public static WeaponOptions WeaponOptionsMenu { get; private set; }
        public static Recording RecordingMenu { get; private set; }
        public static MiscSettings MiscSettingsMenu { get; private set; }
        public static VoiceChat VoiceChatSettingsMenu { get; private set; }
        public static About AboutMenu { get; private set; }
        public static Menu NoClipMenu { get; } = new NoclipMenu().GetMenu();
        public static bool NoClipEnabled { get; set; } = false;
        public static PlayerList PlayersList;

        // Only used when debugging is enabled:
        //private BarTimerBar bt = new BarTimerBar("Opening Menu");

        public static bool DebugMode = GetResourceMetadata(GetCurrentResourceName(), "client_debug_mode", 0) == "true" ? true : false;
        public static bool EnableExperimentalFeatures = /*true;*/ (GetResourceMetadata(GetCurrentResourceName(), "experimental_features_enabled", 0) ?? "0") == "1";
        public static string Version { get { return GetResourceMetadata(GetCurrentResourceName(), "version", 0); } }

        public static bool DontOpenMenus { get { return MenuController.DontOpenAnyMenu; } set { MenuController.DontOpenAnyMenu = value; } }
        public static bool DisableControls { get { return MenuController.DisableMenuButtons; } set { MenuController.DisableMenuButtons = value; } }
        //private Menu currentMenu = null;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainMenu()
        {
            PlayersList = Players;

            if (EnableExperimentalFeatures || DebugMode)
            {
                RegisterCommand("testped", new Action<dynamic, List<string>, string>((dynamic source, List<string> args, string rawCommand) =>
                {
                    PedHeadBlendData data = Game.PlayerPed.GetHeadBlendData();
                    Debug.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
                }), false);
            }

            RegisterCommand("tattoo", new Action<dynamic, List<string>, string>((dynamic source, List<string> args, string rawCommand) =>
            {
                if (args != null && args[0] != null && args[1] != null)
                {
                    Debug.WriteLine(args[0].ToString() + " " + args[1].ToString());
                    TattooCollectionData d = Game.GetTattooCollectionData(int.Parse(args[0].ToString()), int.Parse(args[1].ToString()));
                    Debug.WriteLine("check");
                    Debug.Write(JsonConvert.SerializeObject(d, Formatting.Indented) + "\n");
                }
            }), false);


            RegisterCommand("vmenuclient", new Action<dynamic, List<string>, string>((dynamic source, List<string> args, string rawCommand) =>
            {
                if (args != null)
                {
                    if (args.Count > 0)
                    {
                        if (args[0].ToString().ToLower() == "debug")
                        {
                            DebugMode = !DebugMode;
                            Notify.Custom($"Debug mode is now set to: {DebugMode}.");
                            // Set discord rich precense once, allowing it to be overruled by other resources once those load.
                            if (DebugMode)
                            {
                                SetRichPresence($"Debugging vMenu {Version}!");
                            }
                            else
                            {
                                SetRichPresence($"Enjoying FiveM!");
                            }
                        }
                        else if (args[0].ToString().ToLower() == "gc")
                        {
                            GC.Collect();
                            Debug.WriteLine("Cleared memory.");
                        }
                    }
                    else
                    {
                        Notify.Custom($"vMenu is currently running version: {Version}.");
                    }
                }
            }), false);

            // Set discord rich precense once, allowing it to be overruled by other resources once those load.
            if (DebugMode)
            {
                SetRichPresence($"Debugging vMenu {Version}!");
            }

            if (GetCurrentResourceName() != "vMenu")
            {

                Exception InvalidNameException = new Exception("\r\n\r\n[vMenu] INSTALLATION ERROR!\r\nThe name of the resource is not valid. Please change the folder name from '" + GetCurrentResourceName() + "' to 'vMenu' (case sensitive) instead!\r\n\r\n\r\n");
                try
                {
                    throw InvalidNameException;
                }
                catch (Exception e)
                {
                    Log(e.Message);
                }
                TriggerEvent("chatMessage", "^3IMPORTANT: vMenu IS NOT SETUP CORRECTLY. PLEASE CHECK THE SERVER LOG FOR MORE INFO.");
            }
            else
            {
                Tick += OnTick;
                //Tick += ProcessMainButtons;
                //Tick += ProcessDirectionalButtons;
            }

            SetClockDate(DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year);

        }

        #region Set Permissions function
        /// <summary>
        /// Set the permissions for this client.
        /// </summary>
        /// <param name="dict"></param>
        public static void SetPermissions(dynamic dict)
        {
            // Loop through the dynamic object and get the keys and values.
            foreach (dynamic permission in dict)
            {
                // Add the new permission to the dictionary.
                PermissionsManager.SetPermission(permission.Key.ToString(), permission.Value);
            }
            Log(JsonConvert.SerializeObject(PermissionsManager.Permissions).ToString());

            VehicleSpawner.allowedCategories = new List<bool>()
            {
                IsAllowed(Permission.VSCompacts),
                IsAllowed(Permission.VSSedans),
                IsAllowed(Permission.VSSUVs),
                IsAllowed(Permission.VSCoupes),
                IsAllowed(Permission.VSMuscle),
                IsAllowed(Permission.VSSportsClassic),
                IsAllowed(Permission.VSSports),
                IsAllowed(Permission.VSSuper),
                IsAllowed(Permission.VSMotorcycles),
                IsAllowed(Permission.VSOffRoad),
                IsAllowed(Permission.VSIndustrial),
                IsAllowed(Permission.VSUtility),
                IsAllowed(Permission.VSVans),
                IsAllowed(Permission.VSCycles),
                IsAllowed(Permission.VSBoats),
                IsAllowed(Permission.VSHelicopters),
                IsAllowed(Permission.VSPlanes),
                IsAllowed(Permission.VSService),
                IsAllowed(Permission.VSEmergency),
                IsAllowed(Permission.VSMilitary),
                IsAllowed(Permission.VSCommercial),
                IsAllowed(Permission.VSTrains),
            };
        }
        #endregion


        /// <summary>
        /// Main OnTick task runs every game tick and handles all the menu stuff.
        /// </summary>
        /// <returns></returns>
        private async Task OnTick()
        {
            #region FirstTick
            // Only run this the first tick.
            if (firstTick)
            {
                firstTick = false;
                // Clear all previous pause menu info/brief messages on resource start.
                ClearBrief();

                // Request the permissions data from the server.
                TriggerServerEvent("vMenu:RequestPermissions", Game.Player.Handle);

                // Wait until the data is received and the player's name is loaded correctly.
                while (!PreSetupComplete || Game.Player.Name == "**Invalid**" || Game.Player.Name == "** Invalid **")
                {
                    await Delay(0);
                }
                if ((IsAllowed(Permission.Staff) && GetSettingsBool(Setting.vmenu_menu_staff_only)) || GetSettingsBool(Setting.vmenu_menu_staff_only) == false)
                {
                    if (GetSettingsInt(Setting.vmenu_menu_toggle_key) != -1)
                    {
                        MenuController.MenuToggleKey = (Control)GetSettingsInt(Setting.vmenu_menu_toggle_key);
                        //MenuToggleKey = GetSettingsInt(Setting.vmenu_menu_toggle_key);
                    }
                    if (GetSettingsInt(Setting.vmenu_noclip_toggle_key) != -1)
                    {
                        NoClipKey = GetSettingsInt(Setting.vmenu_noclip_toggle_key);
                    }
                    // Create the main menu.
                    Menu = new Menu(Game.Player.Name, "Main Menu");

                    // Add the main menu to the menu pool.
                    MenuController.AddMenu(Menu);
                    MenuController.MainMenu = Menu;

                    Menu.RefreshIndex();
                    //Menu.UpdateScaleform();

                    // Create all (sub)menus.
                    CreateSubmenus();
                }

                // Manage Stamina
                if (PlayerOptionsMenu != null && PlayerOptionsMenu.PlayerStamina && IsAllowed(Permission.POUnlimitedStamina))
                {
                    StatSetInt((uint)GetHashKey("MP0_STAMINA"), 100, true);
                }
                else
                {
                    StatSetInt((uint)GetHashKey("MP0_STAMINA"), 0, true);
                }
                // Manage other stats.
                StatSetInt((uint)GetHashKey("MP0_STRENGTH"), 100, true);
                StatSetInt((uint)GetHashKey("MP0_LUNG_CAPACITY"), 80, true); // reduced because it was over powered
                StatSetInt((uint)GetHashKey("MP0_WHEELIE_ABILITY"), 100, true);
                StatSetInt((uint)GetHashKey("MP0_FLYING_ABILITY"), 100, true);
                StatSetInt((uint)GetHashKey("MP0_SHOOTING_ABILITY"), 50, true); // reduced because it was over powered
                StatSetInt((uint)GetHashKey("MP0_STEALTH_ABILITY"), 100, true);
            }
            #endregion


            // If the setup (permissions) is done, and it's not the first tick, then do this:
            if (PreSetupComplete && !firstTick)
            {
                #region Handle Opening/Closing of the menu.


                var tmpMenu = GetOpenMenu();
                if (MpPedCustomizationMenu != null)
                {
                    if (tmpMenu == MpPedCustomizationMenu.createCharacterMenu)
                    {
                        MpPedCustomization.DisableBackButton = true;
                        MpPedCustomization.DontCloseMenus = true;
                    }
                    else
                    {
                        MpPedCustomization.DisableBackButton = false;
                        MpPedCustomization.DontCloseMenus = false;
                    }
                }

                if (Game.IsDisabledControlJustReleased(0, Control.PhoneCancel) && MpPedCustomization.DisableBackButton)
                {
                    await Delay(0);
                    Notify.Alert("You must save your ped first before exiting, or click the ~r~Exit Without Saving~s~ button.");
                }

                if (Game.CurrentInputMode == InputMode.MouseAndKeyboard)
                {
                    if (!MenuController.IsAnyMenuOpen() || NoClipEnabled)
                    {
                        if (Game.IsControlJustPressed(0, (Control)NoClipKey) && IsAllowed(Permission.NoClip))
                        {
                            if (MenuController.IsAnyMenuOpen())
                            {
                                if (MenuController.GetCurrentMenu() != null && MenuController.GetCurrentMenu() != NoClipMenu)
                                {
                                    MenuController.CloseAllMenus();
                                }
                            }
                            if (Game.PlayerPed.IsInVehicle())
                            {
                                Vehicle veh = GetVehicle();
                                if (veh != null && veh.Exists() && !veh.IsDead && veh.Driver == Game.PlayerPed)
                                {
                                    NoClipEnabled = !NoClipEnabled;
                                    MenuController.DontOpenAnyMenu = NoClipEnabled;
                                }
                                else
                                {
                                    NoClipEnabled = false;
                                    MenuController.DontOpenAnyMenu = NoClipEnabled;
                                    Notify.Error("You need to be the driver of this vehicle to enable noclip!");
                                }
                            }
                            else
                            {
                                NoClipEnabled = !NoClipEnabled;
                                MenuController.DontOpenAnyMenu = NoClipEnabled;
                            }
                        }
                    }
                }

                if (NoClipEnabled)
                {
                    MenuController.DontOpenAnyMenu = true;
                }

                #endregion

                // Menu toggle button.
                Game.DisableControlThisFrame(0, (Control)MenuToggleKey);


            }
        }

        #region Add Menu Function
        /// <summary>
        /// Add the menu to the menu pool and set it up correctly.
        /// Also add and bind the menu buttons.
        /// </summary>
        /// <param name="submenu"></param>
        /// <param name="menuButton"></param>
        private void AddMenu(Menu submenu, MenuItem menuButton)
        {
            Menu.AddMenuItem(menuButton);
            MenuController.AddSubmenu(Menu, submenu);
            MenuController.BindMenuItem(Menu, submenu, menuButton);
            //Mp.Add(submenu);
            submenu.RefreshIndex();
            //submenu.UpdateScaleform();
        }
        #endregion
        #region Create Submenus
        /// <summary>
        /// Creates all the submenus depending on the permissions of the user.
        /// </summary>
        private void CreateSubmenus()
        {
            // Add the online players menu.
            if (IsAllowed(Permission.OPMenu))
            {
                OnlinePlayersMenu = new OnlinePlayers();
                Menu menu = OnlinePlayersMenu.GetMenu();
                MenuItem button = new MenuItem("Online Players", "All currently connected players.");
                button.Label = "→→→";
                AddMenu(menu, button);
                Menu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == button)
                    {
                        OnlinePlayersMenu.UpdatePlayerlist();
                        menu.RefreshIndex();
                        //menu.UpdateScaleform();
                    }
                };
            }
            if (IsAllowed(Permission.OPUnban) || IsAllowed(Permission.OPViewBannedPlayers))
            {
                BannedPlayersMenu = new BannedPlayers();
                Menu menu = BannedPlayersMenu.GetMenu();
                MenuItem button = new MenuItem("Banned Players", "View and manage all banned players in this menu.");
                button.Label = "→→→";
                AddMenu(menu, button);
                Menu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == button)
                    {
                        TriggerServerEvent("vMenu:RequestBanList", Game.Player.Handle);
                        menu.RefreshIndex();
                        //menu.UpdateScaleform();
                    }
                };
            }

            // Add the player options menu.
            if (IsAllowed(Permission.POMenu))
            {
                PlayerOptionsMenu = new PlayerOptions();
                Menu menu = PlayerOptionsMenu.GetMenu();
                MenuItem button = new MenuItem("Player Options", "Common player options can be accessed here.");
                button.Label = "→→→";
                AddMenu(menu, button);
            }

            // Add the vehicle options Menu.
            if (IsAllowed(Permission.VOMenu))
            {
                VehicleOptionsMenu = new VehicleOptions();
                Menu menu = VehicleOptionsMenu.GetMenu();
                MenuItem button = new MenuItem("Vehicle Options", "Here you can change common vehicle options, as well as tune & style your vehicle.");
                button.Label = "→→→";
                AddMenu(menu, button);
            }

            var vl = new Vehicles().VehicleClasses;
            // Add the vehicle spawner menu.
            if (IsAllowed(Permission.VSMenu))
            {
                VehicleSpawnerMenu = new VehicleSpawner();
                Menu menu = VehicleSpawnerMenu.GetMenu();
                MenuItem button = new MenuItem("Vehicle Spawner", "Spawn a vehicle by name or choose one from a specific category.");
                button.Label = "→→→";
                AddMenu(menu, button);
            }

            // Add Saved Vehicles menu.
            if (IsAllowed(Permission.SVMenu))
            {
                SavedVehiclesMenu = new SavedVehicles();
                Menu menu = SavedVehiclesMenu.GetMenu();
                MenuItem button = new MenuItem("Saved Vehicles", "Save new vehicles, or spawn or delete already saved vehicles.");
                button.Label = "→→→";
                AddMenu(menu, button);
                Menu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == button)
                    {
                        SavedVehiclesMenu.UpdateMenuAvailableCategories();
                    }
                };
            }

            // Add the player appearance menu.
            if (IsAllowed(Permission.PAMenu))
            {
                PlayerAppearanceMenu = new PlayerAppearance();
                Menu menu = PlayerAppearanceMenu.GetMenu();
                MenuItem button = new MenuItem("Player Appearance", "Choose a ped model, customize it and save & load your customized characters.");
                button.Label = "→→→";
                AddMenu(menu, button);

                MpPedCustomizationMenu = new MpPedCustomization();
                Menu menu2 = MpPedCustomizationMenu.GetMenu();
                MenuItem button2 = new MenuItem("MP Ped Customization", "Create, edit, save and load multiplayer peds. ~r~Note, you can only save peds created in this submenu. vMenu can NOT detect peds created outside of this submenu. Simply due to GTA limitations.");
                button2.Label = "→→→";
                AddMenu(menu2, button2);


            }

            // Add the time options menu.
            // check for 'not true' to make sure that it _ONLY_ gets disabled if the owner _REALLY_ wants it disabled, not if they accidentally spelled "false" wrong or whatever.
            if (IsAllowed(Permission.TOMenu) && GetSettingsBool(Setting.vmenu_enable_time_sync))
            {
                TimeOptionsMenu = new TimeOptions();
                Menu menu = TimeOptionsMenu.GetMenu();
                MenuItem button = new MenuItem("Time Options", "Change the time, and edit other time related options.");
                button.Label = "→→→";
                AddMenu(menu, button);
            }

            // Add the weather options menu.
            // check for 'not true' to make sure that it _ONLY_ gets disabled if the owner _REALLY_ wants it disabled, not if they accidentally spelled "false" wrong or whatever.
            if (IsAllowed(Permission.WOMenu) && GetSettingsBool(Setting.vmenu_enable_weather_sync))
            {
                WeatherOptionsMenu = new WeatherOptions();
                Menu menu = WeatherOptionsMenu.GetMenu();
                MenuItem button = new MenuItem("Weather Options", "Change all weather related options here.");
                button.Label = "→→→";
                AddMenu(menu, button);
            }

            // Add the weapons menu.
            if (IsAllowed(Permission.WPMenu))
            {
                WeaponOptionsMenu = new WeaponOptions();
                Menu menu = WeaponOptionsMenu.GetMenu();
                MenuItem button = new MenuItem("Weapon Options", "Add/remove weapons, modify weapons and set ammo options.");
                button.Label = "→→→";
                AddMenu(menu, button);
            }

            // Add Voice Chat Menu.
            if (IsAllowed(Permission.VCMenu))
            {
                VoiceChatSettingsMenu = new VoiceChat();
                Menu menu = VoiceChatSettingsMenu.GetMenu();
                MenuItem button = new MenuItem("Voice Chat Settings", "Change Voice Chat options here.");
                button.Label = "→→→";
                AddMenu(menu, button);
            }

            {
                RecordingMenu = new Recording();
                Menu menu = RecordingMenu.GetMenu();
                MenuItem button = new MenuItem("Recording Options", "In-game recording options.");
                button.Label = "→→→";
                AddMenu(menu, button);
            }

            // Add misc settings menu.
            //if (CommonFunctions.IsAllowed(Permission.MSMenu))
            // removed the permissions check, because the misc menu should've never been restricted in the first place.
            // not sure why I even added this before... saving of preferences and similar functions should always be allowed.
            // no matter what.
            {
                MiscSettingsMenu = new MiscSettings();
                Menu menu = MiscSettingsMenu.GetMenu();
                MenuItem button = new MenuItem("Misc Settings", "Miscellaneous vMenu options/settings can be configured here. You can also save your settings in this menu.");
                button.Label = "→→→";
                AddMenu(menu, button);
            }

            // Add About Menu.
            AboutMenu = new About();
            Menu sub = AboutMenu.GetMenu();
            MenuItem btn = new MenuItem("About vMenu", "Information about vMenu.");
            btn.Label = "→→→";
            AddMenu(sub, btn);

            // Refresh everything.
            MenuController.Menus.ForEach((m) => m.RefreshIndex());

            if (!GetSettingsBool(Setting.vmenu_use_permissions))
            {
                Notify.Alert("vMenu is set up to ignore permissions, default permissions will be used.");
            }
        }
        #endregion
    }
}
