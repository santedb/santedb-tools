using SanteDB.PakMan.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.PakMan
{
    /// <summary>
    /// Management API
    /// </summary>
    internal class Manager
    {

        // Parameters
        private readonly PakManParameters m_parameters;

        /// <summary>
        /// Create a new manager class
        /// </summary>
        public Manager(PakManParameters parameters)
        {
            this.m_parameters = parameters;
        }

        /// <summary>
        /// Get the package
        /// </summary>
        public int Get()
        {
            try
            {
                var version = this.m_parameters.Version;
                if(String.IsNullOrEmpty(version))
                {
                    Emit.Message("INFO", "Retrieving latest version for {0}...", this.m_parameters.Get);
                    version = PackageRepositoryUtil.FindFromAny(o => o.Id == this.m_parameters.Get, 0, 50).OrderByDescending(o => o.GetVersion()).FirstOrDefault()?.Version;
                }
                if(String.IsNullOrEmpty(version))
                {
                    throw new InvalidOperationException($"Cannot find any versions of {this.m_parameters.Get}");
                }

                var output = this.m_parameters.Output;
                if(String.IsNullOrEmpty(output))
                {
                    output = $"{this.m_parameters.Get}-{version}.pak";
                }

                Emit.Message("INFO", "Fetching {0} version {1} -> {2}", this.m_parameters.Get, version, output);
                var fetched = PackageRepositoryUtil.GetFromAny(this.m_parameters.Get, new Version(version));
                if (fetched == null)
                {
                    throw new InvalidOperationException($"Cannot fetch {this.m_parameters.Get}/{version}");
                }

                using(var stream = File.Create(output))
                {
                    fetched.Save(stream);
                }

                if(this.m_parameters.Install)
                {
                    Emit.Message("INFO", "Installing package to cache...");
                    PackageRepositoryUtil.InstallCache(fetched);
                }

                return 0;
            }
            catch (Exception e)
            {
                Emit.Message("ERROR", e.Message);
                //Console.Error.WriteLine("Cannot compose solution {0}: {1}", this.m_parms.Source, e);
                return -1;
            }
        }
    }
}
