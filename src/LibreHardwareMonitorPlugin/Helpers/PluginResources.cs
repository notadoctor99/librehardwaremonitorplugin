namespace NotADoctor99.LibreHardwareMonitorPlugin
{
    using System;
    using System.IO;
    using System.Reflection;

    using Loupedeck;

    internal static class PluginResources
    {
        private static Assembly _assembly;

        public static void Init(Assembly assembly)
        {
            assembly.CheckNullArgument(nameof(assembly));
            PluginResources._assembly = assembly;
        }

        public static String[] GetFilesInFolder(String folderName) => PluginResources._assembly.GetFilesInFolder(folderName);

        public static String FindFile(String fileName) => PluginResources._assembly.FindFile(fileName);

        public static String[] FindFiles(String regexPattern) => PluginResources._assembly.FindFiles(regexPattern);

        public static Stream GetStream(String resourceName) => PluginResources._assembly.GetStream(PluginResources.FindFile(resourceName));

        public static String ReadTextFile(String resourceName) => PluginResources._assembly.ReadTextFile(PluginResources.FindFile(resourceName));

        public static Byte[] ReadBinaryFile(String resourceName) => PluginResources._assembly.ReadBinaryFile(PluginResources.FindFile(resourceName));

        public static BitmapImage ReadImage(String resourceName) => PluginResources._assembly.ReadImage(PluginResources.FindFile(resourceName));

        public static void ExtractFile(String resourceName, String filePathName) => PluginResources._assembly.ExtractFile(PluginResources.FindFile(resourceName), filePathName);
    }
}
