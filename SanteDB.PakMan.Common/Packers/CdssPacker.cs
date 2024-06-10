/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-12-11
 */
using SanteDB.Cdss.Xml.Antlr;
using SanteDB.Cdss.Xml.Exceptions;
using SanteDB.Core.Applets.Model;
using System;
using System.IO;

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
                using (var fs = System.IO.File.OpenRead(file))
                {
                    var tps = CdssLibraryTranspiler.Transpile(fs, true);
                    using (var ms = new MemoryStream())
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
            catch (CdssTranspilationException e)
            {
                throw new Exception($"Could not transpile {file}", e);
            }
        }
    }
}
