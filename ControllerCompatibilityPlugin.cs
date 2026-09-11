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
using System.Windows.Threading;
using System.Runtime.CompilerServices;
using System.ComponentModel;

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
                Text = "??",
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
    private static readonly ILogger logger = LogManager.GetLogger();
    private System.Collections.Generic.Dictionary<System.Guid, string> _gameCompatibilityOverrides = new System.Collections.Generic.Dictionary<System.Guid, string>();
    private ControllerDetectionService _controllerService;
    private CompatibilityDatabase _compatibilityDatabase;

    // Raised when a game's stored compatibility changes so the bound tile control can refresh itself
    public event Action<System.Guid> CompatibilityChanged;
    internal ControllerDetectionService ControllerService => _controllerService;
    private string _instanceId = Guid.NewGuid().ToString();
    private string _overridesPath;
    private int _overlayRefreshVersion;
    private bool _customTileControlActive;
    private DispatcherTimer _layoutRefreshDebounceTimer;
    private bool _layoutRefreshHooked;
    private static readonly DependencyPropertyDescriptor TileDataContextDescriptor =
        DependencyPropertyDescriptor.FromProperty(FrameworkElement.DataContextProperty, typeof(ListBoxItem));
    // Tracks tiles we've already attached a DataContext-changed hook to, so overlays update
    // synchronously the instant WPF reassigns a recycled container's data (no scan/poll races).
    private readonly Dictionary<int, WeakReference<ListBoxItem>> _hookedTiles = new Dictionary<int, WeakReference<ListBoxItem>>();
    private readonly string _logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Playnite", "ExtensionsData", "ControllerCompatibility", "controller_plugin_log.txt");

        public override Guid Id { get; } = Guid.Parse("12345678-1234-1234-1234-123456789012");

        public ControllerCompatibilityPlugin(IPlayniteAPI api) : base(api)
        {
            // Ensure the log directory exists before any logging happens
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_logPath));

            // IMMEDIATE LOGGING - Check if constructor is called
            System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === NEW VERSION CONSTRUCTOR START - INSTANCE {_instanceId} ===\r\n");

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
                        System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === COPIED ICON TO USER DATA FOLDER ===\r\n");
                    }
                    else
                    {
                        System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === ICON FILE MISSING IN INSTALL DIR ===\r\n");
                    }
                }
                else
                {
                    System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === ICON ALREADY PRESENT IN USER DATA FOLDER ===\r\n");
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === ICON COPY ERROR: {ex.Message} ===\r\n");
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
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === SERVICES INITIALIZED SUCCESSFULLY - INSTANCE {_instanceId} ===\r\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === SERVICE INITIALIZATION FAILED: {ex.Message} - INSTANCE {_instanceId} ===\r\n");
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === STACK TRACE: {ex.StackTrace} - INSTANCE {_instanceId} ===\r\n");
            }

            // Overlay refresh timer removed; overlays now update only on compatibility/controller changes

            System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === CONTROLLER COMPATIBILITY PLUGIN CONSTRUCTOR END - INSTANCE {_instanceId} ===\r\n");
            System.Diagnostics.Debug.WriteLine("=== CONTROLLER COMPATIBILITY PLUGIN CONSTRUCTOR END ===");
            Console.WriteLine("=== CONTROLLER COMPATIBILITY PLUGIN CONSTRUCTOR END ===");
            // Overlay restoration: Use GetGameViewControl to trigger overlay refresh on view change
        }

        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === NEW VERSION GET TOP PANEL ITEMS CALLED ===\r\n");

            if (_controllerService == null)
            {
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === CONTROLLER SERVICE IS NULL ===\r\n");
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
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === CONTROLLER REFRESH ERROR: {ex.Message} ===\r\n");
            }

            // Get detected controllers
            var controllers = new List<DetectedController>();
            try
            {
                controllers = _controllerService.GetConnectedControllers();
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === FOUND {controllers.Count} CONTROLLERS ===\r\n");
                foreach (var controller in controllers)
                {
                    System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === CONTROLLER: {controller.Name} ({controller.Type}) ===\r\n");
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === GET CONTROLLERS ERROR: {ex.Message} ===\r\n");
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
            System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === NEW VERSION GET GAME MENU ITEMS CALLED - INSTANCE {_instanceId} ===\r\n");

            yield return new GameMenuItem
            {
                Description = "Check Controller Compatibility",
                Action = (gameMenuItemActionArgs) =>
                {
                    System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === MENU ACTION CALLED - INSTANCE {_instanceId} ===\r\n");
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
            try
            {
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === GET GAME VIEW CONTROL CALLED - Mode: {args.Mode} ===\r\n");
                _customTileControlActive = true;
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === CUSTOM TILE CONTROL ACTIVE ===\r\n");

                // Each tile gets its own control bound to its own DataContext; no manual visual-tree refresh needed
                var control = new ControllerCompatibilityItemControl(this);
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === CREATED CONTROLLERCONTROLCOMPATIBILITYITEMCONTROL ===\r\n");
                return control;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to create ControllerCompatibilityItemControl");
                return null;
            }
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === ON APPLICATION STARTED CALLED ===\r\n");

            // Start controller monitoring
            try
            {
                _controllerService.StartMonitoring();
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === CONTROLLER MONITORING STARTED ===\r\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === CONTROLLER MONITORING FAILED: {ex.Message} ===\r\n");
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
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === ADD CUSTOM ELEMENT SUPPORT CALLED - INSTANCE {_instanceId} ===\r\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === ADD CUSTOM ELEMENT SUPPORT FAILED: {ex.Message} - INSTANCE {_instanceId} ===\r\n");
            }

            // Delayed refresh timers removed for performance

            if (!_customTileControlActive)
            {
                EnsureLayoutRefreshHook();
                RefreshVisibleTileOverlays();
            }
        }

        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {
            System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === ON GAME SELECTED CALLED - Type: {args.NewValue?.GetType().Name ?? "null"} ===\r\n");
            // Selection changes can fire rapidly while scrolling; avoid using this noisy signal
            // to drive fallback overlay updates.
            return;
        }

        private void EnsureLayoutRefreshHook()
        {
            if (_layoutRefreshHooked)
            {
                return;
            }

            var dispatcher = Application.Current?.Dispatcher;
            var mainWindow = Application.Current?.MainWindow;
            if (dispatcher == null || mainWindow == null)
            {
                return;
            }

            _layoutRefreshDebounceTimer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };

            _layoutRefreshDebounceTimer.Tick += (s, e) =>
            {
                _layoutRefreshDebounceTimer.Stop();
                if (!_customTileControlActive)
                {
                    RefreshVisibleTileOverlays();
                }
            };

            mainWindow.LayoutUpdated += (s, e) =>
            {
                if (_customTileControlActive)
                {
                    return;
                }

                _layoutRefreshDebounceTimer.Stop();
                _layoutRefreshDebounceTimer.Start();
            };

            _layoutRefreshHooked = true;
            System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === LAYOUT REFRESH HOOK ENABLED ===\r\n");
        }

        internal string GetGameCompatibility(Playnite.SDK.Models.Game game)
        {
            // Check if we have a manual override first (but ignore "Unknown" overrides)
            if (_gameCompatibilityOverrides.ContainsKey(game.Id) && _gameCompatibilityOverrides[game.Id] != "Unknown")
            {
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === MANUAL OVERRIDE FOUND FOR '{game.Name}': {_gameCompatibilityOverrides[game.Id]} ===\r\n");
                return _gameCompatibilityOverrides[game.Id];
            }

            // If override is "Unknown" or no override exists, check database only.
            // Auto-detection must be explicitly requested from the menu action, never from UI refresh/binding.
            if (_compatibilityDatabase != null)
            {
                var compatibilityInfo = _compatibilityDatabase.GetCompatibilityInfo(game);
                if (compatibilityInfo.SupportLevel != ControllerSupportLevel.Unknown)
                {
                    System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === DATABASE INFO FOUND FOR '{game.Name}': {compatibilityInfo.SupportLevel} ===\r\n");
                    return compatibilityInfo.SupportLevel.ToString();
                }
            }
            else
            {
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === COMPATIBILITY DATABASE IS NULL ===\r\n");
            }

            // Fallback: return Unknown if detection fails
            System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === FALLBACK TO UNKNOWN FOR '{game.Name}' ===\r\n");
            return "Unknown";
        }

        internal ControllerSupportLevel GetCompatibilityLevel(Playnite.SDK.Models.Game game)
        {
            var compatibility = GetGameCompatibility(game);
            return Enum.TryParse(compatibility, true, out ControllerSupportLevel level) ? level : ControllerSupportLevel.Unknown;
        }

        private void SetManualCompatibility(System.Collections.Generic.IEnumerable<Playnite.SDK.Models.Game> games, string compatibility)
        {
            System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === SET MANUAL COMPATIBILITY CALLED: {compatibility} ===\r\n");
            foreach (var game in games)
            {
                // UpdateGameCompatibility persists the override and notifies bound tile controls
                UpdateGameCompatibility(game, compatibility);
                System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === SET MANUAL COMPATIBILITY: {game.Name} -> {compatibility} ===\r\n");
            }

            if (!_customTileControlActive)
            {
                RefreshVisibleTileOverlays();
            }
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
                        System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === AUTO-DETECTED COMPATIBILITY: {game.Name} -> {compatibilityInfo.SupportLevel} ===\r\n");
                        CompatibilityChanged?.Invoke(game.Id);
                    }
                }
                catch (Exception ex)
                {
                    System.IO.File.AppendAllText(_logPath, $"{DateTime.Now}: === AUTO-DETECTION FAILED FOR '{game.Name}': {ex.Message} ===\r\n");
                }
            }

            if (!_customTileControlActive)
            {
                RefreshVisibleTileOverlays();
            }
        }

        private void UpdateGameCompatibility(Playnite.SDK.Models.Game game, string compatibility)
        {
            _gameCompatibilityOverrides[game.Id] = compatibility;
            SaveOverrides();
            CompatibilityChanged?.Invoke(game.Id);
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

        private void RefreshVisibleTileOverlays(HashSet<Guid> targetGameIds = null, bool clearUnresolved = true)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                return;
            }

            var refreshVersion = ++_overlayRefreshVersion;

            dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (refreshVersion != _overlayRefreshVersion)
                    {
                        return;
                    }

                    var mainWindow = Application.Current?.MainWindow;
                    if (mainWindow == null)
                    {
                        return;
                    }

                    // Drop hooks for containers that have been garbage collected.
                    foreach (var deadKey in _hookedTiles.Where(kvp => !kvp.Value.TryGetTarget(out _)).Select(kvp => kvp.Key).ToList())
                    {
                        _hookedTiles.Remove(deadKey);
                    }

                    var tiles = FindVisualChildren<ListBoxItem>(mainWindow).ToList();

                    foreach (var tile in tiles)
                    {
                        if (refreshVersion != _overlayRefreshVersion)
                        {
                            return;
                        }

                        var tileKey = RuntimeHelpers.GetHashCode(tile);
                        if (!_hookedTiles.ContainsKey(tileKey))
                        {
                            _hookedTiles[tileKey] = new WeakReference<ListBoxItem>(tile);
                            // Fires synchronously whenever WPF reassigns this container's DataContext
                            // (including virtualization recycling), so the overlay is never out of sync.
                            TileDataContextDescriptor.AddValueChanged(tile, OnTileDataContextChanged);
                        }

                        if (targetGameIds == null)
                        {
                            UpdateTileOverlayFromCurrentDataContext(tile, clearUnresolved);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to refresh visible tile overlays");
                }
            }), DispatcherPriority.Background);
        }

        private void OnTileDataContextChanged(object sender, EventArgs e)
        {
            if (sender is ListBoxItem tile)
            {
                UpdateTileOverlayFromCurrentDataContext(tile, clearUnresolved: true);
            }
        }

        private void UpdateTileOverlayFromCurrentDataContext(ListBoxItem tile, bool clearUnresolved)
        {
            var game = ExtractGameFromDataContext(tile.DataContext);
            if (game == null)
            {
                if (clearUnresolved)
                {
                    ClearOverlayFromTile(tile);
                }
                return;
            }

            // This is read-only and never auto-detects/writes.
            var supportLevel = GetCompatibilityLevel(game);
            var hasController = _controllerService?.GetConnectedControllers().Any() == true;
            ApplyOverlayToTile(tile, supportLevel, hasController);
        }

        private static Game ExtractGameFromDataContext(object dataContext)
        {
            if (dataContext is Game game)
            {
                return game;
            }

            if (dataContext == null)
            {
                return null;
            }

            var dataContextType = dataContext.GetType();

            // Only accept known item wrappers. Other view-models may expose a Game property that
            // is unrelated to this tile and causes random overlay remapping while scrolling.
            if (!string.Equals(dataContextType.Name, "GamesCollectionViewEntry", StringComparison.Ordinal))
            {
                return null;
            }

            var gameProperty = dataContextType.GetProperty("Game");
            if (gameProperty != null && typeof(Game).IsAssignableFrom(gameProperty.PropertyType))
            {
                return gameProperty.GetValue(dataContext) as Game;
            }

            return null;
        }

        private static bool ShouldShowOverlay(ControllerSupportLevel supportLevel, bool hasController)
        {
            if (supportLevel == ControllerSupportLevel.Full ||
                supportLevel == ControllerSupportLevel.Partial ||
                supportLevel == ControllerSupportLevel.Community)
            {
                return true;
            }

            return hasController &&
                (supportLevel == ControllerSupportLevel.None || supportLevel == ControllerSupportLevel.Unknown);
        }

        private static void GetOverlayStyle(ControllerSupportLevel supportLevel, out Brush background, out string text, out string tooltip)
        {
            switch (supportLevel)
            {
                case ControllerSupportLevel.Full:
                    background = Brushes.Green;
                    text = "F";
                    tooltip = "Full Controller Support";
                    return;
                case ControllerSupportLevel.Partial:
                    background = Brushes.Orange;
                    text = "P";
                    tooltip = "Partial Controller Support";
                    return;
                case ControllerSupportLevel.Community:
                    background = Brushes.CornflowerBlue;
                    text = "C";
                    tooltip = "Community Controller Configs";
                    return;
                case ControllerSupportLevel.None:
                    background = Brushes.Red;
                    text = "N";
                    tooltip = "No Controller Support";
                    return;
                default:
                    background = Brushes.Gray;
                    text = "?";
                    tooltip = "Unknown Controller Support";
                    return;
            }
        }

        private void ApplyOverlayToTile(ListBoxItem tile, ControllerSupportLevel supportLevel, bool hasController)
        {
            var shouldShow = ShouldShowOverlay(supportLevel, hasController);
            GetOverlayStyle(supportLevel, out var background, out var text, out var tooltip);

            ClearOverlayFromTile(tile);

            // In many Playnite themes, ListBoxItem.Content is data (not a UIElement), so attach to
            // the first rendered panel inside the item's visual tree instead of wrapping Content.
            var hostPanel = FindVisualChildren<Grid>(tile).FirstOrDefault() as Panel
                ?? FindVisualChildren<Panel>(tile).FirstOrDefault();
            if (hostPanel == null)
            {
                return;
            }

            var overlay = new Border
            {
                Tag = "ControllerCompatibilityOverlay",
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(3),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 2, 0),
                Opacity = 0.9,
                Child = new TextBlock
                {
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                }
            };
            hostPanel.Children.Add(overlay);

            var iconText = overlay.Child as TextBlock;
            if (iconText == null)
            {
                iconText = new TextBlock
                {
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                };
                overlay.Child = iconText;
            }

            iconText.Text = "🎮";
            iconText.Foreground = Brushes.White;

            overlay.Background = background;
            overlay.ToolTip = tooltip;
            overlay.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void ClearOverlayFromTile(ListBoxItem tile)
        {
            foreach (var panel in FindVisualChildren<Panel>(tile))
            {
                var overlays = panel.Children
                    .OfType<Border>()
                    .Where(b => Equals(b.Tag, "ControllerCompatibilityOverlay"))
                    .ToList();

                foreach (var overlay in overlays)
                {
                    panel.Children.Remove(overlay);
                }
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
            {
                yield break;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                {
                    yield return match;
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
