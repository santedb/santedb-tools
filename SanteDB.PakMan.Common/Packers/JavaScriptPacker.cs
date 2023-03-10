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
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SanteDB.PakMan.Packers
{
    public class JavaScriptPacker : IFilePacker
    {
        /// <summary>
        /// Extensions to be packaged
        /// </summary>
        public string[] Extensions => new String[] { ".js" };

        /// <summary>
        /// Process the file
        /// </summary>
        public AppletAsset Process(string file, bool optimize)
        {
            try
            {
                String content = File.ReadAllText(file);
                if (optimize && !file.Contains("rules") && !file.Contains(".min.js"))
                {
                    var minifier = new Ext.Net.Utilities.JSMin();
                    // HACK : JSMIN Hates /// Reference 
                    content = new Regex(@"\/\/\/\s?\<Reference.*", RegexOptions.IgnoreCase).Replace(content, "");
                    content = minifier.Minify(content);
                }
                return new AppletAsset()
                {
                    MimeType = "text/javascript",
                    Content = PakManTool.CompressContent(content)
                };

            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Cannot process JavaScript {file}", e);
            }
        }
    }
}
