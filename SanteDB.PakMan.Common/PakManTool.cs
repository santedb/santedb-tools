/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using SanteDB.Core.Applets.Model;
using SanteDB.PakMan.Packers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

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
                m_packers = AppDomain.CurrentDomain.GetAllTypes()
                    .Where(t => typeof(IFilePacker).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                    .Select(t => Activator.CreateInstance(t))
                    .OfType<IFilePacker>()
                    .SelectMany(i => i.Extensions.Select(e => new { Ext = e, Pakr = i }))
                    .ToDictionary(o => o.Ext, o => o.Pakr);


            if (m_packers.TryGetValue(ext, out IFilePacker retVal))
                return retVal;
            else return m_packers["*"];
        }

        /// <summary>
        /// Sign the specified package
        /// </summary>
        public static AppletPackage SignPackage(AppletPackage package, X509Certificate2 signCert, bool embedCert)
        {

            if (!signCert.HasPrivateKey)
            {
                throw new InvalidOperationException($"You do not have the private key for certificiate {signCert.Subject}");
            }

            var mfst = package.Unpack();
            mfst.Info.TimeStamp = DateTime.Now; // timestamp
            mfst.Info.PublicKeyToken = signCert.Thumbprint;
            var retVal = mfst.CreatePackage();

            retVal.Meta.Hash = SHA256.Create().ComputeHash(retVal.Manifest);
            retVal.Meta.PublicKeyToken = signCert.Thumbprint;

            if (embedCert)
                retVal.PublicKey = signCert.Export(X509ContentType.Cert);

            if (!signCert.HasPrivateKey)
                throw new SecurityException($"Provided key {signCert} has no private key");
            RSACryptoServiceProvider rsa = signCert.PrivateKey as RSACryptoServiceProvider;
            retVal.Meta.Signature = rsa.SignData(retVal.Manifest, CryptoConfig.MapNameToOID("SHA1"));
            return retVal;
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
