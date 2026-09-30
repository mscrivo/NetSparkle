using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace NetSparkle;

/// <summary>
///     Verifies the Authenticode signature of a downloaded update before it is allowed to run.
/// </summary>
public static class AuthenticodeVerifier
{
    private const string CommonNameOid = "2.5.4.3";
    private const string OrganizationOid = "2.5.4.10";

    /// <summary>
    ///     Returns true when <paramref name="filePath" /> has a valid, trusted Authenticode signature and, if
    ///     <paramref name="expectedPublisher" /> is given, the signing certificate was issued to that publisher.
    /// </summary>
    /// <param name="filePath">the file to verify</param>
    /// <param name="expectedPublisher">
    ///     the required common name and organization of the signing certificate, or null to accept any trusted signer
    /// </param>
    public static bool IsTrusted(string filePath, string? expectedPublisher)
    {
        if (!HasValidSignature(filePath))
        {
            return false;
        }

        if (expectedPublisher == null)
        {
            return true;
        }

        try
        {
            // WinVerifyTrust has already validated the signature; this only reads the signer to check who it is.
            // X509CertificateLoader cannot read Authenticode signatures, so the obsolete API is still required.
#pragma warning disable SYSLIB0057
            using var signer = X509Certificate.CreateFromSignedFile(filePath);
#pragma warning restore SYSLIB0057
            using var certificate = new X509Certificate2(signer);
            return IsIssuedTo(certificate.SubjectName, expectedPublisher);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    /// <summary>
    ///     Returns true when both the common name and the organization of <paramref name="subject" /> are exactly
    ///     <paramref name="publisher" />.
    /// </summary>
    public static bool IsIssuedTo(X500DistinguishedName subject, string publisher)
    {
        var values = subject.EnumerateRelativeDistinguishedNames()
            .Where(rdn => !rdn.HasMultipleElements)
            .Select(rdn => (Oid: rdn.GetSingleElementType().Value, Value: rdn.GetSingleElementValue()))
            .ToList();

        return IsOnly(values, CommonNameOid, publisher) && IsOnly(values, OrganizationOid, publisher);
    }

    private static bool IsOnly(List<(string? Oid, string? Value)> values, string oid,
        string expected)
    {
        var matches = values.Where(v => v.Oid == oid).ToList();
        return matches.Count == 1 && string.Equals(matches[0].Value, expected, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Returns true when <paramref name="filePath" /> has an embedded Authenticode signature that chains to a
    ///     trusted root and whose certificates have not been revoked.
    /// </summary>
    public static bool HasValidSignature(string filePath)
    {
        var filePathPtr = Marshal.StringToCoTaskMemUni(filePath);
        var fileInfoPtr = IntPtr.Zero;
        try
        {
            var fileInfo = new WinTrustFileInfo
            {
                cbStruct = (uint)Marshal.SizeOf<WinTrustFileInfo>(),
                pcwszFilePath = filePathPtr
            };
            fileInfoPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf<WinTrustFileInfo>());
            Marshal.StructureToPtr(fileInfo, fileInfoPtr, false);

            var trustData = new WinTrustData
            {
                cbStruct = (uint)Marshal.SizeOf<WinTrustData>(),
                dwUIChoice = WtdUiNone,
                fdwRevocationChecks = WtdRevokeWholeChain,
                dwUnionChoice = WtdChoiceFile,
                pFile = fileInfoPtr,
                dwStateAction = WtdStateActionIgnore,
                dwProvFlags = WtdRevocationCheckChainExcludeRoot
            };

            var action = WinTrustActionGenericVerifyV2;
            return WinVerifyTrust(new IntPtr(-1), ref action, ref trustData) == 0;
        }
        finally
        {
            if (fileInfoPtr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(fileInfoPtr);
            }

            Marshal.FreeCoTaskMem(filePathPtr);
        }
    }

    #region WinVerifyTrust interop

    private static readonly Guid WinTrustActionGenericVerifyV2 = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

    private const uint WtdUiNone = 2;
    private const uint WtdRevokeWholeChain = 1;
    private const uint WtdChoiceFile = 1;
    private const uint WtdStateActionIgnore = 0;
    private const uint WtdRevocationCheckChainExcludeRoot = 0x80;

    [DllImport("wintrust.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int WinVerifyTrust(IntPtr hwnd, ref Guid pgActionId, ref WinTrustData pWvtData);

    [StructLayout(LayoutKind.Sequential)]
    private struct WinTrustFileInfo
    {
        public uint cbStruct;
        public IntPtr pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WinTrustData
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPClientData;
        public uint dwUIChoice;
        public uint fdwRevocationChecks;
        public uint dwUnionChoice;
        public IntPtr pFile;
        public uint dwStateAction;
        public IntPtr hWVTStateData;
        public IntPtr pwszURLReference;
        public uint dwProvFlags;
        public uint dwUIContext;
        public IntPtr pSignatureSettings;
    }

    #endregion
}
