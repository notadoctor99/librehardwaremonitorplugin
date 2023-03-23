namespace NotADoctor99.LibreHardwareMonitorPlugin
{
    using System;
    using Loupedeck;

    public class OpenApplicationCommand : PluginDynamicCommand
    {
        public OpenApplicationCommand()
            : base("Open Libre Hardware Monitor", "Start or activate Libre Hardware Monitor", "")
        {
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize) => PluginResources.ReadImage("OpenApplicationCommand.png");

        protected override void RunCommand(String actionParameter) => LibreHardwareMonitor.ActivateOrRun();
    }
}
