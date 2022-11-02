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
using MohawkCollege.Util.Console.Parameters;
using SanteDB.Client;
using SanteDB.Client.Configuration;
using SanteDB.Client.Rest;
using SanteDB.Core;
using SanteDB.Core.Services;
using SanteDB.Core.Services.Impl;
using SanteDB.DevTools.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace SanteDB.SDK.AppletDebugger
{
    internal class Program
    {

        [STAThread()]
        private static void Main(string[] args)
        {

            // Gets the console arguments
            var consoleArgs = new ParameterParser<ConsoleParameters>().Parse(args);
            consoleArgs.InstanceName = consoleArgs.InstanceName ?? "default";

            // Setup basic parameters
            string appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "santedb", "sdk", "ade", consoleArgs.InstanceName);
            string appConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "santedb", "sdk", "ade", consoleArgs.InstanceName);

            // Create dependent directories
            if (!Directory.Exists(appDataDirectory))
            {
                Directory.CreateDirectory(appDataDirectory);
            }
            if (!Directory.Exists(appConfigDirectory))
            {
                Directory.CreateDirectory(appConfigDirectory);
            }

            // Emit the copyright information
            Console.WriteLine("SanteDB - Disconnected Client Debugging Tool");
            Console.WriteLine("Version {0}", Assembly.GetEntryAssembly().GetName().Version);
            Console.WriteLine(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright);

            if (consoleArgs.Help || args.Length == 0)
            {
                new ParameterParser<ConsoleParameters>().WriteHelp(Console.Out);
            }
            else
            {

                if (consoleArgs.Reset)
                {
                    if (Directory.Exists(appDataDirectory)) Directory.Delete(appDataDirectory, true);
                    if (Directory.Exists(appConfigDirectory)) Directory.Delete(appConfigDirectory, true);
                    Console.WriteLine("Environment Reset Successful");
                    return;
                }

                // Create start the context
                try
                {

                    // Load reference assemblies.
                    if (consoleArgs.Assemblies != null)
                    {
                        foreach (var itm in consoleArgs.Assemblies)
                        {
                            try
                            {
                                Console.WriteLine("Loading reference assembly {0}...", itm);
                                Assembly.LoadFile(itm);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Error loading assembly {0}: {1}", itm, e);
                            }
                        }
                    }
                    Directory.GetFiles(Path.GetDirectoryName(typeof(Program).Assembly.Location), "Sante*.dll").ToList().ForEach(itm =>
                    {
                        try
                        {
                            Console.WriteLine("Loading reference assembly {0}...", itm);
                            Assembly.LoadFile(itm);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error loading assembly {0}: {1}", itm, e);
                        }
                    });

                    // Different binding port?
                    if (String.IsNullOrEmpty(consoleArgs.BaseUrl))
                    {
                        consoleArgs.BaseUrl = "http://127.0.0.1:9200";
                    }

                    AppDomain.CurrentDomain.SetData(RestServiceInitialConfigurationProvider.BINDING_BASE_DATA, consoleArgs.BaseUrl);

                    // Establish a configuration environment 
                    IConfigurationManager configurationManager = null;
                    var configurationFile = Path.Combine(appConfigDirectory, "santedb.config");
                    if (File.Exists(configurationFile))
                    {
                        configurationManager = new FileConfigurationService(configurationFile, true);
                    }
                    else
                    {
                        configurationManager = new InitialConfigurationManager(SanteDBHostType.Gateway, consoleArgs.InstanceName);
                    }

                    var context = new DebuggerApplicationContext(consoleArgs, configurationManager);
                    ServiceUtil.Start(Guid.NewGuid(), context);

                    if (!consoleArgs.NoBrowser)
                    {
                        if (configurationManager is InitialConfigurationManager)
                        {
                            Process.Start($"{consoleArgs.BaseUrl}/#!/config/initialSettings");
                        }
                        else
                        {
                            Process.Start($"{consoleArgs.BaseUrl}/#!/");
                        }
                    }
                    ManualResetEvent stopEvent = new ManualResetEvent(false);
                    Console.CancelKeyPress += (o, e) =>
                    {
                        ServiceUtil.Stop();
                        stopEvent.Set();
                    };
                    context.Stopped += (o, e) =>
                    {
                        // The host context has stopped by request of the system
                        stopEvent.Set();
                    };

                    Console.WriteLine("Press CTRL+C key to close...");
                    stopEvent.WaitOne();

                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("FATAL ERROR: {0}", e);
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }
    }
}