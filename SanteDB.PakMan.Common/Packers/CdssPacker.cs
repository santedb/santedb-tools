using SanteDB.Cdss.Xml.Antlr;
using SanteDB.Cdss.Xml.Exceptions;
using SanteDB.Core.Applets.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SanteDB.PakMan.Packers
{
    /// <summary>
    /// A packer which transpiles CDSS definitions
    /// </summary>
    public class CdssPacker : IFilePacker
    {
        /// <inheritdoc/>
        public string[] Extensions => new string[] { ".cdss" };

        /// <inheritdoc/>
        public AppletAsset Process(string file, bool optimize)
        {
            try
            {
                using(var fs = System.IO.File.OpenRead(file))
                {
                    var tps = CdssLibraryTranspiler.Transpile(fs, true);
                    using(var ms = new MemoryStream())
                    {
                        tps.Save(ms);
                        return new AppletAsset()
                        {
                            Content = PakManTool.CompressContent(ms.ToArray()),
                            MimeType = "application/xml"
                        };
                    }
                }
            }
            catch(CdssTranspilationException e)
            {
                throw new Exception($"Could not transpile {file}", e);
            }
        }
    }
}
