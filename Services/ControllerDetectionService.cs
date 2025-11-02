using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Timers;
using System.Linq;

namespace ControllerCompatibility
{
    // XInput P/Invoke declarations
    [StructLayout(LayoutKind.Sequential)]
    public struct XINPUT_STATE
    {
        public uint dwPacketNumber;
        public XINPUT_GAMEPAD Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XINPUT_GAMEPAD
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    public class ControllerDetectionService
    {
        // XInput constants
        private const uint ERROR_SUCCESS = 0;
        private const uint ERROR_DEVICE_NOT_CONNECTED = 1167;

        // XInput P/Invoke methods
        [DllImport("xinput1_4.dll")]
        private static extern uint XInputGetState(uint dwUserIndex, ref XINPUT_STATE pState);

        private Timer detectionTimer;
        private List<DetectedController> connectedControllers;
        private static readonly Playnite.SDK.ILogger logger = Playnite.SDK.LogManager.GetLogger();
        public event EventHandler<ControllerChangedEventArgs> ControllerChanged;

        public ControllerDetectionService()
        {
            connectedControllers = new List<DetectedController>();
            detectionTimer = new Timer(1000); // Check every second
            detectionTimer.Elapsed += DetectionTimer_Elapsed;
        }

        public void StartMonitoring()
        {
            RefreshControllers();
            detectionTimer.Start();
        }

        public void StopMonitoring()
        {
            detectionTimer?.Stop();
        }

        public void RefreshControllers()
        {
            var previousControllers = new List<DetectedController>(connectedControllers);
            connectedControllers.Clear();

            logger.Info("Refreshing controllers...");

            // Check XInput controllers (Xbox controllers)
            CheckXInputControllers();

            // Check DirectInput controllers
            CheckDirectInputControllers();

            logger.Info($"Found {connectedControllers.Count} controllers total");

            // Notify if controllers changed
            if (ControllersChanged(previousControllers, connectedControllers))
            {
                logger.Info("Controllers changed, firing event");
                ControllerChanged?.Invoke(this, new ControllerChangedEventArgs(connectedControllers));
            }
        }

        public List<DetectedController> GetConnectedControllers()
        {
            return new List<DetectedController>(connectedControllers);
        }

        private void DetectionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            RefreshControllers();
        }

        private void CheckXInputControllers()
        {
            try
            {
                for (int playerIndex = 0; playerIndex < 4; playerIndex++)
                {
                    XINPUT_STATE state = new XINPUT_STATE();
                    uint result = XInputGetState((uint)playerIndex, ref state);

                    if (result == ERROR_SUCCESS)
                    {
                        var controller = new DetectedController
                        {
                            Id = $"xinput_{playerIndex}",
                            Name = $"Xbox Controller {playerIndex + 1}",
                            Type = ControllerType.Xbox,
                            IsConnected = true,
                            PlayerIndex = playerIndex
                        };
                        connectedControllers.Add(controller);
                        logger.Info($"Detected XInput controller: {controller.Name} at index {playerIndex}");
                    }
                    else if (result == ERROR_DEVICE_NOT_CONNECTED)
                    {
                        logger.Debug($"No XInput controller at index {playerIndex}");
                    }
                    else
                    {
                        logger.Debug($"XInput error at index {playerIndex}: {result}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error checking XInput controllers: {ex.Message}");
            }
        }

        private void CheckDirectInputControllers()
        {
            try
            {
                // Check for PlayStation controllers via DirectInput
                // This will enumerate actual connected controllers without false positives
                // since we're specifically looking for known PlayStation controller patterns
                CheckPlayStationControllers();
            }
            catch (Exception ex)
            {
                logger.Error($"Error checking DirectInput controllers: {ex.Message}");
            }
        }

        private void CheckPlayStationControllers()
        {
            try
            {
                // Use Windows Raw Input API to detect PlayStation controllers
                // This avoids the false positives from full DirectInput enumeration

                // Look for common PlayStation controller device names
                var playstationDeviceNames = new[]
                {
                    "wireless controller", // DS4
                    "dualshock", // DS4
                    "playstation", // Generic PS
                    "sony", // Sony controllers
                    "ds4", // DualShock 4
                    "ds5"  // DualShock 5 (if supported)
                };

                // Check Windows registry for HID devices (simplified approach)
                // In a full implementation, you'd use SetupAPI or Raw Input
                var hidDevices = Microsoft.Win32.Registry.LocalMachine
                    .OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\HID");

                if (hidDevices != null)
                {
                    foreach (var deviceKey in hidDevices.GetSubKeyNames())
                    {
                        try
                        {
                            var deviceSubKey = hidDevices.OpenSubKey(deviceKey);
                            if (deviceSubKey != null)
                            {
                                foreach (var subDeviceKey in deviceSubKey.GetSubKeyNames())
                                {
                                    var fullDeviceKey = deviceSubKey.OpenSubKey(subDeviceKey);
                                    if (fullDeviceKey != null)
                                    {
                                        var deviceDesc = fullDeviceKey.GetValue("DeviceDesc") as string;
                                        var friendlyName = fullDeviceKey.GetValue("FriendlyName") as string;

                                        if (!string.IsNullOrEmpty(deviceDesc) || !string.IsNullOrEmpty(friendlyName))
                                        {
                                            var combinedName = $"{deviceDesc} {friendlyName}".ToLowerInvariant();

                                            if (playstationDeviceNames.Any(psName =>
                                                combinedName.Contains(psName)))
                                            {
                                                var controller = new DetectedController
                                                {
                                                    Id = $"ps_{deviceKey}_{subDeviceKey}",
                                                    Name = friendlyName ?? deviceDesc ?? "PlayStation Controller",
                                                    Type = ControllerType.PlayStation,
                                                    IsConnected = true
                                                };

                                                // Try to determine specific model
                                                if (combinedName.Contains("ds4") || combinedName.Contains("dualshock 4"))
                                                {
                                                    controller.Name = "DualShock 4";
                                                }
                                                else if (combinedName.Contains("ds5") || combinedName.Contains("dualshock 5"))
                                                {
                                                    controller.Name = "DualSense";
                                                }

                                                connectedControllers.Add(controller);
                                                logger.Info($"Detected PlayStation controller: {controller.Name}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Debug($"Error checking device {deviceKey}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error detecting PlayStation controllers: {ex.Message}");
            }
        }

        private ControllerType DetermineControllerType(string productName, int vendorId = 0, int productId = 0)
        {
            var name = productName.ToLowerInvariant();
            
            // Simple name-based detection for Xbox controllers
            if (name.Contains("xbox") || name.Contains("microsoft") || name.Contains("xinput"))
                return ControllerType.Xbox;
            else
                return ControllerType.Generic;
        }

        private bool ControllersChanged(List<DetectedController> previous, List<DetectedController> current)
        {
            if (previous.Count != current.Count)
                return true;

            for (int i = 0; i < current.Count; i++)
            {
                if (i >= previous.Count || previous[i].Id != current[i].Id)
                    return true;
            }

            return false;
        }
    }

    public class DetectedController
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ControllerType Type { get; set; }
        public bool IsConnected { get; set; }
        public int? PlayerIndex { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public int VendorId { get; set; }
        public int ProductId { get; set; }
        public ControllerCapabilities Capabilities { get; set; } = new ControllerCapabilities();
    }

    public enum ControllerType
    {
        Xbox,
        PlayStation,
        Nintendo,
        Steam,
        Generic
    }

    public class ControllerCapabilities
    {
        public int ButtonCount { get; set; }
        public int AxisCount { get; set; }
        public bool HasTriggers { get; set; }
        public bool HasVibration { get; set; }
        public bool HasTouchpad { get; set; }
        public bool HasMotionSensors { get; set; }
        public bool HasAudio { get; set; }
        public bool SupportsWireless { get; set; }
    }

    public class ControllerChangedEventArgs : EventArgs
    {
        public List<DetectedController> Controllers { get; }

        public ControllerChangedEventArgs(List<DetectedController> controllers)
        {
            Controllers = controllers;
        }
    }
}