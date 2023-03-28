namespace NotADoctor99.LibreHardwareMonitorPlugin
{
    using System;
    using Loupedeck;

    public class LibreHardwareMonitorPlugin : Plugin
    {
        public override Boolean UsesApplicationApiOnly => true;

        public override Boolean HasNoApplication => true;

        public static LibreHardwareMonitor HardwareMonitor { get; } = new LibreHardwareMonitor();

        public LibreHardwareMonitorPlugin()
        {
            PluginLog.Init(this.Log);
            PluginResources.Init(this.Assembly);
        }

        public override void Load()
        {
            if (!LibreHardwareMonitor.IsInstalled())
            {
                this.OnPluginStatusChanged(Loupedeck.PluginStatus.Error, "Libre Hardware Monitor is not installed", "https://github.com/notadoctor99/librehardwaremonitorplugin/wiki/Install-Libre-Hardware-Monitor", "Click here to install");
            }
            else if (!LibreHardwareMonitor.IsRunning())
            {
                this.ReportNotRunning();
            }
            else
            {
                this.ReportNormalOperation();
            }

            LibreHardwareMonitorPlugin.HardwareMonitor.ProcessStarted += this.OnProcessStarted;
            LibreHardwareMonitorPlugin.HardwareMonitor.ProcessExited += this.OnProcessExited;

            LibreHardwareMonitorPlugin.HardwareMonitor.StartMonitoring();

            this.ServiceEvents.UrlCallbackReceived += this.OnUrlCallbackReceived;
        }

        public override void Unload()
        {
            this.ServiceEvents.UrlCallbackReceived -= this.OnUrlCallbackReceived;

            LibreHardwareMonitorPlugin.HardwareMonitor.StopMonitoring();

            LibreHardwareMonitorPlugin.HardwareMonitor.ProcessStarted -= this.OnProcessStarted;
            LibreHardwareMonitorPlugin.HardwareMonitor.ProcessExited -= this.OnProcessExited;
        }

        private void OnProcessStarted(Object sender, EventArgs e) => this.ReportNormalOperation();

        private void OnProcessExited(Object sender, EventArgs e) => this.ReportNotRunning();

        private void ReportNotRunning() => this.OnPluginStatusChanged(Loupedeck.PluginStatus.Error, "Libre Hardware Monitor is not running", "loupedeck://plugin/LibreHardwareMonitor/callback/start", "Click here to start");

        private void ReportNormalOperation() => this.OnPluginStatusChanged(Loupedeck.PluginStatus.Normal, "", null, null);

        private void OnUrlCallbackReceived(Object sender, UrlCallbackReceivedEventArgs e)
        {
            if (e.PathAndQuery.EqualsNoCase("start"))
            {
                LibreHardwareMonitor.ActivateOrRun();
            }
        }
    }
}
