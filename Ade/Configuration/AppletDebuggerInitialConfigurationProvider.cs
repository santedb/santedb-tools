using SanteDB.BI.Services.Impl;
using SanteDB.BusinessRules.JavaScript;
using SanteDB.Caching.Memory;
using SanteDB.Caching.Memory.Session;
using SanteDB.Cdss.Xml;
using SanteDB.Client.Configuration;
using SanteDB.Client.Tickles;
using SanteDB.Client.Upstream;
using SanteDB.Core.Applets.Configuration;
using SanteDB.Core.Applets.Services.Impl;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Http;
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Core.Diagnostics.Tracing;
using SanteDB.Core.Http;
using SanteDB.Core.Model.Audit;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Protocol;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Security.Privacy;
using SanteDB.Core.Services.Impl;
using SanteDB.Disconnected.Data.Synchronization.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Data.Backup;
using SanteDB.Tools.Debug.Services;

namespace SanteDB.SDK.AppletDebugger.Configuration
{
    /// <summary>
    /// Applet debugger initial configuration provider
    /// </summary>
    public class AppletDebuggerInitialConfigurationProvider : IInitialConfigurationProvider
    {
        /// <summary>
        /// Provide the default configuration
        /// </summary>
        public SanteDBConfiguration Provide(SanteDBConfiguration configuration)
        {


            var appServiceSection = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
            var instanceName = appServiceSection.InstanceName;
            var localDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "santedb", "sdk", "ade");

            appServiceSection.ServiceProviders = new List<TypeReferenceConfiguration>() {
                    new TypeReferenceConfiguration(typeof(AesSymmetricCrypographicProvider)),
                    new TypeReferenceConfiguration(typeof(InMemoryTickleService)),
                    new TypeReferenceConfiguration(typeof(DefaultNetworkInformationService)),
                    new TypeReferenceConfiguration(typeof(SHA256PasswordHashingService)),
                    new TypeReferenceConfiguration(typeof(DefaultPolicyDecisionService)),
                    new TypeReferenceConfiguration(typeof(MemoryAdhocCacheService)),
                    new TypeReferenceConfiguration(typeof(AppletLocalizationService)),
                    new TypeReferenceConfiguration(typeof(AppletBusinessRulesDaemon)),
                    new TypeReferenceConfiguration(typeof(MemoryCacheService)),
                    new TypeReferenceConfiguration(typeof(DefaultThreadPoolService)),
                    new TypeReferenceConfiguration(typeof(SimpleCarePlanService)),
                    new TypeReferenceConfiguration(typeof(MemorySessionManagerService)),
                    new TypeReferenceConfiguration(typeof(RemoteUpdateManager)), // AmiUpdateManager
                    new TypeReferenceConfiguration(typeof(AppletClinicalProtocolInstaller)),
                    new TypeReferenceConfiguration(typeof(MemoryQueryPersistenceService)),
                    new TypeReferenceConfiguration(typeof(FileSystemDispatcherQueueService)),
                    new TypeReferenceConfiguration(typeof(SimplePatchService)),
                    new TypeReferenceConfiguration(typeof(DefaultBackupManager)),
                    new TypeReferenceConfiguration(typeof(RemoteSecurityChallengeProvider)), // AmiSecurityChallengeProvider
                    new TypeReferenceConfiguration(typeof(DebugAppletManagerService)),
                    new TypeReferenceConfiguration(typeof(AppletBiRepository)),
                    new TypeReferenceConfiguration(typeof(DataPolicyFilterService)),
                    new TypeReferenceConfiguration(typeof(DefaultOperatingSystemInfoService)),
                    new TypeReferenceConfiguration(typeof(AppletSubscriptionRepository)),
                    new TypeReferenceConfiguration(typeof(InMemoryPivotProvider)),
                    new TypeReferenceConfiguration(typeof(AuditDaemonService)),
                    new TypeReferenceConfiguration(typeof(DefaultDataSigningService)),
                    new TypeReferenceConfiguration(typeof(DefaultBarcodeProviderService)),
                    new TypeReferenceConfiguration(typeof(FileSystemDispatcherQueueService))
               
            };

            // Security configuration
            var wlan = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(o => o.NetworkInterfaceType == NetworkInterfaceType.Ethernet || o.Description.StartsWith("wlan"));
            String macAddress = Guid.NewGuid().ToString();
            if (wlan != null)
            {
                macAddress = wlan.GetPhysicalAddress().ToString();
            }
            //else

            // Upstream default configuration
            UpstreamConfigurationSection upstreamConfiguration = new UpstreamConfigurationSection()
            {
                Credentials = new List<UpstreamCredentialConfiguration>()
                {
                    new UpstreamCredentialConfiguration()
                    {
                        CredentialName = $"Debugee-{macAddress.Replace(" ", "")}",
                        Conveyance = UpstreamCredentialConveyance.Secret,
                        CredentialType = UpstreamCredentialType.Device,
                        CredentialSecret = Guid.NewGuid().ToByteArray().HexEncode()
                    }
                }
            };

            // Rest Client Configuration
            RestClientConfigurationSection serviceSection = new RestClientConfigurationSection()
            {
                RestClientType = new TypeReferenceConfiguration(typeof(RestClient))
            };

            // Trace writer
#if DEBUG
            DiagnosticsConfigurationSection diagSection = new DiagnosticsConfigurationSection()
            {
                TraceWriter = new System.Collections.Generic.List<TraceWriterConfiguration>() {
                    new TraceWriterConfiguration () {
                        Filter = System.Diagnostics.Tracing.EventLevel.LogAlways,
                        InitializationData = "santedb",
                        TraceWriter = typeof(DebugDiagnosticsTraceWriter)
                    },
                    new TraceWriterConfiguration() {
                        Filter = System.Diagnostics.Tracing.EventLevel.LogAlways,
                        InitializationData = Path.Combine(localDataPath, "log", "santedb.log"),
                        TraceWriter = typeof(RolloverTextWriterTraceWriter)
                    },
                    new TraceWriterConfiguration() {
                        Filter = System.Diagnostics.Tracing.EventLevel.LogAlways,
                        InitializationData = "santedb",
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
                        TraceWriter = typeof(RolloverTextWriterTraceWriter)
                    },
                    new TraceWriterConfiguration() {
                        Filter = System.Diagnostics.Tracing.EventLevel.Informational,
                        InitializationData = "SanteDB",
                        TraceWriter = typeof(ConsoleTraceWriter)
                    }
                }
            };
#endif

            configuration.Sections.Add(new FileSystemDispatcherQueueConfigurationSection()
            {
                QueuePath = Path.Combine(localDataPath, "queue"),
            });

            configuration.Sections.Add(diagSection);
            configuration.Sections.Add(upstreamConfiguration);
            configuration.Sections.Add(serviceSection);
            configuration.Sections.Add(new AuditAccountabilityConfigurationSection()
            {
                AuditFilters = new List<AuditFilterConfiguration>()
                {
                    // Audit any failure - No matter which event
                    new AuditFilterConfiguration(null, null, OutcomeIndicator.EpicFail | OutcomeIndicator.MinorFail | OutcomeIndicator.SeriousFail, true, true),
                    // Audit anything that creates, reads, or updates data
                    new AuditFilterConfiguration(ActionType.Create | ActionType.Read | ActionType.Update | ActionType.Delete, null, null, true, true)
                }
            });
            configuration.Sections.Add(new SynchronizationConfigurationSection()
            {
                PollInterval = new TimeSpan(0, 15, 0),
                ForbidSending = new List<ResourceTypeReferenceConfiguration>()
                {
                    new ResourceTypeReferenceConfiguration(typeof(DeviceEntity)),
                    new ResourceTypeReferenceConfiguration(typeof(ApplicationEntity)),
                    new ResourceTypeReferenceConfiguration(typeof(Concept)),
                    new ResourceTypeReferenceConfiguration(typeof(ConceptSet)),
                    new ResourceTypeReferenceConfiguration(typeof(Place)),
                    new ResourceTypeReferenceConfiguration(typeof(ReferenceTerm)),
                    new ResourceTypeReferenceConfiguration(typeof(AssigningAuthority)),
                    new ResourceTypeReferenceConfiguration(typeof(UserEntity)),
                    new ResourceTypeReferenceConfiguration(typeof(SecurityUser)),
                    new ResourceTypeReferenceConfiguration(typeof(SecurityDevice)),
                    new ResourceTypeReferenceConfiguration(typeof(SecurityApplication))
                }
            });
            return configuration;
        }
    }
}
