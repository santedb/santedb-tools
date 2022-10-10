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
using SanteDB.Core.Configuration;
using SanteDB.DisconnectedClient.Ags.Behaviors;
using SanteDB.DisconnectedClient.Ags.Configuration;
using SanteDB.DisconnectedClient.Configuration;
using SanteDB.Messaging.Metadata;
using SanteDB.Messaging.Metadata.Configuration;
using SanteDB.Messaging.Metadata.Rest;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.SDK.AppletDebugger.Configuration
{
    /// <summary>
    /// Swagger initial configuration provider
    /// </summary>
    public class MetadataInitialConfigurationProvider : IInitialConfigurationProvider
    {
        /// <summary>
        /// Get the default configuration for this service
        /// </summary>
        public SanteDBConfiguration Provide(SanteDBConfiguration existing)
        {

            // Add swagger configuration
            if (!existing.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.Any(o => o.Type == typeof(MetadataMessageHandler)))
            {
                existing.AddSection(new MetadataConfigurationSection()
                {
                    Services = new List<SanteDB.Core.Interop.ServiceEndpointOptions>()
                });
                existing.GetSection<AgsConfigurationSection>().Services.Add(new AgsServiceConfiguration(typeof(MetadataServiceBehavior))
                {
                    Behaviors = new List<AgsBehaviorConfiguration>()
                {
                    new AgsBehaviorConfiguration(typeof(AgsErrorHandlerServiceBehavior))
                },
                    Endpoints = new List<AgsEndpointConfiguration>()
                {
                    new AgsEndpointConfiguration()
                            {
                                Address = "http://127.0.0.1:9200/api-docs",
                                Behaviors = new List<AgsBehaviorConfiguration>() {
                                    new AgsBehaviorConfiguration(typeof(AgsSerializationEndpointBehavior))
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
