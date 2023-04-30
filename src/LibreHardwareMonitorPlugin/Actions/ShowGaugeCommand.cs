namespace NotADoctor99.LibreHardwareMonitorPlugin
{
    using System;

    using Loupedeck;

    public class ShowGaugeCommand : PluginDynamicCommand
    {
        public ShowGaugeCommand()
        {
            this.IsWidget = true;
            this.GroupName = "Gauges";

            AddParameter(LibreHardwareMonitorGaugeType.CPU);
            AddParameter(LibreHardwareMonitorGaugeType.GPU);
            AddParameter(LibreHardwareMonitorGaugeType.Memory);
            AddParameter(LibreHardwareMonitorGaugeType.Battery);

            void AddParameter(LibreHardwareMonitorGaugeType gaugeType) => this.AddParameter(gaugeType.ToString(), gaugeType.ToString(), this.GroupName);
        }

        protected override Boolean OnLoad()
        {
            LibreHardwareMonitorPlugin.HardwareMonitor.SensorListChanged += this.OnSensorListChanged;
            LibreHardwareMonitorPlugin.HardwareMonitor.GaugeValuesChanged += this.OnGaugeValuesChanged;
            LibreHardwareMonitorPlugin.HardwareMonitor.ProcessStarted += this.OnHardwareMonitorProcessStarted;
            LibreHardwareMonitorPlugin.HardwareMonitor.ProcessExited += this.OnHardwareMonitorProcessExited;

            return true;
        }

        protected override Boolean OnUnload()
        {
            LibreHardwareMonitorPlugin.HardwareMonitor.SensorListChanged -= this.OnSensorListChanged;
            LibreHardwareMonitorPlugin.HardwareMonitor.GaugeValuesChanged -= this.OnGaugeValuesChanged;
            LibreHardwareMonitorPlugin.HardwareMonitor.ProcessStarted -= this.OnHardwareMonitorProcessStarted;
            LibreHardwareMonitorPlugin.HardwareMonitor.ProcessExited -= this.OnHardwareMonitorProcessExited;

            return true;
        }

        protected override void RunCommand(String actionParameter) => LibreHardwareMonitor.ActivateOrRun();

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) => PluginHelpers.GetNotAvailableButtonText();

        private readonly Int32[] _lastLevels = new Int32[(Int32)LibreHardwareMonitorGaugeType.Count];

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            if (!Enum.TryParse<LibreHardwareMonitorGaugeType>(actionParameter, out var gaugeType) || !LibreHardwareMonitorPlugin.HardwareMonitor.TryGetSensor(gaugeType, out var sensor))
            {
                return PluginHelpers.GetNotAvailableButtonImage();
            }

            var level = this._lastLevels[(Int32)gaugeType];
            var index = 0;

            switch (gaugeType)
            {
                case LibreHardwareMonitorGaugeType.CPU:
                    index = Helpers.MinMax((level + 19) / 20, 1, 5);
                    break;
                case LibreHardwareMonitorGaugeType.GPU:
                    index = Helpers.MinMax((level + 19) / 20, 1, 5);
                    break;
                case LibreHardwareMonitorGaugeType.Memory:
                case LibreHardwareMonitorGaugeType.Battery:
                    index = Helpers.MinMax((level + 9) / 20, 0, 5);
                    break;
            }

            using (var bitmapBuilder = new BitmapBuilder(PluginImageSize.Width90))
            {
                bitmapBuilder.Clear(BitmapColor.Black);
                var imageBytes = PluginResources.ReadBinaryFile($"{gaugeType}{index}.png");
                bitmapBuilder.DrawImage(imageBytes, 0, 0);
                bitmapBuilder.DrawText($"{level} %", 0, 40, 80, 40);
                return bitmapBuilder.ToImage();
            }
        }

        private void OnGaugeValuesChanged(Object sender, LibreHardwareMonitorGaugeValueChangedEventArgs e)
        {
            foreach (var gaugeType in e.GaugeTypes)
            {
                if (this.UpdateGaugeIndex(gaugeType))
                {
                    this.ActionImageChanged(gaugeType.ToString());
                }
            }
        }

        private void OnSensorListChanged(Object sender, EventArgs e)
        {
            this.UpdateGaugeIndex(LibreHardwareMonitorGaugeType.CPU);
            this.UpdateGaugeIndex(LibreHardwareMonitorGaugeType.GPU);
            this.UpdateGaugeIndex(LibreHardwareMonitorGaugeType.Memory);
            this.UpdateGaugeIndex(LibreHardwareMonitorGaugeType.Battery);
            this.ActionImageChanged(null);
        }

        private Boolean UpdateGaugeIndex(LibreHardwareMonitorGaugeType gaugeType)
        {
            if (!LibreHardwareMonitorPlugin.HardwareMonitor.TryGetSensor(gaugeType, out var sensor))
            {
                return false;
            }

            var level = (Int32)Math.Round(sensor.Value);

            if (this._lastLevels[(Int32)gaugeType] != level)
            {
                this._lastLevels[(Int32)gaugeType] = level;
                return true;
            }

            return false;
        }

        private void OnHardwareMonitorProcessStarted(Object sender, EventArgs e) => this.ActionImageChanged(null);

        private void OnHardwareMonitorProcessExited(Object sender, EventArgs e) => this.ActionImageChanged(null);
    }
}
