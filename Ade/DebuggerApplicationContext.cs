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
using SanteDB.Client;
using SanteDB.Client.Configuration;
using SanteDB.Client.UserInterface;
using SanteDB.Core;
using SanteDB.Core.i18n;
using SanteDB.Core.Services;
using SanteDB.DevTools.Configuration;
using SanteDB.Tools.Debug.Services;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

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
            configurationManager.Configuration.RemoveSection<DebugAppletConfigurationSection>();
            configurationManager.Configuration.AddSection(appletConfiguration);
            this.ServiceManager.AddServiceProvider(typeof(DebugAppletManagerService));
        }

        /// <inheritdoc/>
        protected override void OnRestartRequested(object sender)
        {
            // Delay fire - allow other objects to finish up on the restart request event
            var uiInteraction = this.GetService<IUserInterfaceInteractionProvider>();
            if (this.GetService<IConfigurationManager>() is InitialConfigurationManager || uiInteraction.Confirm(UserMessages.RESTART_REQUESTED_CONFIRM))
            {
                try
                {
                    Thread.Sleep(1000);
                    ServiceUtil.Stop();
                }
                catch
                {
                }

                var pi = new ProcessStartInfo(typeof(Program).Assembly.Location, string.Join(" ", this.m_consoleParameters.ToArgumentList()));
                Process.Start(pi);
            }
        }
    }
}
