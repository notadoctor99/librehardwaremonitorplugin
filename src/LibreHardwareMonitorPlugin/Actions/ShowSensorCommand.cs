namespace NotADoctor99.LibreHardwareMonitorPlugin
{
    using System;
    using Loupedeck;

    public class ShowSensorCommand : PluginDynamicCommand
    {
        public ShowSensorCommand()
        {
            //this.IsWidget = true;
            this.GroupName = "Sensors";
        }

        protected override Boolean OnLoad()
        {
            this.UpdateParameters();

            LibreHardwareMonitorPlugin.HardwareMonitor.SensorListChanged += this.OnSensorListChanged;
            LibreHardwareMonitorPlugin.HardwareMonitor.SensorValuesChanged += this.OnSensorValuesChanged;
            LibreHardwareMonitorPlugin.HardwareMonitor.ProcessStarted += this.OnHardwareMonitorProcessStarted;
            LibreHardwareMonitorPlugin.HardwareMonitor.ProcessExited += this.OnHardwareMonitorProcessExited;

            return true;
        }

        protected override Boolean OnUnload()
        {
            LibreHardwareMonitorPlugin.HardwareMonitor.SensorListChanged -= this.OnSensorListChanged;
            LibreHardwareMonitorPlugin.HardwareMonitor.SensorValuesChanged -= this.OnSensorValuesChanged;
            LibreHardwareMonitorPlugin.HardwareMonitor.ProcessStarted -= this.OnHardwareMonitorProcessStarted;
            LibreHardwareMonitorPlugin.HardwareMonitor.ProcessExited -= this.OnHardwareMonitorProcessExited;

            return true;
        }

        protected override void RunCommand(String actionParameter) => LibreHardwareMonitor.ActivateOrRun();

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
            => LibreHardwareMonitorPlugin.HardwareMonitor.TryGetSensor(actionParameter, out var sensor) ? sensor.GetButtonText() : PluginHelpers.GetNotAvailableButtonText();

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
            => LibreHardwareMonitorPlugin.HardwareMonitor.TryGetSensor(actionParameter, out var sensor) ? null : PluginHelpers.GetNotAvailableButtonImage();

        private void OnSensorListChanged(Object sender, EventArgs e) => this.UpdateParameters();

        private void OnHardwareMonitorProcessExited(Object sender, EventArgs e) => this.ActionImageChanged(null);

        private void OnHardwareMonitorProcessStarted(Object sender, EventArgs e) => this.ActionImageChanged(null);

        private void UpdateParameters()
        {
            this.RemoveAllParameters();

            foreach (var sensor in LibreHardwareMonitorPlugin.HardwareMonitor.Sensors)
            {
                this.AddParameter(sensor.Name, sensor.DisplayName, this.GroupName);
            }

            this.ParametersChanged();
            this.ActionImageChanged(null);
        }

        private void OnSensorValuesChanged(Object sender, LibreHardwareMonitorSensorValuesChangedEventArgs e)
        {
            foreach (var sensorName in e.SensorNames)
            {
                this.ActionImageChanged(sensorName);
            }
        }
    }
}
