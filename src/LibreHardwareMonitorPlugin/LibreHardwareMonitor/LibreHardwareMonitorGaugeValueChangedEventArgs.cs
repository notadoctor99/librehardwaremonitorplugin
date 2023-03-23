namespace NotADoctor99.LibreHardwareMonitorPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class LibreHardwareMonitorGaugeValueChangedEventArgs : EventArgs
    {
        private readonly LibreHardwareMonitorGaugeType[] _modifiedGaugeTypes;

        public IEnumerable<LibreHardwareMonitorGaugeType> GaugeTypes => this._modifiedGaugeTypes;

        internal LibreHardwareMonitorGaugeValueChangedEventArgs(IEnumerable<LibreHardwareMonitorGaugeType> modifiedGaugeTypes) => this._modifiedGaugeTypes = modifiedGaugeTypes.ToArray();
    }
}
