using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Playnite.SDK;

namespace ControllerCompatibility
{
    public partial class CompatibilityStatsWindow : Window
    {
        private CompatibilityDatabase compatibilityDb;
        private ControllerDetectionService controllerService;
        private static readonly ILogger logger = LogManager.GetLogger();

        public CompatibilityStatsWindow(CompatibilityDatabase db, ControllerDetectionService controller)
        {
            InitializeComponent();
            compatibilityDb = db;
            controllerService = controller;
            Loaded += (s, e) =>
            {
                try
                {
                    RefreshStats();
                }
                catch (Exception ex)
                {
                    logger.Error($"Error in refresh stats: {ex.Message}");
                    logger.Error($"Stack trace: {ex.StackTrace}");
                    // Show error message to user
                    MessageBox.Show($"Error loading compatibility stats: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }

        private void RefreshStats()
        {
            try
            {
                logger.Info("Refresh stats start");

                if (compatibilityDb == null)
                {
                    logger.Error("Compatibility DB is null");
                    throw new Exception("Compatibility database is null");
                }

                if (controllerService == null)
                {
                    logger.Error("Controller service is null");
                    throw new Exception("Controller service is null");
                }

                var allGames = compatibilityDb.GetAllCompatibilityInfo();
                logger.Info($"Got all games: {allGames.Count}");
                var connectedControllers = controllerService.GetConnectedControllers();
                logger.Info($"Got controllers: {connectedControllers.Count}");

                // Calculate statistics
                var stats = new Dictionary<ControllerSupportLevel, int>();
                foreach (ControllerSupportLevel level in Enum.GetValues(typeof(ControllerSupportLevel)))
                {
                    stats[level] = allGames.Count(g => g.SupportLevel == level);
                }
                logger.Info("Calculated stats");

                // Update pie chart
                DrawPieChart(stats);

                // Update statistics panel
                UpdateStatsPanel(stats, allGames.Count);

                // Update controllers panel
                UpdateControllersPanel(connectedControllers);
                logger.Info("Refresh stats complete");
            }
            catch (Exception ex)
            {
                logger.Error($"Error in refresh stats: {ex.Message}");
                logger.Error($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to be caught by the Loaded handler
            }
        }

        private void DrawPieChart(Dictionary<ControllerSupportLevel, int> stats)
        {
            PieChartCanvas.Children.Clear();

            var total = stats.Values.Sum();
            if (total == 0) return;

            var centerX = PieChartCanvas.ActualWidth / 2;
            var centerY = PieChartCanvas.ActualHeight / 2;
            var radius = Math.Min(centerX, centerY) - 20;

            double startAngle = 0;

            foreach (var kvp in stats.Where(s => s.Value > 0))
            {
                var sweepAngle = (kvp.Value / (double)total) * 360;

                // Create pie slice
                var path = new Path
                {
                    Fill = GetCompatibilityColor(kvp.Key),
                    Stroke = Brushes.White,
                    StrokeThickness = 2
                };

                var pathGeometry = new PathGeometry();
                var pathFigure = new PathFigure
                {
                    StartPoint = new Point(centerX, centerY)
                };

                // Calculate arc points
                var startPoint = GetPointOnCircle(centerX, centerY, radius, startAngle);
                var endPoint = GetPointOnCircle(centerX, centerY, radius, startAngle + sweepAngle);

                // Create arc segment
                var isLargeArc = sweepAngle > 180;
                var arcSegment = new ArcSegment
                {
                    Point = endPoint,
                    Size = new Size(radius, radius),
                    SweepDirection = SweepDirection.Clockwise,
                    IsLargeArc = isLargeArc
                };

                pathFigure.Segments.Add(new LineSegment(startPoint, true));
                pathFigure.Segments.Add(arcSegment);
                pathFigure.Segments.Add(new LineSegment(new Point(centerX, centerY), true));

                pathGeometry.Figures.Add(pathFigure);
                path.Data = pathGeometry;

                PieChartCanvas.Children.Add(path);

                // Add percentage label
                var labelAngle = startAngle + sweepAngle / 2;
                var labelPoint = GetPointOnCircle(centerX, centerY, radius * 0.7, labelAngle);
                var percentage = (kvp.Value / (double)total * 100).ToString("0.#") + "%";

                var label = new TextBlock
                {
                    Text = percentage,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Canvas.SetLeft(label, labelPoint.X - 15);
                Canvas.SetTop(label, labelPoint.Y - 10);
                PieChartCanvas.Children.Add(label);

                startAngle += sweepAngle;
            }

            // Add legend
            AddLegend(stats);
        }

        private void AddLegend(Dictionary<ControllerSupportLevel, int> stats)
        {
            var legendPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(10)
            };

            foreach (var kvp in stats.Where(s => s.Value > 0))
            {
                var legendItem = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 2, 0, 2)
                };

                var colorRect = new Rectangle
                {
                    Width = 16,
                    Height = 16,
                    Fill = GetCompatibilityColor(kvp.Key),
                    Stroke = Brushes.White,
                    StrokeThickness = 1,
                    Margin = new Thickness(0, 0, 8, 0)
                };

                var label = new TextBlock
                {
                    Text = $"{GetCompatibilityName(kvp.Key)}: {kvp.Value}",
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 11
                };

                legendItem.Children.Add(colorRect);
                legendItem.Children.Add(label);
                legendPanel.Children.Add(legendItem);
            }

            Canvas.SetLeft(legendPanel, 10);
            Canvas.SetTop(legendPanel, 10);
            PieChartCanvas.Children.Add(legendPanel);
        }

        private void UpdateStatsPanel(Dictionary<ControllerSupportLevel, int> stats, int totalGames)
        {
            StatsPanel.Children.Clear();

            var totalText = new TextBlock
            {
                Text = $"Total Games: {totalGames}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            StatsPanel.Children.Add(totalText);

            foreach (var kvp in stats.Where(s => s.Value > 0))
            {
                var percentage = totalGames > 0 ? (kvp.Value / (double)totalGames * 100).ToString("0.#") : "0";
                var statText = new TextBlock
                {
                    Text = $"{GetCompatibilityName(kvp.Key)}: {kvp.Value} ({percentage}%)",
                    Margin = new Thickness(0, 2, 0, 2),
                    FontSize = 12
                };
                StatsPanel.Children.Add(statText);
            }

            // Add controller-ready percentage
            var controllerReady = stats[ControllerSupportLevel.Full] + stats[ControllerSupportLevel.Partial];
            var readyPercentage = totalGames > 0 ? (controllerReady / (double)totalGames * 100).ToString("0.#") : "0";
            var readyText = new TextBlock
            {
                Text = $"Controller Ready: {controllerReady} ({readyPercentage}%)",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Green,
                Margin = new Thickness(0, 10, 0, 0)
            };
            StatsPanel.Children.Add(readyText);
        }

        private void UpdateControllersPanel(List<DetectedController> controllers)
        {
            ControllersPanel.Children.Clear();

            if (controllers.Any())
            {
                foreach (var controller in controllers)
                {
                    var controllerPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 2, 0, 2)
                    };

                    var icon = new TextBlock
                    {
                        Text = GetControllerEmoji(controller.Type),
                        FontSize = 16,
                        Margin = new Thickness(0, 0, 8, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    var name = new TextBlock
                    {
                        Text = controller.Name,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 12
                    };

                    controllerPanel.Children.Add(icon);
                    controllerPanel.Children.Add(name);
                    ControllersPanel.Children.Add(controllerPanel);
                }
            }
            else
            {
                var noControllers = new TextBlock
                {
                    Text = "No controllers detected",
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic
                };
                ControllersPanel.Children.Add(noControllers);
            }
        }

        private Point GetPointOnCircle(double centerX, double centerY, double radius, double angleDegrees)
        {
            var angleRadians = angleDegrees * Math.PI / 180;
            return new Point(
                centerX + radius * Math.Cos(angleRadians - Math.PI / 2),
                centerY + radius * Math.Sin(angleRadians - Math.PI / 2)
            );
        }

        private Brush GetCompatibilityColor(ControllerSupportLevel level)
        {
            return level switch
            {
                ControllerSupportLevel.Full => Brushes.Green,
                ControllerSupportLevel.Partial => Brushes.Orange,
                ControllerSupportLevel.Community => Brushes.CornflowerBlue,
                ControllerSupportLevel.None => Brushes.Red,
                ControllerSupportLevel.Unknown => Brushes.Gray,
                _ => Brushes.Gray
            };
        }

        private string GetCompatibilityName(ControllerSupportLevel level)
        {
            return level switch
            {
                ControllerSupportLevel.Full => "Full Support",
                ControllerSupportLevel.Partial => "Partial Support",
                ControllerSupportLevel.Community => "Community Configs",
                ControllerSupportLevel.None => "No Support",
                ControllerSupportLevel.Unknown => "Unknown",
                _ => "Unknown"
            };
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

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshStats();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}