using System;
using System.Net.Http;
using System.Xml;

namespace NetSparkle
{
    /// <summary>
    ///     An app-cast
    /// </summary>
    public class NetSparkleAppCast
    {
        private const string ItemNode = "item";
        private const string EnclosureNode = "enclosure";
        private const string ReleaseNotesLinkNode = "sparkle:releaseNotesLink";
        private const string VersionAttribute = "sparkle:version";
        private const string DeltaFromAttribute = "sparkle:deltaFrom";
        private const string DasSignature = "sparkle:dsaSignature";
        private const string UrlAttribute = "url";
        private readonly string _castUrl;
        private readonly NetSparkleConfiguration _config;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="castUrl">the URL of the appcast file</param>
        /// <param name="config">the current configuration</param>
        public NetSparkleAppCast(string castUrl, NetSparkleConfiguration config)
        {
            _config = config;
            _castUrl = castUrl;
        }

        /// <summary>
        ///     Gets the latest version
        /// </summary>
        /// <returns>the AppCast item corresponding to the latest version</returns>
        public NetSparkleAppCastItem? GetLatestVersion()
        {
            NetSparkleAppCastItem? latestVersion;

            if (_castUrl.StartsWith("file://")) //handy for testing
            {
                var path = _castUrl.Replace("file://", "");
                using var reader = XmlReader.Create(path);
                latestVersion = ReadAppCast(reader, null, _config.InstalledVersion);
            }
            else
            {
                // build a http web request stream
                var client = new HttpClient();

                var webRequest = new HttpRequestMessage(HttpMethod.Get, _castUrl);
                var response = client.Send(webRequest);

                using var reader = new XmlTextReader(response.Content.ReadAsStream());
                latestVersion = ReadAppCast(reader, null, _config.InstalledVersion);
            }

            if (latestVersion == null)
            {
                return null;
            }

            latestVersion.AppName = _config.ApplicationName;
            latestVersion.AppVersionInstalled = _config.InstalledVersion;
            return latestVersion;

        }

        private static NetSparkleAppCastItem? ReadAppCast(XmlReader reader,
            NetSparkleAppCastItem? latestVersion,
            string installedVersion)
        {
            NetSparkleAppCastItem currentItem = null;

            // The fourth segment of the version number is ignored by Windows Installer:
            var installedVersionV = new Version(installedVersion);
            var installedVersionWithoutFourthSegment = new Version(installedVersionV.Major, installedVersionV.Minor,
                installedVersionV.Build);

            while (reader.Read())
            {
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (reader.Name)
                        {
                            case ItemNode:
                                {
                                    currentItem = new NetSparkleAppCastItem();
                                    break;
                                }
                            case ReleaseNotesLinkNode:
                                {
                                    if (currentItem != null)
                                    {
                                        currentItem.ReleaseNotesLink = reader.ReadString().Trim();
                                    }

                                    break;
                                }
                            case EnclosureNode:
                                {
                                    var deltaFrom = reader.GetAttribute(DeltaFromAttribute);
                                    if (deltaFrom == null || deltaFrom == installedVersionWithoutFourthSegment.ToString())
                                    {
                                        if (currentItem != null)
                                        {
                                            currentItem.Version = reader.GetAttribute(VersionAttribute);
                                            currentItem.DownloadLink = reader.GetAttribute(UrlAttribute);
                                            currentItem.DSASignature = reader.GetAttribute(DasSignature);
                                        }
                                    }
                                    break;
                                }
                        }

                        break;
                    case XmlNodeType.EndElement:
                        switch (reader.Name)
                        {
                            case ItemNode:
                                {
                                    if (latestVersion == null)
                                    {
                                        latestVersion = currentItem;
                                    }
                                    else if (currentItem != null && currentItem.CompareTo(latestVersion) > 0)
                                    {
                                        latestVersion = currentItem;
                                    }
                                    break;
                                }
                        }

                        break;
                }
            }
            return latestVersion;
        }
    }
}
