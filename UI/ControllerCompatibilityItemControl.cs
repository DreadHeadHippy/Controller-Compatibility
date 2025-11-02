using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace ControllerCompatibility
{
    public class ControllerCompatibilityItemControl : System.Windows.Controls.ContentControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private CompatibilityDatabase compatibilityDb;
        private ControllerDetectionService controllerService;

        public ControllerCompatibilityItemControl()
        {
            System.Diagnostics.Debug.WriteLine("=== CONTROLLER COMPATIBILITY ITEM CONTROL CREATED ===");
            Console.WriteLine("=== CONTROLLER COMPATIBILITY ITEM CONTROL CREATED ===");

            compatibilityDb = new CompatibilityDatabase();
            controllerService = new ControllerDetectionService();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Create the controller compatibility overlay (Steam-style badge on game tile)
            var overlayPanel = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Background overlay for compatibility badge (positioned like Steam)
            var backgroundOverlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)), // Semi-transparent black
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(8),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Padding = new Thickness(8, 4, 8, 4),
                MaxWidth = 180 // Prevent overly wide badges
            };

            var badgePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Controller icon
            var controllerIcon = new TextBlock
            {
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };

            // Compatibility text
            var compatibilityText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            badgePanel.Children.Add(controllerIcon);
            badgePanel.Children.Add(compatibilityText);
            backgroundOverlay.Child = badgePanel;

            overlayPanel.Children.Add(backgroundOverlay);

            Content = overlayPanel;

            // Update when DataContext changes (when bound to different games)
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"=== DATACONTEXT CHANGED: Old={e.OldValue?.GetType().Name}, New={e.NewValue?.GetType().Name} ===");
            Console.WriteLine($"=== DATACONTEXT CHANGED: Old={e.OldValue?.GetType().Name}, New={e.NewValue?.GetType().Name} ===");

            if (e.NewValue is Game game)
            {
                System.Diagnostics.Debug.WriteLine($"=== BINDING TO GAME: {game.Name} ===");
                Console.WriteLine($"=== BINDING TO GAME: {game.Name} ===");
                UpdateCompatibilityOverlay(game);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("=== NOT A GAME OBJECT ===");
                Console.WriteLine("=== NOT A GAME OBJECT ===");
            }
        }

        private void UpdateCompatibilityOverlay(Game game)
        {
            try
            {
                var compatibility = compatibilityDb.GetCompatibilityInfo(game);
                var connectedControllers = controllerService.GetConnectedControllers();

                var overlayPanel = Content as Grid;
                var backgroundOverlay = overlayPanel?.Children[0] as Border;
                var badgePanel = backgroundOverlay?.Child as StackPanel;
                var controllerIcon = badgePanel?.Children[0] as TextBlock;
                var compatibilityText = badgePanel?.Children[1] as TextBlock;

                if (controllerIcon == null || compatibilityText == null || backgroundOverlay == null) return;

                // Set icon, text, and colors based on compatibility level
                var overlayInfo = GetCompatibilityOverlayInfo(compatibility.SupportLevel, connectedControllers.Any());

                controllerIcon.Text = overlayInfo.Icon;
                controllerIcon.Foreground = new SolidColorBrush(overlayInfo.TextColor);

                compatibilityText.Text = overlayInfo.Text;
                compatibilityText.Foreground = new SolidColorBrush(overlayInfo.TextColor);

                backgroundOverlay.Background = new SolidColorBrush(overlayInfo.BackgroundColor);
                backgroundOverlay.ToolTip = overlayInfo.Tooltip;

                // Show/hide overlay based on relevance
                var shouldShow = ShouldShowOverlay(compatibility.SupportLevel, connectedControllers.Any());
                backgroundOverlay.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;

                // Adjust opacity based on whether controllers are connected
                backgroundOverlay.Opacity = connectedControllers.Any() ? 1.0 : 0.7;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error updating compatibility overlay for game: {game?.Name}");
                if (Content is Grid grid && grid.Children.Count > 0)
                {
                    grid.Children[0].Visibility = Visibility.Collapsed;
                }
            }
        }

        private CompatibilityOverlayInfo GetCompatibilityOverlayInfo(ControllerSupportLevel supportLevel, bool hasConnectedController)
        {
            switch (supportLevel)
            {
                case ControllerSupportLevel.Full:
                    return new CompatibilityOverlayInfo
                    {
                        Icon = "F",
                        Text = "Full Controller Support",
                        TextColor = Colors.LimeGreen,
                        BackgroundColor = Color.FromArgb(200, 34, 139, 34), // Dark green
                        Tooltip = "This game has excellent native controller support"
                    };

                case ControllerSupportLevel.Partial:
                    return new CompatibilityOverlayInfo
                    {
                        Icon = "P",
                        Text = "Partial Controller Support",
                        TextColor = Colors.Orange,
                        BackgroundColor = Color.FromArgb(200, 255, 140, 0), // Dark orange
                        Tooltip = "This game supports controllers but may have limitations"
                    };

                case ControllerSupportLevel.Community:
                    return new CompatibilityOverlayInfo
                    {
                        Icon = "C",
                        Text = "Community Configs",
                        TextColor = Colors.CornflowerBlue,
                        BackgroundColor = Color.FromArgb(200, 70, 130, 180), // Steel blue
                        Tooltip = "Community-created controller configurations available"
                    };

                case ControllerSupportLevel.None:
                    if (hasConnectedController)
                    {
                        return new CompatibilityOverlayInfo
                        {
                            Icon = "N",
                            Text = "No Controller Support",
                            TextColor = Colors.Red,
                            BackgroundColor = Color.FromArgb(200, 220, 20, 60), // Crimson
                            Tooltip = "This game requires keyboard and mouse - controllers not supported"
                        };
                    }
                    break;

                case ControllerSupportLevel.Unknown:
                default:
                    if (hasConnectedController)
                    {
                        return new CompatibilityOverlayInfo
                        {
                            Icon = "?",
                            Text = "Unknown Compatibility",
                            TextColor = Colors.Gray,
                            BackgroundColor = Color.FromArgb(180, 64, 64, 64), // Dark gray
                            Tooltip = "Controller compatibility for this game has not been determined"
                        };
                    }
                    break;
            }

            // Return empty overlay info (will be hidden)
            return new CompatibilityOverlayInfo
            {
                Icon = "",
                Text = "",
                TextColor = Colors.Transparent,
                BackgroundColor = Colors.Transparent,
                Tooltip = ""
            };
        }

        private bool ShouldShowOverlay(ControllerSupportLevel supportLevel, bool hasConnectedController)
        {
            // Show overlay if:
            // 1. Game has known compatibility status, OR
            // 2. Controllers are connected and game has no support (warning), OR
            // 3. Controllers are connected and compatibility is unknown

            if (supportLevel == ControllerSupportLevel.Full ||
                supportLevel == ControllerSupportLevel.Partial ||
                supportLevel == ControllerSupportLevel.Community)
            {
                return true; // Always show positive compatibility info
            }

            if (hasConnectedController &&
                (supportLevel == ControllerSupportLevel.None ||
                 supportLevel == ControllerSupportLevel.Unknown))
            {
                return true; // Show warnings when controllers are connected
            }

            return false; // Hide overlay for unsupported games when no controllers connected
        }
    }

    public class CompatibilityOverlayInfo
    {
        public string Icon { get; set; }
        public string Text { get; set; }
        public Color TextColor { get; set; }
        public Color BackgroundColor { get; set; }
        public string Tooltip { get; set; }
    }
}