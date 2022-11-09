using SanteDB.Client;
using SanteDB.Core;
using SanteDB.Core.Services;
using SanteDB.DevTools.Configuration;
using SanteDB.Tools.Debug.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SanteDB.SDK.AppletDebugger
{
    /// <summary>
    /// Debugger application context
    /// </summary>
    internal class DebuggerApplicationContext : ClientApplicationContextBase
    {

        // The original console parameters
        private ConsoleParameters m_consoleParameters;

        /// <inheritdoc/>
        public DebuggerApplicationContext(ConsoleParameters debugParameters, IConfigurationManager configurationManager) : base(SanteDBHostType.Gateway, debugParameters.InstanceName, configurationManager)
        {

            // Now create the debug applet configuration from the parameters
            var appletConfiguration = new DebugAppletConfigurationSection();
            this.m_consoleParameters = debugParameters;
            if (debugParameters.BaseRefs)
            {
                appletConfiguration.AppletReferences.AddRange(new string[]
                {
                        "org.santedb.core",
                        "org.santedb.uicore",
                        "org.santedb.config",
                        "org.santedb.bicore",
                        "org.santedb.config.init",
                        "org.santedb.i18n.en"
                });
            }
            if (debugParameters.References != null)
            {
                appletConfiguration.AppletReferences.AddRange(debugParameters.References.OfType<String>());
            }
            appletConfiguration.SolutionToDebug = debugParameters.SolutionFile;
            if (debugParameters.AppletDirectories != null)
            {
                appletConfiguration.AppletsToDebug.AddRange(debugParameters.AppletDirectories.OfType<String>());
            }
            configurationManager.Configuration.AddSection(appletConfiguration);
            this.ServiceManager.AddServiceProvider(typeof(DebugAppletManagerService));
        }

        /// <inheritdoc/>
        protected override void OnRestartRequested(object sender)
        {
            // Delay fire - allow other objects to finish up on the restart request event
            Thread.Sleep(1000);
            ServiceUtil.Stop();
            var pi = new ProcessStartInfo(typeof(Program).Assembly.Location, string.Join(" ", this.m_consoleParameters.ToArgumentList())) ;
            var process = Process.Start(pi);
        }
    }
}
