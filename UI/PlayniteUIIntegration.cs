using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Playnite.SDK;
using Playnite.SDK.Controls;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace ControllerCompatibility
{
    public class ControllerCompatibilityTopPanelItem : TopPanelItem
    {
        private ControllerDetectionService controllerService;
        private CompatibilityDatabase compatibilityDb;

        public ControllerCompatibilityTopPanelItem(ControllerDetectionService controller, CompatibilityDatabase database)
        {
            System.Diagnostics.Debug.WriteLine("=== CONTROLLER COMPATIBILITY TOP PANEL ITEM CREATED ===");
            Console.WriteLine("=== CONTROLLER COMPATIBILITY TOP PANEL ITEM CREATED ===");

            controllerService = controller;
            compatibilityDb = database;

            InitializeComponent();

            // Subscribe to controller changes
            controllerService.ControllerChanged += OnControllerChanged;

            // Initial update
            UpdateControllerStatus();
        }

        private void InitializeComponent()
        {
            Title = "Controller Status";
            // Use absolute path to the icon file in the plugin directory
            string pluginPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Playnite", "Extensions", "ControllerCompatibility", "toppanelicon.png");
            Icon = pluginPath;
            Visible = true;
            Activated = () => ShowControllerStatusDialog();
        }

        private void OnControllerChanged(object sender, ControllerChangedEventArgs e)
        {
            Application.Current?.Dispatcher.Invoke(() => UpdateControllerStatus());
        }

        private void UpdateControllerStatus()
        {
            var controllers = controllerService.GetConnectedControllers();

            if (controllers.Count == 0)
            {
                Title = "No Controllers";
                // Icon = CreateStatusIcon(false);
            }
            else if (controllers.Count == 1)
            {
                var controller = controllers[0];
                Title = GetShortControllerName(controller.Name);
                // Icon = CreateControllerTypeIcon(controller.Type);
            }
            else
            {
                Title = $"{controllers.Count} Controllers";
                // Icon = CreateStatusIcon(true);
            }
        }

        private void ShowControllerStatusDialog()
        {
            // TODO: Show detailed controller status dialog
            // For now, just show a notification
            // var dialog = new ControllerStatusDialog(controllerService);
            // dialog.ShowDialog();
        }

        private string GetShortControllerName(string fullName)
        {
            // Shorten common controller names for top panel display
            if (fullName.ToLowerInvariant().Contains("xbox"))
                return "Xbox Controller";
            if (fullName.ToLowerInvariant().Contains("playstation") || fullName.ToLowerInvariant().Contains("dualshock"))
                return "PS Controller";
            if (fullName.ToLowerInvariant().Contains("nintendo") || fullName.ToLowerInvariant().Contains("pro controller"))
                return "Nintendo Controller";
            if (fullName.ToLowerInvariant().Contains("steam"))
                return "Steam Controller";

            return "Controller";
        }

        private System.Windows.Media.ImageSource CreateControllerTypeIcon(ControllerType type)
        {
            // Create appropriate icon based on controller type
            // In a full implementation, these would be actual controller icons
            return CreateStatusIcon(true);
        }

        private System.Windows.Media.ImageSource CreateStatusIcon(bool connected)
        {
            var drawingVisual = new System.Windows.Media.DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                var brush = connected ?
                    System.Windows.Media.Brushes.LimeGreen :
                    System.Windows.Media.Brushes.Gray;

                context.DrawEllipse(brush, null, new System.Windows.Point(10, 10), 8, 8);
            }

            var renderBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(20, 20, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            renderBitmap.Render(drawingVisual);
            return renderBitmap;
        }
    }

    public class ControllerCompatibilityGameMenuItemProvider
    {
        private CompatibilityDatabase compatibilityDb;
        private ControllerDetectionService controllerService;
        private IPlayniteAPI PlayniteApi;

        public ControllerCompatibilityGameMenuItemProvider(CompatibilityDatabase database, IPlayniteAPI api)
        {
            compatibilityDb = database;
            PlayniteApi = api;
        }

        public ControllerCompatibilityGameMenuItemProvider(CompatibilityDatabase database, ControllerDetectionService controller, IPlayniteAPI api)
        {
            compatibilityDb = database;
            controllerService = controller;
            PlayniteApi = api;
        }

        public IEnumerable<GameMenuItem> GetMenuItems(GetGameMenuItemsArgs args)
        {
            if (args.Games?.Any() != true)
                yield break;

            yield return new GameMenuItem
            {
                Description = "Check Controller Compatibility",
                Action = (gameMenuItemActionArgs) =>
                {
                    var game = gameMenuItemActionArgs.Games.First();
                    ShowCompatibilityDialog(game);
                }
            };

            // Add statistics menu item (not game-specific)
            yield return new GameMenuItem
            {
                Description = "View Compatibility Statistics",
                Action = (gameMenuItemActionArgs) => ShowCompatibilityStatistics()
            };

            if (args.Games.Count() == 1)
            {
                var game = args.Games.First();
                var compatibility = compatibilityDb.GetCompatibilityInfo(game);

                // Dynamic menu items based on compatibility
                switch (compatibility.SupportLevel)
                {
                    case ControllerSupportLevel.None:
                        yield return new GameMenuItem
                        {
                            Description = "N No Controller Support",
                            Action = null // Just informational
                        };
                        break;

                    case ControllerSupportLevel.Unknown:
                        // Removed community config and test support for now
                        break;
                }

                // Always offer manual override
                yield return new GameMenuItem
                {
                    Description = "✏️ Set Compatibility Level",
                    Action = (gameMenuItemActionArgs) => SetCompatibilityLevel(game)
                };
            }
        }

        private void ShowCompatibilityDialog(Game game)
        {
            var dialog = new GameCompatibilityDialog(game, compatibilityDb.GetCompatibilityInfo(game), controllerService);
            dialog.ShowDialog();
        }

        private void ShowCompatibilityStatistics()
        {
            var statsWindow = new CompatibilityStatsWindow(compatibilityDb, controllerService);
            statsWindow.ShowDialog();
        }

        private void SetCompatibilityLevel(Game game)
        {
            var options = new List<GenericItemOption>
            {
                new GenericItemOption("✅ Full Controller Support", ControllerSupportLevel.Full.ToString()),
                new GenericItemOption("C Partial Controller Support", ControllerSupportLevel.Partial.ToString()),
                new GenericItemOption("❌ No Controller Support", ControllerSupportLevel.None.ToString()),
                new GenericItemOption("C Community Configurations", ControllerSupportLevel.Community.ToString()),
                new GenericItemOption("? Unknown", ControllerSupportLevel.Unknown.ToString())
            };

            var selectedOption = PlayniteApi.Dialogs.ChooseItemWithSearch(
                options,
                (search) => options.Where(o => o.Name.Contains(search ?? "")).ToList(),
                "Select controller compatibility level:");

            if (selectedOption != null)
            {
                var level = (ControllerSupportLevel)Enum.Parse(typeof(ControllerSupportLevel), selectedOption.Description);
                compatibilityDb.UpdateGameCompatibility(game, level, CompatibilitySource.User);

                PlayniteApi.Notifications.Add(new NotificationMessage(
                    "compatibility-updated",
                    $"Updated {game.Name} compatibility to: {selectedOption.Name}",
                    NotificationType.Info
                ));
            }
        }
    }

    // Extension to integrate controller indicators into game library views
    public class ControllerCompatibilityViewExtension
    {
        public System.Windows.Controls.Control GetGameViewControl(GetGameViewControlArgs args)
        {
            if (args.Mode == ApplicationMode.Desktop)
            {
                // Add controller compatibility column
                return new ControllerCompatibilityItemControl();
            }

            return null;
        }
    }
}