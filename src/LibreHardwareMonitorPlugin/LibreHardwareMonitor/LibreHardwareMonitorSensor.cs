namespace NotADoctor99.LibreHardwareMonitorPlugin
{
    using System;

    public class LibreHardwareMonitorSensor
    {
        private Boolean _isModified = false;

        public String Id { get; }

        public String Name { get; }

        public String InstanceId { get; }

        public String Identifier { get; }

        public String DisplayName { get; }

        public String FormatString { get; }

        public LibreHardwareMonitorGaugeType GaugeType { get; }

        public Single Value { get; private set; }

        internal LibreHardwareMonitorSensor(String name, String instanceId, String identifier, String displayName, String formatString, Single value, LibreHardwareMonitorGaugeType gaugeType)
        {
            this.Id = LibreHardwareMonitorSensor.CreateSensorId(instanceId, identifier);

            this.Name = name;
            this.InstanceId = instanceId;
            this.Identifier = identifier;
            this.DisplayName = displayName;
            this.FormatString = formatString;
            this.Value = value;
            this.GaugeType = gaugeType;
        }

        public Boolean IsModified()
        {
            var isModified = this._isModified;
            this._isModified = false;
            return isModified;
        }

        internal Boolean SetValue(Single value)
        {
            this._isModified = Math.Abs(this.Value - value) >= 0.1;

            if (!this._isModified)
            {
                return false;
            }

            this.Value = value;
            return true;
        }

        public String GetButtonText() => String.Format(this.FormatString, this.Value);

        internal static String CreateSensorId(String instanceId, String identifier) => $"{instanceId}-{identifier}";
    }
}
