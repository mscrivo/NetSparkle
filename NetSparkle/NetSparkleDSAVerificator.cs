using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace NetSparkle
{
    /// <summary>
    ///     Class to verify a DSA signature
    /// </summary>
    public sealed class NetSparkleDSAVerificator : IDisposable
    {
        private readonly DSACryptoServiceProvider _provider;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="publicKey">the public key</param>
        public NetSparkleDSAVerificator(string publicKey)
        {
            // 1. try to load this from resource
            var data = TryGetResourceStream(publicKey) ?? TryGetFileResource(publicKey);

            // 2. check the resource
            if (data == null)
                throw new Exception("Couldn't find public key for verification");

            // 3. read out the key value
            using var reader = new StreamReader(data);
            var key = reader.ReadToEnd();
            _provider = new DSACryptoServiceProvider();
            _provider.FromXmlString(key);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        ///     Determines if a public key exists in this
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public static bool ExistsPublicKey(string publicKey)
        {
            // 1. try to load this from resource
            var data = TryGetResourceStream(publicKey) ?? TryGetFileResource(publicKey);

            // 2. check the resource
            return data != null;
        }

        /// <summary>
        ///     Verifies the DSA signature
        /// </summary>
        /// <param name="signature">expected signature</param>
        /// <param name="binaryPath">the path to the binary</param>
        /// <returns><c>true</c> if the signature matches the expected signature.</returns>
        public bool VerifyDSASignature(string signature, string binaryPath)
        {
            if (_provider == null)
                return false;

            // convert signature
            var bHash = Convert.FromBase64String(signature);

            // read the data
            byte[] bData;
            using (Stream inputStream = File.OpenRead(binaryPath))
            {
                bData = new byte[inputStream.Length];
                inputStream.Read(bData, 0, bData.Length);
            }

            // verify
            return _provider.VerifyData(bData, bHash);
        }

        /// <summary>
        ///     Gets a file resource
        /// </summary>
        /// <param name="publicKey">the public key</param>
        /// <returns>the data stream</returns>
        private static Stream TryGetFileResource(string publicKey)
        {
            if (File.Exists(publicKey))
            {
                return File.OpenRead(publicKey);
            }
            return null;
        }

        /// <summary>
        ///     Get a resource stream
        /// </summary>
        /// <param name="publicKey">the public key</param>
        /// <returns>a stream</returns>
        private static Stream TryGetResourceStream(string publicKey)
        {
            Stream data = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string[] resources;
                try
                {
                    resources = asm.GetManifestResourceNames();
                }
                catch (NotSupportedException)
                {
                    Debug.WriteLine("Skipped assembly {0}", asm.FullName);
                    continue;
                }
                var resourceName = resources.FirstOrDefault(s => s.IndexOf(publicKey, StringComparison.OrdinalIgnoreCase) > -1);
                if (!string.IsNullOrEmpty(resourceName))
                {
                    data = asm.GetManifestResourceStream(resourceName);
                    if (data != null)
                        break;
                }
            }
            return data;
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                _provider.Dispose();
            }

            // free native resources if there are any.
        }
    }
}