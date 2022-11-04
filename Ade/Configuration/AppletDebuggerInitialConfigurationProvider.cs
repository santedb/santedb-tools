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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Data.Backup;
using SanteDB.Tools.Debug.Services;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Rest.HDSI;
using SanteDB.Rest.AMI;
using SanteDB.Rest.BIS;
using SanteDB.Client.OAuth;
using SanteDB.Client.UserInterface.Impl;
using SanteDB.Rest.Common;
using SanteDB.DevTools.Services;
using SanteDB.Client.Disconnected.Data.Synchronization.Configuration;
using SanteDB.Rest.OAuth.Configuration;
using SanteDB.Client.Upstream.Security;
using SanteDB.Client.Upstream.Management;
using SanteDB.Client.Upstream.Repositories;
using SanteDB.Client.Repositories;
using SanteDB.Security.Certs.BouncyCastle;

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
        public SanteDBConfiguration Provide(SanteDBHostType hostContext, SanteDBConfiguration configuration)
        {

            var appServiceSection = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
            var instanceName = appServiceSection.InstanceName;
            var localDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "santedb", "sdk", "ade", instanceName);

            appServiceSection.ServiceProviders.AddRange(new List<TypeReferenceConfiguration>() {
                    new TypeReferenceConfiguration(typeof(AesSymmetricCrypographicProvider)),
                    new TypeReferenceConfiguration(typeof(InMemoryTickleService)),
                    new TypeReferenceConfiguration(typeof(DefaultNetworkInformationService)),
                    new TypeReferenceConfiguration(typeof(SHA256PasswordHashingService)),
                    new TypeReferenceConfiguration(typeof(DefaultPolicyDecisionService)),
                    new TypeReferenceConfiguration(typeof(MemoryAdhocCacheService)),
                    new TypeReferenceConfiguration(typeof(AppletLocalizationService)),
                    new TypeReferenceConfiguration(typeof(AppletBusinessRulesDaemon)),
                    new TypeReferenceConfiguration(typeof(DefaultUpstreamManagementService)),
                    new TypeReferenceConfiguration(typeof(DefaultUpstreamIntegrationService)),
                    new TypeReferenceConfiguration(typeof(DefaultUpstreamAvailabilityProvider)),
                    new TypeReferenceConfiguration(typeof(MemoryCacheService)),
                    new TypeReferenceConfiguration(typeof(DefaultThreadPoolService)),
                    new TypeReferenceConfiguration(typeof(ConsoleUserInterfaceInteractionProvider)),
                    new TypeReferenceConfiguration(typeof(MemoryQueryPersistenceService)),
                    new TypeReferenceConfiguration(typeof(FileSystemDispatcherQueueService)),
                    new TypeReferenceConfiguration(typeof(SimplePatchService)),
                    new TypeReferenceConfiguration(typeof(DefaultBackupManager)),
                    new TypeReferenceConfiguration(typeof(DebugAppletManagerService)),
                    new TypeReferenceConfiguration(typeof(AppletBiRepository)),
                    new TypeReferenceConfiguration(typeof(OAuthClient)),
                    new TypeReferenceConfiguration(typeof(MemorySessionManagerService)),
                    new TypeReferenceConfiguration(typeof(UpstreamUpdateManagerService)), // AmiUpdateManager
                    new TypeReferenceConfiguration(typeof(UpstreamIdentityProvider)),
                    new TypeReferenceConfiguration(typeof(UpstreamApplicationIdentityProvider)),
                    new TypeReferenceConfiguration(typeof(UpstreamSecurityChallengeProvider)), // AmiSecurityChallengeProvider
                    new TypeReferenceConfiguration(typeof(UpstreamRoleProviderService)),
                    new TypeReferenceConfiguration(typeof(UpstreamSecurityRepository)),
                    new TypeReferenceConfiguration(typeof(UpstreamRepositoryFactory)),
                    new TypeReferenceConfiguration(typeof(UpstreamPolicyInformationService)),
                    new TypeReferenceConfiguration(typeof(DataPolicyFilterService)),
                    new TypeReferenceConfiguration(typeof(DefaultOperatingSystemInfoService)),
                    new TypeReferenceConfiguration(typeof(AppletSubscriptionRepository)),
                    new TypeReferenceConfiguration(typeof(InMemoryPivotProvider)),
                    new TypeReferenceConfiguration(typeof(AuditDaemonService)),
                    new TypeReferenceConfiguration(typeof(DefaultDataSigningService)),
                    new TypeReferenceConfiguration(typeof(DefaultBarcodeProviderService)),
                    new TypeReferenceConfiguration(typeof(FileSystemDispatcherQueueService)),
                    new TypeReferenceConfiguration(typeof(BouncyCastleCertificateGenerator))
            });

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
                        CredentialType = UpstreamCredentialType.Device
                    },
                    new UpstreamCredentialConfiguration()
                    {
                        CredentialName = "org.santedb.debug",
                        CredentialSecret = "A1CF054D04D04CD1897E114A904E328D",
                        Conveyance = UpstreamCredentialConveyance.Secret,
                        CredentialType = UpstreamCredentialType.Application
                    }
                }
            };

            // Rest Client Configuration
            RestClientConfigurationSection serviceSection = new RestClientConfigurationSection()
            {
                RestClientType = new TypeReferenceConfiguration(typeof(RestClient))
            };

            configuration.AddSection(new SecurityConfigurationSection()
            {
                PasswordRegex = @"^(?=.*\d){1,}(?=.*[a-z]){1,}(?=.*[A-Z]){1,}(?=.*[^\w\d]){1,}.{6,}$",
                SecurityPolicy = new List<SecurityPolicyConfiguration>()
                {
                    new SecurityPolicyConfiguration(SecurityPolicyIdentification.SessionLength, new TimeSpan(0,30,0)),
                    new SecurityPolicyConfiguration(SecurityPolicyIdentification.RefreshLength, new TimeSpan(0,35,0))
                },
                Signatures = new List<SanteDB.Core.Security.Configuration.SecuritySignatureConfiguration>()
                {
                    new SanteDB.Core.Security.Configuration.SecuritySignatureConfiguration()
                    {
                        KeyName ="default",
                        Algorithm = SanteDB.Core.Security.Configuration.SignatureAlgorithm.HS256,
                        HmacSecret = "@SanteDBDefault$$$2021"
                    }
                }
            });
            // Trace writer
#if DEBUG
            DiagnosticsConfigurationSection diagSection = new DiagnosticsConfigurationSection()
            {
                TraceWriter = new System.Collections.Generic.List<TraceWriterConfiguration>() {
                    new TraceWriterConfiguration () {
                        Filter = System.Diagnostics.Tracing.EventLevel.Warning,
                        InitializationData = "santedb",
                        TraceWriter = typeof(DebugDiagnosticsTraceWriter)
                    },
                    new TraceWriterConfiguration() {
                        Filter = System.Diagnostics.Tracing.EventLevel.Warning,
                        InitializationData = Path.Combine(localDataPath, "log", "santedb.log"),
                        TraceWriter = typeof(RolloverTextWriterTraceWriter)
                    },
                    new TraceWriterConfiguration() {
                        Filter = System.Diagnostics.Tracing.EventLevel.Error,
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

            // Setup the tracers 
            diagSection.TraceWriter.ForEach(o => Tracer.AddWriter(Activator.CreateInstance(o.TraceWriter, o.Filter, o.InitializationData, null) as TraceWriter, o.Filter));
            configuration.Sections.Add(new FileSystemDispatcherQueueConfigurationSection()
            {
                QueuePath = Path.Combine(localDataPath, "queue"),
            });

            var backupSection = new BackupConfigurationSection()
            {
                PrivateBackupLocation = Path.Combine(localDataPath, "backup"),
                PublicBackupLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "santedb", "sdk", "backup")
            };

            configuration.Sections.Add(new RestClientConfigurationSection()
            {
                RestClientType = new TypeReferenceConfiguration(typeof(RestClient))
            });
            configuration.Sections.Add(new OAuthConfigurationSection()
            {
                IssuerName = upstreamConfiguration.Credentials[0].CredentialName,
                AllowClientOnlyGrant = false,
                JwtSigningKey = "jwsdefault",
                TokenType = "bearer"
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
