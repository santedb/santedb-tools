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
using SanteDB.DisconnectedClient.Ags;
using SanteDB.DisconnectedClient.Backup;
using SanteDB.DisconnectedClient.Configuration;
using SanteDB.DisconnectedClient.Security;
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
        // Trusted certificates
        private static List<String> s_trustedCerts = new List<string>();

        [STAThread()]
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (o, e) =>
            {
                string pAsmName = e.Name;
                if (pAsmName.Contains(","))
                    pAsmName = pAsmName.Substring(0, pAsmName.IndexOf(","));

                var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => e.Name == a.FullName) ??
                    AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => pAsmName == a.GetName().Name);
                return asm;
            };

            // Start up!!!
            var consoleArgs = new ParameterParser<ConsoleParameters>().Parse(args);
            consoleArgs.InstanceName = consoleArgs.InstanceName ?? "default";

            // Setup basic parameters
            String[] directory = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "santedb", "sdk", "ade", consoleArgs.InstanceName),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "santedb", "sdk", "ade", consoleArgs.InstanceName)
                };

            foreach (var dir in directory)
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

            // Token validator
            TokenValidationManager.SymmetricKeyValidationCallback += (o, k, i) =>
            {
                return MessageBox.Show(String.Format("Trust issuer {0} with symmetric key?", i), "Token Validation Error", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes;
            };
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, error) =>
            {
                if (certificate == null || chain == null)
                    return false;
                else
                {
                    var valid = s_trustedCerts.Contains(certificate.Subject);
                    if (!valid && (chain.ChainStatus.Length > 0 || error != SslPolicyErrors.None))
                        if (MessageBox.Show(String.Format("The remote certificate is not trusted. The error was {0}. The certificate is: \r\n{1}\r\nWould you like to temporarily trust this certificate?", error, certificate.Subject), "Certificate Error", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
                            return false;
                        else
                            s_trustedCerts.Add(certificate.Subject);

                    return true;
                    //isValid &= chain.ChainStatus.Length == 0;
                }
            };

            Console.WriteLine("SanteDB - Disconnected Client Debugging Tool");
            Console.WriteLine("Version {0}", Assembly.GetEntryAssembly().GetName().Version);
            Console.WriteLine(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright);

            if (consoleArgs.Help || args.Length == 0)
                new ParameterParser<ConsoleParameters>().WriteHelp(Console.Out);
            else
            {
                if (consoleArgs.Reset)
                {
                    var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "santedb", "sdk", "ade", consoleArgs.InstanceName);
                    var cData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "santedb", "sdk", "ade", consoleArgs.InstanceName);
                    if (Directory.Exists(appData)) Directory.Delete(cData, true);
                    if (Directory.Exists(appData)) Directory.Delete(appData, true);
                    Console.WriteLine("Environment Reset Successful");
                    return;
                }
                else if (consoleArgs.Restore)
                {
                    // Start a temporary session
                    MiniApplicationContext.StartTemporary(consoleArgs);

                    // Browse for backup
                    var dlgOpen = new OpenFileDialog()
                    {
                        CheckFileExists = true,
                        CheckPathExists = true,
                        DefaultExt = "sdbk",
                        Filter = "SanteDB Backup Files (*.sdbk)|*.sdbk",
                        Title = "Restore from Backup"
                    };
                    if (dlgOpen.ShowDialog() != DialogResult.Cancel)
                    {
                        var pwdDialog = new frmKeyPassword(dlgOpen.FileName);
                        if (pwdDialog.ShowDialog() == DialogResult.OK)
                        {
                            // Attempt to unpack
                            try
                            {
                                new DefaultBackupService().RestoreFiles(dlgOpen.FileName, pwdDialog.Password, MiniApplicationContext.Current.GetService<IConfigurationPersister>().ApplicationDataDirectory);
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show($"Error restoring {dlgOpen.Filter} - {e.Message}", "Error Restoring Backup");
                            }
                        }
                    }
                    return;
                }
                // Load reference assemblies.
                if (consoleArgs.Assemblies != null)
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

                MiniApplicationContext.ProgressChanged += (o, e) =>
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(">>> PROGRESS >>> {0} : {1:#0%}", e.ProgressText, e.Progress);
                    Console.ResetColor();
                };

                if (consoleArgs.BaseRefs)
                {
                    if (consoleArgs.References == null)
                        consoleArgs.References = new System.Collections.Specialized.StringCollection();

                    consoleArgs.References.AddRange(new string[]
                    {
                        "org.santedb.core",
                        "org.santedb.uicore",
                        "org.santedb.config",
                        "org.santedb.bicore",
                        "org.santedb.config.init",
                        "org.santedb.i18n.en"
                    });
                }

                try
                {
                    if (!MiniApplicationContext.Start(consoleArgs))
                    {
                        Console.WriteLine("Need to conifgure the system");
                        MiniApplicationContext.StartTemporary(consoleArgs);
                        MiniApplicationContext.Current.ConfigurationManager.SetAppSetting("http.bypassMagic", MiniApplicationContext.Current.ExecutionUuid.ToString());
                        // Forward
                        if (MiniApplicationContext.Current.GetService<AgsService>().IsRunning)
                            Process.Start("http://127.0.0.1:9200/#!/config/initialSettings");
                        else
                            MiniApplicationContext.Current.GetService<AgsService>().Started += (oo, oe) =>
                                Process.Start("http://127.0.0.1:9200/#!/config/initialSettings");
                    }
                    else
                    {
                        MiniApplicationContext.Current.ConfigurationManager.SetAppSetting("http.bypassMagic", MiniApplicationContext.Current.ExecutionUuid.ToString());
                        var appletConfig = MiniApplicationContext.Current.Configuration.GetSection<AppletConfigurationSection>();

                        // Forward
                        if (MiniApplicationContext.Current.GetService<AgsService>().IsRunning)
                            Process.Start("http://127.0.0.1:9200/#!/");
                        else
                            MiniApplicationContext.Current.GetService<AgsService>().Started += (oo, oe) =>
                                Process.Start("http://127.0.0.1:9200/#!/");
                    }

                    ManualResetEvent stopEvent = new ManualResetEvent(false);

                    Console.CancelKeyPress += (o, e) =>
                    {
                        stopEvent.Set();
                    };

                    MiniApplicationContext.Current.Stopped += (o, e) =>
                    {
                        // The host context has stopped by request of the system
                        stopEvent.Set();
                    };

                    Console.WriteLine("Press CTRL+C key to close...");
                    stopEvent.WaitOne();

                    if (MiniApplicationContext.Current?.IsRunning == true)
                    {
                        MiniApplicationContext.Current.Stop(); // stop
                    }
                    else
                    {
                        // Service stopped the context so we want to restart
                        Console.WriteLine("Will restart context, waiting for main teardown in 5 seconds...");
                        Thread.Sleep(5000);
                        var pi = new ProcessStartInfo(typeof(Program).Assembly.Location, string.Join(" ", args));
                        Process.Start(pi);
                        Environment.Exit(0);
                    }
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }
    }
}