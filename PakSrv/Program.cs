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
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Threading;

namespace SanteDB.PakSrv
{
    /// <summary>
    /// PakSrv is a PakMan remote repository server
    /// </summary>
    public class Program
    {
        /// <summary>
        ///  Main program entry point
        /// </summary>
        static void Main(string[] args)
        {

            var parser = new ParameterParser<PakSrvParameters>();

            // Parse parameters
            var parms = parser.Parse(args);
            Trace.TraceInformation("Starting Packaging Server");

            try
            {
                if(!String.IsNullOrEmpty(parms.AddAuth))
                {
                    var hasher = SHA256.Create();
                    var userHash = BitConverter.ToString(hasher.ComputeHash(System.Text.Encoding.UTF8.GetBytes(parms.AddAuth))).Replace("-", "");
                    var pass = Guid.NewGuid().ToByteArray().HexEncode();   
                    var pwdHash = BitConverter.ToString(hasher.ComputeHash(System.Text.Encoding.UTF8.GetBytes(pass))).Replace("-", "");
                    Console.WriteLine("Secret: {0}", pass);
                    File.AppendAllLines(".access", new String[] { $"{userHash}:{pwdHash}" });

                }
                else if (parms.Install)
                {
                    string serviceName = $"sdb-pkg-srvr";
                    if (!ServiceTools.ServiceInstaller.ServiceIsInstalled(serviceName))
                    {
                        String argList = String.Empty;

                        ServiceTools.ServiceInstaller.Install(
                            serviceName, $"SanteDB Package Server",
                            $"{Assembly.GetEntryAssembly().Location} {argList}",
                            null, null, ServiceTools.ServiceBootFlag.AutoStart);
                    }
                    else
                    {
                        throw new InvalidOperationException("Service instance already installed");
                    }
                }
                else if (parms.Uninstall)
                {
                    string serviceName = $"sdb-pkg-srvr";
                    if (ServiceTools.ServiceInstaller.ServiceIsInstalled(serviceName))
                    {
                        ServiceTools.ServiceInstaller.Uninstall(serviceName);
                    }
                    else
                    {
                        throw new InvalidOperationException("Service instance not installed");
                    }
                }
                else if (parms.Console)
                {
                    var pakSrv = new PakSrvHost();
                    pakSrv.Start();
                    ManualResetEvent stopEvent = new ManualResetEvent(false);
                    Console.CancelKeyPress += (o, e) => stopEvent.Set();
                    Console.WriteLine("Press CTRL+C key to close...");
                    stopEvent.WaitOne();
                    pakSrv.Stop();
                }
                else
                {
                    ServiceBase[] ServicesToRun;
                    ServicesToRun = new ServiceBase[]
                    {
                        new PakSrvService()
                    };
                    ServiceBase.Run(ServicesToRun);
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.TraceError("011 899 981 199 911 9725 3!!! {0}", e.ToString());
                Console.WriteLine("011 899 981 199 911 9725 3!!! {0}", e.ToString());

#else
                Trace.TraceError("Error encountered: {0}. Will terminate", e);
                EventLog.WriteEntry("SanteDB Gateway", $"Fatal service error: {e}", EventLogEntryType.Error, 911);
#endif
                Environment.Exit(911);
            }
        }
    }
}
