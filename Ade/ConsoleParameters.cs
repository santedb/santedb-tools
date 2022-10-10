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
using System.Collections.Specialized;
using System.ComponentModel;

namespace SanteDB.SDK.AppletDebugger
{
    /// <summary>
    /// Console parameters to the mini ims
    /// </summary>
    public class ConsoleParameters
    {

        /// <summary>
        /// Include all base files
        /// </summary>
        [Parameter("core")]
        [Description("Include core libraries (default SanteDB core libraries). Don't use this if you're debugging core applets")]
        public bool BaseRefs { get; set; }

        /// <summary>
        /// Gets or sets the solution file.
        /// </summary>
        [Parameter("solution")]
        [Description("Identifies the solution which is being debugged")]
        public string SolutionFile { get; set; }

        /// <summary>
        /// Applet directory 
        /// </summary>
        [Parameter("applet")]
        [Description("Identifies the source code directories to debug")]
        public StringCollection AppletDirectories { get; set; }

        /// <summary>
        /// Applet directory 
        /// </summary>
        [Parameter("ref")]
        [Description("Reference an already compiled (and signed) applet")]
        public StringCollection References { get; set; }

        /// <summary>
        /// Restore from backup
        /// </summary>
        [Parameter("restore")]
        [Description("Restore a configuration from a backup")]
        public bool Restore { get; set; }

        /// <summary>
        /// Show help and exit
        /// </summary>
        [Parameter("help")]
        [Description("Shows help and exits")]
        public bool Help { get; set; }

        /// <summary>
        /// Instructs the minims to remove itself
        /// </summary>
        [Parameter("reset")]
        [Description("Deletes all configuration data restoring the debugger to its default state")]
        public bool Reset { get; set; }

        /// <summary>
        /// Loads the specified assemblies
        /// </summary>
        [Parameter("assembly")]
        [Description("Loads the specified assembly into the debugger environment (for testing .NET plugins)")]
        public StringCollection Assemblies { get; set; }

        /// <summary>
        /// Gets or sets the development environment name
        /// </summary>
        [Parameter("name")]
        [Description("Allows for separate environment names for multiple debugging")]
        public string InstanceName { get; set; }

    }
}
