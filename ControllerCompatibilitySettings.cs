using Playnite.SDK;
using System.Collections.Generic;
using System.ComponentModel;

namespace ControllerCompatibility
{
    public class ControllerCompatibilitySettings : System.Collections.Generic.ObservableObject, Playnite.SDK.ISettings
    {
        private bool showControllerStatus = true;
        private bool showCompatibilityWarnings = true;
        private bool autoDetectCompatibility = true;
        private bool enableCommunityDatabase = true;
        private bool autoDetectionCompleted = false;
        private int controllerCheckInterval = 1000;
        private List<string> ignoredGames = new List<string>();

        public bool ShowControllerStatus
        {
            get => showControllerStatus;
            set
            {
                showControllerStatus = value;
                OnPropertyChanged();
            }
        }

        public bool ShowCompatibilityWarnings
        {
            get => showCompatibilityWarnings;
            set
            {
                showCompatibilityWarnings = value;
                OnPropertyChanged();
            }
        }

        public bool AutoDetectCompatibility
        {
            get => autoDetectCompatibility;
            set
            {
                autoDetectCompatibility = value;
                OnPropertyChanged();
            }
        }

        public bool EnableCommunityDatabase
        {
            get => enableCommunityDatabase;
            set
            {
                enableCommunityDatabase = value;
                OnPropertyChanged();
            }
        }

        public bool AutoDetectionCompleted
        {
            get => autoDetectionCompleted;
            set
            {
                autoDetectionCompleted = value;
                OnPropertyChanged();
            }
        }

        public int ControllerCheckInterval
        {
            get => controllerCheckInterval;
            set
            {
                controllerCheckInterval = value;
                OnPropertyChanged();
            }
        }

        public List<string> IgnoredGames
        {
            get => ignoredGames;
            set
            {
                ignoredGames = value;
                OnPropertyChanged();
            }
        }

        // Parameterless constructor for serialization
        public ControllerCompatibilitySettings()
        {
        }

        // Constructor for plugin initialization
        public ControllerCompatibilitySettings(ControllerCompatibilityPlugin plugin)
        {
            var savedSettings = plugin.LoadPluginSettings<ControllerCompatibilitySettings>();
            if (savedSettings != null)
            {
                ShowControllerStatus = savedSettings.ShowControllerStatus;
                ShowCompatibilityWarnings = savedSettings.ShowCompatibilityWarnings;
                AutoDetectCompatibility = savedSettings.AutoDetectCompatibility;
                EnableCommunityDatabase = savedSettings.EnableCommunityDatabase;
                AutoDetectionCompleted = savedSettings.AutoDetectionCompleted;
                ControllerCheckInterval = savedSettings.ControllerCheckInterval;
                IgnoredGames = savedSettings.IgnoredGames ?? new List<string>();
            }
        }

        public void BeginEdit()
        {
            // Called when settings editing starts
        }

        public void CancelEdit()
        {
            // Called when settings editing is cancelled
        }

        public void EndEdit()
        {
            // Called when settings editing ends
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();

            if (ControllerCheckInterval < 100)
            {
                errors.Add("Controller check interval must be at least 100ms");
            }

            if (ControllerCheckInterval > 10000)
            {
                errors.Add("Controller check interval should not exceed 10 seconds");
            }

            return errors.Count == 0;
        }
    }
}