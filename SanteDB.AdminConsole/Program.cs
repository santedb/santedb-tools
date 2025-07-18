﻿/*
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
using SanteDB.AdminConsole.Parameters;
using SanteDB.AdminConsole.Shell;
using SanteDB.Core.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Reflection;

namespace SanteDB.AdminConsole
{
    [ExcludeFromCodeCoverage]
    class Program
    {
        /// <summary>
        /// Main program entry point
        /// </summary>
        static void Main(string[] args)
        {
            PrintHeader();

            var pp = new ParameterParser<ConsoleParameters>();
            var options = pp.Parse(args);

            if (options.Help)
            {
                pp.WriteHelp(Console.Out);
            }
            else
            {
                try
                {
                    if (string.IsNullOrEmpty(options.Verbosity))
                    {
                        options.Verbosity = "Error";
                    }

                    Tracer.AddWriter(new Shell.ConsoleTraceWriter(options.Verbosity, new Dictionary<String, EventLevel>()), (EventLevel)Enum.Parse(typeof(EventLevel), options.Verbosity));

                    ApplicationContext.Initialize(options);
                    ApplicationContext.Current.Start();
                    new InteractiveShell().Exec(options);
                }
                catch (Exception e)
                {
                    Console.WriteLine("FATAL: {0}", e);

                }
                finally
                {
                    Console.ResetColor();
                }
            }
#if DEBUG
            if (Debugger.IsAttached)
            {
                Console.ReadKey();
            }
#endif
        }

        public static void PrintHeader()
        {
            var assembly = typeof(Program).Assembly;
            Console.WriteLine("{0} v{1} ({2})", assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description, assembly.GetName().Version, assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
            Console.WriteLine(assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright);
        }
    }
}
