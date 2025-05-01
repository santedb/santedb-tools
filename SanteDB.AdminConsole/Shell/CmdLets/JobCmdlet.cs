/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using MohawkCollege.Util.Console.Parameters;
using SanteDB.AdminConsole.Attributes;
using SanteDB.AdminConsole.Util;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.AMI.Jobs;
using SanteDB.Core.Model.Parameters;
using SanteDB.Messaging.AMI.Client;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SanteDB.AdminConsole.Shell.CmdLets
{
    /// <summary>
    /// Job comamndlet
    /// </summary>
    [AdminCommandlet]
    [ExcludeFromCodeCoverage]
    public static class JobCmdlet
    {

        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.AdministrationIntegrationService));

        [AdminCommand("job.list", "Lists all jobs which are registered on the server")]
        public static void ListJobs()
        {
            var jobs = m_client.Client.Get<AmiCollection>("JobInfo");
            DisplayUtil.TablePrint(jobs.CollectionItem.OfType<JobInfo>(),
                new string[] { "ID", "Name", "Last Finished", "State", "Schedule", "Status" },
                new int[] { 38, 48, 24, 16, 16, 48 },
                o => o.Key,
                o => o.Name,
                o => o.LastFinish,
                o => o.State,
                o => o.Schedule.Any() ? o.Schedule.First().Type.ToString() : null,
                o => o.State == Core.Jobs.JobStateType.Running ? $"{o.StatusText} ({o.Progress:0%})" : null);
        }

        public class StartJobParameters
        {

            [Parameter("job")]
            public String JobId { get; set; }

        }

        [AdminCommand("job.run", "Lists all jobs which are registered on the server")]
        public static void StartJob(StartJobParameters jobParameters)
        {
            if (String.IsNullOrEmpty(jobParameters.JobId))
            {
                throw new ArgumentNullException("job");
            }
            else if (!Guid.TryParse(jobParameters.JobId, out var uuid))
            {
                throw new ArgumentOutOfRangeException("job must be uuid");
            }

            m_client.Client.Post<ParameterCollection, Object>($"JobInfo/{jobParameters.JobId}/$start", new ParameterCollection());
        }
    }
}
