using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Playnite.SDK.Models;

namespace ControllerCompatibility
{
    public class GameCompatibilityDialog : Window
    {
        private Game game;
        private GameCompatibilityInfo compatibilityInfo;
        private ControllerDetectionService controllerService;

        public GameCompatibilityDialog(Game game, GameCompatibilityInfo compatibility, ControllerDetectionService controllerService)
        {
            this.game = game;
            this.compatibilityInfo = compatibility;
            this.controllerService = controllerService;

            InitializeDialog();
        }

        private void InitializeDialog()
        {
            Title = $"Controller Compatibility - {game.Name}";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResize;

            var mainPanel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            // Game title
            var titleBlock = new TextBlock
            {
                Text = game.Name,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15)
            };

            // Compatibility status with icon
            var statusPanel = CreateCompatibilityStatusPanel();

            // Connected controllers section
            var controllersSection = CreateConnectedControllersSection();

            // Compatibility details
            var detailsSection = CreateCompatibilityDetailsSection();

            // Action buttons
            var buttonPanel = CreateButtonPanel();

            mainPanel.Children.Add(titleBlock);
            mainPanel.Children.Add(statusPanel);
            mainPanel.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });
            mainPanel.Children.Add(controllersSection);
            mainPanel.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });
            mainPanel.Children.Add(detailsSection);
            mainPanel.Children.Add(buttonPanel);

            Content = new ScrollViewer { Content = mainPanel };
        }

        private StackPanel CreateCompatibilityStatusPanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Status icon
            var statusIcon = new TextBlock
            {
                FontSize = 24,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            // Status text
            var statusText = new StackPanel();

            var levelText = new TextBlock
            {
                FontSize = 16,
                FontWeight = FontWeights.SemiBold
            };

            var descriptionText = new TextBlock
            {
                FontSize = 12,
                Opacity = 0.8,
                TextWrapping = TextWrapping.Wrap
            };

            switch (compatibilityInfo.SupportLevel)
            {
                case ControllerSupportLevel.Full:
                    statusIcon.Text = "C";
                    statusIcon.Foreground = Brushes.Green;
                    levelText.Text = "Full Controller Support";
                    levelText.Foreground = Brushes.Green;
                    descriptionText.Text = "This game has excellent native controller support and should work perfectly with your gamepad.";
                    break;

                case ControllerSupportLevel.Partial:
                    statusIcon.Text = "P";
                    statusIcon.Foreground = Brushes.Orange;
                    levelText.Text = "Partial Controller Support";
                    levelText.Foreground = Brushes.Orange;
                    descriptionText.Text = "This game supports controllers but may have some limitations or require additional setup.";
                    break;

                case ControllerSupportLevel.Community:
                    statusIcon.Text = "C";
                    statusIcon.Foreground = Brushes.CornflowerBlue;
                    levelText.Text = "Community Configurations Available";
                    levelText.Foreground = Brushes.CornflowerBlue;
                    descriptionText.Text = "Community-created controller configurations are available for this game.";
                    break;

                case ControllerSupportLevel.None:
                    statusIcon.Text = "N";
                    statusIcon.Foreground = Brushes.Red;
                    levelText.Text = "No Controller Support";
                    levelText.Foreground = Brushes.Red;
                    descriptionText.Text = "This game requires keyboard and mouse input. Controllers are not supported.";
                    break;

                default:
                    statusIcon.Text = "?";
                    statusIcon.Foreground = Brushes.Gray;
                    levelText.Text = "Unknown Compatibility";
                    levelText.Foreground = Brushes.Gray;
                    descriptionText.Text = "Controller compatibility for this game has not been determined yet.";
                    break;
            }

            statusText.Children.Add(levelText);
            statusText.Children.Add(descriptionText);

            panel.Children.Add(statusIcon);
            panel.Children.Add(statusText);

            return panel;
        }

        private StackPanel CreateConnectedControllersSection()
        {
            var section = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 15)
            };

            var headerText = new TextBlock
            {
                Text = "Connected Controllers",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };

            section.Children.Add(headerText);

            if (controllerService != null)
            {
                var controllers = controllerService.GetConnectedControllers();

                if (controllers.Any())
                {
                    foreach (var controller in controllers)
                    {
                        var controllerPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Margin = new Thickness(10, 2, 0, 2)
                        };

                        var controllerIcon = new TextBlock
                        {
                            Text = GetControllerEmoji(controller.Type),
                            FontSize = 16,
                            Margin = new Thickness(0, 0, 8, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        };

                        var controllerName = new TextBlock
                        {
                            Text = controller.Name,
                            VerticalAlignment = VerticalAlignment.Center
                        };

                        controllerPanel.Children.Add(controllerIcon);
                        controllerPanel.Children.Add(controllerName);
                        section.Children.Add(controllerPanel);
                    }
                }
                else
                {
                    var noControllersText = new TextBlock
                    {
                        Text = "No controllers detected",
                        Margin = new Thickness(10, 0, 0, 0),
                        Opacity = 0.6
                    };
                    section.Children.Add(noControllersText);
                }
            }

            return section;
        }

        private StackPanel CreateCompatibilityDetailsSection()
        {
            var section = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 20)
            };

            var headerText = new TextBlock
            {
                Text = "Compatibility Details",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };

            section.Children.Add(headerText);

            // Source information
            var sourceText = new TextBlock
            {
                Text = $"Source: {compatibilityInfo.Source}",
                Margin = new Thickness(10, 2, 0, 2),
                FontSize = 11,
                Opacity = 0.7
            };

            // Last updated
            var updatedText = new TextBlock
            {
                Text = $"Last Updated: {compatibilityInfo.LastUpdated:MMM dd, yyyy}",
                Margin = new Thickness(10, 2, 0, 2),
                FontSize = 11,
                Opacity = 0.7
            };

            // Notes (if any)
            if (!string.IsNullOrEmpty(compatibilityInfo.Notes))
            {
                var notesHeader = new TextBlock
                {
                    Text = "Notes:",
                    Margin = new Thickness(10, 8, 0, 2),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12
                };

                var notesText = new TextBlock
                {
                    Text = compatibilityInfo.Notes,
                    Margin = new Thickness(10, 2, 0, 2),
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 11
                };

                section.Children.Add(notesHeader);
                section.Children.Add(notesText);
            }

            section.Children.Add(sourceText);
            section.Children.Add(updatedText);

            return section;
        }

        private StackPanel CreateButtonPanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var closeButton = new Button
            {
                Content = "Close",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5, 0, 0, 0)
            };

            closeButton.Click += (s, e) => Close();

            // Remove community config button for now
            panel.Children.Add(closeButton);

            return panel;
        }

        private string GetControllerEmoji(ControllerType type)
        {
            return type switch
            {
                ControllerType.Xbox => "X",
                ControllerType.PlayStation => "P",
                ControllerType.Nintendo => "N",
                ControllerType.Steam => "S",
                _ => "C"
            };
        }
    }
}