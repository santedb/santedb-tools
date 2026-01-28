/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using SanteDB.SDK.BreDebugger.Options;
using SanteDB.SDK.BreDebugger.Shell;
using System;
using System.Reflection;

namespace SanteDB.SDK.BreDebugger
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("SanteDB Business Rule & CDSS Debugger v{0} ({1})", Assembly.GetEntryAssembly().GetName().Version, Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
            Console.WriteLine(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright);

            ParameterParser<DebuggerParameters> parser = new ParameterParser<DebuggerParameters>();
            var parameters = parser.Parse(args);

            if (parameters.Help || args.Length == 0)
            {
                parser.WriteHelp(Console.Out);
            }
            else if (parameters.Protocol)
            {
                new ProtoDebugger(parameters).Debug();
            }
            else if (parameters.BusinessRule)
            {
                new Shell.BreDebugger(parameters).Debug();
            }
            else
            {
                Console.WriteLine("Nothing to do!");
            }
        }
    }
}