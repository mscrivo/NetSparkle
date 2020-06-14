﻿using System;

namespace NetSparkle
{
    /// <summary>
    ///     Abstract class to handle
    ///     update intervals.
    ///     CheckForUpdate  - Boolean    - Whether NetSparkle should check for updates
    ///     LastCheckTime   - time_t     - Time of last check
    ///     SkipThisVersion - String     - If the user skipped an update, then the version to ignore is stored here (e.g.
    ///     "1.4.3")
    ///     DidRunOnce      - Boolean    - Check only one time when the app launched
    /// </summary>
    public abstract class NetSparkleConfiguration
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="referenceAssembly">the name of hte reference assembly</param>
        /// <param name="isReflectionBasedAssemblyAccessorUsed"><c>true</c> if reflection is used to access the assembly.</param>
        protected NetSparkleConfiguration(string? referenceAssembly, bool isReflectionBasedAssemblyAccessorUsed = true)
        {
            // set the value
            UseReflectionBasedAssemblyAccessor = isReflectionBasedAssemblyAccessorUsed;

            // save the reference assembly
            ReferenceAssembly = referenceAssembly;

            // set default values
            InitWithDefaultValues();

            try
            {
                // set some value from the binary
                var accessor = new NetSparkleAssemblyAccessor(referenceAssembly, UseReflectionBasedAssemblyAccessor);
                ApplicationName = accessor.AssemblyProduct;
                InstalledVersion = accessor.AssemblyVersion;
            }
            catch
            {
                CheckForUpdate = false;
                throw;
            }
        }

        /// <summary>
        ///     The application name
        /// </summary>
        public string ApplicationName { get; protected set; }

        /// <summary>
        ///     The currently-installed version
        /// </summary>
        public string InstalledVersion { get; protected set; }

        /// <summary>
        ///     Flag to indicate if we should check for updates
        /// </summary>
        public bool CheckForUpdate { get; protected set; }

        /// <summary>
        ///     Last check time
        /// </summary>
        public DateTime LastCheckTime { get; protected set; }

        /// <summary>
        ///     The last-skipped version number
        /// </summary>
        public string? SkipThisVersion { get; protected set; }

        /// <summary>
        ///     The application ran once
        /// </summary>
        public bool DidRunOnce { get; protected set; }

        /// <summary>
        ///     Last profile update
        /// </summary>
        public DateTime LastProfileUpdate { get; protected set; }

        /// <summary>
        ///     If this property is true a reflection based accessor will be used
        ///     to determine the assmebly name and verison, otherwise a System.Diagnostics
        ///     based access will be used
        /// </summary>
        public bool UseReflectionBasedAssemblyAccessor { get; protected set; }

        /// <summary>
        ///     The reference assembly name
        /// </summary>
        protected string? ReferenceAssembly { get; set; }

        /// <summary>
        ///     Touches to profile time
        /// </summary>
        public virtual void TouchProfileTime()
        {
            LastProfileUpdate = DateTime.Now;
        }

        /// <summary>
        ///     Touches the check time to now, should be used after a check directly
        /// </summary>
        public virtual void TouchCheckTime()
        {
            LastCheckTime = DateTime.Now;
        }

        /// <summary>
        ///     This method allows to skip a specific version
        /// </summary>
        /// <param name="version">the version to skeip</param>
        public virtual void SetVersionToSkip(string version)
        {
            SkipThisVersion = version;
        }

        /// <summary>
        ///     Reloads the configuration object
        /// </summary>
        public abstract void Reload();

        /// <summary>
        ///     This method set's default values for the config
        /// </summary>
        protected void InitWithDefaultValues()
        {
            CheckForUpdate = true;
            LastCheckTime = new DateTime(0);
            SkipThisVersion = string.Empty;
            DidRunOnce = false;
            UseReflectionBasedAssemblyAccessor = true;
        }
    }
}
