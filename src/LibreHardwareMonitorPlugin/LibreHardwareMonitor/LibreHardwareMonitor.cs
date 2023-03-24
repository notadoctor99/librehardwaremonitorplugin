namespace NotADoctor99.LibreHardwareMonitorPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Management;
    using System.Reflection;
    using System.Timers;

    using Loupedeck;

    using Microsoft.Win32;

    public sealed class LibreHardwareMonitor : IDisposable
    {
        private readonly Timer _periodicTimer = new Timer();

        private const String LibreHardwareMonitorScope = @"root\LibreHardwareMonitor";
        private readonly ManagementEventWatcher _sensorListChangeWatcher;
        private readonly Timer _sensorListChangeTimer = new Timer();

        public LibreHardwareMonitor()
        {
            this._periodicTimer.Interval = 2_000;
            this._periodicTimer.AutoReset = true;
            this._periodicTimer.Elapsed += this.OnPeriodicTimerElapsed;

            var scope = new ManagementScope(LibreHardwareMonitorScope);
            var query = new WqlEventQuery("__InstanceCreationEvent", new TimeSpan(0, 0, 1), "TargetInstance ISA \"Sensor\"");
            this._sensorListChangeWatcher = new ManagementEventWatcher(scope, query);
            this._sensorListChangeWatcher.EventArrived += this.OnSensorListChangeWatcherEvent;

            this._sensorListChangeTimer.Interval = 2_000;
            this._sensorListChangeTimer.AutoReset = false;
            this._sensorListChangeTimer.Elapsed += this.OnSensorListChangeTimerElapsed;
        }

        public void Dispose()
        {
            this._periodicTimer.Stop();
            this._periodicTimer.Elapsed -= this.OnPeriodicTimerElapsed;
            this._periodicTimer.Dispose();

            this._sensorListChangeWatcher.Stop();
            this._sensorListChangeWatcher.EventArrived -= this.OnSensorListChangeWatcherEvent;
            this._sensorListChangeWatcher.Dispose();

            this._sensorListChangeTimer.Stop();
            this._sensorListChangeTimer.Elapsed -= this.OnSensorListChangeTimerElapsed;
            this._sensorListChangeTimer.Dispose();
        }

        public void StartMonitoring()
        {
            this._isRunning = LibreHardwareMonitor.IsRunning();

            if (this._isRunning)
            {
                this._sensorListChangeWatcher.Start();

                this.GetAvailableSensors();
            }

            this._periodicTimer.Start();
        }

        public void StopMonitoring()
        {
            this._periodicTimer.Stop();
            this._sensorListChangeWatcher.Stop();
            this._sensorListChangeTimer.Stop();
        }

        // process

        private Boolean _isRunning = false;

        public event EventHandler<EventArgs> ProcessStarted;

        public event EventHandler<EventArgs> ProcessExited;

        private void OnPeriodicTimerElapsed(Object sender, ElapsedEventArgs e)
        {
            var isRunning = LibreHardwareMonitor.IsRunning();

            if (!this._isRunning && isRunning)
            {
                this._isRunning = true;

                this.ProcessStarted?.BeginInvoke(this, new EventArgs());

                this._sensorListChangeWatcher.Start();
            }
            else if (this._isRunning && !isRunning)
            {
                this._isRunning = false;

                this._sensorListChangeWatcher.Stop();
                this._sensorListChangeTimer.Stop();

                this.ProcessExited?.BeginInvoke(this, new EventArgs());
            }
            else if (this._isRunning)
            {
                this.UpdateSensorValues();
            }
        }

        private void OnSensorListChangeWatcherEvent(Object sender, EventArrivedEventArgs e)
        {
            this._sensorListChangeTimer.Stop();
            this._sensorListChangeTimer.Start();
        }

        private void OnSensorListChangeTimerElapsed(Object sender, ElapsedEventArgs e)
        {
            this._sensorListChangeTimer.Stop();

            this.GetAvailableSensors();
        }

        // sensors

        public Boolean IsMonitoringStarted { get; private set; }

        public event EventHandler<EventArgs> SensorListChanged;

        private readonly Dictionary<String, LibreHardwareMonitorSensor> _sensorsByName = new Dictionary<String, LibreHardwareMonitorSensor>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Dictionary<String, LibreHardwareMonitorSensor> _sensorsById = new Dictionary<String, LibreHardwareMonitorSensor>(StringComparer.InvariantCultureIgnoreCase);
        private readonly Dictionary<LibreHardwareMonitorGaugeType, LibreHardwareMonitorSensor> _sensorsByGaugeType = new Dictionary<LibreHardwareMonitorGaugeType, LibreHardwareMonitorSensor>();

        public IReadOnlyCollection<LibreHardwareMonitorSensor> Sensors => this._sensorsByName.Values;

        public event EventHandler<LibreHardwareMonitorSensorValuesChangedEventArgs> SensorValuesChanged;

        public event EventHandler<LibreHardwareMonitorGaugeValueChangedEventArgs> GaugeValuesChanged;

        public Boolean TryGetSensor(String sensorName, out LibreHardwareMonitorSensor sensor) => this._sensorsByName.TryGetValueSafe(sensorName, out sensor);

        public Boolean TryGetSensor(ManagementBaseObject wmiSensor, out LibreHardwareMonitorSensor sensor)
        {
            var instanceId = wmiSensor.GetInstanceId();
            var identifier = wmiSensor.GetIdentifier();
            var sensorId = LibreHardwareMonitorSensor.CreateSensorId(instanceId, identifier);

            return this._sensorsById.TryGetValueSafe(sensorId, out sensor);
        }

        public Boolean TryGetSensor(LibreHardwareMonitorGaugeType gaugeType, out LibreHardwareMonitorSensor sensor) => this._sensorsByGaugeType.TryGetValueSafe(gaugeType, out sensor);

        private Boolean TryGetProcessId(out String processId)
        {
            try
            {
                var processQuery = $"SELECT ProcessId FROM Hardware";
                using (var hardwareSearcher = new ManagementObjectSearcher(LibreHardwareMonitorScope, processQuery))
                {
                    foreach (var hardware in hardwareSearcher.Get())
                    {
                        processId = hardware.GetProcessId();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error getting process ID");
            }

            processId = null;
            return false;
        }

        private Int32 GetAvailableSensors()
        {
            lock (this._sensorsByName)
            {
                this._sensorsByName.Clear();
                this._sensorsById.Clear();
                this._sensorsByGaugeType.Clear();

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                try
                {
                    // read hardware IDs

                    if (!this.TryGetProcessId(out var processId))
                    {
                        PluginLog.Error("Cannot get sensors list. Is LibreHardwareMonitor running?");
                        return 0;
                    }

                    var cpuId = "";
                    var memoryId = "";
                    var batteryId = "";

                    var hardwareQuery = $"SELECT HardwareType,Identifier FROM Hardware WHERE ProcessId = \"{processId}\"";
                    using (var hardwareSearcher = new ManagementObjectSearcher(LibreHardwareMonitorScope, hardwareQuery))
                    {
                        foreach (var hardware in hardwareSearcher.Get())
                        {
                            var hardwareType = hardware.GetHardwareType();
                            var hardwareIdentifier = hardware.GetIdentifier();

                            if (hardwareType.StartsWith("cpu", StringComparison.InvariantCultureIgnoreCase))
                            {
                                cpuId = hardwareIdentifier;
                            }
                            if (hardwareType.StartsWith("memory", StringComparison.InvariantCultureIgnoreCase))
                            {
                                memoryId = hardwareIdentifier;
                            }
                            if (hardwareType.StartsWith("battery", StringComparison.InvariantCultureIgnoreCase))
                            {
                                batteryId = hardwareIdentifier;
                            }
                        }
                    }

                    // read sensors

                    var parentName = "CPU";
                    var parentId = cpuId;

                    AddSensor("Clock", @"{-}\n{0:N1} MHz");
                    AddSensor("Load", @"CPU\n{0:N1} %");
                    AddSensor("Power", @"{-}\n{0:N1} W");
                    AddSensor("Temperature", @"{-}\n{0:N1} °C");
                    AddSensor("Voltage", @"{-}\n{0:N3} V");

                    parentName = "Memory";
                    parentId = memoryId;

                    AddSensor("Load", @"{-}\n{0:N1} %");
                    AddSensor("Data", @"{-}\n{0:N1} GB");

                    parentName = "Battery";
                    parentId = batteryId;

                    AddSensor("Level", @"{-}\n{0:N1} %");
                    AddSensor("Voltage", @"{-}\n{0:N1} V");

                    void AddSensor(String sensorType, String formatString)
                    {
                        var sensorQuery = $"SELECT InstanceId,Identifier,Name,Value FROM Sensor WHERE ProcessId = \"{processId}\" AND Parent = \"{parentId}\" AND SensorType = \"{sensorType}\"";
                        using (var sensorSearcher = new ManagementObjectSearcher(LibreHardwareMonitorScope, sensorQuery))
                        {
                            foreach (var wmiSensor in sensorSearcher.Get())
                            {
                                var displayName = wmiSensor.GetDisplayName();

                                if (!displayName.Contains(" #"))
                                {
                                    var name = $"{parentName}-{sensorType}-{displayName}".Replace(' ', '.');

                                    var identifier = wmiSensor.GetIdentifier();

                                    var gaugeType = LibreHardwareMonitorGaugeType.None;
                                    if (identifier.EndsWithNoCase("/load/0") && displayName.EqualsNoCase("CPU Total"))
                                    {
                                        gaugeType = LibreHardwareMonitorGaugeType.CPU;
                                    }
                                    else if (identifier.EqualsNoCase("/ram/load/0"))
                                    {
                                        gaugeType = LibreHardwareMonitorGaugeType.Memory;
                                    }
                                    else if (identifier.EqualsNoCase("/battery/level/0") && displayName.EqualsNoCase("Charge Level"))
                                    {
                                        gaugeType = LibreHardwareMonitorGaugeType.Battery;
                                    }

                                    var itemFormatString = formatString.Replace("{-}", displayName);
                                    var itemDisplayName = $"[{parentName} {sensorType}] {displayName}";

                                    var sensor = new LibreHardwareMonitorSensor(name, wmiSensor.GetInstanceId(), identifier, itemDisplayName, itemFormatString, wmiSensor.GetValue(), gaugeType);
                                    this._sensorsByName[sensor.Name] = sensor;
                                    this._sensorsById[sensor.Id] = sensor;

                                    if (sensor.GaugeType != LibreHardwareMonitorGaugeType.None)
                                    {
                                        this._sensorsByGaugeType[sensor.GaugeType] = sensor;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, "Error getting sensor list");
                }

                // all done

                stopwatch.Stop();
                PluginLog.Info($"{this._sensorsByName.Count}/{this._sensorsById.Count}/{this._sensorsByGaugeType.Count} sensors found in {stopwatch.Elapsed.TotalMilliseconds:N0} ms");

                this.SensorListChanged?.BeginInvoke(this, new EventArgs());

                return this._sensorsByName.Count;
            }
        }

        private readonly List<LibreHardwareMonitorGaugeType> _modifiedGaugeTypes = new List<LibreHardwareMonitorGaugeType>();
        private readonly List<String> _modifiedSensorNames = new List<String>();

        private void UpdateSensorValues()
        {
            try
            {
                this._modifiedGaugeTypes.Clear();
                this._modifiedSensorNames.Clear();

                var sensorQuery = $"SELECT InstanceId,Identifier,Value FROM Sensor";
                using (var sensorSearcher = new ManagementObjectSearcher(LibreHardwareMonitorScope, sensorQuery))
                {
                    foreach (var wmiSensor in sensorSearcher.Get())
                    {
                        if (this.TryGetSensor(wmiSensor, out var sensor))
                        {
                            var value = wmiSensor.GetValue();

                            if (sensor.SetValue(value))
                            {
                                if (sensor.GaugeType != LibreHardwareMonitorGaugeType.None)
                                {
                                    this._modifiedGaugeTypes.Add(sensor.GaugeType);
                                }

                                this._modifiedSensorNames.Add(sensor.Name);
                            }
                        }
                    }
                }

                if (this._modifiedGaugeTypes.Count > 0)
                {
                    this.GaugeValuesChanged?.BeginInvoke(this, new LibreHardwareMonitorGaugeValueChangedEventArgs(this._modifiedGaugeTypes));
                }

                if (this._modifiedSensorNames.Count > 0)
                {
                    this.SensorValuesChanged?.BeginInvoke(this, new LibreHardwareMonitorSensorValuesChangedEventArgs(this._modifiedSensorNames));
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error updating sensor values");
            }
        }

        // static methods

        public static Boolean IsInstalled() => LibreHardwareMonitor.TryFindExecutableFilePath(out _);

        public static Boolean IsRunning() => LibreHardwareMonitor.TryGetProcesses(out _);

        public static Boolean Run()
        {
            try
            {
                if (LibreHardwareMonitor.IsRunning())
                {
                    return true;
                }

                if (LibreHardwareMonitor.TryFindExecutableFilePath(out var executableFilePath))
                {
                    Process.Start(executableFilePath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error starting LibreHardwareMonitor");
                return false;
            }
        }

        public static Boolean Activate()
        {
            try
            {
                if (LibreHardwareMonitor.TryGetProcesses(out var processes))
                {
                    foreach (var process in processes)
                    {
                        NativeMethods.SetForegroundWindow(process.MainWindowHandle);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error activating LibreHardwareMonitor");
            }

            return false;
        }

        public static Boolean ActivateOrRun()
        {
            if (LibreHardwareMonitor.IsRunning())
            {
                return LibreHardwareMonitor.Activate();
            }
            else if (LibreHardwareMonitor.IsInstalled())
            {
                return LibreHardwareMonitor.Run();
            }
            else
            {
                return false;
            }
        }

        // static helpers

        private const String ExecutableFileName = "LibreHardwareMonitor.exe";
        private const String ProcessName = "LibreHardwareMonitor";

        private static Boolean TryGetProcesses(out Process[] processes)
        {
            try
            {
                processes = Process.GetProcessesByName(LibreHardwareMonitor.ProcessName);
                if (processes.Length > 0)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Error getting LibreHardwareMonitor process");
            }

            processes = null;
            return false;
        }

        private static Boolean TryFindExecutableFilePath(out String executableFilePath)
        {
            // First try to get the last used LibreHardwareMonitor installation
            // HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched

            try
            {
                using (var root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                {
                    using (var appSwitched = root.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage\AppSwitched"))
                    {
                        foreach (var appSwitchedFilePath in appSwitched.GetValueNames())
                        {
                            if (appSwitchedFilePath.EndsWith(LibreHardwareMonitor.ExecutableFileName, StringComparison.InvariantCultureIgnoreCase) && File.Exists(appSwitchedFilePath))
                            {
                                executableFilePath = appSwitchedFilePath;
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "Error finding LibreHardwareMonitor executable in Registry");
            }

            // Otherwise use the one that is distributed with plugin

            var assemblyPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath);
            executableFilePath = Path.Combine(assemblyPath, "LibreHardwareMonitor", "LibreHardwareMonitor.exe");

            if (File.Exists(executableFilePath))
            {
                return true;
            }

            PluginLog.Warning("Cannot find LibreHardwareMonitor executable");
            return false;
        }
    }
}
