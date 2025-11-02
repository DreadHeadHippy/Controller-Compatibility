using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Playnite.SDK.Models;

namespace ControllerCompatibility
{
    public class AdvancedCompatibilityDetector
    {
        private Dictionary<string, ControllerSupportLevel> knownEngines;
        private Dictionary<Regex, ControllerSupportLevel> executablePatterns;
        private readonly WeightedDetectionRules detectionRules;

        public AdvancedCompatibilityDetector()
        {
            InitializeKnownEngines();
            InitializeExecutablePatterns();
            detectionRules = new WeightedDetectionRules();
        }

        public ControllerSupportLevel DetectCompatibility(Game game)
        {
            var detectionResults = new List<DetectionResult>();

            // Multiple detection methods with confidence scores
            // Always try file analysis - it's crucial for launcher-installed games
            detectionResults.Add(DetectByGameEngine(game));
            detectionResults.Add(DetectByExecutableAnalysis(game));
            detectionResults.Add(DetectByMetadataAnalysis(game));
            detectionResults.Add(DetectByGenreAndTags(game));
            detectionResults.Add(DetectByReleaseDate(game));
            detectionResults.Add(DetectByPlatform(game));
            detectionResults.Add(DetectByPublisher(game));

            // Combine results using weighted scoring
            return CombineDetectionResults(detectionResults);
        }

        private DetectionResult DetectByGameEngine(Game game)
        {
            var confidence = 0.0;
            var supportLevel = ControllerSupportLevel.Unknown;

            // Check game installation directory for engine indicators
            if (!string.IsNullOrEmpty(game.InstallDirectory) && Directory.Exists(game.InstallDirectory))
            {
                var engineInfo = AnalyzeGameDirectory(game.InstallDirectory);
                if (engineInfo != null)
                {
                    supportLevel = engineInfo.SupportLevel;
                    confidence = engineInfo.Confidence;
                }
            }

            // Fallback: check by game name patterns
            if (confidence < 0.5)
            {
                foreach (var engine in knownEngines)
                {
                    if (game.Name?.ToLowerInvariant().Contains(engine.Key.ToLowerInvariant()) == true)
                    {
                        supportLevel = engine.Value;
                        confidence = 0.3; // Lower confidence for name-based detection
                        break;
                    }
                }
            }

            // Debug logging
            System.IO.File.AppendAllText(@"C:\Temp\detection_debug.txt",
                $"{DateTime.Now}: Game '{game.Name}' - Engine: {supportLevel} (conf: {confidence}) - Dir: '{game.InstallDirectory}'\r\n");

            return new DetectionResult(supportLevel, confidence, "Game Engine Analysis");
        }

        private DetectionResult DetectByExecutableAnalysis(Game game)
        {
            var confidence = 0.0;
            var supportLevel = ControllerSupportLevel.Unknown;

            try
            {
                // Analyze game executable for controller-related imports/dependencies
                var gameActions = game.GameActions?.Where(a => a.Type == GameActionType.File);
                if (gameActions?.Any() == true)
                {
                    foreach (var action in gameActions)
                    {
                        var exePath = action.Path;
                        if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                        {
                            var analysis = AnalyzeExecutable(exePath);
                            if (analysis.Confidence > confidence)
                            {
                                supportLevel = analysis.SupportLevel;
                                confidence = analysis.Confidence;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore errors in executable analysis
            }

            return new DetectionResult(supportLevel, confidence, "Executable Analysis");
        }

        private DetectionResult DetectByMetadataAnalysis(Game game)
        {
            var analyzer = new MetadataAnalyzer();
            return analyzer.AnalyzeGameMetadata(game);
        }

        private DetectionResult DetectByGenreAndTags(Game game)
        {
            var confidence = 0.0;
            var supportLevel = ControllerSupportLevel.Partial; // Default to partial

            var genres = game.Genres?.Select(g => g.Name.ToLowerInvariant()) ?? new List<string>();
            var tags = game.Tags?.Select(t => t.Name.ToLowerInvariant()) ?? new List<string>();
            var features = game.Features?.Select(f => f.Name.ToLowerInvariant()) ?? new List<string>();

            // Apply weighted genre analysis
            var genreScore = CalculateGenreScore(genres);
            var tagScore = CalculateTagScore(tags);
            var featureScore = CalculateFeatureScore(features);

            var combinedScore = (genreScore * 0.4 + tagScore * 0.3 + featureScore * 0.3);

            if (combinedScore > 0.5)
            {
                supportLevel = ControllerSupportLevel.Full;
                confidence = Math.Min(0.8, combinedScore);
            }
            else if (combinedScore > 0.1)
            {
                supportLevel = ControllerSupportLevel.Partial;
                confidence = Math.Max(0.4, Math.Abs(combinedScore));
            }
            else if (combinedScore > -0.2)
            {
                supportLevel = ControllerSupportLevel.Partial;
                confidence = 0.3; // Low confidence default
            }
            else if (combinedScore < -0.2)
            {
                supportLevel = ControllerSupportLevel.None;
                confidence = Math.Min(0.6, Math.Abs(combinedScore));
            }

            return new DetectionResult(supportLevel, confidence, "Genre & Tags Analysis");
        }        private DetectionResult DetectByReleaseDate(Game game)
        {
            var confidence = 0.4; // Higher confidence for date-based detection
            var supportLevel = ControllerSupportLevel.Partial; // Default to partial support

            if (game.ReleaseDate.HasValue)
            {
                var releaseYear = game.ReleaseDate.Value.Year;

                // Modern games (2010+) are very likely to have controller support
                if (releaseYear >= 2018)
                {
                    supportLevel = ControllerSupportLevel.Full;
                    confidence = 0.7;
                }
                else if (releaseYear >= 2010)
                {
                    supportLevel = ControllerSupportLevel.Partial;
                    confidence = 0.6;
                }
                else if (releaseYear >= 2000 && releaseYear < 2010)
                {
                    supportLevel = ControllerSupportLevel.Partial;
                    confidence = 0.4;
                }
                else if (releaseYear < 2000)
                {
                    // Very old games less likely to have native controller support
                    supportLevel = ControllerSupportLevel.None;
                    confidence = 0.5;
                }
            }

            return new DetectionResult(supportLevel, confidence, "Release Date Analysis");
        }        private DetectionResult DetectByPlatform(Game game)
        {
            var confidence = 0.5; // Higher base confidence
            var supportLevel = ControllerSupportLevel.Partial; // Default PC games to partial support

            var platforms = game.Platforms?.Select(p => p.Name.ToLowerInvariant()) ?? new List<string>();

            // Console ports are more likely to have controller support
            var consolePlatforms = new[] { "xbox", "playstation", "nintendo", "switch", "ps3", "ps4", "ps5" };

            if (platforms.Any(p => consolePlatforms.Any(c => p.Contains(c))))
            {
                supportLevel = ControllerSupportLevel.Full;
                confidence = 0.8;
            }
            else if (platforms.Any(p => p.Contains("pc") || p.Contains("windows") || p.Contains("linux") || p.Contains("mac")))
            {
                // PC games - most modern PC games support controllers
                supportLevel = ControllerSupportLevel.Partial;
                confidence = 0.6;
            }

            return new DetectionResult(supportLevel, confidence, "Platform Analysis");
        }

        private DetectionResult DetectByPublisher(Game game)
        {
            var confidence = 0.0;
            var supportLevel = ControllerSupportLevel.Unknown;

            var publisherName = game.Publishers?.FirstOrDefault()?.Name?.ToLowerInvariant();
            
            if (!string.IsNullOrEmpty(publisherName))
            {
                // Publishers known for good controller support
                var controllerFriendlyPublishers = new[]
                {
                    "microsoft", "sony", "nintendo", "valve", "ubisoft", "activision", 
                    "electronic arts", "ea", "square enix", "capcom", "bandai namco"
                };

                if (controllerFriendlyPublishers.Any(p => publisherName.Contains(p)))
                {
                    supportLevel = ControllerSupportLevel.Partial;
                    confidence = 0.3;
                }
            }

            return new DetectionResult(supportLevel, confidence, "Publisher Analysis");
        }

        private ControllerSupportLevel CombineDetectionResults(List<DetectionResult> results)
        {
            if (!results.Any())
                return ControllerSupportLevel.Partial; // Default to partial support

            // Weight results by confidence and combine
            var weightedScores = new Dictionary<ControllerSupportLevel, double>();

            foreach (var result in results.Where(r => r.Confidence > 0))
            {
                if (!weightedScores.ContainsKey(result.SupportLevel))
                    weightedScores[result.SupportLevel] = 0;

                weightedScores[result.SupportLevel] += result.Confidence;
            }

            if (!weightedScores.Any())
                return ControllerSupportLevel.Partial; // Default to partial support

            // Return the support level with highest weighted score
            var bestResult = weightedScores.OrderByDescending(kvp => kvp.Value).First();

            // If file analysis provided any signal, boost confidence
            var hasFileAnalysis = results.Any(r => r.Method.Contains("Engine") || r.Method.Contains("Executable"));
            if (hasFileAnalysis && bestResult.Value >= 0.3)
            {
                // File analysis present - be more confident
                return bestResult.Key;
            }
            else if (bestResult.Value < 0.4)
            {
                // Low confidence overall - default to partial for modern games
                return ControllerSupportLevel.Partial;
            }

            return bestResult.Key;
        }

        private double CalculateGenreScore(IEnumerable<string> genres)
        {
            var genreWeights = new Dictionary<string, double>
            {
                // Controller-friendly genres (positive scores)
                {"action", 0.8}, {"adventure", 0.6}, {"racing", 0.9}, {"sports", 0.9},
                {"fighting", 0.9}, {"platformer", 0.8}, {"shooter", 0.7}, {"rpg", 0.6},
                {"arcade", 0.8}, {"simulation", 0.4},

                // Mixed genres (neutral)
                {"puzzle", 0.0}, {"indie", 0.0},

                // Keyboard/mouse preferred genres (negative scores)
                {"strategy", -0.7}, {"real-time strategy", -0.8}, {"rts", -0.8},
                {"turn-based strategy", -0.6}, {"point & click", -0.8}, {"visual novel", -0.5},
                {"management", -0.6}, {"city builder", -0.7}, {"mmorpg", -0.4}
            };

            return genres.Sum(genre => genreWeights.TryGetValue(genre, out var weight) ? weight : 0.0);
        }

        private double CalculateTagScore(IEnumerable<string> tags)
        {
            var tagWeights = new Dictionary<string, double>
            {
                // Positive indicators
                {"controller", 0.9}, {"gamepad", 0.9}, {"xbox controller", 0.9},
                {"playstation controller", 0.9}, {"full controller support", 1.0},
                {"partial controller support", 0.6}, {"co-op", 0.4}, {"local co-op", 0.5},
                {"split screen", 0.6}, {"multiplayer", 0.3},

                // Negative indicators  
                {"keyboard only", -1.0}, {"mouse only", -1.0}, {"point and click", -0.8},
                {"text heavy", -0.4}, {"menu heavy", -0.3}, {"complex ui", -0.4}
            };

            return tags.Sum(tag => tagWeights.TryGetValue(tag, out var weight) ? weight : 0.0);
        }

        private double CalculateFeatureScore(IEnumerable<string> features)
        {
            var featureWeights = new Dictionary<string, double>
            {
                {"full controller support", 1.0}, {"partial controller support", 0.6},
                {"steam controller support", 0.8}, {"xbox controller support", 0.9},
                {"playstation controller support", 0.9}, {"remote play", 0.5},
                {"local co-op", 0.6}, {"shared/split screen", 0.7}
            };

            return features.Sum(feature => featureWeights.TryGetValue(feature, out var weight) ? weight : 0.0);
        }

        private DirectoryAnalysisResult AnalyzeGameDirectory(string installPath)
        {
            try
            {
                var files = Directory.GetFiles(installPath, "*", SearchOption.AllDirectories);
                var fileNames = files.Select(f => Path.GetFileName(f).ToLowerInvariant()).ToList();

                // Look for engine-specific files (expanded for more launcher games)
                var engineIndicators = new Dictionary<string[], ControllerSupportLevel>
                {
                    {new[] {"ue4game.exe", "engine.ini", "unrealengine"}, ControllerSupportLevel.Full}, // Unreal Engine 4+
                    {new[] {"unityplayer.dll", "unityengine", "mono.dll"}, ControllerSupportLevel.Full}, // Unity
                    {new[] {"sourceengine", "hl2.exe", "engine.dll"}, ControllerSupportLevel.Partial}, // Source Engine
                    {new[] {"d3d11.dll", "xinput1_3.dll", "dinput8.dll"}, ControllerSupportLevel.Partial}, // DirectX games
                    {new[] {"sdl2.dll", "sdl.dll"}, ControllerSupportLevel.Full}, // SDL (usually good controller support)
                    {new[] {"game.exe", "data.win"}, ControllerSupportLevel.Partial}, // GameMaker
                    {new[] {"godot", "godot.exe"}, ControllerSupportLevel.Full}, // Godot
                    {new[] {"renpy", "renpy.exe"}, ControllerSupportLevel.Partial}, // Ren'Py (visual novels)
                    {new[] {"nw.exe", "nwjs"}, ControllerSupportLevel.Partial}, // NW.js (web-based games)
                };

                foreach (var indicator in engineIndicators)
                {
                    if (indicator.Key.Any(pattern => fileNames.Any(f => f.Contains(pattern))))
                    {
                        return new DirectoryAnalysisResult(indicator.Value, 0.8);
                    }
                }

                // Check for common game executables that suggest controller support
                var controllerFriendlyExes = new[] {"game.exe", "bin\\win64\\*.exe", "*.exe"};
                if (controllerFriendlyExes.Any(pattern => fileNames.Any(f => f.EndsWith(".exe"))))
                {
                    return new DirectoryAnalysisResult(ControllerSupportLevel.Partial, 0.4);
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\Temp\detection_debug.txt",
                    $"{DateTime.Now}: Directory analysis error for '{installPath}': {ex.Message}\r\n");
            }

            return null;
        }

        private ExecutableAnalysisResult AnalyzeExecutable(string executablePath)
        {
            // Simplified executable analysis - in a real implementation, you'd use PE file analysis
            try
            {
                var filename = Path.GetFileName(executablePath).ToLowerInvariant();
                
                foreach (var pattern in executablePatterns)
                {
                    if (pattern.Key.IsMatch(filename))
                    {
                        return new ExecutableAnalysisResult(pattern.Value, 0.5);
                    }
                }
            }
            catch (Exception)
            {
                // Ignore analysis errors
            }

            return new ExecutableAnalysisResult(ControllerSupportLevel.Unknown, 0.0);
        }

        private void InitializeKnownEngines()
        {
            knownEngines = new Dictionary<string, ControllerSupportLevel>
            {
                {"unreal", ControllerSupportLevel.Full},
                {"unity", ControllerSupportLevel.Full},
                {"source", ControllerSupportLevel.Partial},
                {"gamemaker", ControllerSupportLevel.Full},
                {"construct", ControllerSupportLevel.Partial},
                {"godot", ControllerSupportLevel.Full}
            };
        }

        private void InitializeExecutablePatterns()
        {
            executablePatterns = new Dictionary<Regex, ControllerSupportLevel>
            {
                {new Regex(@".*-win64.*\.exe$", RegexOptions.IgnoreCase), ControllerSupportLevel.Partial},
                {new Regex(@".*unity.*\.exe$", RegexOptions.IgnoreCase), ControllerSupportLevel.Full},
                {new Regex(@".*unreal.*\.exe$", RegexOptions.IgnoreCase), ControllerSupportLevel.Full},
            };
        }
    }

    public class DetectionResult
    {
        public ControllerSupportLevel SupportLevel { get; }
        public double Confidence { get; }
        public string Method { get; }

        public DetectionResult(ControllerSupportLevel supportLevel, double confidence, string method)
        {
            SupportLevel = supportLevel;
            Confidence = Math.Max(0.0, Math.Min(1.0, confidence)); // Clamp between 0 and 1
            Method = method;
        }
    }

    public class DirectoryAnalysisResult
    {
        public ControllerSupportLevel SupportLevel { get; }
        public double Confidence { get; }

        public DirectoryAnalysisResult(ControllerSupportLevel supportLevel, double confidence)
        {
            SupportLevel = supportLevel;
            Confidence = confidence;
        }
    }

    public class ExecutableAnalysisResult
    {
        public ControllerSupportLevel SupportLevel { get; }
        public double Confidence { get; }

        public ExecutableAnalysisResult(ControllerSupportLevel supportLevel, double confidence)
        {
            SupportLevel = supportLevel;
            Confidence = confidence;
        }
    }

    public class ControllerMentionCount
    {
        public int PositiveMentions { get; }
        public int NegativeMentions { get; }

        public ControllerMentionCount(int positive, int negative)
        {
            PositiveMentions = positive;
            NegativeMentions = negative;
        }
    }

    public class WeightedDetectionRules
    {
        // Future enhancement: Machine learning-based rule weighting
        // Could be trained on community data to improve accuracy
    }

    public class MetadataAnalyzer
    {
        public DetectionResult AnalyzeGameMetadata(Game game)
        {
            var confidence = 0.0;
            var supportLevel = ControllerSupportLevel.Unknown;

            // Analyze description for controller mentions
            var description = game.Description?.ToLowerInvariant() ?? "";
            var controllerMentions = CountControllerMentions(description);

            if (controllerMentions.PositiveMentions > controllerMentions.NegativeMentions)
            {
                supportLevel = controllerMentions.PositiveMentions > 2 ? 
                    ControllerSupportLevel.Full : ControllerSupportLevel.Partial;
                confidence = Math.Min(0.8, controllerMentions.PositiveMentions * 0.2);
            }
            else if (controllerMentions.NegativeMentions > 0)
            {
                supportLevel = ControllerSupportLevel.None;
                confidence = Math.Min(0.6, controllerMentions.NegativeMentions * 0.3);
            }

            return new DetectionResult(supportLevel, confidence, "Metadata Analysis");
        }

        private ControllerMentionCount CountControllerMentions(string text)
        {
            var positiveKeywords = new[] { "controller support", "gamepad", "xbox controller", 
                "playstation controller", "full controller", "controller compatible" };
            var negativeKeywords = new[] { "keyboard only", "mouse required", "no controller" };

            var positive = positiveKeywords.Sum(keyword => CountOccurrences(text, keyword));
            var negative = negativeKeywords.Sum(keyword => CountOccurrences(text, keyword));

            return new ControllerMentionCount(positive, negative);
        }

        private int CountOccurrences(string text, string keyword)
        {
            return (text.Length - text.Replace(keyword, "").Length) / keyword.Length;
        }
    }
}