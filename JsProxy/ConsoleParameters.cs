/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-6-21
 */
using MohawkCollege.Util.Console.Parameters;
using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SanteDB.SDK.JsProxy
{
    /// <summary>
    /// Console parameters to the JS proxy
    /// </summary>
    public class ConsoleParameters
    {

        [Parameter("asm")]
        [Description("The compiled SanteDB binary from which to operate")]
        public StringCollection AssemblyFile { get; set; }

        [Parameter("xml")]
        [Description("The .NET XML documentation file related to the assembly passed by --asm")]
        public String DocumentationFile { get; set; }

        [Parameter("out")]
        [Description("The output file which should be generated")]
        public String Output { get; set; }

        [Parameter("noabs")]
        [Description("When specified indicates no abstract types should be emitted")]
        public bool NoAbstract { get; set; }

        [Parameter("help")]
        [Description("Show help and exit")]
        public bool Help { get; set; }

        [Parameter("proxy")]
        [Description("When specified, generate the JavaScript proxy")]
        public bool JsProxy { get; set; }

        [Parameter("serializer")]
        [Description("When specified generate the C# Serializer Helpers")]
        public bool ViewModelSerializer { get; set; }

        [Parameter("namespace")]
        [Description("Specifies the namespace of the resulting file")]
        public string Namespace { get; set; }

        /// <summary>
        /// Service Documentation
        /// </summary>
        [Parameter("sdoc")]
        [Description("Generate service documentation in markdown")]
        public bool ServiceDocumentation { get; internal set; }

        [Parameter("tocroot")]
        [Description("The root where a TOC should be generated")]
        public string WikiRoot { get; set; }
    }

}
