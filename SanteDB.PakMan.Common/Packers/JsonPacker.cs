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
using SanteDB.Core.Applets.Model;
using System;
using System.IO;

namespace SanteDB.PakMan.Packers
{
    public class JsonPacker : IFilePacker
    {
        /// <summary>
        /// Extensions to be packaged
        /// </summary>
        public string[] Extensions => new String[] { ".json" };

        /// <summary>
        /// Process the file
        /// </summary>
        public AppletAsset Process(string file, bool optimize)
        {
            try
            {
                String content = File.ReadAllText(file);

                return new AppletAsset()
                {
                    MimeType = "application/json",
                    Content = PakManTool.CompressContent(content)
                };

            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Cannot process JSON {file}", e);
            }
        }
    }
}
