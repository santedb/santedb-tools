using SanteDB.Core.Applets;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Diagnostics;
using System;
using System.Collections.Generic;
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
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(DebugAppletManagerService));

        public ReadonlyAppletCollection Applets => throw new NotImplementedException();

        public event EventHandler Changed;

        public AppletManifest GetApplet(string appletId)
        {
            throw new NotImplementedException();
        }

        public byte[] GetPackage(string appletId)
        {
            throw new NotImplementedException();
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
