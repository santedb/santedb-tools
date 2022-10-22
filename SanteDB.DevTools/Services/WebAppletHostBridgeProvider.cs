using SanteDB.Disconnected.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SanteDB.DevTools.Services
{
    /// <summary>
    /// Web applet host bridge provider
    /// </summary>
    public class WebAppletHostBridgeProvider : IAppletHostBridgeProvider
    {
        /// <summary>
        /// Get the bridge script
        /// </summary>
        public string GetBridgeScript()
        {
            using(var streamReader = new StreamReader(typeof(WebAppletHostBridgeProvider).Assembly.GetManifestResourceStream("SanteDB.DevTools.Resources.WebAppletBridge.js")))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}
