using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
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
        [Editor("System.Windows.Forms.Design.FolderNameEditor, System.Design", "System.Drawing.Design.UITypeEditor, System.Drawing")]
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
