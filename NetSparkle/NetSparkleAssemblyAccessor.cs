﻿using NetSparkle.Interfaces;

namespace NetSparkle;

/// <summary>
///     An assembly accessor
/// </summary>
public class NetSparkleAssemblyAccessor : INetSparkleAssemblyAccessor
{
    private readonly INetSparkleAssemblyAccessor _internalAccessor;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="assemblyName">the assembly name</param>
    /// <param name="isReflectionAccessorUsed"><c>true</c> if reflection is used to access the attributes.</param>
    public NetSparkleAssemblyAccessor(string? assemblyName, bool isReflectionAccessorUsed)
    {
        if (isReflectionAccessorUsed)
        {
            _internalAccessor = new NetSparkleAssemblyReflectionAccessor(assemblyName);
        }
        else
        {
            _internalAccessor = new NetSparkleAssemblyDiagnosticsAccessor(assemblyName);
        }
    }

    #region INetSparkleAssemblyAccessor Members

    /// <summary>
    ///     Gets the company
    /// </summary>
    public string AssemblyCompany => _internalAccessor.AssemblyCompany;

    /// <summary>
    ///     Gets the copyright
    /// </summary>
    public string AssemblyCopyright => _internalAccessor.AssemblyCopyright;

    /// <summary>
    ///     Gets the description
    /// </summary>
    public string AssemblyDescription => _internalAccessor.AssemblyDescription;

    /// <summary>
    ///     Gets the product
    /// </summary>
    public string AssemblyProduct => _internalAccessor.AssemblyProduct;

    /// <summary>
    ///     Gets the title
    /// </summary>
    public string AssemblyTitle => _internalAccessor.AssemblyTitle;

    /// <summary>
    ///     Gets the version
    /// </summary>
    public string AssemblyVersion => _internalAccessor.AssemblyVersion;

    #endregion
}