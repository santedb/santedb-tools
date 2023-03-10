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
    /// <summary>
    /// CSS file packager
    /// </summary>
    public class CssPacker : IFilePacker
    {
        /// <summary>
        /// Extensions for this CSS packager
        /// </summary>
        public string[] Extensions => new string[] { ".css" };

        /// <summary>
        /// Process this package
        /// </summary>
        public AppletAsset Process(string file, bool optimize)
        {
            try
            {
                if (optimize && !file.EndsWith(".min.css"))
                {

                    var content = RemoveWhiteSpaceFromStylesheets(File.ReadAllText(file));
                    return new AppletAsset()
                    {
                        MimeType = "text/css",
                        Content = PakManTool.CompressContent(content)
                    };
                }
                else
                    return new AppletAsset()
                    {
                        MimeType = "text/css",
                        Content = PakManTool.CompressContent(File.ReadAllText(file))
                    };
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Cannot CSS process {file}", e);
            }
        }

        /// <summary>
        /// From https://madskristensen.net/blog/efficient-stylesheet-minification-in-c
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public static string RemoveWhiteSpaceFromStylesheets(string body)

        {
            body = Regex.Replace(body, @"[a-zA-Z]+#", "#");
            body = Regex.Replace(body, @"[\n\r]+\s*", string.Empty);
            body = Regex.Replace(body, @"\s+", " ");
            body = Regex.Replace(body, @"\s?([:,;{}])\s?", "$1");
            body = body.Replace(";}", "}");
            body = Regex.Replace(body, @"([\s:]0)(px|pt|%|em)", "$1");

            // Remove comments from CSS
            body = Regex.Replace(body, @"/\*[\d\D]*?\*/", string.Empty);
            return body;

        }
    }
}
