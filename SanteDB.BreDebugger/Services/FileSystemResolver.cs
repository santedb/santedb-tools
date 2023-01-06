using SanteDB.BusinessRules.JavaScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.SDK.BreDebugger.Services
{
    /// <summary>
    /// File system resolver
    /// </summary>
    internal class FileSystemResolver : IDataReferenceResolver
    {
        public String RootDirectory { get; set; }

        public FileSystemResolver()
        {
            this.RootDirectory = Environment.CurrentDirectory;
        }

        /// <summary>
        /// Resolve specified reference
        /// </summary>
        public Stream Resolve(string reference)
        {
            reference = reference.Replace("~", this.RootDirectory);
            if (File.Exists(reference))
                return File.OpenRead(reference);
            else
            {
                Console.Error.WriteLine("ERR: {0}", reference);
                return null;
            }
        }
    }
}
