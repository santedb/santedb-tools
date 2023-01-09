﻿using SanteDB.Core.Applets.Services;
using SanteDB.Core.Applets.Services.Impl;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using SanteDB.Client.Services;
using SanteDB.Tools.Debug.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using SanteDB.Client.UserInterface;

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
