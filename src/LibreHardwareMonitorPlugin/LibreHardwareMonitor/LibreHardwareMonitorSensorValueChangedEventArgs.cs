namespace NotADoctor99.LibreHardwareMonitorPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class LibreHardwareMonitorSensorValuesChangedEventArgs : EventArgs
    {
        private readonly String[] _modifiedSensorNames;

        public IEnumerable<String> SensorNames => this._modifiedSensorNames;

        internal LibreHardwareMonitorSensorValuesChangedEventArgs(IEnumerable<String> modifiedSensorNames) => this._modifiedSensorNames = modifiedSensorNames.ToArray();
    }
}
