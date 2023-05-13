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
using MohawkCollege.Util.Console.Parameters;
using System;
using System.Reflection;

namespace SanteDB.PakMan
{
    internal class Program
    {
        /// <summary>
        /// The main program
        /// </summary>
        private static int Main(string[] args)
        {
            Console.WriteLine("SanteDB HTML Applet Compiler v{0} ({1})", Assembly.GetEntryAssembly().GetName().Version, Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
            Console.WriteLine(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright);

            AppDomain.CurrentDomain.UnhandledException += (o, e) =>
            {
                Console.WriteLine("Could not complete operation - {0}", e.ExceptionObject);
            };

            ParameterParser<PakManParameters> parser = new ParameterParser<PakManParameters>();
            var parameters = parser.Parse(args);

            if (!String.IsNullOrEmpty(parameters.Version) && parameters.Version.Contains("-"))
            {
                parameters.Version = parameters.Version.Substring(0, parameters.Version.IndexOf("-"));
            }
            if (parameters.Help)
            {
                parser.WriteHelp(Console.Out);
                return 0;
            }
            else if (parameters.Info)
            {
                // Load the file and dump the contents
                return new Inspector(parameters).Dump();
            }
            else if (parameters.Compose)
            {
                var retVal = new Composer(parameters).Compose();
                if (parameters.DcdrAssets?.Count > 0)
                    return new Distributor(parameters).Package();
                return retVal;
            }
            else if (parameters.Compile)
                return new Packer(parameters).Compile();
            else if (parameters.Sign)
                return new Signer(parameters).Sign();
            else
            {
                Console.WriteLine("Nothing to do!");
                return 0;
            }
        }
    }
}