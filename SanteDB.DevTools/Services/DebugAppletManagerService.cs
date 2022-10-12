using SanteDB.Core.Applets;
using SanteDB.Core.Applets.Configuration;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Services;
using SanteDB.Disconnected.Services;
using SanteDB.PakMan;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SanteDB.Tools.Debug.Services
{
    /// <summary>
    /// The applet manager service which manages applets using files
    /// </summary>
    public class DebugAppletManagerService : IAppletManagerService
    {


        // XSD SanteDB
        private static readonly XNamespace xs_santedb = "http://santedb.org/applet";

        /// <summary>
        /// Applet source setting to the manifest
        /// </summary>
        public const string APPLET_SOURCE = "$source";

        // Tracer for the file based applet manager
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(DebugAppletManagerService));

        // Applet bas directory
        private readonly Dictionary<AppletManifest, String> m_appletBaseDir = new Dictionary<AppletManifest, string>();

        // File system watchers which will re-process the applications
        private readonly Dictionary<String, FileSystemWatcher> m_fsWatchers = new Dictionary<string, FileSystemWatcher>();

        // Applet collection
        private AppletCollection m_appletCollection = new AppletCollection();

        // RO applet collection
        private ReadonlyAppletCollection m_readonlyAppletCollection;

        // Configuration 
        private readonly AppletConfigurationSection m_configuration;
        private readonly IAppletHostBridgeProvider m_hostBridgeProvider;
        private readonly ILocalizationService m_localizationService;
        private readonly IThreadPoolService m_threadPoolService;

        /// <summary>
        /// New constructor for the applet manager
        /// </summary>
        public DebugAppletManagerService(IConfigurationManager configurationManager, 
            IAppletHostBridgeProvider hostBridgeProvider, 
            ILocalizationService localizationService,
            IThreadPoolService threadPoolService)
        {
            this.m_appletCollection = new AppletCollection();
            this.m_readonlyAppletCollection = this.m_appletCollection.AsReadonly();
            this.m_readonlyAppletCollection.CollectionChanged += (o, e) => this.Changed?.Invoke(o, e);
            this.m_configuration = configurationManager.GetSection<AppletConfigurationSection>();
            this.m_hostBridgeProvider = hostBridgeProvider;
            this.m_localizationService = localizationService;
            this.m_threadPoolService = threadPoolService;
        }

        /// <summary>
        /// Gets the applets installed on this provider
        /// </summary>
        public ReadonlyAppletCollection Applets => this.m_readonlyAppletCollection;

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
                this.GetApplet(appletId).CreatePackage().Save(ms);
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
            if (!String.IsNullOrEmpty(sourceManifest) && File.Exists(sourceManifest))
            {
                applet = this.LoadSourceApplet(sourceManifest, applet);
            }

            this.m_tracer.TraceInfo("Adding reference {0}...", applet.Info.Id);
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
                    baseDirectory += Path.DirectorySeparatorChar.ToString();
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
                    applet.Info.Version = applet.Info.Version.Replace("*", "0000");
            }
            else if (applet.Assets.Any(a => a.Name.Contains("santedb.js"))) // Inject SHIM
            {
                foreach (var itm in applet.Assets.Where(a => a.Name.Contains("santedb.js")))
                {
                    //System.Diagnostics.Debugger.Break();
                    if (itm.Content is byte[] ba)
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
                    retVal.Add(asset);

            }

            // Process sub directories
            foreach (var dir in Directory.GetDirectories(source))
                if (!Path.GetFileName(dir).StartsWith("."))
                    retVal.AddRange(ProcessDirectory(dir, path));
                else
                    Console.WriteLine("Skipping directory {0}", dir);

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

                if (Path.GetFileName(source).ToLower() == "manifest.xml")
                    return null;
                else
                    switch (Path.GetExtension(source))
                    {
                        case ".html":
                        case ".htm":
                        case ".xhtml":
                            XElement xe = XElement.Load(source);
                            // Now we have to iterate throuh and add the asset\

                            var demand = xe.DescendantNodes().OfType<XElement>().Where(o => o.Name == xs_santedb + "demand").Select(o => o.Value).ToList();


                            return new AppletAsset()
                            {
                                Name = CorrectAppletName(source.Replace(path, "")),
                                MimeType = "text/html",
                                Content = null,
                                Policies = demand

                            };
                        case ".css":
                            return new AppletAsset()
                            {
                                Name = CorrectAppletName(source.Replace(path, "")),
                                MimeType = "text/css",
                                Content = null
                            };
                        case ".js":
                            return new AppletAsset()
                            {
                                Name = CorrectAppletName(source.Replace(path, "")),
                                MimeType = "text/javascript",
                                Content = null
                            };
                        case ".json":
                            return new AppletAsset()
                            {
                                Name = CorrectAppletName(source.Replace(path, "")),
                                MimeType = "application/json",
                                Content = null
                            };

                        default:
                            string mt = null;
                            return new AppletAsset()
                            {
                                Name = CorrectAppletName(source.Replace(path, "")),
                                MimeType = MimeMapping.MimeUtility.GetMimeMapping(source) ?? "application/octet-stream",
                                Content = null
                            };
                    }
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

                        if (!File.Exists(e.FullPath)) return;
                        // Wait until file is not locked so we can process it
                        bool isEmpty = false;
                        while (this.IsFileLocked(e.FullPath, out isEmpty))
                        {
                            Thread.Sleep(100);
                        }
                        if (isEmpty) return;

                        // Manifest has changed so re-process
                        if (e.Name.ToLower() == "manifest.xml")
                        {
                            if (!IsFileLocked(e.FullPath, out isEmpty) && !isEmpty)
                                try
                                {
                                    using (var fs = File.OpenRead(e.FullPath))
                                    {
                                        var newManifest = AppletManifest.Load(fs);
                                        applet.Settings = newManifest.Settings;
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
                        else
                        {
                            var newAsset = this.ProcessItem(e.FullPath, fsWatcherInfo.Value.Path);
                            if (newAsset != null)
                            {
                                // Add? 
                                if (asset != null)
                                    applet.Assets.Remove(asset);
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
                        if (asset != null) asset.Name = e.Name;
                        break;
                }
                AppletCollection.ClearCaches();
                this.m_localizationService.Reload();
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
                    stream.Close();
            }

            //file is not locked
            return false;
        }


        /// <summary>
        /// Resolve the specified applet name
        /// </summary>
        private String CorrectAppletName(string value)
        {
            return value?.ToLower().Replace("\\", "/");
        }
    }
}
