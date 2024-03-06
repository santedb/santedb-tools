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
using MohawkCollege.Util.Console.Parameters;
using System;
using System.Collections.Generic;
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
        /// Bind the certificate 
        /// </summary>
        [Parameter("install-cert")]
        [Description("Bind a debugging certificate to the HTTPs port")]
        public bool AutoBindCertificate { get; set; }

        /// <summary>
        /// Restore from backup
        /// </summary>
        [Parameter("restore")]
        [Description("Restore a configuration from a backup")]
        public bool Restore { get; set; }

        /// <summary>
        /// Don't launch browser
        /// </summary>
        [Parameter("nobrowser")]
        [Description("When true, don't launch the default web-browser")]
        public bool NoBrowser { get; set; }

        /// <summary>
        /// Convert this object back to an argument list
        /// </summary>
        internal IEnumerable<String> ToArgumentList()
        {
            if (!String.IsNullOrEmpty(this.InstanceName))
            {
                yield return $"--name=\"{this.InstanceName}\"";
            }
            if (this.Assemblies != null)
            {
                foreach (var asm in this.Assemblies)
                {
                    yield return $"--assembly=\"{asm}\"";
                }
            }
            if (this.BaseRefs)
            {
                yield return "--core";
            }
            if (!String.IsNullOrEmpty(this.SolutionFile))
            {
                yield return $"--solution=\"{this.SolutionFile}\"";
            }
            if (this.References != null)
            {
                foreach (var refr in this.References)
                {
                    yield return $"--ref=\"{refr}\"";
                }
            }
            if (this.AppletDirectories != null)
            {
                foreach (var dir in this.AppletDirectories)
                {
                    yield return $"--applet=\"{dir}\"";
                }
            }
            if (!String.IsNullOrEmpty(this.BaseUrl))
            {
                yield return $"--base=\"{this.BaseUrl}\"";
            }
            if (NoBrowser)
            {
                yield return "--nobrowser";
            }
        }

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

        /// <summary>
        /// Gets or sets the base url of the debugger
        /// </summary>
        [Parameter("base")]
        [Description("Allows for the changing of the base URL (default is http://127.0.0.1)")]
        public string BaseUrl { get; set; }
    }
}
