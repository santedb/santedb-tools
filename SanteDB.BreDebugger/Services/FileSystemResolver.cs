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
