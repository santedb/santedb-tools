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
using SanteDB.BusinessRules.JavaScript.Configuration;
using SanteDB.Cdss.Xml;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using SanteDB.Core.Data;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Protocol;
using SanteDB.Core.Services.Impl;
using SanteDB.DisconnectedClient.Caching;
using SanteDB.DisconnectedClient.Configuration;
using SanteDB.DisconnectedClient.Configuration.Data;
using SanteDB.DisconnectedClient.Diagnostics;
using SanteDB.DisconnectedClient.Http;
using SanteDB.DisconnectedClient.Net;
using SanteDB.DisconnectedClient.Rules;
using SanteDB.DisconnectedClient.Security.Session;
using SanteDB.DisconnectedClient.Services;
using SanteDB.DisconnectedClient.Services.Local;
using SanteDB.DisconnectedClient.SQLite;
using SanteDB.DisconnectedClient.SQLite.Connection;
using SanteDB.DisconnectedClient.SQLite.Security;
using SdbDebug.Options;
using SdbDebug.Shell;
using System;
using System.Collections.Generic;
using System.IO;

namespace SanteDB.SDL.BreDebugger.Core
{
    /// <summary>
    /// Represents a configuration manager which is used for the debugger
    /// </summary>
    public class DebugConfigurationManager : IConfigurationPersister
    {
        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(DebugConfigurationManager));

        // Configuration path
        private readonly string m_configPath = String.Empty;

        // Data path
        private readonly string m_dataPath = string.Empty;

        /// <summary>
        /// Creates a new debug configuration manager with the specified parameters
        /// </summary>
        public DebugConfigurationManager(DebuggerParameters parms)
        {
            // Get parameters from args
            if (!String.IsNullOrEmpty(parms.ConfigurationFile))
                this.m_configPath = parms.ConfigurationFile;
            if (!String.IsNullOrEmpty(parms.DatabaseFile))
                this.m_dataPath = parms.DatabaseFile;
        }

        /// <summary>
        /// Gets the application data directory
        /// </summary>
        public string ApplicationDataDirectory
        {
            get
            {
                if (this.m_dataPath != null)
                    return Path.GetDirectoryName(this.m_dataPath);
                else
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SanteDBDC");
            }
        }

        /// <summary>
        /// Returns true if the
        /// </summary>
        public bool IsConfigured
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Perform a backup
        /// </summary>
        public void Backup(SanteDBConfiguration config)
        {
            throw new NotImplementedException("Debug environment cannot backup");
        }

        /// <summary>
        /// Get whether there is a backup
        /// </summary>
        /// <returns></returns>
        public bool HasBackup()
        {
            return false;
        }

        /// <summary>
        /// Load the configuration file
        /// </summary>
        public SanteDBConfiguration Load()
        {
            if (!String.IsNullOrEmpty(this.m_configPath))
                using (var fs = File.OpenRead(this.m_configPath))
                {
                    return SanteDBConfiguration.Load(fs);
                }
            else
            {
                var retVal = new SanteDBConfiguration();

                // Inital data source
                DcDataConfigurationSection dataSection = new DcDataConfigurationSection()
                {
                    MainDataSourceConnectionStringName = "santeDbData",
                    MessageQueueConnectionStringName = "santeDbData",
                    MailDataStore = "santeDbData",
                    ConnectionString = new System.Collections.Generic.List<ConnectionString>() {
                    new ConnectionString () {
                        Name = "santeDbData",
                        Value = $"dbfile={(String.IsNullOrEmpty(this.m_dataPath) ? "SanteDB.debug.sqlite" : this.m_dataPath )}",
                        Provider = "sqlite"
                    }
                }
                };

                JavascriptRulesConfigurationSection jsConfiguration = new JavascriptRulesConfigurationSection()
                {
                    DebugMode = true,
                    WorkerInstances = 1
                };

                // Initial Applet configuration
                AppletConfigurationSection appletSection = new AppletConfigurationSection()
                {
                    Security = new AppletSecurityConfiguration()
                    {
                        AllowUnsignedApplets = true,
                        TrustedPublishers = new List<string>() { "82C63E1E9B87578D0727E871D7613F2F0FAF683B" }
                    }
                };

                // Initial applet style
                ApplicationConfigurationSection appSection = new ApplicationConfigurationSection()
                {
                    Style = StyleSchemeType.Dark,
                    UserPrefDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SdbDebug", "userpref"),
                    Cache = new CacheConfiguration()
                    {
                        MaxAge = new TimeSpan(0, 5, 0).Ticks,
                        MaxSize = 1000,
                        MaxDirtyAge = new TimeSpan(0, 20, 0).Ticks,
                        MaxPressureAge = new TimeSpan(0, 2, 0).Ticks
                    }
                };

                // Application service section
                ApplicationServiceContextConfigurationSection appServiceSection = new ApplicationServiceContextConfigurationSection()
                {
                    ThreadPoolSize = Environment.ProcessorCount,
                    ServiceProviders = new List<TypeReferenceConfiguration>() {
                        new TypeReferenceConfiguration(typeof(SanteDB.Core.Security.DefaultPolicyDecisionService)),
                        new TypeReferenceConfiguration(typeof(SQLitePolicyInformationService)),
                        new TypeReferenceConfiguration(typeof(LocalRepositoryFactoryService)),
                        //typeof(LocalAlertService).AssemblyQualifiedName,
                        new TypeReferenceConfiguration(typeof(LocalTagPersistenceService)),
                        new TypeReferenceConfiguration(typeof(NetworkInformationService)),
                        new TypeReferenceConfiguration(typeof(BusinessRulesDaemonService)),
                        new TypeReferenceConfiguration(typeof(PersistenceEntitySource)),
                        new TypeReferenceConfiguration(typeof(SanteDB.Caching.Memory.MemoryCacheService)),
                        new TypeReferenceConfiguration(typeof(SanteDB.Core.Services.Impl.DefaultThreadPoolService)),
                        new TypeReferenceConfiguration(typeof(MemorySessionManagerService)),
                        new TypeReferenceConfiguration(typeof(AmiUpdateManager)),
                        new TypeReferenceConfiguration(typeof(AppletClinicalProtocolInstaller)),
                        new TypeReferenceConfiguration(typeof(MemoryQueryPersistenceService)),
                        new TypeReferenceConfiguration(typeof(SimpleQueueFileProvider)),
                        new TypeReferenceConfiguration(typeof(SimpleCarePlanService)),
                        new TypeReferenceConfiguration(typeof(SimplePatchService)),
                        new TypeReferenceConfiguration(typeof(DebugAppletManagerService)),
                        new TypeReferenceConfiguration(typeof(SQLiteConnectionManager)),
                        new TypeReferenceConfiguration(typeof(SQLitePersistenceService)),
                        new TypeReferenceConfiguration(typeof(SQLite.Net.Platform.SqlCipher.SQLitePlatformSqlCipher))
                    }
                };

                // Security configuration
                SecurityConfigurationSection secSection = new SecurityConfigurationSection()
                {
                    DeviceName = Environment.MachineName,
                    AuditRetention = new TimeSpan(30, 0, 0, 0, 0)
                };

                // Device key
                //var certificate = X509CertificateUtils.FindCertificate(X509FindType.FindBySubjectName, StoreLocation.LocalMachine, StoreName.My, String.Format("DN={0}.mobile.santedb.org", macAddress));
                //secSection.DeviceSecret = certificate?.Thumbprint;

                // Rest Client Configuration
                ServiceClientConfigurationSection serviceSection = new ServiceClientConfigurationSection()
                {
                    RestClientType = typeof(RestClient)
                };

                // Trace writer
                DiagnosticsConfigurationSection diagSection = new DiagnosticsConfigurationSection()
                {
                    TraceWriter = new System.Collections.Generic.List<TraceWriterConfiguration>() {
                    new TraceWriterConfiguration () {
                        Filter = System.Diagnostics.Tracing.EventLevel.Error,
                        InitializationData = "SanteDB",
                        TraceWriter = typeof(ConsoleTraceWriter)
                    },
                    new TraceWriterConfiguration() {
                        Filter = System.Diagnostics.Tracing.EventLevel.LogAlways,
                        InitializationData = "SanteDB",
                        TraceWriter = typeof(FileTraceWriter)
                    }
                }
                };

                retVal.Sections.Add(appServiceSection);
                retVal.Sections.Add(appletSection);
                retVal.Sections.Add(dataSection);
                retVal.Sections.Add(diagSection);
                retVal.Sections.Add(appSection);
                retVal.Sections.Add(secSection);
                retVal.Sections.Add(serviceSection);
                retVal.Sections.Add(jsConfiguration);
                retVal.Sections.Add(new SynchronizationConfigurationSection()
                {
                    PollInterval = new TimeSpan(0, 5, 0)
                });

                return retVal;
            }
        }

        /// <summary>
        /// Restoration
        /// </summary>
        public SanteDBConfiguration Restore()
        {
            throw new NotImplementedException("Debug environment cannot restore backups");
        }

        /// <summary>
        /// Save the configuation
        /// </summary>
        public void Save(SanteDBConfiguration config)
        {
        }
    }
}