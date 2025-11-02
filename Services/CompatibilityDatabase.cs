using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace ControllerCompatibility
{
    public class CompatibilityDatabase
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private Dictionary<string, GameCompatibilityInfo> gameCompatibility;
        private readonly string databasePath;

        public CompatibilityDatabase()
        {
            gameCompatibility = new Dictionary<string, GameCompatibilityInfo>();
            databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                                      "Playnite", "ExtensionsData", "ControllerCompatibility", "compatibility.json");
            
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath));
            
            // Load existing data
            LoadDatabase();
        }

        public void LoadDatabase()
        {
            try
            {
                if (File.Exists(databasePath))
                {
                    var json = File.ReadAllText(databasePath);
                    gameCompatibility = JsonConvert.DeserializeObject<Dictionary<string, GameCompatibilityInfo>>(json) 
                                       ?? new Dictionary<string, GameCompatibilityInfo>();
                }
                else
                {
                    InitializeDefaultDatabase();
                }
            }
            catch (Exception ex)
            {
                // Log error and initialize empty database
                logger.Error($"Error loading compatibility database: {ex.Message}");
                gameCompatibility = new Dictionary<string, GameCompatibilityInfo>();
            }
        }

        public void SaveDatabase()
        {
            try
            {
                var json = JsonConvert.SerializeObject(gameCompatibility, Formatting.Indented);
                File.WriteAllText(databasePath, json);
            }
            catch (Exception ex)
            {
                logger.Error($"Error saving compatibility database: {ex.Message}");
            }
        }

        public GameCompatibilityInfo GetCompatibilityInfo(Game game)
        {
            var key = GenerateGameKey(game);
            
            if (gameCompatibility.TryGetValue(key, out var info))
            {
                return info;
            }

            // Try to match by name or alternative names
            var matchedInfo = FindCompatibilityByName(game);
            if (matchedInfo != null)
            {
                return matchedInfo;
            }

            // Return default compatibility info
            return new GameCompatibilityInfo
            {
                GameId = game.Id.ToString(),
                GameName = game.Name,
                SupportLevel = ControllerSupportLevel.Unknown,
                LastUpdated = DateTime.Now,
                Source = CompatibilitySource.Unknown
            };
        }

        public void UpdateGameCompatibility(Game game, ControllerSupportLevel supportLevel, CompatibilitySource source = CompatibilitySource.User)
        {
            var key = GenerateGameKey(game);
            var info = new GameCompatibilityInfo
            {
                GameId = game.Id.ToString(),
                GameName = game.Name,
                SupportLevel = supportLevel,
                LastUpdated = DateTime.Now,
                Source = source
            };

            gameCompatibility[key] = info;
            SaveDatabase();
        }

        public void UpdateGameCompatibility(Game game)
        {
            // Auto-detect compatibility based on game properties
            var supportLevel = DetectControllerSupport(game);
            UpdateGameCompatibility(game, supportLevel, CompatibilitySource.AutoDetected);
        }

        public List<GameCompatibilityInfo> GetAllCompatibilityInfo()
        {
            return gameCompatibility.Values.ToList();
        }

        public List<GameCompatibilityInfo> GetGamesByCompatibility(ControllerSupportLevel supportLevel)
        {
            return gameCompatibility.Values.Where(info => info.SupportLevel == supportLevel).ToList();
        }

        private string GenerateGameKey(Game game)
        {
            // Use multiple identifiers to create a unique key
            if (!string.IsNullOrEmpty(game.GameId))
            {
                return $"{game.Source?.Name}_{game.GameId}".ToLowerInvariant();
            }
            
            return game.Name.ToLowerInvariant().Replace(" ", "_");
        }

        private GameCompatibilityInfo FindCompatibilityByName(Game game)
        {
            // Try exact name match
            var exactMatch = gameCompatibility.Values.FirstOrDefault(info => 
                string.Equals(info.GameName, game.Name, StringComparison.OrdinalIgnoreCase));
            
            if (exactMatch != null)
                return exactMatch;

            // Try partial name match
            var partialMatch = gameCompatibility.Values.FirstOrDefault(info =>
                info.GameName.ToLowerInvariant().Contains(game.Name.ToLowerInvariant()) ||
                game.Name.ToLowerInvariant().Contains(info.GameName.ToLowerInvariant()));

            return partialMatch;
        }

        private ControllerSupportLevel DetectControllerSupport(Game game)
        {
            var detector = new AdvancedCompatibilityDetector();
            return detector.DetectCompatibility(game);
        }

        private void InitializeDefaultDatabase()
        {
            // Start with empty database - compatibility will be detected on-demand
            gameCompatibility = new Dictionary<string, GameCompatibilityInfo>();
        }
    }

    public class GameCompatibilityInfo
    {
        public string GameId { get; set; }
        public string GameName { get; set; }
        public ControllerSupportLevel SupportLevel { get; set; }
        public string Notes { get; set; }
        public CompatibilitySource Source { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<string> RecommendedConfigurations { get; set; } = new List<string>();
        public int CommunityRating { get; set; } // 1-5 stars
        public int TotalRatings { get; set; }
    }

    public enum ControllerSupportLevel
    {
        Unknown,
        None,           // No controller support
        Partial,        // Limited controller support
        Full,           // Full native controller support
        Community       // Community configurations available
    }

    public enum CompatibilitySource
    {
        Unknown,
        Official,       // From game developer/publisher
        Community,      // From community database
        User,           // User-defined
        AutoDetected    // Automatically detected
    }
}