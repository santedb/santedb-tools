/*
 * Portions Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2022 SanteSuite Contributors (See NOTICE)
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
 * DatERROR: 2021-8-27
 */
using SanteDB.Core;
using SanteDB.Core.Applets;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Services;
using SanteDB.DisconnectedClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;

namespace SanteDB.SDK.AppletDebugger
{
    /// <summary>
    /// Applet manager service which overrides the local applet manager service
    /// </summary>
    /// <remarks>This file is different than the UI Core service in that it allows opening of files from the hard drive rather than PAK files</remarks>
    public class MiniAppletManagerService : LocalAppletManagerService, IDisposable
    {

        // XSD SanteDB
        private static readonly XNamespace xs_santedb = "http://santedb.org/applet";

        // Applet bas directory
        internal Dictionary<AppletManifest, String> m_appletBaseDir = new Dictionary<AppletManifest, string>();

        // File system watchers which will re-process the applications
        private Dictionary<String, FileSystemWatcher> m_fsWatchers = new Dictionary<string, FileSystemWatcher>();

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(MiniAppletManagerService));

        /// <summary>
        /// Install applet
        /// </summary>
        /// <param name="package"></param>
        /// <param name="isUpgrade"></param>
        /// <returns></returns>
        public override bool Install(AppletPackage package, bool isUpgrade = false)
        {
            return false;
        }

        /// <summary>
        /// Mime types
        /// </summary>
        private readonly Dictionary<String, String> s_mime = new Dictionary<string, string>()
        {
            { ".eot", "application/vnd.ms-fontobject" },
            { ".woff", "application/font-woff" },
            { ".woff2", "application/font-woff2" },
            { ".ttf", "application/octet-stream" },
            { ".svg", "image/svg+xml" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".gif", "image/gif" },
            { ".png", "image/png" },
            { ".bmp", "image/bmp" },
            { ".json", "application/json" }

        };

        /// <summary>
        /// Resolving of assets
        /// </summary>
        public MiniAppletManagerService()
        {
            this.m_appletCollection.Resolver = this.ResolveAppletAsset;
            this.m_appletCollection.CachePages = false;
        }

        /// <summary>
        /// Resolve the specified applet name
        /// </summary>
        private String ResolveName(string value)
        {
            return value?.ToLower().Replace("\\", "/");
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
                                Name = ResolveName(source.Replace(path, "")),
                                MimeType = "text/html",
                                Content = null,
                                Policies = demand

                            };
                        case ".css":
                            return new AppletAsset()
                            {
                                Name = ResolveName(source.Replace(path, "")),
                                MimeType = "text/css",
                                Content = null
                            };
                        case ".js":
                            return new AppletAsset()
                            {
                                Name = ResolveName(source.Replace(path, "")),
                                MimeType = "text/javascript",
                                Content = null
                            };
                        case ".json":
                            return new AppletAsset()
                            {
                                Name = ResolveName(source.Replace(path, "")),
                                MimeType = "application/json",
                                Content = null
                            };

                        default:
                            string mt = null;
                            return new AppletAsset()
                            {
                                Name = ResolveName(source.Replace(path, "")),
                                MimeType = s_mime.TryGetValue(Path.GetExtension(source), out mt) ? mt : "application/octet-stream",
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
                                        applet.Configuration = newManifest.Configuration;
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
                ApplicationServiceContext.Current.GetService<ILocalizationService>().Reload();
                base.NotifyChanged();
            }
            catch (IOException)
            { // This happens when process that created the file still has a lock - need to write a better version of this whole listener
                if (sender != this)
                    ApplicationServiceContext.Current.GetService<IThreadPoolService>().QueueUserWorkItem(_ =>
                    {
                        // HACK: Wait the thread and attempt reload
                        Thread.Sleep(500);
                        fsw_Changed(sender, e);
                    });
            }
        }

        /// <summary>
        /// Load applet
        /// </summary>
        public override bool LoadApplet(AppletManifest applet)
        {
            if (applet.Assets.Count(o => !(o.Content is AppletAssetVirtual)) == 0)
            {
                var baseDirectory = this.m_appletBaseDir[applet];
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
            else if(applet.Assets.Any(a=>a.Name.Contains("santedb.js"))) // Inject SHIM
            {
                foreach(var itm in applet.Assets.Where(a=>a.Name.Contains("santedb.js")))
                {
                    //System.Diagnostics.Debugger.Break();
                    if(itm.Content is byte[] ba)
                    {
                        itm.Content = System.Text.Encoding.UTF8.GetString(this.m_appletCollection.RenderAssetContent(itm)) + "\r\n" + this.GetShimMethods() ;
                        AppletCollection.ClearCaches();

                    }
                }
            }
            return base.LoadApplet(applet);
        }

        /// <summary>
        /// Get applet asset
        /// </summary>
        public object ResolveAppletAsset(AppletAsset navigateAsset)
        {

            String itmPath = System.IO.Path.Combine(
                                        this.m_appletBaseDir[navigateAsset.Manifest],
                                        navigateAsset.Name).Replace('/', Path.DirectorySeparatorChar);

            if (!File.Exists(itmPath))
                return null;
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
                        View = (AppletWidgetView)Enum.Parse(typeof(AppletWidgetView), widgetEle.Attribute("altViews")?.Value ?? "None"),
                        ColorClass = widgetEle.Attribute("headerClass")?.Value ?? "bg-light",
                        Priority = Int32.Parse(widgetEle.Attribute("priority")?.Value ?? "0"),
                        MaxStack = Int32.Parse(widgetEle.Attribute("maxStack")?.Value ?? "2"),
                        Order = Int32.Parse(widgetEle.Attribute("order")?.Value ?? "0"),
                        Context = widgetEle.Attribute("context")?.Value,
                        Description = widgetEle.Elements().Where(o => o.Name == xs_santedb + "description").Select(o => new LocaleString() { Value = o.Value, Language = o.Attribute("lang")?.Value }).ToList(),
                        Name = widgetEle.Attribute("name")?.Value,
                        Controller = widgetEle.Element(xs_santedb + "controller")?.Value,
                        Guard = widgetEle.Elements().Where(o => o.Name == xs_santedb + "guard").Select(o => o.Value).ToList()
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
                    htmlAsset.Layout = ResolveName(xe.Attribute(xs_santedb + "layout")?.Value);
                    htmlAsset.Static = xe.Attribute(xs_santedb + "static")?.Value == "true";
                }

                htmlAsset.Titles = new List<LocaleString>(xe.Descendants().OfType<XElement>().Where(o => o.Name == xs_santedb + "title").Select(o => new LocaleString() { Language = o.Attribute("lang")?.Value, Value = o.Value }));
                htmlAsset.Bundle = new List<string>(xe.Descendants().OfType<XElement>().Where(o => o.Name == xs_santedb + "bundle").Select(o => ResolveName(o.Value)));
                htmlAsset.Script = new List<AssetScriptReference>(xe.Descendants().OfType<XElement>().Where(o => o.Name == xs_santedb + "script").Select(o => new AssetScriptReference()
                {
                    Reference = ResolveName(o.Value),
                    IsStatic = Boolean.Parse(o.Attribute("static")?.Value ?? "true")
                }));
                htmlAsset.Style = new List<string>(xe.Descendants().OfType<XElement>().Where(o => o.Name == xs_santedb + "style").Select(o => ResolveName(o.Value)));

                var demand = xe.DescendantNodes().OfType<XElement>().Where(o => o.Name == xs_santedb + "demand").Select(o => o.Value).ToList();

                var includes = xe.DescendantNodes().OfType<XComment>().Where(o => o?.Value?.Trim().StartsWith("#include virtual=\"") == true).ToList();
                foreach (var inc in includes)
                {
                    String assetName = inc.Value.Trim().Substring(18); // HACK: Should be a REGEX
                    if (assetName.EndsWith("\""))
                        assetName = assetName.Substring(0, assetName.Length - 1);
                    if (assetName == "content")
                        continue;
                    var includeAsset = ResolveName(assetName);
                    inc.AddAfterSelf(new XComment(String.Format("#include virtual=\"{0}\"", includeAsset)));
                    inc.Remove();
                }

                var xel = xe.Descendants().OfType<XElement>().Where(o => o.Name.Namespace == xs_santedb).ToList();
                if (xel != null)
                    foreach (var x in xel)
                        x.Remove();
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
                    script += this.GetShimMethods();
                return script;
            }
            else
                return File.ReadAllBytes(itmPath);
        }

        /// <summary>
        /// Get the SHIM methods
        /// </summary>
        /// <returns></returns>
        public String GetShimMethods()
        {

            // Load the default SHIM
            // Write the generated shims
            using (StringWriter tw = new StringWriter())
            {
                tw.WriteLine("/// START SANTEDB SHIM");
                // Version
                tw.WriteLine("__SanteDBAppService.GetMagic = function() {{ return '{0}'; }}", ApplicationContext.Current.ExecutionUuid);
                tw.WriteLine("__SanteDBAppService.GetVersion = function() {{ return '{0} ({1})'; }}", typeof(SanteDBConfiguration).Assembly.GetName().Version, typeof(SanteDBConfiguration).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
                tw.WriteLine("__SanteDBAppService.GetString = function(key) {");
                tw.WriteLine("\tvar strData = __SanteDBAppService._stringData[__SanteDBAppService.GetLocale()] || __SanteDBAppService._stringData['en'];");
                tw.WriteLine("\treturn strData[key] || key;");
                tw.WriteLine("}");

                tw.WriteLine("__SanteDBAppService._stringData = {};");
                var languages = this.Applets.SelectMany(a => a.Strings).Select(o => o.Language).Distinct();
                foreach (var lang in languages)
                {
                    tw.WriteLine("\t__SanteDBAppService._stringData['{0}'] = {{", lang);
                    foreach (var itm in ApplicationContext.Current.GetService<ILocalizationService>().GetStrings(lang))
                    {
                        tw.WriteLine("\t\t'{0}': '{1}',", itm.Key, itm.Value?.EncodeAscii().Replace("'", "\\'").Replace("\r", "").Replace("\n", ""));
                    }
                    tw.WriteLine("\t\t'none':'none' };");
                }

                tw.WriteLine("__SanteDBAppService.GetTemplateForm = function(templateId) {");
                tw.WriteLine("\tswitch(templateId) {");
                foreach (var itm in this.Applets.SelectMany(o => o.Templates))
                {
                    tw.WriteLine("\t\tcase '{0}': return '{1}'; break;", itm.Mnemonic.ToLowerInvariant(), itm.Form);
                }
                tw.WriteLine("\t}");
                tw.WriteLine("}");

                tw.WriteLine("__SanteDBAppService.GetTemplateView = function(templateId) {");
                tw.WriteLine("\tswitch(templateId) {");
                foreach (var itm in this.Applets.SelectMany(o => o.Templates))
                {
                    tw.WriteLine("\t\tcase '{0}': return '{1}'; break;", itm.Mnemonic.ToLowerInvariant(), itm.View);
                }
                tw.WriteLine("\t}");
                tw.WriteLine("}");

                tw.WriteLine("__SanteDBAppService.GetTemplates = function() {");
                tw.WriteLine("return '[{0}]'", String.Join(",", this.Applets.SelectMany(o => o.Templates).Where(o => o.Public).Select(o => $"\"{o.Mnemonic}\"")));
                tw.WriteLine("}");
                tw.WriteLine("__SanteDBAppService.GetDataAsset = function(assetId) {");
                tw.WriteLine("\tswitch(assetId) {");
                foreach (var itm in this.Applets.SelectMany(o => o.Assets).Where(o => o.Name.StartsWith("data/")))
                    tw.WriteLine("\t\tcase '{0}': return '{1}'; break;", itm.Name.Replace("data/", ""), Convert.ToBase64String(this.Applets.RenderAssetContent(itm)).Replace("'", "\\'"));
                tw.WriteLine("\t}");
                tw.WriteLine("}");
                // Read the static shim
                using (StreamReader shim = new StreamReader(typeof(MiniApplicationContext).Assembly.GetManifestResourceStream("AppletDebugger.lib.shim.js")))
                    tw.Write(shim.ReadToEnd());

                return tw.ToString();
            }
        }

        /// <summary>
        /// Dispose the fsrs
        /// </summary>
        public void Dispose()
        {
            foreach (var fsr in this.m_fsWatchers)
                fsr.Value.Dispose();
        }
    }
}
