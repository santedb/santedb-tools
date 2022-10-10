﻿/*
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
using SanteDB.Core.Applets.Model;
using System.IO;
using System.Xml;

namespace SanteDB.PakMan.Packers
{
    /// <summary>
    /// Packager for XML
    /// </summary>
    public class XmlPacker : IFilePacker
    {
        /// <summary>
        /// Extensions
        /// </summary>
        public virtual string[] Extensions => new string[] { ".xml", ".dataset" };

        /// <summary>
        /// Process the XML file
        /// </summary>
        public virtual AppletAsset Process(string file, bool optimize)
        {

            // Verify that the file is XML
            try
            {
                var xe = new XmlDocument();
                xe.Load(file);

                if (optimize)
                    using (var ms = new MemoryStream())
                    using (var xw = XmlWriter.Create(ms, new XmlWriterSettings() { Indent = false, OmitXmlDeclaration = true }))
                    {
                        xe.WriteContentTo(xw);
                        xw.Flush();
                        return new AppletAsset()
                        {
                            MimeType = "text/xml",
                            Content = PakManTool.CompressContent(ms.ToArray())
                        };
                    }
                else
                    return new AppletAsset()
                    {
                        MimeType = "text/xml",
                        Content = PakManTool.CompressContent(File.ReadAllBytes(file))
                    };
            }
            catch (XmlException e)
            {
                Emit.Message("ERROR", " {0} is not well formed - {1} - @{2}:{3}", file, e.Message, e.LineNumber, e.LinePosition);

                throw;
            }
        }
    }
}
