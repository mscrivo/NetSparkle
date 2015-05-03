using System.Diagnostics;
using NetSparkle.Interfaces;

namespace NetSparkle
{
    /// <summary>
    ///     A diagnostic accessor
    /// </summary>
    public class NetSparkleAssemblyDiagnosticsAccessor : INetSparkleAssemblyAccessor
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="assemblyName">the assembly name</param>
        public NetSparkleAssemblyDiagnosticsAccessor(string assemblyName)
        {
            if (assemblyName != null)
            {
                AssemblyVersion = FileVersionInfo.GetVersionInfo(assemblyName).FileVersion;
                AssemblyProduct = FileVersionInfo.GetVersionInfo(assemblyName).ProductVersion;
                AssemblyTitle = FileVersionInfo.GetVersionInfo(assemblyName).ProductName;
                AssemblyCompany = FileVersionInfo.GetVersionInfo(assemblyName).CompanyName;
                AssemblyCopyright = FileVersionInfo.GetVersionInfo(assemblyName).LegalCopyright;
                AssemblyDescription = FileVersionInfo.GetVersionInfo(assemblyName).FileDescription;
            }
        }

        #region Assembly Attribute Accessors

        /// <summary>
        ///     Gets the Title
        /// </summary>
        public string AssemblyTitle { get; }

        /// <summary>
        ///     Gets the version
        /// </summary>
        public string AssemblyVersion { get; }

        /// <summary>
        ///     Gets the description
        /// </summary>
        public string AssemblyDescription { get; }

        /// <summary>
        ///     gets the product
        /// </summary>
        public string AssemblyProduct { get; }

        /// <summary>
        ///     Gets the copyright
        /// </summary>
        public string AssemblyCopyright { get; }

        /// <summary>
        ///     Gets the company
        /// </summary>
        public string AssemblyCompany { get; }

        #endregion
    }
}