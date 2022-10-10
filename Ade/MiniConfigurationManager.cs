/*
 * Portions Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2022 SanteSuite Contributors (See NOTICE)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * DatERROR: 2021-8-27
 */
using SanteDB.BI.Services.Impl;
using SanteDB.Cdss.Xml;
using SanteDB.Core.Applets.Services.Impl;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Protocol;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Privacy;
using SanteDB.Core.Services.Impl;
using SanteDB.DisconnectedClient;
using SanteDB.DisconnectedClient.Ags;
using SanteDB.DisconnectedClient.Backup;
using SanteDB.DisconnectedClient.Caching;
using SanteDB.DisconnectedClient.Configuration;
using SanteDB.DisconnectedClient.Configuration.Data;
using SanteDB.DisconnectedClient.Diagnostics;
using SanteDB.DisconnectedClient.Http;
using SanteDB.DisconnectedClient.Net;
using SanteDB.DisconnectedClient.Rules;
using SanteDB.DisconnectedClient.Security;
using SanteDB.DisconnectedClient.Security.Remote;
using SanteDB.DisconnectedClient.Security.Session;
using SanteDB.DisconnectedClient.Services;
using SanteDB.DisconnectedClient.Services.Local;
using SanteDB.DisconnectedClient.Synchronization;
using SanteDB.DisconnectedClient.Tickler;
using SanteDB.DisconnectedClient.UI.Services;
using SharpCompress.Compressors.LZMA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;

namespace SanteDB.SDK.AppletDebugger
{
    /// <summary>
    /// Configuration manager
    /// </summary>
    internal class MiniConfigurationManager : IConfigurationPersister
    {
        private const int PROVIDER_RSA_FULL = 1;

        // Tracer
        private Tracer m_tracer;

        // Configuration path
        private readonly String m_configPath;

        // Instance name
        private string m_instanceName;

        /// <summary>
        /// Creates a new mini configuration manager
        /// </summary>
        public MiniConfigurationManager(String instanceName)
        {
            this.m_instanceName = instanceName;

            this.m_configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "santedb", "sdk", "ade", this.m_instanceName, "SanteDB.config");
        }

        /// <summary>
        /// Returns true if SanteDB is configured
        /// </summary>
        /// <value><c>true</c> if this instance is configured; otherwise, <c>false</c>.</value>
        public bool IsConfigured
        {
            get
            {
                return File.Exists(this.m_configPath);
            }
        }

        /// <summary>
        /// Get a bare bones configuration
        /// </summary>
        public SanteDBConfiguration GetDefaultConfiguration(String instanceName)
        {
            // TODO: Bring up initial settings dialog and utility
            var retVal = new SanteDBConfiguration();

            // Inital data source
            DcDataConfigurationSection dataSection = new DcDataConfigurationSection()
            {
                MainDataSourceConnectionStringName = "santeDbData",
                MessageQueueConnectionStringName = "santeDbQueue"
            };

            // Initial Applet configuration
            AppletConfigurationSection appletSection = new AppletConfigurationSection()
            {
                AppletDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "santedb", "sdk", "ade", this.m_instanceName, "applets"),
                StartupAsset = "org.santedb.uicore",
                Security = new AppletSecurityConfiguration()
                {
                    TrustedPublishers = new List<string>() { "82C63E1E9B87578D0727E871D7613F2F0FAF683B" }
                }
            };

            // Initial applet style
            ApplicationConfigurationSection appSection = new ApplicationConfigurationSection()
            {
                Style = StyleSchemeType.Dark,
                UserPrefDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "santedb", "sdk", "ade", this.m_instanceName, "userpref"),
                Cache = new CacheConfiguration()
                {
                    MaxAge = new TimeSpan(0, 5, 0).Ticks,
                    MaxSize = 1000,
                    MaxDirtyAge = new TimeSpan(0, 20, 0).Ticks,
                    MaxPressureAge = new TimeSpan(0, 2, 0).Ticks
                }
            };

            var appServiceSection = new ApplicationServiceContextConfigurationSection()
            {
                ThreadPoolSize = Environment.ProcessorCount * 16,
                ServiceProviders = new List<TypeReferenceConfiguration>() {
                    new TypeReferenceConfiguration(typeof(AesSymmetricCrypographicProvider)),
                    new TypeReferenceConfiguration(typeof(MemoryTickleService)),
                    new TypeReferenceConfiguration(typeof(NetworkInformationService)),
                    new TypeReferenceConfiguration(typeof(SHA256PasswordHasher)),
                    new TypeReferenceConfiguration(typeof(SanteDB.Core.Security.DefaultPolicyDecisionService)),
                    new TypeReferenceConfiguration(typeof(SanteDB.Caching.Memory.MemoryAdhocCacheService)),
                    new TypeReferenceConfiguration(typeof(AppletLocalizationService)),
                    new TypeReferenceConfiguration(typeof(BusinessRulesDaemonService)),
                    new TypeReferenceConfiguration(typeof(AgsService)),
                    new TypeReferenceConfiguration(typeof(SanteDB.Caching.Memory.MemoryCacheService)),
                    new TypeReferenceConfiguration(typeof(DefaultThreadPoolService)),
                    new TypeReferenceConfiguration(typeof(SimpleCarePlanService)),
                    new TypeReferenceConfiguration(typeof(MemorySessionManagerService)),
                    new TypeReferenceConfiguration(typeof(AmiUpdateManager)),
                    new TypeReferenceConfiguration(typeof(AppletClinicalProtocolRepository)),
                    new TypeReferenceConfiguration(typeof(MemoryQueryPersistenceService)),
                    new TypeReferenceConfiguration(typeof(SimpleQueueFileProvider)),
                    new TypeReferenceConfiguration(typeof(SimplePatchService)),
                    new TypeReferenceConfiguration(typeof(DefaultBackupService)),
                    new TypeReferenceConfiguration(typeof(AmiSecurityChallengeProvider)),
                    new TypeReferenceConfiguration(typeof(MiniAppletManagerService)),
                    new TypeReferenceConfiguration(typeof(AppletBiRepository)),
                    new TypeReferenceConfiguration(typeof(SHA256PasswordHasher)),
                    new TypeReferenceConfiguration(typeof(DataPolicyFilterService)),
                    new TypeReferenceConfiguration(typeof(DefaultOperatingSystemInfoService)),
                    new TypeReferenceConfiguration(typeof(AppletSubscriptionRepository)),
                    new TypeReferenceConfiguration(typeof(InMemoryPivotProvider)),
                    new TypeReferenceConfiguration(typeof(AuditDaemonService)),
                    new TypeReferenceConfiguration(typeof(DefaultDataSigningService)),
                    new TypeReferenceConfiguration(typeof(GenericConfigurationPushService)),
                    new TypeReferenceConfiguration(typeof(QrBarcodeGenerator)),
                    new TypeReferenceConfiguration(typeof(FileSystemDispatcherQueueService))
                },
                AppSettings = new List<AppSettingKeyValuePair>()
                {
                }
            };

            // Security configuration
            var wlan = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(o => o.NetworkInterfaceType == NetworkInterfaceType.Ethernet || o.Description.StartsWith("wlan"));
            String macAddress = Guid.NewGuid().ToString();
            if (wlan != null)
                macAddress = wlan.GetPhysicalAddress().ToString();
            //else

            SecurityConfigurationSection secSection = new SecurityConfigurationSection()
            {
                DeviceName = String.Format("Debugee-{0}", macAddress).Replace(" ", ""),
                AuditRetention = new TimeSpan(30, 0, 0, 0, 0),
                DomainAuthentication = DomainClientAuthentication.Inline
            };

            // Device key
            var certificate = X509CertificateUtils.FindCertificate(X509FindType.FindBySubjectName, StoreLocation.LocalMachine, StoreName.My, String.Format("DN={0}.mobile.santedb.org", macAddress));
            secSection.DeviceSecret = certificate?.Thumbprint;

            // Rest Client Configuration
            ServiceClientConfigurationSection serviceSection = new ServiceClientConfigurationSection()
            {
                RestClientType = typeof(RestClient)
            };

            // Trace writer
#if DEBUG
            DiagnosticsConfigurationSection diagSection = new DiagnosticsConfigurationSection()
            {
                TraceWriter = new System.Collections.Generic.List<TraceWriterConfiguration>() {
                    new TraceWriterConfiguration () {
                        Filter = System.Diagnostics.Tracing.EventLevel.LogAlways,
                        InitializationData = "SanteDB",
                        TraceWriter = typeof(LogTraceWriter)
                    },
                    new TraceWriterConfiguration() {
                        Filter = System.Diagnostics.Tracing.EventLevel.LogAlways,
                        InitializationData = "SanteDB",
                        TraceWriter = typeof(FileTraceWriter)
                    },
                    new TraceWriterConfiguration() {
                        Filter = System.Diagnostics.Tracing.EventLevel.LogAlways,
                        InitializationData = "SanteDB",
                        TraceWriter = typeof(ConsoleTraceWriter)
                    }
                }
            };
#else
            DiagnosticsConfigurationSection diagSection = new DiagnosticsConfigurationSection()
            {
                TraceWriter = new List<TraceWriterConfiguration>() {
                    new TraceWriterConfiguration () {
                        Filter = System.Diagnostics.Tracing.EventLevel.Informational,
                        InitializationData = "SanteDB",
                        TraceWriter = typeof(FileTraceWriter)
                    },
                    new TraceWriterConfiguration() {
                        Filter = System.Diagnostics.Tracing.EventLevel.Informational,
                        InitializationData = "SanteDB",
                        TraceWriter = typeof(ConsoleTraceWriter)
                    }
                }
            };
#endif
            retVal.Sections.Add(new FileSystemDispatcherQueueConfigurationSection()
            {
                QueuePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "santedb", "sdk", "ade", this.m_instanceName, "queue"),
            });
            retVal.Sections.Add(appServiceSection);
            retVal.Sections.Add(appletSection);
            retVal.Sections.Add(dataSection);
            retVal.Sections.Add(diagSection);
            retVal.Sections.Add(appSection);
            retVal.Sections.Add(secSection);
            retVal.Sections.Add(serviceSection);
            retVal.Sections.Add(new AuditAccountabilityConfigurationSection()
            {
                AuditFilters = new List<AuditFilterConfiguration>()
                {
                    // Audit any failure - No matter which event
                    new AuditFilterConfiguration(null, null, SanteDB.Core.Auditing.OutcomeIndicator.EpicFail | SanteDB.Core.Auditing.OutcomeIndicator.MinorFail | SanteDB.Core.Auditing.OutcomeIndicator.SeriousFail, true, true),
                    // Audit anything that creates, reads, or updates data
                    new AuditFilterConfiguration(SanteDB.Core.Auditing.ActionType.Create | SanteDB.Core.Auditing.ActionType.Read | SanteDB.Core.Auditing.ActionType.Update | SanteDB.Core.Auditing.ActionType.Delete, null, null, true, true)
                }
            });
            retVal.Sections.Add(AgsService.GetDefaultConfiguration());
            retVal.Sections.Add(new SynchronizationConfigurationSection()
            {
                PollInterval = new TimeSpan(0, 15, 0),
                ForbiddenResouces = new List<SynchronizationForbidConfiguration>()
                {
                    new SynchronizationForbidConfiguration(SynchronizationOperationType.All, "DeviceEntity"),
                    new SynchronizationForbidConfiguration(SynchronizationOperationType.All, "ApplicationEntity"),
                    new SynchronizationForbidConfiguration(SynchronizationOperationType.All, "Concept"),
                    new SynchronizationForbidConfiguration(SynchronizationOperationType.All, "ConceptSet"),
                    new SynchronizationForbidConfiguration(SynchronizationOperationType.All, "Place"),
                    new SynchronizationForbidConfiguration(SynchronizationOperationType.All, "ReferenceTerm"),
                    new SynchronizationForbidConfiguration(SynchronizationOperationType.All, "AssigningAuthority"),
                    new SynchronizationForbidConfiguration(SynchronizationOperationType.Obsolete, "UserEntity")
                }
            });

            var initConfig = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).SelectMany(a => a.ExportedTypes).Where(t => typeof(IInitialConfigurationProvider).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
            foreach (var t in initConfig.Distinct())
                retVal = (Activator.CreateInstance(t) as IInitialConfigurationProvider).Provide(retVal);

            return retVal;
        }

        /// <summary>
        /// Load the configuration
        /// </summary>
        public SanteDBConfiguration Load()
        {
            // Configuration exists?
            if (this.IsConfigured)
                using (var fs = File.OpenRead(this.m_configPath))
                {
                    return SanteDBConfiguration.Load(fs);
                }
            else
                return this.GetDefaultConfiguration(this.m_instanceName);
        }

        /// <summary>
        /// Application data directory
        /// </summary>
        public string ApplicationDataDirectory
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "santedb", "sdk", "ade", this.m_instanceName);
            }
        }

        /// <summary>
        /// Save the specified configuration
        /// </summary>
        /// <param name="config">Config.</param>
        public void Save(SanteDBConfiguration config)
        {
            try
            {
                this.m_tracer?.TraceInfo("Saving configuration to {0}...", this.m_configPath);
                if (!Directory.Exists(Path.GetDirectoryName(this.m_configPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(this.m_configPath));

                using (FileStream fs = File.Create(this.m_configPath))
                {
                    config.Save(fs);
                    fs.Flush();
                }
            }
            catch (Exception e)
            {
                this.m_tracer?.TraceError(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Backup the configuration
        /// </summary>
        public void Backup(SanteDBConfiguration configuration)
        {
            using (var lzs = new LZipStream(File.Create(Path.ChangeExtension(this.m_configPath, "bak.7z")), SharpCompress.Compressors.CompressionMode.Compress))
                configuration.Save(lzs);
        }

        /// <summary>
        /// True if the configuration has a backup
        /// </summary>
        public bool HasBackup()
        {
            return File.Exists(Path.ChangeExtension(this.m_configPath, "bak.7z"));
        }

        /// <summary>
        /// Restore the configuration
        /// </summary>
        public SanteDBConfiguration Restore()
        {
            using (var lzs = new LZipStream(File.OpenRead(Path.ChangeExtension(this.m_configPath, "bak.7z")), SharpCompress.Compressors.CompressionMode.Decompress))
            {
                var retVal = SanteDBConfiguration.Load(lzs);
                this.Save(retVal);
                ApplicationContext.Current?.ConfigurationManager.Reload();
                return retVal;
            }
        }
    }
}