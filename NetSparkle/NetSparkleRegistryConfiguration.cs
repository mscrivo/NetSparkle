using Microsoft.Win32;
using System;
using System.Globalization;

namespace NetSparkle
{
    /// <summary>
    ///     This class handles all registry values which are used from sparkle to handle
    ///     update intervalls. All values are stored in HKCU\Software\Vendor\AppName which
    ///     will be read ot from the assembly information. All values are of the REG_SZ
    ///     type, no matter what their "logical" type is. The following options are
    ///     available:
    ///     CheckForUpdate  - Boolean    - Whether NetSparkle should check for updates
    ///     LastCheckTime   - time_t     - Time of last check
    ///     SkipThisVersion - String     - If the user skipped an update, then the version to ignore is stored here (e.g.
    ///     "1.4.3")
    ///     DidRunOnce      - Boolean    - Check only one time when the app launched
    /// </summary>
    public class NetSparkleRegistryConfiguration : NetSparkleConfiguration
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="referenceAssembly">the name of hte reference assembly</param>
        /// <param name="isReflectionBasedAssemblyAccessorUsed"><c>true</c> if reflection is used to access the assembly.</param>
        public NetSparkleRegistryConfiguration(string? referenceAssembly, bool isReflectionBasedAssemblyAccessorUsed = true) : base(referenceAssembly, isReflectionBasedAssemblyAccessorUsed)
        {
            try
            {
                // build the reg path
                var regPath = BuildRegistryPath();

                // load the values
                LoadValuesFromPath(regPath);
            }
            catch (NetSparkleException)
            {
                // disable update checks when exception was called 
                CheckForUpdate = false;
                throw;
            }
        }

        /// <summary>
        ///     Touches to profile time
        /// </summary>
        public override void TouchProfileTime()
        {
            base.TouchProfileTime();
            // save the values
            SaveValuesToPath(BuildRegistryPath());
        }

        /// <summary>
        ///     Touches the check time to now, should be used after a check directly
        /// </summary>
        public override void TouchCheckTime()
        {
            base.TouchCheckTime();
            // save the values
            SaveValuesToPath(BuildRegistryPath());
        }

        /// <summary>
        ///     This method allows to skip a specific version
        /// </summary>
        /// <param name="version">the version to skeip</param>
        public override void SetVersionToSkip(string version)
        {
            base.SetVersionToSkip(version);
            SaveValuesToPath(BuildRegistryPath());
        }

        /// <summary>
        ///     Reloads the configuration object
        /// </summary>
        public override void Reload()
        {
            LoadValuesFromPath(BuildRegistryPath());
        }

        /// <summary>
        ///     This function build a valid registry path in dependecy to the
        ///     assembly information
        /// </summary>
        /// <returns></returns>
        private string BuildRegistryPath()
        {
            var accessor = new NetSparkleAssemblyAccessor(ReferenceAssembly, UseReflectionBasedAssemblyAccessor);

            if (string.IsNullOrEmpty(accessor.AssemblyCompany) || string.IsNullOrEmpty(accessor.AssemblyProduct))
            {
                throw new NetSparkleException("STOP: Sparkle is missing the company or productname tag in " + ReferenceAssembly);
            }

            return "Software\\" + accessor.AssemblyCompany + "\\" + accessor.AssemblyProduct + "\\AutoUpdate";
        }

        private static string ConvertDateToString(DateTime dt)
        {
            return dt.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
        }

        private static DateTime ConvertStringToDate(string str)
        {
            return DateTime.ParseExact(str, DateTimeFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     This method loads the values from registry
        /// </summary>
        /// <param name="regPath">the registry path</param>
        /// <returns><c>true</c> if the items were loaded</returns>
        private void LoadValuesFromPath(string regPath)
        {
            var key = Registry.CurrentUser.OpenSubKey(regPath);
            if (key == null)
            {
                return;
            }

            // read out                
            var strCheckForUpdate = key.GetValue("CheckForUpdate", "True") as string;
            var strLastCheckTime = key.GetValue("LastCheckTime", ConvertDateToString(new DateTime(0))) as string;
            var strSkipThisVersion = key.GetValue("SkipThisVersion", "") as string;
            var strDidRunOnc = key.GetValue("DidRunOnce", "False") as string;
            var strProfileTime = key.GetValue("LastProfileUpdate", ConvertDateToString(new DateTime(0))) as string;

            // convert the right datatypes
            CheckForUpdate = Convert.ToBoolean(strCheckForUpdate);
            try
            {
                LastCheckTime =
                    ConvertStringToDate(strLastCheckTime ?? new DateTime(0).ToString(CultureInfo.InvariantCulture));
            }
            catch (FormatException)
            {
                LastCheckTime = new DateTime(0);
            }

            SkipThisVersion = strSkipThisVersion;
            DidRunOnce = Convert.ToBoolean(strDidRunOnc);
            try
            {
                LastProfileUpdate = ConvertStringToDate(strProfileTime ?? new DateTime(0).ToString(CultureInfo.InvariantCulture));
            }
            catch (FormatException)
            {
                LastProfileUpdate = new DateTime(0);
            }
        }

        /// <summary>
        ///     This method store the information into registry
        /// </summary>
        /// <param name="regPath">the registry path</param>
        /// <returns><c>true</c> if the values were saved to the registry</returns>
        private void SaveValuesToPath(string regPath)
        {
            var key = Registry.CurrentUser.CreateSubKey(regPath);
            if (key == null)
            {
                return;
            }

            // convert to regsz
            var strCheckForUpdate = CheckForUpdate.ToString();
            var strLastCheckTime = ConvertDateToString(LastCheckTime);
            var strSkipThisVersion = SkipThisVersion;
            var strDidRunOnc = DidRunOnce.ToString();
            var strProfileTime = ConvertDateToString(LastProfileUpdate);

            // set the values
            key.SetValue("CheckForUpdate", strCheckForUpdate, RegistryValueKind.String);
            key.SetValue("LastCheckTime", strLastCheckTime, RegistryValueKind.String);
            if (strSkipThisVersion != null)
            {
                key.SetValue("SkipThisVersion", strSkipThisVersion, RegistryValueKind.String);
            }

            key.SetValue("DidRunOnce", strDidRunOnc, RegistryValueKind.String);
            key.SetValue("LastProfileUpdate", strProfileTime, RegistryValueKind.String);
        }
    }
}
