namespace NotADoctor99.LibreHardwareMonitorPlugin
{
    using System;
    using System.Diagnostics;
    using System.Management;

    class Program
    {
        static void Main(String[] _)
        {
            try
            {
                Console.WriteLine($"Is installed: {LibreHardwareMonitor.IsInstalled()}");
                Console.WriteLine($"Is running: {LibreHardwareMonitor.IsRunning()}");

                if (LibreHardwareMonitor.IsRunning())
                {
                    LibreHardwareMonitor.Activate();
                }
                else if (LibreHardwareMonitor.IsInstalled())
                {
                    Console.WriteLine("Start application [Y/N]?");
                    if (Console.ReadKey(true).Key == ConsoleKey.Y)
                    {
                        LibreHardwareMonitor.Run();
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("Not installed");
                    return;
                }

                Console.WriteLine();

                var hardwareMonitor = new LibreHardwareMonitor();

                hardwareMonitor.ProcessStarted += OnProcessStarted;
                hardwareMonitor.ProcessExited += OnProcessExited;
                hardwareMonitor.SensorListChanged += OnSensorListChanged;
                hardwareMonitor.SensorValuesChanged += OnSensorValuesChanged;

                hardwareMonitor.StartMonitoring();

                var scope = new ManagementScope(@"root\LibreHardwareMonitor");

                var query = new WqlEventQuery("__InstanceCreationEvent", new TimeSpan(0, 0, 1), "TargetInstance isa \"Sensor\"");
                var watcher = new ManagementEventWatcher(scope, query);
                watcher.EventArrived += OnCreationEventArrived;
                watcher.Start();

                Console.WriteLine("Monitoring started");
                Console.WriteLine("Press any key to exit");
                Console.ReadKey(true);

                hardwareMonitor.StopMonitoring();

                watcher.Stop();
                watcher.EventArrived -= OnCreationEventArrived;
                watcher.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void OnProcessStarted(Object sender, EventArgs e) => Console.WriteLine($"--- {Now()} ---  ProcessStarted");

        private static void OnProcessExited(Object sender, EventArgs e) => Console.WriteLine($"--- {Now()} ---  ProcessExited");

        private static void OnSensorListChanged(Object sender, EventArgs e)
        {
            Console.WriteLine($"--- {Now()} ---  Monitoring started");
            if (sender is LibreHardwareMonitor hardwareMonitor)
            {
                Console.WriteLine($"{hardwareMonitor.Sensors.Count} sensors found:");

                foreach (var sensor in hardwareMonitor.Sensors)
                {
                    Console.WriteLine($"{sensor.InstanceId} '{sensor.Identifier}' {sensor.Value} = '{sensor.GetButtonText()}'");
                }
            }
        }

        private static void OnSensorValuesChanged(Object sender, LibreHardwareMonitorSensorValuesChangedEventArgs e)
        {
            if (sender is LibreHardwareMonitor hardwareMonitor)
            {
                Console.WriteLine($"--- {Now()} --- SensorValuesChanged:");
                foreach (var sensorName in e.SensorNames)
                {
                    Console.WriteLine($"\t{sensorName} = {(hardwareMonitor.TryGetSensor(sensorName, out var sensor) ? sensor.Value.ToString("N1") : "NOT FOUND")}");
                }
            }
        }

        private static void OnCreationEventArrived(Object sender, EventArrivedEventArgs e)
        {
            var targetInstance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
            var instanceId = targetInstance.GetInstanceId();
            var identifier = targetInstance.GetIdentifier();
            Console.WriteLine($"--- {Now()} --- CREATED '{instanceId}-{identifier}'");
        }

        private static String Now() => $"{DateTime.Now:hh:mm:ss.fff}";
    }
}
