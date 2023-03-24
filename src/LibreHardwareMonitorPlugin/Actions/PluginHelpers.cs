namespace NotADoctor99.LibreHardwareMonitorPlugin
{
    using System;

    using Loupedeck;

    internal static class PluginHelpers
    {
        public static String GetNotAvailableButtonText() => LibreHardwareMonitor.IsRunning() ? "Sensor not\navailable" : "Press\nto start";

        public static BitmapImage GetNotAvailableButtonImage()
            => PluginResources.ReadImage(LibreHardwareMonitor.IsRunning() ? "SensorNotAvailable.png" : "PressToStart.png");
    }
}
