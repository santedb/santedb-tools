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
using SanteDB.PakMan.Packers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SanteDB.PakMan
{
    /// <summary>
    /// Package manager constants
    /// </summary>
    public static class PakManTool
    {

        // File packers
        private static IDictionary<String, IFilePacker> m_packers;

        /// <summary>
        /// XML namespace for applets
        /// </summary>
        public const string XS_APPLET = "http://santedb.org/applet";

        /// <summary>
        /// XML namepace for html
        /// </summary>
        public const string XS_HTML = "http://www.w3.org/1999/xhtml";

        /// <summary>
        /// Resolve the specified applet name
        /// </summary>
        public static String TranslatePath(string value)
        {

            return value?.ToLower().Replace("\\", "/");
        }

        /// <summary>
        /// Gets the file packager for the specifie string
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static IFilePacker GetPacker(String file)
        {
            var ext = Path.GetExtension(file);
            if (m_packers == null)
                m_packers = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic)
                    .SelectMany(a => a.ExportedTypes)
                    .Where(t => typeof(IFilePacker).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                    .Select(t => Activator.CreateInstance(t))
                    .OfType<IFilePacker>()
                    .SelectMany(i => i.Extensions.Select(e => new { Ext = e, Pakr = i }))
                    .ToDictionary(o => o.Ext, o => o.Pakr);


            if (m_packers.TryGetValue(ext, out IFilePacker retVal))
                return retVal;
            else return m_packers["*"];
        }

        internal static string ApplyVersion(string version1, object version2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Apply version code
        /// </summary>
        internal static string ApplyVersion(string version)
        {
            return version.Replace("*", (DateTime.Now.Subtract(new DateTime(DateTime.Now.Year, 1, 1)).TotalSeconds % 100000).ToString("00000"));
        }
        /// <summary>
        /// Compress content
        /// </summary>
        public static byte[] DeCompressContent(byte[] content)
        {
            using (var ms = new MemoryStream(content))
            {
                using (var cs = new SharpCompress.Compressors.LZMA.LZipStream(ms, SharpCompress.Compressors.CompressionMode.Decompress))
                {
                    using(var oms = new MemoryStream())
                    {
                        cs.CopyTo(oms);
                        return oms.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Compress content
        /// </summary>
        public static byte[] CompressContent(byte[] content)
        {
            using (var ms = new MemoryStream())
            {
                using (var cs = new SharpCompress.Compressors.LZMA.LZipStream(ms, SharpCompress.Compressors.CompressionMode.Compress))
                {
                    cs.Write(content, 0, content.Length);
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Compress content
        /// </summary>
        public static byte[] CompressContent(String content)
        {
            using (var ms = new MemoryStream())
            {
                using (var cs = new SharpCompress.Compressors.LZMA.LZipStream(ms, SharpCompress.Compressors.CompressionMode.Compress))
                using(var sw = new StreamWriter(cs, System.Text.Encoding.UTF8))
                {
                    sw.Write(content);    
                }
                return ms.ToArray();
            }
        }
    }
}
