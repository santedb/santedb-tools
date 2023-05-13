/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using SanteDB.Client.Configuration;
using SanteDB.Client.Rest;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Messaging.Metadata;
using SanteDB.Messaging.Metadata.Configuration;
using SanteDB.Messaging.Metadata.Rest;
using SanteDB.Rest.Common.Behavior;
using SanteDB.Rest.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.SDK.AppletDebugger.Configuration
{
    /// <summary>
    /// Swagger initial configuration provider
    /// </summary>
    public class MetadataInitialConfigurationProvider : IInitialConfigurationProvider
    {

        /// <inheritdoc/>
        public int Order => 0;

        /// <summary>
        /// Get the default configuration for this service
        /// </summary>
        public SanteDBConfiguration Provide(SanteDBHostType hostContext, SanteDBConfiguration existing)
        {
            if (hostContext != SanteDBHostType.Gateway)
            {
                return existing;
            }

            var bindingBase = new Uri(AppDomain.CurrentDomain.GetData(RestServiceInitialConfigurationProvider.BINDING_BASE_DATA)?.ToString());
            if (bindingBase == null)
            {
                bindingBase = new Uri("http://0.0.0.0:9200");
            }

            var appServiceConfig = existing.GetSection<ApplicationServiceContextConfigurationSection>();
            var restServiceConfig = existing.GetSection<RestConfigurationSection>();
            if (restServiceConfig == null)
            {
                restServiceConfig = new RestConfigurationSection();
                existing.AddSection(restServiceConfig);
            }

            // Add swagger configuration
            if (!appServiceConfig.ServiceProviders.Any(o => o.Type == typeof(MetadataMessageHandler)))
            {
                appServiceConfig.ServiceProviders.Add(new TypeReferenceConfiguration(typeof(MetadataMessageHandler)));
                existing.AddSection(new MetadataConfigurationSection()
                {
                    Services = new List<SanteDB.Core.Interop.ServiceEndpointOptions>()
                });

                restServiceConfig.Services.Add(new RestServiceConfiguration(typeof(MetadataServiceBehavior))
                {
                    ConfigurationName = MetadataMessageHandler.ConfigurationName,
                    Behaviors = new List<RestServiceBehaviorConfiguration>()
                    {
                        new RestServiceBehaviorConfiguration(typeof(ErrorServiceBehavior))
                    },
                    Endpoints = new List<RestEndpointConfiguration>()
                    {
                        new RestEndpointConfiguration()
                                {
                                    Address = $"{bindingBase.Scheme}://{bindingBase.Host}:{bindingBase.Port}/api-docs/",
                                    Behaviors = new List<RestEndpointBehaviorConfiguration>() {
                                        new RestEndpointBehaviorConfiguration(typeof(MessageDispatchFormatterBehavior))
                                    },
                                    Contract = typeof(IMetadataServiceContract)
                                }
                    }
                });
            }
            return existing;
        }
    }
}
