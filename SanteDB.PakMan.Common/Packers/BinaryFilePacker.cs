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
using System;
using System.IO;

namespace SanteDB.PakMan.Packers
{
    /// <summary>
    /// Binary file package
    /// </summary>
    public class BinaryFilePacker : IFilePacker
    {
        /// <summary>
        /// Extnesions
        /// </summary>
        public string[] Extensions => new string[] { "*" };

        /// <summary>
        /// Process the file
        /// </summary>
        public AppletAsset Process(string file, bool optimize)
        {
            try
            {
                var mime = MimeMapping.MimeUtility.GetMimeMapping(file);
                if (String.IsNullOrEmpty(mime))
                    mime = "application/x-octet-stream";
                return new AppletAsset()
                {
                    MimeType = mime,
                    Content = PakManTool.CompressContent(File.ReadAllBytes(file))
                };
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Cannot process {file}", e);
            }
        }
    }
}