/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * Date: 2023-6-21
 */
using SanteDB.BusinessRules.JavaScript;
using SanteDB.BusinessRules.JavaScript.Configuration;
using SanteDB.Caching.Memory;
using SanteDB.Cdss.Xml;
using SanteDB.Client.Configuration;
using SanteDB.Client.UserInterface.Impl;
using SanteDB.Core;
using SanteDB.Core.Applets.Configuration;
using SanteDB.Core.Applets.Services.Impl;
using SanteDB.Core.Cdss;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using SanteDB.Core.Data;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Services.Impl;
using SanteDB.Core.Services.Impl.Repository;
using SanteDB.DevTools.Configuration;
using SanteDB.OrmLite.Configuration;
using SanteDB.OrmLite.Providers.Sqlite;
using SanteDB.Persistence.Data.Services;
using SanteDB.SDK.BreDebugger.Options;
using SanteDB.SDK.BreDebugger.Services;
using SanteDB.SDK.BreDebugger.Shell;
using SanteDB.Tools.Debug.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SanteDB.SDK.BreDebugger.Core
{
    /// <summary>
    /// Represents a configuration manager which is used for the debugger
    /// </summary>
    public class DebuggerInitialConfigurationProvider : IInitialConfigurationProvider
    {
        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(DebuggerInitialConfigurationProvider));

        /// <summary>
        /// Order of applying this initial configuratoin
        /// </summary>
        public int Order => 0;

        /// <inheritdoc/>
        public SanteDBConfiguration Provide(SanteDBHostType hostContext, SanteDBConfiguration configuration)
        {
            var appServiceSection = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
            if (appServiceSection == null)
            {
                configuration.AddSection(new ApplicationServiceContextConfigurationSection());
            }
            var localDataPath = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
            var parameters = AppDomain.CurrentDomain.GetData("Argv") as DebuggerParameters;

            configuration.AddSection(new JavascriptRulesConfigurationSection()
            {
                DebugMode = true,
                WorkerInstances = 1
            });

            // Initial Applet configuration
            configuration.AddSection(new AppletConfigurationSection()
            {
                AllowUnsignedApplets = true
            });

            if (parameters.References != null)
            {
                configuration.AddSection(new DebugAppletConfigurationSection()
                {
                    AppletReferences = parameters.References.OfType<String>().ToList()
                });
            }
            else
            {
                configuration.AddSection(new DebugAppletConfigurationSection());

            }

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
                        HmacSecret = $"@SanteDBDefault$$${DateTime.Now.Year}_{Environment.MachineName}_{Guid.NewGuid()}"
                    }
                }
            });

            var providers = DataConfigurationSection.GetDataConfigurationProviders()
                .Where(o => o.HostType.HasFlag(hostContext));
            // Add ORM configuration section
            configuration.AddSection(new OrmConfigurationSection()
            {
                Providers = providers.Select(o => new ProviderRegistrationConfiguration(o.Invariant, o.DbProviderType)).ToList(),
                AdoProvider = providers.Select(t => new ProviderRegistrationConfiguration(t.Invariant, t.AdoNetFactoryType)).ToList()
            });


            // Construct the inital data section
            var dataSection = configuration.GetSection<DataConfigurationSection>();
            if (dataSection == null)
            {
                dataSection = new DataConfigurationSection();
                configuration.AddSection(dataSection);
            }
            dataSection.ConnectionString = new List<ConnectionString>();

            var configProvider = DataConfigurationSection.GetDataConfigurationProvider(SqliteProvider.InvariantName);
            if (!String.IsNullOrEmpty(parameters.DatabaseFile))
            {
                dataSection.ConnectionString.Add(configProvider.CreateConnectionString(new Dictionary<String, Object>() {
                    { "Data Source", parameters.DatabaseFile }
                }));
            }
            else
            {
                dataSection.ConnectionString.Add(configProvider.CreateConnectionString(new Dictionary<String, Object>() {
                    { "Data Source", Path.Combine(localDataPath,"debug.sqlite") }
                }));
            }
            dataSection.ConnectionString.First().Name = "debug";
            //configuration.AddSection(dataSection);

            // Add all ORM configuration sections
            foreach (var itm in AppDomain.CurrentDomain.GetAllTypes().Where(t => typeof(OrmConfigurationBase).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
            {
                if (configuration.GetSection(itm) == null)
                {
                    var sectionInstance = Activator.CreateInstance(itm) as OrmConfigurationBase;
                    sectionInstance.ReadonlyConnectionString = sectionInstance.ReadWriteConnectionString = "debug";
                    sectionInstance.ProviderType = SqliteProvider.InvariantName;
                    configuration.AddSection(sectionInstance);
                }
            }

            // Application service section
            appServiceSection.ServiceProviders.AddRange(new Type[]
            {
                typeof(FileSystemResolver),
                typeof(DebugProtocolRepository),
                        typeof(DefaultPolicyDecisionService),
                        typeof(AdoIdentityProvider),
                        typeof(AdoRoleProvider),
                        typeof(AdoDeviceIdentityProvider),
                        typeof(AdoApplicationIdentityProvider),
                        typeof(AdoSecurityChallengeProvider),
                        typeof(AdoCertificateIdentityProvider),
                        typeof(AdoFreetextSearchService),
                        typeof(AdoPersistenceService),
                        typeof(AdoPolicyInformationService),
                        typeof(AdoRelationshipValidationProvider),
                        typeof(PersistenceEntitySource),
                        typeof(LocalRepositoryFactory),
                        typeof(DefaultNetworkInformationService),
                        typeof(AppletLocalizationService),
                        typeof(AppletBusinessRulesDaemon),
                        typeof(ConsoleUserInterfaceInteractionProvider),
                        typeof(PersistenceEntitySource),
                        typeof(SanteDB.Caching.Memory.MemoryCacheService),
                        typeof(SanteDB.Core.Services.Impl.DefaultThreadPoolService),
                        typeof(AppletClinicalProtocolInstaller),
                        typeof(MemoryAdhocCacheService),
                        typeof(MemoryQueryPersistenceService),
                        typeof(MemoryCacheService),
                        typeof(SimpleDecisionSupportService),
                        typeof(SimplePatchService),
                        typeof(DebugAppletManagerService),
            }.Select(o => new TypeReferenceConfiguration(o)));

            // Trace writer
            configuration.AddSection(new DiagnosticsConfigurationSection()
            {
                TraceWriter = new System.Collections.Generic.List<TraceWriterConfiguration>() {
                    new TraceWriterConfiguration () {
                        Filter = System.Diagnostics.Tracing.EventLevel.Error,
                        InitializationData = "SanteDB",
                        TraceWriter = typeof(ConsoleTraceWriter)
                    }
                }
            });

            return configuration;
        }
    }
}