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
 * Date: 2023-5-19
 */
using System;
using System.Diagnostics;
using System.ServiceProcess;

namespace SanteDB.PakSrv
{
    public partial class PakSrvService : ServiceBase
    {

        // Service host
        private PakSrvHost m_host = new PakSrvHost();

        /// <summary>
        /// SanteDB Service
        /// </summary>
        public PakSrvService()
        {
            InitializeComponent();
            this.ServiceName = "SanteDB PakSrv";
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Start command is sent to the service by the Service Control Manager (SCM) or when the operating system starts (for a service that starts automatically). Specifies actions to take when the service starts.
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        protected override void OnStart(string[] args)
        {
            try
            {

                EventLog.WriteEntry("SanteDB Package Host Service", $"Service is ready to accept connections", EventLogEntryType.Information);
                this.m_host.Start();
            }
            catch (Exception e)
            {
                Trace.TraceError("The service reported an error: {0}", e);
                EventLog.WriteEntry("SanteDB Package Host Service", $"Service Startup reported an error: {e}", EventLogEntryType.Error, 1911);
                Environment.FailFast($"Error starting service: {e.Message}");
            }
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Stop command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service stops running.
        /// </summary>
        protected override void OnStop()
        {
            try
            {
                this.m_host.Stop();
                EventLog.WriteEntry("SanteDB Package Host Service", $"Gateway has been shutdown successfully", EventLogEntryType.Information);

            }
            catch (Exception e)
            {
                Trace.TraceError("The service reported an error on shutdown: {0}", e);
                EventLog.WriteEntry("SanteDB Package Host Service", $"Service Shutdown reported an error: {e}", EventLogEntryType.Error, 1911);

                Environment.FailFast($"Error stopping service: {e.Message}");

            }
        }
    }
}
