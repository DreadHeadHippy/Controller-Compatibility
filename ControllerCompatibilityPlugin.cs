using System;
using System.Collections.Generic;
using System.Linq;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using Playnite.SDK.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ControllerCompatibility
{
    // Custom attached property to add controller compatibility overlays
    public static class ControllerCompatibilityOverlay
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(ControllerCompatibilityOverlay),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && (bool)e.NewValue)
            {
                AddControllerOverlay(element);
            }
        }

        private static void AddControllerOverlay(FrameworkElement element)
        {
            // Create overlay border
            var overlay = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(4),
                Background = Brushes.Green,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 4, 0),
                ToolTip = "Controller Compatible",
                Opacity = 0.8
            };

            // Add controller icon
            var icon = new TextBlock
            {
                Text = "🎮",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            overlay.Child = icon;

            // Add to element
            if (element is Panel panel)
            {
                panel.Children.Add(overlay);
            }
            else if (element is ContentControl contentControl)
            {
                // Wrap in grid if needed
                var grid = new Grid();
                grid.Children.Add(contentControl.Content as UIElement);
                grid.Children.Add(overlay);
                contentControl.Content = grid;
            }
        }
    }
    public class ControllerCompatibilityPlugin : GenericPlugin
    {
    // Removed overlay refresh timer for performance
    private System.Collections.Generic.HashSet<string> _gamesWithOverlays = new System.Collections.Generic.HashSet<string>();
    private static readonly ILogger logger = LogManager.GetLogger();
    private System.Collections.Generic.Dictionary<System.Guid, string> _gameCompatibilityOverrides = new System.Collections.Generic.Dictionary<System.Guid, string>();
    private ControllerDetectionService _controllerService;
    private CompatibilityDatabase _compatibilityDatabase;
    private string _instanceId = Guid.NewGuid().ToString();
    private string _overridesPath;
    private readonly string _logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Playnite", "ExtensionsData", "ControllerCompatibility", "controller_plugin_log.txt");

        public override Guid Id { get; } = Guid.Parse("12345678-1234-1234-1234-123456789012");

        public ControllerCompatibilityPlugin(IPlayniteAPI api) : base(api)
        {
            // IMMEDIATE LOGGING - Check if constructor is called
            System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === NEW VERSION CONSTRUCTOR START - INSTANCE {_instanceId} ===\r\n");

            System.Diagnostics.Debug.WriteLine("=== CONTROLLER COMPATIBILITY PLUGIN CONSTRUCTOR START ===");
            Console.WriteLine("=== CONTROLLER COMPATIBILITY PLUGIN CONSTRUCTOR START ===");

            // Ensure icon is present in user data folder for top panel
            try
            {
                var userDataPath = GetPluginUserDataPath();
                var iconFileName = "controller_overlay_icon.png";
                var userIconPath = System.IO.Path.Combine(userDataPath, iconFileName);
                var pluginInstallDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var installIconPath = System.IO.Path.Combine(pluginInstallDir, iconFileName);
                if (!System.IO.File.Exists(userIconPath))
                {
                    if (System.IO.File.Exists(installIconPath))
                    {
                        System.IO.File.Copy(installIconPath, userIconPath, true);
                        System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === COPIED ICON TO USER DATA FOLDER ===\r\n");
                    }
                    else
                    {
                        System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === ICON FILE MISSING IN INSTALL DIR ===\r\n");
                    }
                }
                else
                {
                    System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === ICON ALREADY PRESENT IN USER DATA FOLDER ===\r\n");
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === ICON COPY ERROR: {ex.Message} ===\r\n");
            }

            // Initialize services
            try
            {
                _controllerService = new ControllerDetectionService();
                _compatibilityDatabase = new CompatibilityDatabase();
                _overridesPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                                      "Playnite", "ExtensionsData", "ControllerCompatibility", "overrides.json");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_overridesPath));
                LoadOverrides();
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === SERVICES INITIALIZED SUCCESSFULLY - INSTANCE {_instanceId} ===\r\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === SERVICE INITIALIZATION FAILED: {ex.Message} - INSTANCE {_instanceId} ===\r\n");
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === STACK TRACE: {ex.StackTrace} - INSTANCE {_instanceId} ===\r\n");
            }

            // Overlay refresh timer removed; overlays now update only on compatibility/controller changes

            System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === CONTROLLER COMPATIBILITY PLUGIN CONSTRUCTOR END - INSTANCE {_instanceId} ===\r\n");
            System.Diagnostics.Debug.WriteLine("=== CONTROLLER COMPATIBILITY PLUGIN CONSTRUCTOR END ===");
            Console.WriteLine("=== CONTROLLER COMPATIBILITY PLUGIN CONSTRUCTOR END ===");
            // Overlay restoration: Use GetGameViewControl to trigger overlay refresh on view change
        }

        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === NEW VERSION GET TOP PANEL ITEMS CALLED ===\r\n");

            if (_controllerService == null)
            {
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === CONTROLLER SERVICE IS NULL ===\r\n");
                yield return new TopPanelItem
                {
                    Title = "Controller Service Error",
                    Icon = null
                };
                yield break;
            }

            try
            {
                // Refresh controllers to get latest status
                _controllerService.RefreshControllers();
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === CONTROLLER REFRESH ERROR: {ex.Message} ===\r\n");
            }

            // Get detected controllers
            var controllers = new List<DetectedController>();
            try
            {
                controllers = _controllerService.GetConnectedControllers();
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === FOUND {controllers.Count} CONTROLLERS ===\r\n");
                foreach (var controller in controllers)
                {
                    System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === CONTROLLER: {controller.Name} ({controller.Type}) ===\r\n");
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === GET CONTROLLERS ERROR: {ex.Message} ===\r\n");
            }

            var overlayIconPath = System.IO.Path.Combine(GetPluginUserDataPath(), "controller_overlay_icon.png");

            if (controllers.Any())
            {
                // Show the first detected controller
                var primaryController = controllers.First();
                yield return new TopPanelItem
                {
                    Title = $"{primaryController.Name}",
                    Icon = overlayIconPath
                };

                if (controllers.Count > 1)
                {
                    // If multiple controllers, show count
                    yield return new TopPanelItem
                    {
                        Title = $"+{controllers.Count - 1} more",
                        Icon = overlayIconPath
                    };
                }
            }
            else
            {
                yield return new TopPanelItem
                {
                    Title = "No Controllers Detected",
                    Icon = overlayIconPath
                };
            }
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === NEW VERSION GET GAME MENU ITEMS CALLED - INSTANCE {_instanceId} ===\r\n");

            yield return new GameMenuItem
            {
                Description = "Check Controller Compatibility",
                Action = (gameMenuItemActionArgs) =>
                {
                    System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === MENU ACTION CALLED - INSTANCE {_instanceId} ===\r\n");
                    ShowCompatibilityStatsWindow();
                }
            };

            yield return new GameMenuItem
            {
                Description = "-"
            };

            yield return new GameMenuItem
            {
                Description = "Set Controller: Full Support",
                Action = (gameMenuItemActionArgs) =>
                {
                    SetManualCompatibility(gameMenuItemActionArgs.Games, "Full");
                }
            };

            yield return new GameMenuItem
            {
                Description = "Set Controller: Partial Support",
                Action = (gameMenuItemActionArgs) =>
                {
                    SetManualCompatibility(gameMenuItemActionArgs.Games, "Partial");
                }
            };

            yield return new GameMenuItem
            {
                Description = "Set Controller: No Support",
                Action = (gameMenuItemActionArgs) =>
                {
                    SetManualCompatibility(gameMenuItemActionArgs.Games, "None");
                }
            };

            yield return new GameMenuItem
            {
                Description = "Auto-Detect Controller Compatibility",
                Action = (gameMenuItemActionArgs) =>
                {
                    AutoDetectCompatibility(gameMenuItemActionArgs.Games);
                }
            };
        }

        public override System.Windows.Controls.Control GetGameViewControl(GetGameViewControlArgs args)
        {
            System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === GET GAME VIEW CONTROL CALLED - Mode: {args.Mode} ===\r\n");
            System.Diagnostics.Debug.WriteLine($"=== GET GAME VIEW CONTROL CALLED - Mode: {args.Mode} ===");
            Console.WriteLine($"=== GET GAME VIEW CONTROL CALLED - Mode: {args.Mode} ===");

            // Refresh overlays when view changes
            RefreshAllOverlays();

            // Return the proper controller compatibility overlay control
            var control = new ControllerCompatibilityItemControl();
            System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === CREATED CONTROLLERCONTROLCOMPATIBILITYITEMCONTROL ===\r\n");
            return control;
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === ON APPLICATION STARTED CALLED ===\r\n");

            // Start controller monitoring
            try
            {
                _controllerService.StartMonitoring();
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === CONTROLLER MONITORING STARTED ===\r\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === CONTROLLER MONITORING FAILED: {ex.Message} ===\r\n");
            }

            // Overlay refresh timer and layout event hooks removed for performance
            // Unsubscribe from layout events (no longer needed)
            // LayoutUpdated event handler fully removed

            // Try to add custom element support for visual overlays
            try
            {
                AddCustomElementSupport(new AddCustomElementSupportArgs
                {
                    ElementList = new System.Collections.Generic.List<string> { "ControllerCompatibility" },
                    SourceName = "Controller Compatibility"
                });
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === ADD CUSTOM ELEMENT SUPPORT CALLED - INSTANCE {_instanceId} ===\r\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === ADD CUSTOM ELEMENT SUPPORT FAILED: {ex.Message} - INSTANCE {_instanceId} ===\r\n");
            }

            // Refresh overlays for games with manual overrides on startup
            try
            {
                RefreshAllOverlays();
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === INITIAL OVERLAY REFRESH COMPLETED - INSTANCE {_instanceId} ===\r\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === INITIAL OVERLAY REFRESH FAILED: {ex.Message} - INSTANCE {_instanceId} ===\r\n");
            }

            // Delayed refresh timers removed for performance

            // Add controller compatibility features to games for testing
            try
            {
                var games = PlayniteApi.Database.Games.Take(5).ToList(); // Test with first 5 games
                foreach (var game in games)
                {
                    // Add a "Controller Compatible" feature
                    var controllerFeature = PlayniteApi.Database.Features.Add("Controller Compatible");
                    if (!game.FeatureIds.Contains(controllerFeature.Id))
                    {
                        game.FeatureIds.Add(controllerFeature.Id);
                        PlayniteApi.Database.Games.Update(game);
                        System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === ADDED CONTROLLER FEATURE TO: {game.Name} ===\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === FAILED TO ADD FEATURES: {ex.Message} ===\r\n");
            }
        }

        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {
            System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === ON GAME SELECTED CALLED - Type: {args.NewValue?.GetType().Name ?? "null"} ===\r\n");

            // Do not refresh overlays on every selection in details view
            // Only add overlays if a game is selected in grid view (if you want, you can add logic here based on args or UI state)
        }

        private void AddVisualOverlaysToGames(System.Collections.Generic.List<Playnite.SDK.Models.Game> games)
        {
            // Try to find game UI elements and add overlays
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow == null)
            {
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === MAIN WINDOW IS NULL ===\r\n");
                return;
            }

            System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === MAIN WINDOW TYPE: {mainWindow.GetType().Name} ===\r\n");
            System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === MAIN WINDOW VISUAL CHILDREN COUNT: {VisualTreeHelper.GetChildrenCount(mainWindow)} ===\r\n");

            // Look for ListBoxItem elements (game tiles in ListBox-based library)
            var listBoxItems = FindVisualChildren<System.Windows.Controls.ListBoxItem>(mainWindow).ToList();
            System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === FOUND {listBoxItems.Count} LISTBOXITEMS ===\r\n");

            // Log some sample DataContext values to understand the structure
            var sampleItems = listBoxItems.Take(3).ToList();
            foreach (var item in sampleItems)
            {
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === SAMPLE LISTBOXITEM: DataContext='{item.DataContext}' ===\r\n");
            }

            // Find game tiles by matching game names
            foreach (var game in games)
            {
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === ADDING OVERLAYS FOR GAME: '{game.Name}' (ID: {game.Id}) ===\r\n");

                // Try exact match first
                var matchingTiles = listBoxItems.Where(item =>
                    item.DataContext?.ToString() == game.Name).ToList();

                // If no exact match, try partial match
                if (matchingTiles.Count == 0)
                {
                    matchingTiles = listBoxItems.Where(item =>
                        item.DataContext?.ToString()?.Contains(game.Name) == true ||
                        game.Name.Contains(item.DataContext?.ToString() ?? "")).ToList();
                }

                // If still no match, try normalized match (remove special chars, spaces)
                if (matchingTiles.Count == 0)
                {
                    var normalizedGameName = System.Text.RegularExpressions.Regex.Replace(game.Name.ToLower(), @"[^a-z0-9]", "");
                    normalizedGameName = System.Text.RegularExpressions.Regex.Replace(normalizedGameName, @"0+(\d)", "$1");
                    matchingTiles = listBoxItems.Where(item =>
                    {
                        string itemText;
                        string dataContextType = item.DataContext?.GetType().Name ?? "null";
                        if (item.DataContext is Playnite.SDK.Models.Game tileGame)
                        {
                            itemText = tileGame.Name;
                        }
                        else if (dataContextType == "GamesCollectionViewEntry")
                        {
                            // Try to access the Game property from GamesCollectionViewEntry
                            var gameProperty = item.DataContext.GetType().GetProperty("Game");
                            if (gameProperty != null)
                            {
                                var gameObj = gameProperty.GetValue(item.DataContext) as Playnite.SDK.Models.Game;
                                if (gameObj != null)
                                {
                                    itemText = gameObj.Name;
                                }
                                else
                                {
                                    itemText = item.DataContext?.ToString() ?? "";
                                }
                            }
                            else
                            {
                                itemText = item.DataContext?.ToString() ?? "";
                            }
                        }
                        else
                        {
                            itemText = item.DataContext?.ToString() ?? "";
                        }

                        var normalizedItemText = System.Text.RegularExpressions.Regex.Replace(itemText.ToLower(), @"[^a-z0-9]", "");
                        normalizedItemText = System.Text.RegularExpressions.Regex.Replace(normalizedItemText, @"0+(\d)", "$1");
                        var lengthDiff = System.Math.Abs(normalizedItemText.Length - normalizedGameName.Length);
                        var matches = normalizedItemText == normalizedGameName ||
                                     (lengthDiff <= 3 && (normalizedItemText.Contains(normalizedGameName) || normalizedGameName.Contains(normalizedItemText)));

                        // Also try base name matching (remove numbers)
                        if (!matches)
                        {
                            var baseGameName = System.Text.RegularExpressions.Regex.Replace(normalizedGameName, @"\d+", "");
                            var baseTileName = System.Text.RegularExpressions.Regex.Replace(normalizedItemText, @"\d+", "");
                            matches = baseGameName == baseTileName && !string.IsNullOrEmpty(baseGameName);
                        }

                        if (!matches)
                        {
                            System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === TILE '{itemText}' (DataContext: {dataContextType}) normalized '{normalizedItemText}' (len {normalizedItemText.Length}) != GAME '{game.Name}' normalized '{normalizedGameName}' (len {normalizedGameName.Length}), diff {lengthDiff} ===\r\n");
                        }
                        else
                        {
                            System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === TILE MATCH FOUND: '{itemText}' (DataContext: {dataContextType}) for game '{game.Name}' ===\r\n");
                        }
                        return matches;
                    }).ToList();
                }

                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === GAME '{game.Name}' FOUND {matchingTiles.Count} MATCHING TILES ===\r\n");

                foreach (var tile in matchingTiles)
                {
                    try
                    {
                        // Determine compatibility status (for now, assume all selected games are compatible)
                        var compatibility = GetGameCompatibility(game);

                        // Save the default compatibility as an override so it persists after restart
                        if (!_gameCompatibilityOverrides.ContainsKey(game.Id))
                        {
                            UpdateGameCompatibility(game, compatibility);
                        }

                        AddOverlayToGameTile(tile, game, compatibility);
                        _gamesWithOverlays.Add(game.Name); // Track games with overlays
                        System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === ADDED {compatibility} OVERLAY TO TILE FOR '{game.Name}' ===\r\n");
                    }
                    catch (Exception ex)
                    {
                        System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === ERROR ADDING OVERLAY TO '{game.Name}': {ex.Message} ===\r\n");
                    }
                }
            }
        }

        private string GetGameCompatibility(Playnite.SDK.Models.Game game)
        {
            // Check if we have a manual override first (but ignore "Unknown" overrides)
            if (_gameCompatibilityOverrides.ContainsKey(game.Id) && _gameCompatibilityOverrides[game.Id] != "Unknown")
            {
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === MANUAL OVERRIDE FOUND FOR '{game.Name}': {_gameCompatibilityOverrides[game.Id]} ===\r\n");
                return _gameCompatibilityOverrides[game.Id];
            }

            // If override is "Unknown" or no override exists, check database or auto-detect
            if (_compatibilityDatabase != null)
            {
                var compatibilityInfo = _compatibilityDatabase.GetCompatibilityInfo(game);
                if (compatibilityInfo.SupportLevel != ControllerSupportLevel.Unknown)
                {
                    System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === DATABASE INFO FOUND FOR '{game.Name}': {compatibilityInfo.SupportLevel} ===\r\n");
                    return compatibilityInfo.SupportLevel.ToString();
                }

                // If not in database, auto-detect and store
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === NO DATABASE INFO FOR '{game.Name}', ATTEMPTING AUTO-DETECTION ===\r\n");
                try
                {
                    _compatibilityDatabase.UpdateGameCompatibility(game);
                    // Get the newly detected compatibility
                    var updatedInfo = _compatibilityDatabase.GetCompatibilityInfo(game);
                    System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === AUTO-DETECTED '{game.Name}': {updatedInfo.SupportLevel} ===\r\n");
                    return updatedInfo.SupportLevel.ToString();
                }
                catch (Exception ex)
                {
                    System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === AUTO-DETECTION FAILED FOR '{game.Name}': {ex.Message} ===\r\n");
                }
            }
            else
            {
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === COMPATIBILITY DATABASE IS NULL ===\r\n");
            }

            // Fallback: return Unknown if detection fails
            System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === FALLBACK TO UNKNOWN FOR '{game.Name}' ===\r\n");
            return "Unknown";
        }

        private void SetManualCompatibility(System.Collections.Generic.IEnumerable<Playnite.SDK.Models.Game> games, string compatibility)
        {
            System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === SET MANUAL COMPATIBILITY CALLED: {compatibility} ===\r\n");
            foreach (var game in games)
            {
                // Store manual compatibility setting (we'll need to implement persistence)
                // For now, just update the overlay immediately
                UpdateGameCompatibility(game, compatibility);
                System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === SET MANUAL COMPATIBILITY: {game.Name} -> {compatibility} ===\r\n");
            }

            // Refresh overlays to show new compatibility
            RefreshAllOverlays();
        }

        private void AutoDetectCompatibility(System.Collections.Generic.IEnumerable<Playnite.SDK.Models.Game> games)
        {
            foreach (var game in games)
            {
                try
                {
                    // Use the database to auto-detect compatibility
                    if (_compatibilityDatabase != null)
                    {
                        _compatibilityDatabase.UpdateGameCompatibility(game);
                        var compatibilityInfo = _compatibilityDatabase.GetCompatibilityInfo(game);
                        System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === AUTO-DETECTED COMPATIBILITY: {game.Name} -> {compatibilityInfo.SupportLevel} ===\r\n");
                    }
                }
                catch (Exception ex)
                {
                    System.IO.File.AppendAllText(@"C:\Temp\controller_plugin_log.txt", $"{DateTime.Now}: === AUTO-DETECTION FAILED FOR '{game.Name}': {ex.Message} ===\r\n");
                }
            }

            // Refresh overlays to show detected compatibility
            RefreshAllOverlays();
        }

        private void UpdateGameCompatibility(Playnite.SDK.Models.Game game, string compatibility)
        {
            // Store the compatibility setting for this game
            if (!_gameCompatibilityOverrides.ContainsKey(game.Id))
            {
                _gameCompatibilityOverrides[game.Id] = compatibility;
            }
            else
            {
                _gameCompatibilityOverrides[game.Id] = compatibility;
            }
            SaveOverrides();
        }

        private void LoadOverrides()
        {
            try
            {
                if (System.IO.File.Exists(_overridesPath))
                {
                    _gameCompatibilityOverrides = new System.Collections.Generic.Dictionary<System.Guid, string>();
                    var lines = System.IO.File.ReadAllLines(_overridesPath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split(':');
                        if (parts.Length == 2 && System.Guid.TryParse(parts[0], out var id))
                        {
                            _gameCompatibilityOverrides[id] = parts[1];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error loading overrides: {ex.Message}");
                _gameCompatibilityOverrides = new System.Collections.Generic.Dictionary<System.Guid, string>();
            }
        }

        private void SaveOverrides()
        {
            try
            {
                var lines = new System.Collections.Generic.List<string>();
                foreach (var kvp in _gameCompatibilityOverrides)
                {
                    lines.Add($"{kvp.Key}:{kvp.Value}");
                }
                System.IO.File.WriteAllLines(_overridesPath, lines);
            }
            catch (Exception ex)
            {
                logger.Error($"Error saving overrides: {ex.Message}");
            }
        }

        private void AddOverlayToGameItem(System.Windows.Controls.ContentControl gameItem, Playnite.SDK.Models.Game game)
        {
            // Create overlay border
            var overlay = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(4),
                Background = Brushes.Green,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 4, 0),
                ToolTip = "Controller Compatible",
                Opacity = 0.9
            };

            // Add controller icon
            var icon = new TextBlock
            {
                Text = "🎮",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            overlay.Child = icon;

            // Add to game item
            if (gameItem.Content is Panel panel)
            {
                panel.Children.Add(overlay);
            }
            else
            {
                // Wrap content in grid
                var grid = new Grid();
                grid.Children.Add(gameItem.Content as UIElement);
                grid.Children.Add(overlay);
                gameItem.Content = grid;
            }
        }

        private void AddOverlayToGameTile(System.Windows.Controls.ListBoxItem gameTile, Playnite.SDK.Models.Game game, string compatibility)
        {
            // Determine colors based on compatibility
            Brush backgroundBrush;
            string tooltip;

            switch (compatibility.ToLower())
            {
                case "full":
                    backgroundBrush = Brushes.Green;
                    tooltip = "Full Controller Support";
                    break;
                case "partial":
                    backgroundBrush = Brushes.Yellow;
                    tooltip = "Partial Controller Support";
                    break;
                case "none":
                    backgroundBrush = Brushes.Red;
                    tooltip = "No Controller Support";
                    break;
                default:
                    backgroundBrush = Brushes.Gray;
                    tooltip = "Unknown Controller Support";
                    break;
            }

            // Try to find an existing Grid in the visual tree to add the overlay to
            var existingGrid = FindVisualChildren<System.Windows.Controls.Grid>(gameTile).FirstOrDefault();

            if (existingGrid != null)
            {
                // Check if overlay already exists
                var existingOverlay = existingGrid.Children.OfType<Border>()
                    .FirstOrDefault(b => b.ToolTip?.ToString()?.Contains("Controller") == true);

                // Only update overlay if compatibility or color has changed
                if (existingOverlay != null)
                {
                    var currentColor = existingOverlay.Background as SolidColorBrush;
                    var newColor = backgroundBrush as SolidColorBrush;
                    if (currentColor == null || newColor == null || currentColor.Color != newColor.Color || existingOverlay.ToolTip?.ToString() != tooltip)
                    {
                        existingOverlay.Background = backgroundBrush;
                        existingOverlay.ToolTip = tooltip;
                    }
                    // No need to re-wrap or re-add overlay
                }
                else
                {
                    // Add overlay only if not present
                    var overlay = new Border
                    {
                        Width = 20,
                        Height = 20,
                        CornerRadius = new CornerRadius(3),
                        Background = backgroundBrush,
                        BorderBrush = Brushes.White,
                        BorderThickness = new Thickness(1),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 2, 2, 0),
                        ToolTip = tooltip,
                        Opacity = 0.9
                    };
                    var icon = new TextBlock
                    {
                        Text = "🎮",
                        FontSize = 10,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontWeight = FontWeights.Bold
                    };
                    overlay.Child = icon;
                    existingGrid.Children.Add(overlay);
                }
            }
            else
            {
                // Fallback: only wrap if overlay not already present
                if (gameTile.Content is Grid grid && grid.Children.OfType<Border>().Any(b => b.ToolTip?.ToString()?.Contains("Controller") == true))
                {
                    // Overlay already present, update if needed
                    var existingOverlay = grid.Children.OfType<Border>().FirstOrDefault(b => b.ToolTip?.ToString()?.Contains("Controller") == true);
                    var currentColor = existingOverlay?.Background as SolidColorBrush;
                    var newColor = backgroundBrush as SolidColorBrush;
                    if (existingOverlay != null && (currentColor == null || newColor == null || currentColor.Color != newColor.Color || existingOverlay.ToolTip?.ToString() != tooltip))
                    {
                        existingOverlay.Background = backgroundBrush;
                        existingOverlay.ToolTip = tooltip;
                    }
                }
                else
                {
                    // Create overlay border
                    var overlay = new Border
                    {
                        Width = 20,
                        Height = 20,
                        CornerRadius = new CornerRadius(3),
                        Background = backgroundBrush,
                        BorderBrush = Brushes.White,
                        BorderThickness = new Thickness(1),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 2, 2, 0),
                        ToolTip = tooltip,
                        Opacity = 0.9
                    };
                    var icon = new TextBlock
                    {
                        Text = "🎮",
                        FontSize = 10,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontWeight = FontWeights.Bold
                    };
                    overlay.Child = icon;
                    // Create a grid to hold both the original content and the overlay
                    var newGrid = new Grid();
                    if (gameTile.Content is UIElement uiElement)
                    {
                        newGrid.Children.Add(uiElement);
                    }
                    newGrid.Children.Add(overlay);
                    gameTile.Content = newGrid;
                }
            }
        }

        // Removed: overlays now refresh only on controller/compatibility changes

        private void RefreshAllOverlays()
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow == null) return;

            try
            {
                // Batch all overlay updates on the UI thread using dispatcher
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Find all ListBoxItem elements
                    var listBoxItems = FindVisualChildren<System.Windows.Controls.ListBoxItem>(mainWindow).ToList();

                    // Get all games that should have overlays (those with manual overrides or in the tracking set)
                    var gamesToRefresh = new System.Collections.Generic.HashSet<string>(_gamesWithOverlays);
                    foreach (var overrideEntry in _gameCompatibilityOverrides)
                    {
                        var game = PlayniteApi.Database.Games.FirstOrDefault(g => g.Id == overrideEntry.Key);
                        if (game != null)
                        {
                            gamesToRefresh.Add(game.Name);
                        }
                    }

                    foreach (var gameName in gamesToRefresh)
                    {
                        var matchingTiles = listBoxItems.Where(item =>
                        {
                            string itemText;
                            string dataContextType = item.DataContext?.GetType().Name ?? "null";
                            if (item.DataContext is Playnite.SDK.Models.Game tileGame)
                            {
                                itemText = tileGame.Name;
                            }
                            else if (dataContextType == "GamesCollectionViewEntry")
                            {
                                var gameProperty = item.DataContext.GetType().GetProperty("Game");
                                if (gameProperty != null)
                                {
                                    var gameObj = gameProperty.GetValue(item.DataContext) as Playnite.SDK.Models.Game;
                                    if (gameObj != null)
                                    {
                                        itemText = gameObj.Name;
                                    }
                                    else
                                    {
                                        itemText = item.DataContext?.ToString() ?? "";
                                    }
                                }
                                else
                                {
                                    itemText = item.DataContext?.ToString() ?? "";
                                }
                            }
                            else
                            {
                                itemText = item.DataContext?.ToString() ?? "";
                            }
                            bool exactMatch = itemText == gameName;
                            bool containsMatch = itemText.Contains(gameName) || gameName.Contains(itemText);
                            return exactMatch || containsMatch;
                        }).ToList();

                        if (matchingTiles.Count == 0)
                        {
                            var normalizedGameName = System.Text.RegularExpressions.Regex.Replace(gameName.ToLower(), @"[^a-z0-9]", "");
                            normalizedGameName = System.Text.RegularExpressions.Regex.Replace(normalizedGameName, @"0+(\d)", "$1");
                            matchingTiles = listBoxItems.Where(item =>
                            {
                                string itemText;
                                string dataContextType = item.DataContext?.GetType().Name ?? "null";
                                if (item.DataContext is Playnite.SDK.Models.Game tileGame)
                                {
                                    itemText = tileGame.Name;
                                }
                                else if (dataContextType == "GamesCollectionViewEntry")
                                {
                                    var gameProperty = item.DataContext.GetType().GetProperty("Game");
                                    if (gameProperty != null)
                                    {
                                        var gameObj = gameProperty.GetValue(item.DataContext) as Playnite.SDK.Models.Game;
                                        if (gameObj != null)
                                        {
                                            itemText = gameObj.Name;
                                        }
                                        else
                                        {
                                            itemText = item.DataContext?.ToString() ?? "";
                                        }
                                    }
                                    else
                                    {
                                        itemText = item.DataContext?.ToString() ?? "";
                                    }
                                }
                                else
                                {
                                    itemText = item.DataContext?.ToString() ?? "";
                                }
                                var normalizedItemText = System.Text.RegularExpressions.Regex.Replace(itemText.ToLower(), @"[^a-z0-9]", "");
                                normalizedItemText = System.Text.RegularExpressions.Regex.Replace(normalizedItemText, @"0+(\d)", "$1");
                                var lengthDiff = System.Math.Abs(normalizedItemText.Length - normalizedGameName.Length);
                                return normalizedItemText == normalizedGameName ||
                                       (lengthDiff <= 3 && (normalizedItemText.Contains(normalizedGameName) || normalizedGameName.Contains(normalizedItemText))) ||
                                       (System.Text.RegularExpressions.Regex.Replace(normalizedGameName, @"\d+", "") == System.Text.RegularExpressions.Regex.Replace(normalizedItemText, @"\d+", "") && !string.IsNullOrEmpty(System.Text.RegularExpressions.Regex.Replace(normalizedGameName, @"\d+", "")));
                            }).ToList();
                        }

                        foreach (var tile in matchingTiles)
                        {
                            var game = PlayniteApi.Database.Games.FirstOrDefault(g => g.Name == gameName);
                            if (game == null) continue;

                            var compatibility = GetGameCompatibility(game);

                            Brush backgroundBrush;
                            string tooltip;

                            switch (compatibility.ToLower())
                            {
                                case "full":
                                    backgroundBrush = Brushes.Green;
                                    tooltip = "Full Controller Support";
                                    break;
                                case "partial":
                                    backgroundBrush = Brushes.Yellow;
                                    tooltip = "Partial Controller Support";
                                    break;
                                case "none":
                                    backgroundBrush = Brushes.Red;
                                    tooltip = "No Controller Support";
                                    break;
                                default:
                                    backgroundBrush = Brushes.Gray;
                                    tooltip = "Unknown Controller Support";
                                    break;
                            }

                            var existingGrid = FindVisualChildren<System.Windows.Controls.Grid>(tile).FirstOrDefault();
                            if (existingGrid != null)
                            {
                                var existingOverlay = existingGrid.Children.OfType<Border>()
                                    .FirstOrDefault(b => b.ToolTip?.ToString()?.Contains("Controller") == true);

                                if (existingOverlay == null)
                                {
                                    var overlay = new Border
                                    {
                                        Width = 20,
                                        Height = 20,
                                        CornerRadius = new CornerRadius(3),
                                        Background = backgroundBrush,
                                        BorderBrush = Brushes.White,
                                        BorderThickness = new Thickness(1),
                                        HorizontalAlignment = HorizontalAlignment.Right,
                                        VerticalAlignment = VerticalAlignment.Top,
                                        Margin = new Thickness(0, 2, 2, 0),
                                        ToolTip = tooltip,
                                        Opacity = 0.9
                                    };
                                    var icon = new TextBlock
                                    {
                                        Text = "🎮",
                                        FontSize = 10,
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        VerticalAlignment = VerticalAlignment.Center,
                                        FontWeight = FontWeights.Bold
                                    };
                                    overlay.Child = icon;
                                    existingGrid.Children.Add(overlay);
                                }
                                else
                                {
                                    existingOverlay.Background = backgroundBrush;
                                    existingOverlay.ToolTip = tooltip;
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // Only log errors, not every refresh
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === OVERLAY REFRESH ERROR: {ex.Message} ===\r\n");
            }
        }
        private System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    yield return typedChild;
                }

                foreach (var descendant in FindVisualChildren<T>(child))
                {
                    yield return descendant;
                }
            }
        }

        private void ShowCompatibilityStatsWindow()
        {
            System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === SHOW COMPATIBILITY STATS WINDOW START - INSTANCE {_instanceId} ===\r\n");

            try
            {

                if (_compatibilityDatabase == null)
                {
                    System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === _compatibilityDatabase IS NULL - INSTANCE {_instanceId} ===\r\n");
                    try
                    {
                        _compatibilityDatabase = new CompatibilityDatabase();
                        System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === REINITIALIZED _compatibilityDatabase - INSTANCE {_instanceId} ===\r\n");
                    }
                    catch (Exception ex)
                    {
                        System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === FAILED TO REINITIALIZE _compatibilityDatabase: {ex.Message} - INSTANCE {_instanceId} ===\r\n");
                    }
                }
                if (_controllerService == null)
                {
                    System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === _controllerService IS NULL - INSTANCE {_instanceId} ===\r\n");
                    try
                    {
                        _controllerService = new ControllerDetectionService();
                        System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === REINITIALIZED _controllerService - INSTANCE {_instanceId} ===\r\n");
                    }
                    catch (Exception ex)
                    {
                        System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === FAILED TO REINITIALIZE _controllerService: {ex.Message} - INSTANCE {_instanceId} ===\r\n");
                    }
                }

                // Update database with manual overrides before showing stats
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === UPDATING DATABASE WITH MANUAL OVERRIDES ===\r\n");
                foreach (var overrideEntry in _gameCompatibilityOverrides)
                {
                    System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === PROCESSING OVERRIDE: {overrideEntry.Key} -> {overrideEntry.Value} ===\r\n");
                    var game = PlayniteApi.Database.Games.FirstOrDefault(g => g.Id == overrideEntry.Key);
                    if (game != null)
                    {
                        var supportLevel = overrideEntry.Value.ToLower() switch
                        {
                            "full" => ControllerSupportLevel.Full,
                            "partial" => ControllerSupportLevel.Partial,
                            "none" => ControllerSupportLevel.None,
                            _ => ControllerSupportLevel.Unknown
                        };
                        System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === UPDATING COMPATIBILITY FOR: {game.Name} ===\r\n");
                        _compatibilityDatabase.UpdateGameCompatibility(game, supportLevel, CompatibilitySource.User);
                    }
                }

                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === CREATING STATS WINDOW ===\r\n");
                var statsWindow = new CompatibilityStatsWindow(_compatibilityDatabase, _controllerService);
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === SHOWING STATS WINDOW ===\r\n");
                statsWindow.ShowDialog();
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === COMPATIBILITY STATS WINDOW SHOWN ===\r\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === ERROR SHOWING COMPATIBILITY STATS WINDOW: {ex.Message} ===\r\n");
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === STACK TRACE: {ex.StackTrace} ===\r\n");
            }
        }
    }
}
