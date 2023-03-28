namespace NotADoctor99.LibreHardwareMonitorPlugin
{
    using System;
    using System.Collections.Generic;
    using Loupedeck;

    public class HardwareMonitorControlCenter : PluginDynamicFolder
    {
        private const String StartMonitorApplicationCommandName = "StartMonitorApplication";

        public HardwareMonitorControlCenter() => this.DisplayName = "Hardware Monitor";

        public override PluginDynamicFolderNavigation GetNavigationArea(DeviceType _) => PluginDynamicFolderNavigation.ButtonArea;

        public override Boolean Load()
        {
            LibreHardwareMonitorPlugin.HardwareMonitor.SensorListChanged += this.OnSensorListChanged;
            LibreHardwareMonitorPlugin.HardwareMonitor.SensorValuesChanged += this.OnSensorValuesChanged;
            LibreHardwareMonitorPlugin.HardwareMonitor.ProcessExited += this.HardwareMonitorProcessExited;

            return true;
        }

        public override Boolean Unload()
        {
            LibreHardwareMonitorPlugin.HardwareMonitor.SensorListChanged -= this.OnSensorListChanged;
            LibreHardwareMonitorPlugin.HardwareMonitor.SensorValuesChanged -= this.OnSensorValuesChanged;
            LibreHardwareMonitorPlugin.HardwareMonitor.ProcessExited -= this.HardwareMonitorProcessExited;

            return true;
        }

        public override BitmapImage GetButtonImage(PluginImageSize imageSize) => PluginResources.ReadImage("HardwareMonitorControlCenter.png");

        public override IEnumerable<String> GetButtonPressActionNames(DeviceType deviceType)
        {
            if (0 == LibreHardwareMonitorPlugin.HardwareMonitor.Sensors.Count)
            {
                yield return this.CreateCommandName(StartMonitorApplicationCommandName);
                yield break;
            }

            foreach (var sensor in LibreHardwareMonitorPlugin.HardwareMonitor.Sensors)
            {
                yield return this.CreateCommandName(sensor.Name);
            }
        }

        public override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
            => actionParameter.EqualsNoCase(StartMonitorApplicationCommandName)
            ? "Start Libre Hardware Monitor"
            : LibreHardwareMonitorPlugin.HardwareMonitor.TryGetSensor(actionParameter, out var sensor) ? sensor.GetButtonText() : actionParameter;

        public override void RunCommand(String actionParameter) => LibreHardwareMonitor.ActivateOrRun();

        private void OnSensorListChanged(Object sender, EventArgs e) => this.ButtonActionNamesChanged();

        private void OnSensorValuesChanged(Object sender, LibreHardwareMonitorSensorValuesChangedEventArgs e)
        {
            foreach (var sensorName in e.SensorNames)
            {
                this.CommandImageChanged(sensorName);
            }
        }

        private void HardwareMonitorProcessExited(Object sender, EventArgs e) => this.ButtonActionNamesChanged();
    }
}
