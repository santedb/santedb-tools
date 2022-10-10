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
using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SanteDB.SDL.BreDebugger.Options
{
    /// <summary>
    /// Represents base parameters for the debugger
    /// </summary>
    public class DebuggerParameters
    {

        /// <summary>
        /// Gets or sets the source of the debugger
        /// </summary>
        [Parameter("*")]
        [Parameter("source")]
        [Description("Identifies the source of the debugger")]
        public StringCollection Sources { get; set; }

        /// <summary>
        /// Gets the working directory of the debugger
        /// </summary>
        [Description("Sets the working directory of the debugger")]
        [Parameter("workDir")]
        public String WorkingDirectory { get; set; }

        /// <summary>
        /// The configuration file to be loaded into the debugger
        /// </summary>
        [Parameter("config")]
        [Description("Identifies a configuration file to be loaded")]
        public String ConfigurationFile { get; set; }

        /// <summary>
        /// Specifies the database file to be loaded into the debugger
        /// </summary>
        [Parameter("db")]
        [Description("Identifies the database file to be loaded")]
        public String DatabaseFile { get; set; }

        /// <summary>
        /// Gets or sets the references
        /// </summary>
        [Description("Identifies referenced pak files")]
        [Parameter("ref")]
        public StringCollection References { get; set; }

        /// <summary>
        /// Show help and exit
        /// </summary>
        [Parameter("help")]
        [Description("Show help and exit")]
        public bool Help { get; internal set; }

        /// <summary>
        /// Starts the bre debugger
        /// </summary>
        [Description("Debug a business rule")]
        [Parameter("bre")]
        public bool BusinessRule { get; set; }

        /// <summary>
        /// Starts the protocol debugger
        /// </summary>
        [Parameter("xprot")]
        [Description("Debug a clinical protocol")]
        public bool Protocol { get; set; }
    }
}
