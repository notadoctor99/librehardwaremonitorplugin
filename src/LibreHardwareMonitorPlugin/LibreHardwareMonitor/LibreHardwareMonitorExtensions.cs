namespace NotADoctor99.LibreHardwareMonitorPlugin
{
    using System;
    using System.Linq;
    using System.Management;

    public static class LibreHardwareMonitorExtensions
    {
        public static String GetHardwareType(this ManagementBaseObject managementBaseObject) => (String)managementBaseObject["HardwareType"];

        public static String GetIdentifier(this ManagementBaseObject managementBaseObject) => (String)managementBaseObject["Identifier"];

        public static String GetInstanceId(this ManagementBaseObject managementBaseObject) => (String)managementBaseObject["InstanceId"];

        public static String GetDisplayName(this ManagementBaseObject managementBaseObject) => (String)managementBaseObject["Name"];

        public static String GetProcessId(this ManagementBaseObject managementBaseObject) => (String)managementBaseObject["ProcessId"];

        public static Single GetValue(this ManagementBaseObject managementBaseObject) => (Single)managementBaseObject["Value"];

        public static Boolean HasSameItemsAs<T>(this T[] array1, T[] array2)
        {
            if ((null == array1) && (null == array2))
            {
                return true;
            }

            if ((null == array1) || (null == array2))
            {
                return false;
            }

            if (array1.Count() != array2.Count())
            {
                return false;
            }

            return !array1.Except(array2).Any() && !array2.Except(array1).Any();
        }
    }
}
