using SanteDB.Core.Applets;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Diagnostics;
using SanteDB.PakMan;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Tools.Debug.Services
{
    /// <summary>
    /// The applet manager service which manages applets using files
    /// </summary>
    public class DebugAppletManagerService : IAppletManagerService
    {
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

        /// <summary>
        /// New constructor for the applet manager
        /// </summary>
        public DebugAppletManagerService()
        {
            this.m_appletCollection = new AppletCollection();
            this.m_readonlyAppletCollection = this.m_appletCollection.AsReadonly();
            this.m_readonlyAppletCollection.CollectionChanged += (o, e) => this.Changed?.Invoke(o, e);
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

        public bool Install(AppletPackage package, bool isUpgrade = false)
        {
            throw new NotImplementedException();
        }

        public bool LoadApplet(AppletManifest applet)
        {
            throw new NotImplementedException();
        }

        public bool UnInstall(string appletId)
        {
            throw new NotImplementedException();
        }
    }
}
