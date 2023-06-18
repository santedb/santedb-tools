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
using SanteDB.Client.UserInterface;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Applets.Services.Impl;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using SanteDB.Tools.Debug.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SanteDB.DevTools.Configuration.Feature
{
    /// <summary>
    /// Development tools feature
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DevToolFeature : IFeature
    {
        private const string DebugAppletEnabledSetting = "Enable Debug Applet Repository";
        private const string DebugAppletConfiguration = "Debug Applet Configuration";

        // Generic feature configuration
        private GenericFeatureConfiguration m_configuration;

        /// <summary>
        /// Gets or sets the configuration object
        /// </summary>
        public object Configuration
        {
            get => this.m_configuration;
            set => this.m_configuration = value as GenericFeatureConfiguration;
        }

        /// <summary>
        /// Gets or sets the configuration type
        /// </summary>
        public Type ConfigurationType => typeof(GenericFeatureConfiguration);

        /// <summary>
        /// Gets the description of this feature
        /// </summary>
        public string Description => "Tools which can assist developers in unsing SanteDB";

        /// <summary>
        /// Flags for the feature
        /// </summary>
        public FeatureFlags Flags => FeatureFlags.None;

        /// <summary>
        /// Gets the group
        /// </summary>
        public string Group => FeatureGroup.Development;

        /// <summary>
        /// Get the name of the feature
        /// </summary>
        public string Name => "Developer Options";

        /// <summary>
        /// Create install tasks
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateInstallTasks()
        {
            yield return new InstallServiceTask(this, typeof(DebugAppletManagerService), () => (bool)this.m_configuration.Values[DebugAppletEnabledSetting] == true, typeof(IAppletManagerService));
            yield return new InstallConfigurationSectionTask(this, this.m_configuration.Values[DebugAppletConfiguration] as IConfigurationSection, "Applet Debugging");
        }

        /// <summary>
        /// Create uninstall tasks
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateUninstallTasks()
        {
            yield return new UnInstallServiceTask(this, typeof(DebugAppletManagerService), () => (bool)this.m_configuration.Values[DebugAppletEnabledSetting] == false);
            yield return new UnInstallServiceTask(this, typeof(WebAppletHostBridgeProvider), () => (bool)this.m_configuration.Values[DebugAppletEnabledSetting] == false);
            yield return new InstallServiceTask(this, typeof(FileSystemAppletManagerService), () => (bool)this.m_configuration.Values[DebugAppletEnabledSetting] == false); //Replace the applet manager
            yield return new UnInstallConfigurationSectionTask(this, this.m_configuration.Values[DebugAppletConfiguration] as IConfigurationSection, "Applet Debugging");
        }

        /// <summary>
        /// Query state
        /// </summary>
        public FeatureInstallState QueryState(SanteDBConfiguration configuration)
        {
            var sectionConfig = configuration.GetSection<DebugAppletConfigurationSection>();
            var appServiceProviders = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders;

            // Is the FILE based BI repository enabled?
            this.m_configuration = new GenericFeatureConfiguration();
            this.m_configuration.Options.Add(DebugAppletConfiguration, () => ConfigurationOptionType.Object);
            this.m_configuration.Values.Add(DebugAppletConfiguration, sectionConfig ?? new DebugAppletConfigurationSection());
            this.m_configuration.Options.Add(DebugAppletEnabledSetting, () => ConfigurationOptionType.Boolean);
            this.m_configuration.Values.Add(DebugAppletEnabledSetting, appServiceProviders.Any(t => typeof(DebugAppletManagerService) == t.Type));
            // Construct the configuration options
            return sectionConfig != null ? FeatureInstallState.Installed : FeatureInstallState.NotInstalled;
        }
    }
}
