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
using MohawkCollege.Util.Console.Parameters;
using System.ComponentModel;

namespace SanteDB.PakSrv
{
    /// <summary>
    /// PakMan package manager parameters
    /// </summary>
    public class PakSrvParameters
    {

        /// <summary>
        /// Console mode
        /// </summary>
        [Parameter("console")]
        [Description("Run the application in console mode")]
        public bool Console { get; set; }

        /// <summary>
        /// Install the service
        /// </summary>
        [Parameter("install")]
        [Description("Install the service into Windows service manager")]
        public bool Install { get; set; }

        /// <summary>
        /// Uninstall the service 
        /// </summary>
        [Parameter("uninstall")]
        [Description("Remove the service from the Windows service manager")]
        public bool Uninstall { get; set; }
    }
}
