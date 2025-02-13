/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 */
using MohawkCollege.Util.Console.Parameters;
using SanteDB.Client.Configuration;
using SanteDB.Client.Rest;
using SanteDB.Core;
using SanteDB.Core.Services;
using SanteDB.Core.Services.Impl;
using SanteDB.Security.Certs.BouncyCastle;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;

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
            string appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "santedb", "sdk", "ade", consoleArgs.InstanceName);
            string appConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "santedb", "sdk", "ade", consoleArgs.InstanceName);
            AppDomain.CurrentDomain.SetData(DebuggerApplicationContext.AppDataDirectorySetting, appDataDirectory);

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
                    if (Directory.Exists(appDataDirectory))
                    {
                        Directory.Delete(appDataDirectory, true);
                    }

                    if (Directory.Exists(appConfigDirectory))
                    {
                        Directory.Delete(appConfigDirectory, true);
                    }

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

                    //Fix for accidentally adding a newline at the end of the arguments in Visual Studio
                    if (!string.IsNullOrEmpty(consoleArgs.BaseUrl))
                        consoleArgs.BaseUrl = consoleArgs.BaseUrl.Trim();

                    // Different binding port?
                    if (String.IsNullOrEmpty(consoleArgs.BaseUrl))
                    {
                        consoleArgs.BaseUrl = "http://127.0.0.1:9200";
                    }

                    // Configure defaults
                    AppDomain.CurrentDomain.SetData(RestServiceInitialConfigurationProvider.BINDING_BASE_DATA, consoleArgs.BaseUrl);
                    ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount ;

                    // Establish a configuration environment 
                    IConfigurationManager configurationManager = null;
                    var configurationFile = Path.Combine(appConfigDirectory, "santedb.config");
                    if (File.Exists(configurationFile))
                    {
                        configurationManager = new FileConfigurationService(configurationFile, false);
                    }
                    else
                    {
                        configurationManager = new InitialConfigurationManager(SanteDBHostType.Gateway, consoleArgs.InstanceName, configurationFile);
                    }

                    var context = new DebuggerApplicationContext(consoleArgs, configurationManager);

                    if (consoleArgs.AutoBindCertificate)
                    {
                        RestDebugCertificateInstallation.InstallDebuggerCertificate(new Uri(consoleArgs.BaseUrl), new BouncyCastleCertificateGenerator());
                    }
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
                    Thread.Sleep(5000); // Allow for restarts
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