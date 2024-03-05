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
using SanteDB.Client.Services;
using SanteDB.Client.UserInterface;
using SanteDB.Core;
using SanteDB.Core.Applets;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Services;
using SanteDB.DevTools.Configuration;
using SanteDB.PakMan;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Xml.Linq;

namespace SanteDB.Tools.Debug.Services
{
    /// <summary>
    /// The applet manager service which manages applets using files
    /// </summary>
    public class DebugAppletManagerService : IAppletManagerService, IAppletSolutionManagerService
    {

        // XSD SanteDB
        private static readonly XNamespace xs_santedb = "http://santedb.org/applet";

        /// <summary>
        /// Applet source setting to the manifest
        /// </summary>
        public const string APPLET_SOURCE = "$source";

        // Tracer for the file based applet manager
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(DebugAppletManagerService));

        // File system watchers which will re-process the applications
        private readonly Dictionary<String, FileSystemWatcher> m_fsWatchers = new Dictionary<string, FileSystemWatcher>();

        // Applet collection
        private AppletCollection m_appletCollection = new AppletCollection();

        // RO applet collection
        private ReadonlyAppletCollection m_readonlyAppletCollection;
        private AppletManifest m_solution;

        // Configuration 
        private readonly DebugAppletConfigurationSection m_configuration;
        private readonly IAppletHostBridgeProvider m_hostBridgeProvider;
        private readonly IThreadPoolService m_threadPoolService;

        /// <summary>
        /// New constructor for the applet manager
        /// </summary>
        public DebugAppletManagerService(IConfigurationManager configurationManager,
            IThreadPoolService threadPoolService,
            IServiceManager serviceManager,
            IAppletHostBridgeProvider hostBridgeProvider = null
            )
        {
            this.m_appletCollection = new AppletCollection();
            this.m_readonlyAppletCollection = this.m_appletCollection.AsReadonly();
            this.m_readonlyAppletCollection.CollectionChanged += (o, e) => this.Changed?.Invoke(o, e);
            this.m_configuration = configurationManager.GetSection<DebugAppletConfigurationSection>();
            this.m_hostBridgeProvider = hostBridgeProvider ?? serviceManager.CreateInjected<WebAppletHostBridgeProvider>();
            this.m_threadPoolService = threadPoolService;
            this.m_appletCollection.Resolver = this.ResolveAppletAsset;
            this.m_appletCollection.CachePages = false;
            this.Initialize();
        }

        /// <summary>
        /// Gets the applets installed on this provider
        /// </summary>
        public ReadonlyAppletCollection Applets => this.m_readonlyAppletCollection;

        /// <inheritdoc/>
        public string ServiceName => "Debug Applet Manager";

        /// <inheritdoc/>
        public IEnumerable<AppletSolution> Solutions
        {
            get
            {
                if (this.m_solution != null)
                {
                    yield return new AppletSolution()
                    {
                        Meta = this.m_solution?.Info,
                        Include = this.Applets.Select(o => o.CreatePackage()).ToList()
                    };
                }
            }
        }

        /// <summary>
        /// Fired when the applet contents have changed
        /// </summary>
        public event EventHandler Changed;


        /// <inheritdoc/>
        public AppletManifest GetApplet(string appletId)
        {
            return this.m_appletCollection.FirstOrDefault(o => o.Info.Id == appletId);
        }

        /// <summary>
        /// Get the package binaries for the applet
        /// </summary>
        public byte[] GetPackage(string appletId)
        {
            using (var ms = new MemoryStream())
            {
                var sourceApplet = this.GetApplet(appletId);
                var newApplet = new AppletManifest()
                {
                    Info = sourceApplet.Info,
                    Strings = sourceApplet.Strings,
                    ErrorAssets = sourceApplet.ErrorAssets,
                    Assets = new List<AppletAsset>(),
                    Locales = sourceApplet.Locales,
                    Settings = sourceApplet.Settings,
                    LoginAsset = sourceApplet.LoginAsset,
                    Menus = sourceApplet.Menus,
                    StartAsset = sourceApplet.StartAsset,
                    Templates = sourceApplet.Templates,
                    ViewModel = sourceApplet.ViewModel
                };
                foreach (var itm in sourceApplet.Assets)
                {
                    var newItm = new AppletAsset()
                    {
                        Content = itm.Content,
                        Language = itm.Language,
                        MimeType = itm.MimeType,
                        Name = itm.Name,
                        Policies = itm.Policies
                    };

                    if (newItm.Content == null)
                    {
                        switch (this.ResolveAppletAsset(itm))
                        {
                            case byte[] bytea:
                                newItm.Content = PakManTool.CompressContent(bytea);
                                break;
                            case String str:
                                newItm.Content = PakManTool.CompressContent(str);
                                break;
                            case Object o:
                                newItm.Content = o;
                                break;
                        }
                    }
                    newApplet.Assets.Add(newItm);
                }
                var package = newApplet.CreatePackage();
                package.Meta.Hash = SHA256.Create().ComputeHash(package.Manifest);
                package.Save(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Install the specified applet
        /// </summary>
        /// <remarks>Applets which are file based should have a hint path setting in their configuration attributes</remarks>
        public bool Install(AppletPackage package, bool isUpgrade = false)
        {
            return true; // We don't support installation on the debug context
        }

        /// <summary>
        /// Load an applet into the collection
        /// </summary>
        /// <param name="applet">The manifest to load</param>
        /// <returns>True if the manifest was added</returns>
        public bool LoadApplet(AppletManifest applet)
        {
            var sourceManifest = applet.Settings.Find(o => o.Name == APPLET_SOURCE)?.Value;
            if (applet.Info.Id == this.m_configuration.DefaultApplet)
            {
                this.m_appletCollection.DefaultApplet = this.m_readonlyAppletCollection.DefaultApplet = applet;
            }
            applet.Initialize();
            this.m_tracer.TraceInfo("Adding {0} -> {1}...", applet.Info.Id, sourceManifest);
            if (!String.IsNullOrEmpty(sourceManifest) && File.Exists(sourceManifest))
            {
                applet = this.LoadSourceApplet(sourceManifest, applet);
            }

            this.m_appletCollection.Add(applet);
            AppletCollection.ClearCaches();
            return true;
        }

        /// <summary>
        /// Load source applet
        /// </summary>
        private AppletManifest LoadSourceApplet(String manifestFile, AppletManifest applet)
        {
            if (applet.Assets.Count(o => !(o.Content is AppletAssetVirtual)) == 0)
            {
                var baseDirectory = Path.GetDirectoryName(manifestFile);
                if (!baseDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    baseDirectory += Path.DirectorySeparatorChar.ToString();
                }

                applet.Assets.AddRange(this.ProcessDirectory(baseDirectory, baseDirectory));

                // Watch for changes
                var fsr = new FileSystemWatcher(baseDirectory) { IncludeSubdirectories = true, EnableRaisingEvents = true };
                fsr.EnableRaisingEvents = true;
                fsr.Changed += fsw_Changed;
                fsr.Created += fsw_Changed;
                fsr.Deleted += fsw_Changed;
                fsr.Renamed += fsw_Changed;
                this.m_fsWatchers.Add(applet.Info.Id, fsr);


                applet.Initialize();
                if (applet.Info.Version.Contains("*"))
                {
                    applet.Info.Version = applet.Info.Version.Replace("*", "0000");
                }
            }
            else if (applet.Assets.Any(a => a.Name.Contains("santedb.js"))) // Inject SHIM
            {
                foreach (var itm in applet.Assets.Where(a => a.Name.Contains("santedb.js")))
                {
                    //System.Diagnostics.Debugger.Break();
                    if (itm.Content is byte[] ba && this.m_hostBridgeProvider != null)
                    {
                        itm.Content = System.Text.Encoding.UTF8.GetString(this.m_appletCollection.RenderAssetContent(itm)) + "\r\n" + this.m_hostBridgeProvider.GetBridgeScript();
                        AppletCollection.ClearCaches();

                    }
                }
            }
            return applet;
        }

        /// <summary>
        /// Unload the applet from the context
        /// </summary>
        public bool UnInstall(string appletId)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Process the specified directory
        /// </summary>
        private IEnumerable<AppletAsset> ProcessDirectory(string source, String path)
        {

            List<AppletAsset> retVal = new List<AppletAsset>();
            foreach (var itm in Directory.GetFiles(source))
            {
                var asset = this.ProcessItem(itm, path);
                if (asset != null)
                {
                    retVal.Add(asset);
                }
            }

            // Process sub directories
            foreach (var dir in Directory.GetDirectories(source))
            {
                if (!Path.GetFileName(dir).StartsWith("."))
                {
                    retVal.AddRange(ProcessDirectory(dir, path));
                }
                else
                {
                    Console.WriteLine("Skipping directory {0}", dir);
                }
            }

            return retVal;

        }

        /// <summary>
        /// Process a single item
        /// </summary>
        private AppletAsset ProcessItem(String source, String path)
        {
            Console.WriteLine("\t Processing {0}...", source);


            try
            {
                var asset = PakManTool.GetPacker(source).Process(source, false);
                asset.Name = PakManTool.TranslatePath(source.Replace(path, ""));
                asset.Content = null;
                return asset;
            }
            catch (IOException) // Timer the load
            {
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: Could not process file {0} due to {1}", source, e.Message);
                return null;
            }
        }


        /// <summary>
        /// File system watcher has changed, re-process directory
        /// </summary>
        private void fsw_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Get the applet that this change is for
                var fsWatcherInfo = this.m_fsWatchers.First(o => o.Value == sender);
                var applet = this.m_appletCollection.First(o => o.Info.Id == fsWatcherInfo.Key);
                var asset = applet.Assets.FirstOrDefault(o => o.Name.Equals(e.FullPath.Replace(fsWatcherInfo.Value.Path, "").Replace("\\", "/"), StringComparison.OrdinalIgnoreCase));

                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Created: // file has been created
                    case WatcherChangeTypes.Changed:

                        if (!File.Exists(e.FullPath))
                        {
                            return;
                        }
                        // Wait until file is not locked so we can process it
                        bool isEmpty = false;
                        while (this.IsFileLocked(e.FullPath, out isEmpty))
                        {
                            Thread.Sleep(100);
                        }
                        if (isEmpty)
                        {
                            return;
                        }

                        // Manifest has changed so re-process
                        if (e.Name.ToLower() == "manifest.xml")
                        {
                            if (!IsFileLocked(e.FullPath, out isEmpty) && !isEmpty)
                            {
                                try
                                {
                                    using (var fs = File.OpenRead(e.FullPath))
                                    {
                                        var newManifest = AppletManifest.Load(fs);
                                        applet.Settings = newManifest.Settings.Union(applet.Settings).ToList();
                                        applet.Info = newManifest.Info;
                                        applet.Menus = newManifest.Menus;
                                        applet.StartAsset = newManifest.StartAsset;
                                        applet.Strings = newManifest.Strings;
                                        applet.Templates = newManifest.Templates;
                                        applet.ViewModel = newManifest.ViewModel;
                                    }
                                }
                                catch (IOException)
                                {
                                    throw;
                                }
                                catch (Exception ex)
                                {
                                    this.m_tracer.TraceError("Error re-reading manifest: {0}", ex);
                                }
                            }
                        }
                        else
                        {
                            var newAsset = this.ProcessItem(e.FullPath, fsWatcherInfo.Value.Path);
                            if (newAsset != null)
                            {
                                // Add? 
                                if (asset != null)
                                {
                                    applet.Assets.Remove(asset);
                                }

                                applet.Assets.Add(newAsset);
                            }
                        }
                        applet.Initialize();

                        break;
                    case WatcherChangeTypes.Deleted:
                        applet.Assets.Remove(asset);
                        break;
                    case WatcherChangeTypes.Renamed:
                        asset = applet.Assets.FirstOrDefault(o => o.Name == (e as RenamedEventArgs).OldFullPath.Replace(fsWatcherInfo.Value.Path, ""));
                        if (asset != null)
                        {
                            asset.Name = e.Name;
                        }

                        break;
                }
                AppletCollection.ClearCaches();
                ApplicationServiceContext.Current.GetService<ILocalizationService>().Reload();
                this.Changed?.Invoke(this, EventArgs.Empty);
            }
            catch (IOException)
            { // This happens when process that created the file still has a lock - need to write a better version of this whole listener
                if (sender != this)
                {
                    this.m_threadPoolService.QueueUserWorkItem(_ =>
                    {
                        // HACK: Wait the thread and attempt reload
                        Thread.Sleep(500);
                        fsw_Changed(sender, e);
                    });
                }
            }
        }

        /// <summary>
        /// Get applet asset from the file system 
        /// </summary>
        private object ResolveAppletAsset(AppletAsset navigateAsset)
        {

            var manifestSource = navigateAsset.Manifest.GetSetting(APPLET_SOURCE);
            if (String.IsNullOrEmpty(manifestSource))
            {
                return null;
            }

            String itmPath = System.IO.Path.Combine(
                                        Path.GetDirectoryName(manifestSource),
                                        navigateAsset.Name).Replace('/', Path.DirectorySeparatorChar);

            if (!File.Exists(itmPath))
            {
                return navigateAsset.Content;
            }
            else if (navigateAsset.MimeType == "text/html")
            {
                XElement xe = XElement.Load(itmPath);

                // Now we have to iterate throuh and add the asset\
                AppletAssetHtml htmlAsset = null;

                if (xe.Elements().OfType<XElement>().Any(o => o.Name == xs_santedb + "widget"))
                {
                    var widgetEle = xe.Elements().OfType<XElement>().FirstOrDefault(o => o.Name == xs_santedb + "widget");
                    htmlAsset = new AppletWidget()
                    {
                        Icon = widgetEle.Element(xs_santedb + "icon")?.Value,
                        Type = (AppletWidgetType)Enum.Parse(typeof(AppletWidgetType), widgetEle.Attribute("type")?.Value),
                        Size = (AppletWidgetSize)Enum.Parse(typeof(AppletWidgetSize), widgetEle.Attribute("size")?.Value ?? "Medium"),
                        ColorClass = widgetEle.Attribute("headerClass")?.Value ?? "bg-light",
                        Priority = Int32.Parse(widgetEle.Attribute("priority")?.Value ?? "0"),
                        MaxStack = Int32.Parse(widgetEle.Attribute("maxStack")?.Value ?? "2"),
                        Order = Int32.Parse(widgetEle.Attribute("order")?.Value ?? "0"),
                        Context = widgetEle.Attribute("context")?.Value,
                        Description = widgetEle.Elements().Where(o => o.Name == xs_santedb + "description").Select(o => new LocaleString() { Value = o.Value, Language = o.Attribute("lang")?.Value }).ToList(),
                        Name = widgetEle.Attribute("name")?.Value,
                        Controller = widgetEle.Element(xs_santedb + "controller")?.Value,
                        Guard = widgetEle.Elements().Where(o => o.Name == xs_santedb + "guard").Select(o => o.Value).ToList(),
                        AlternateViews = widgetEle.Element(xs_santedb + "views")?.Elements().Where(o => o.Name == xs_santedb + "view").Select(o => new AppletWidgetView()
                        {
                            ViewType = (AppletWidgetViewType)Enum.Parse(typeof(AppletWidgetViewType), o.Attribute("type")?.Value ?? "None"),
                            Policies = o.Elements().Where(d => d.Name == xs_santedb + "demand").Select(d => d.Value).ToList()
                        }).ToList()
                    };

                    // TODO Guards
                }
                else
                {
                    htmlAsset = new AppletAssetHtml();
                    // View state data
                    htmlAsset.ViewState = xe.Elements().OfType<XElement>().Where(o => o.Name == xs_santedb + "state").Select(o => new AppletViewState()
                    {
                        Name = o.Attribute("name")?.Value,
                        Priority = Int32.Parse(o.Attribute("priority")?.Value ?? "0"),
                        Route = o.Elements().OfType<XElement>().FirstOrDefault(r => r.Name == xs_santedb + "url" || r.Name == xs_santedb + "route")?.Value,
                        IsAbstract = Boolean.Parse(o.Attribute("abstract")?.Value ?? "False"),
                        View = o.Elements().OfType<XElement>().Where(v => v.Name == xs_santedb + "view")?.Select(v => new AppletView()
                        {
                            Priority = Int32.Parse(o.Attribute("priority")?.Value ?? "0"),
                            Name = v.Attribute("name")?.Value,
                            Controller = v.Element(xs_santedb + "controller")?.Value
                        }).ToList()
                    }).FirstOrDefault();
                    htmlAsset.Titles = xe.Elements().OfType<XElement>().Where(t => t.Name == xs_santedb + "title")?.Select(t => new LocaleString()
                    {
                        Language = t.Attribute("lang")?.Value,
                        Value = t?.Value
                    }).ToList();
                    htmlAsset.Static = xe.Attribute(xs_santedb + "static")?.Value == "true";
                }

                htmlAsset.Titles = new List<LocaleString>(xe.Descendants().OfType<XElement>().Where(o => o.Name == xs_santedb + "title").Select(o => new LocaleString() { Language = o.Attribute("lang")?.Value, Value = o.Value }));
                htmlAsset.Bundle = new List<string>(xe.Descendants().OfType<XElement>().Where(o => o.Name == xs_santedb + "bundle").Select(o => this.CorrectAppletName(o.Value)));
                htmlAsset.Script = new List<AssetScriptReference>(xe.Descendants().OfType<XElement>().Where(o => o.Name == xs_santedb + "script").Select(o => new AssetScriptReference()
                {
                    Reference = this.CorrectAppletName(o.Value),
                    IsStatic = Boolean.Parse(o.Attribute("static")?.Value ?? "true")
                }));
                htmlAsset.Style = new List<string>(xe.Descendants().OfType<XElement>().Where(o => o.Name == xs_santedb + "style").Select(o => this.CorrectAppletName(o.Value)));

                var demand = xe.DescendantNodes().OfType<XElement>().Where(o => o.Name == xs_santedb + "demand").Select(o => o.Value).ToList();

                var includes = xe.DescendantNodes().OfType<XComment>().Where(o => o?.Value?.Trim().StartsWith("#include virtual=\"") == true).ToList();
                foreach (var inc in includes)
                {
                    String assetName = inc.Value.Trim().Substring(18); // HACK: Should be a REGEX
                    if (assetName.EndsWith("\""))
                    {
                        assetName = assetName.Substring(0, assetName.Length - 1);
                    }

                    if (assetName == "content")
                    {
                        continue;
                    }

                    var includeAsset = this.CorrectAppletName(assetName);
                    inc.AddAfterSelf(new XComment(String.Format("#include virtual=\"{0}\"", includeAsset)));
                    inc.Remove();
                }

                var xel = xe.Descendants().OfType<XElement>().Where(o => o.Name.Namespace == xs_santedb).ToList();
                if (xel != null)
                {
                    foreach (var x in xel)
                    {
                        x.Remove();
                    }
                }

                htmlAsset.Html = xe;
                return htmlAsset;
            }
            else if (navigateAsset.MimeType == "text/javascript" ||
                navigateAsset.MimeType == "text/css" ||
                navigateAsset.MimeType == "application/json" ||
                navigateAsset.MimeType == "text/json" ||
                navigateAsset.MimeType == "text/xml")
            {
                var script = File.ReadAllText(itmPath);
                if (itmPath.Contains("santedb.js") || itmPath.Contains("santedb.min.js"))
                {
                    script += this.m_hostBridgeProvider?.GetBridgeScript();
                }

                return script;
            }
            else
            {
                return File.ReadAllBytes(itmPath);
            }
        }

        /// <summary>
        /// Determines whether the specified file is locked
        /// </summary>
        private bool IsFileLocked(String fileName, out bool isEmpty)
        {
            FileStream stream = null;
            try
            {
                stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                isEmpty = stream.Length == 0;
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                isEmpty = false;
                return true;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }

            //file is not locked
            return false;
        }


        /// <inheritdoc/>
        public void Initialize()
        {
            try
            {
                this.LoadReferences();
                this.LoadSolution();
                this.LoadApplets();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Could not start the debug application context", e);
            }
        }

        /// <summary>
        /// Load applets which are referenced in the configuration file
        /// </summary>
        private void LoadApplets()
        {
            if (this.m_configuration.AppletReferences?.Any() == true)
            {
                foreach (var appletDir in this.m_configuration.AppletReferences)
                {
                    try
                    {
                        if (!Directory.Exists(appletDir) || !File.Exists(Path.Combine(appletDir, "manifest.xml")))
                        {
                            throw new DirectoryNotFoundException($"Applet {appletDir} not found");
                        }

                        String appletPath = Path.Combine(appletDir, "manifest.xml");
                        using (var fs = File.OpenRead(appletPath))
                        {
                            AppletManifest manifest = AppletManifest.Load(fs);
                            manifest.AddSetting(APPLET_SOURCE, appletPath);
                            this.LoadApplet(manifest);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }

            if (m_configuration.AppletsToDebug?.Any() == true)
            {
                foreach (var appletdir in m_configuration.AppletsToDebug)
                {
                    try
                    {
                        if (!Directory.Exists(appletdir) || !File.Exists(Path.Combine(appletdir, "manifest.xml")))
                        {
                            throw new DirectoryNotFoundException($"Applet {appletdir} not found");
                        }

                        String appletPath = Path.Combine(appletdir, "manifest.xml");
                        using (var fs = File.OpenRead(appletPath))
                        {
                            AppletManifest manifest = AppletManifest.Load(fs);
                            manifest.AddSetting(APPLET_SOURCE, appletPath);
                            this.LoadApplet(manifest);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Resolve the specified applet name
        /// </summary>
        private String CorrectAppletName(string value)
        {
            return value?.Replace("\\", "/");
        }

        /// <summary>
        /// Load the solution to debug
        /// </summary>
        private void LoadSolution()
        {
            if (!String.IsNullOrEmpty(this.m_configuration.SolutionToDebug))
            {
                using (var fs = File.OpenRead(this.m_configuration.SolutionToDebug))
                {
                    var solution = AppletManifest.Load(fs);

                    this.m_solution = solution;
                    // Load include elements
                    var solnDir = Path.GetDirectoryName(this.m_configuration.SolutionToDebug);

                    // Preload any manifests
                    var refManifests = new List<AppletManifest>();
                    // Load reference manifests
                    foreach (var mfstFile in Directory.GetFiles(solnDir, "manifest.xml", SearchOption.AllDirectories))
                    {
                        using (var manifestStream = File.OpenRead(mfstFile))
                        {
                            var manifest = AppletManifest.Load(manifestStream);
                            manifest.AddSetting(APPLET_SOURCE, mfstFile);
                            refManifests.Add(manifest);
                        }
                    }

                    // Load dependencies
                    foreach (var dep in solution.Info.Dependencies)
                    {
                        // Attempt to load the appropriate manifest file
                        var cand = refManifests.FirstOrDefault(o => o.Info.Id == dep.Id);
                        if (cand != null)
                        {
                            this.LoadApplet(cand);
                        }
                        else if (!this.Applets.Any(a => a.Info.Id == dep.Id))
                        {
                            this.m_configuration.AppletReferences.Add($"{dep.Id}:{dep.Version ?? "*"}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Load all references
        /// </summary>
        private void LoadReferences()
        {
            if (this.m_configuration.AppletReferences?.Any() == true)
            {
                foreach (var refString in this.m_configuration.AppletReferences)
                {
                    if (File.Exists(refString)) // File reference
                    {
                        using (var fs = File.OpenRead(refString))
                        {
                            var appletPackage = AppletPackage.Load(fs);
                            if (appletPackage is AppletSolution solution)
                            {
                                foreach (var itm in solution.Include)
                                {
                                    this.LoadApplet(itm.Unpack());
                                }
                            }
                            else
                            {
                                this.LoadApplet(appletPackage.Unpack());
                            }
                        }
                    }
                    else // Pakman reference
                    {
                        var appletName = AppletName.Parse(refString);
                        var resolvedPackage = PakMan.Repository.PackageRepositoryUtil.GetFromAny(appletName.Id, appletName.GetVersion());
                        if (resolvedPackage == null)
                        {
                            throw new KeyNotFoundException(appletName.ToString());
                        }
                        if (resolvedPackage is AppletSolution solution)
                        {
                            foreach (var inc in solution.Include)
                            {
                                this.LoadApplet(inc.Unpack());
                            }
                        }
                        else
                        {
                            this.LoadApplet(resolvedPackage.Unpack());
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public ReadonlyAppletCollection GetApplets(string solutionId)
        {
            return this.Applets; // There is only one "solution" here
        }

        /// <inheritdoc/>
        public bool Install(AppletSolution solution, bool isUpgrade = false)
        {
            return true;
        }

        /// <inheritdoc/>
        public AppletManifest GetApplet(string solutionId, string appletId)
        {
            return this.GetApplet(appletId);
        }

        /// <inheritdoc/>
        public byte[] GetPackage(string solutionId, string appletId)
        {
            return this.GetPackage(appletId);
        }
    }
}
