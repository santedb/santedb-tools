using SanteDB.Core.Applets.Model.Extern.SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Applets.Services.Impl;
using SanteDB.Core.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace SanteDB.DevTools.Services
{
    /// <summary>
    /// Debug localization service
    /// </summary>
    public class DebugLocalizationService : AppletLocalizationService
    {

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(DebugLocalizationService));

        /// <summary>
        /// The root location where strings should be saved
        /// </summary>
        private readonly string m_stringSaveDirectory;

        // Locking object
        private readonly object m_lockObject = new object();

        // Resources file
        private readonly ConcurrentDictionary<string, ResourceFile> m_resourceFiles = new ConcurrentDictionary<string, ResourceFile>();

        /// <inheritdoc/>
        public DebugLocalizationService(IAppletManagerService appletManager, IAppletSolutionManagerService solutionManagerService = null) : base(appletManager, solutionManagerService)
        {

            this.m_stringSaveDirectory = Path.Combine(AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString() ?? Path.GetDirectoryName(typeof(DebugLocalizationService).Assembly.Location), "i18n");

            if (!Directory.Exists(this.m_stringSaveDirectory))
            {
                Directory.CreateDirectory(this.m_stringSaveDirectory);
            }
        }

        /// <inheritdoc/>
        public override string ServiceName => "Development Localization Service";

        /// <inheritdoc/>
        public override bool IsReadonly => false;

        /// <summary>
        /// Get the name of the resource file
        /// </summary>
        private string GetLocaleResourceFileName(String localeName)
        {
            var retVal = Path.Combine(this.m_stringSaveDirectory, localeName, "strings.xml");
            if (!Directory.Exists(Path.GetDirectoryName(retVal)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(retVal));
            }
            return retVal;
        }

        /// <inheritdoc/>
        public override IEnumerable<KeyValuePair<string, string>> GetStrings(string locale)
        {
            return this.GetCustomizedResourceStrings(locale).Union(base.GetStrings(locale));
        }

        /// <inheritdoc/>
        public override string GetString(string locale, string stringKey)
        {
            if(this.GetCustomizedResourceStrings(locale).TryGetValue(stringKey, out var retVal))
            {
                return retVal;
            }
            return base.GetString(locale, stringKey);
        }

        /// <inheritdoc/>
        public override void SetString(string locale, string stringKey, string value)
        {
            locale = locale ?? Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

            var resourceFile = this.GetCustomizedResources(locale);

            lock (this.m_lockObject)
            {
                resourceFile.Strings.RemoveAll(o => o.Key == stringKey);
                resourceFile.Strings.Add(new ExternalStringResource() { Key = stringKey, Value = value });

                try
                {
                    // Save the file
                    using (var fs = File.Create(this.GetLocaleResourceFileName(locale)))
                    {
                        resourceFile.Save(fs);
                    }
                }
                catch(Exception e)
                {
                    this.m_tracer.TraceError("Error saving resources file {0} - {1}", locale, e.ToHumanReadableString());
                }
            }

        }

        /// <summary>
        /// Get the customized resource strings 
        /// </summary>
        /// <param name="locale"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private IDictionary<string, string> GetCustomizedResourceStrings(string locale)
        {
            return this.GetCustomizedResources(locale).Strings.ToDictionaryIgnoringDuplicates(o => o.Key, o => o.Value);
        }

        /// <summary>
        /// Get the customized resource source file
        /// </summary>
        private ResourceFile GetCustomizedResources(string locale)
        {
            locale = locale ?? Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
            var resourceFileName = this.GetLocaleResourceFileName(locale);
            if (!this.m_resourceFiles.TryGetValue(locale, out var resourceFile))
            {
                lock (this.m_lockObject)
                {
                    if (File.Exists(resourceFileName))
                    {
                        using (var fs = File.OpenRead(resourceFileName))
                        {
                            resourceFile = ResourceFile.Load(fs);
                        }
                    }
                    else
                    {
                        resourceFile = new ResourceFile();
                    }
                    this.m_resourceFiles.TryAdd(locale, resourceFile);
                }
            }
            return resourceFile;
        }
    }
}
