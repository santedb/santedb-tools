/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using RestSrvr;
using RestSrvr.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SanteDB.PakSrv
{
    /// <summary>
    /// Package service host
    /// </summary>
    public class PakSrvHost
    {

        /// <summary>
        /// The service host
        /// </summary>
        private RestService m_serviceHost = null;

        // Configuration
        internal static PakSrvConfiguration m_configuration;

        /// <summary>
        /// Start the service host
        /// </summary>
        public void Start()
        {
            var configFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "paksrv.config");
            if (!File.Exists(configFile))
            {
                m_configuration = new PakSrvConfiguration()
                {
                    Bindings = new List<string>()
                    {
                        "http://0.0.0.0:6039/paksrv"
                    }
                };

                using (var fs = File.Create(configFile))
                {
                    m_configuration.Save(fs);
                }
            }
            else
            {
                using (var fs = File.OpenRead(configFile))
                {
                    m_configuration = PakSrvConfiguration.Load(fs);
                }
            }

            this.m_serviceHost = new RestService(typeof(PakSrvBehavior));

            foreach (var bind in m_configuration.Bindings)
            {
                this.m_serviceHost.AddServiceEndpoint(new Uri(bind), typeof(IPakSrvContract), new RestHttpBinding());
            }

            this.m_serviceHost.AddServiceBehavior(new PakSrvAuthenticationBehavior(m_configuration));
            this.m_serviceHost.Start();
        }


        /// <summary>
        /// Stop the rest service
        /// </summary>
        public void Stop()
        {
            this.m_serviceHost?.Stop();
            this.m_serviceHost = null;
        }


    }
}
