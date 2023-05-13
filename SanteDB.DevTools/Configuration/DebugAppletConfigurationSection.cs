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
using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.DevTools.Configuration
{
    /// <summary>
    /// Debug applet configuration section
    /// </summary>
    [XmlType(nameof(DebugAppletConfigurationSection), Namespace = "http://santedb.org/configuration/sdk")]
    public class DebugAppletConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DebugAppletConfigurationSection()
        {
            this.AppletReferences = new List<string>();
            this.AppletsToDebug = new List<string>();
        }

        /// <summary>
        /// Gets or sets the default applet
        /// </summary>
        [DisplayName("Default Applet"), Description("The default applet in the collection of applets to debug - this dictates the default set of configuration screens, etc.")]
        [XmlElement("defaultApplet")]
        public String DefaultApplet { get; set; }

        /// <summary>
        /// Applets which should be debugged
        /// </summary>
        [DisplayName("Applets To Debug"), Description("The director(ies) which contain manifest.xml files to use as the basis for debugging applets")]
        [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
        [XmlArray("applets"), XmlArrayItem("add")]
        public List<String> AppletsToDebug { get; set; }

        /// <summary>
        /// The solution to be debugged
        /// </summary>
        [DisplayName("Solution To Debug"), Description("The solution file to debug - this can be used in lieu of specifying multiple applets")]
        [Editor("System.Windows.Forms.Design.FileNameEditor, System.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
        [XmlElement("solution")]
        public String SolutionToDebug { get; set; }

        /// <summary>
        /// Applet references to add
        /// </summary>
        [DisplayName("References"), Description("Third party package references to include when loading the debugger context")]
        [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
        [XmlArray("references"), XmlArrayItem("add")]
        public List<String> AppletReferences { get; set; }


    }
}
