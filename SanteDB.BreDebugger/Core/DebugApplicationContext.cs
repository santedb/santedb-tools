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
using SanteDB.Core;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.EntityLoader;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.DisconnectedClient;
using SanteDB.DisconnectedClient.Configuration;
using SanteDB.DisconnectedClient.Configuration.Data;
using SdbDebug.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;

namespace SanteDB.SDL.BreDebugger.Core
{
    /// <summary>
    /// Represents a debugger application context
    /// </summary>
    public class DebugApplicationContext : ApplicationContext
    {

        // The application
        private static readonly SanteDB.Core.Model.Security.SecurityApplication c_application = new SanteDB.Core.Model.Security.SecurityApplication()
        {
            ApplicationSecret = "A1CF054D04D04CD1897E114A904E328D",
            Key = Guid.Parse("3E16EE70-639D-465B-86DE-043043F41098"),
            Name = "org.santedb.debugger"
        };

        private DebugConfigurationManager m_configurationManager;

        /// <summary>
        /// Creates a new debug application context
        /// </summary>
        public DebugApplicationContext(DebuggerParameters parms) : base(new DebugConfigurationManager(parms))
        {

        }

        /// <summary>
        /// Synchronization modes
        /// </summary>
        public override SynchronizationMode Modes => SynchronizationMode.Offline;

        /// <summary>
        /// Show toast
        /// </summary>
        public override void ShowToast(string subject)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("TOAST >>>> {0}", subject);
            Console.ResetColor();
        }


        /// <summary>
        /// Get the application
        /// </summary>
        public override SecurityApplication Application
        {
            get
            {
                return c_application;
            }
        }


        /// <summary>
        /// Static CTOR bind to global handlers to log errors
        /// </summary>
        /// <value>The current.</value>
        static DebugApplicationContext()
        {

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (ApplicationContext.Current != null)
                {
                    Tracer tracer = Tracer.GetTracer(typeof(ApplicationContext));
                    tracer.TraceEvent(EventLevel.Critical, "Uncaught exception: {0}", e.ExceptionObject.ToString());
                }
            };


        }



        /// <summary>
        /// Start the application context
        /// </summary>
        public static bool Start(DebuggerParameters consoleParms)
        {

            var retVal = new DebugApplicationContext(consoleParms);

            try
            {
                // Set master application context
                ApplicationServiceContext.Current = ApplicationContext.Current = retVal;
                retVal.m_tracer = Tracer.GetTracer(typeof(DebugApplicationContext));

                var appService = retVal.GetService<IAppletManagerService>();

                retVal.SetProgress("Loading configuration", 0.2f);

                if (consoleParms.References != null)
                {
                    // Load references
                    foreach (var appletInfo in consoleParms.References)// Directory.GetFiles(this.m_configuration.GetSection<AppletConfigurationSection>().AppletDirectory)) {
                        try
                        {
                            retVal.m_tracer.TraceInfo("Loading applet {0}", appletInfo);
                            String appletPath = appletInfo;
                            if (!Path.IsPathRooted(appletInfo))
                                appletPath = Path.Combine(Environment.CurrentDirectory, appletPath);
                            using (var fs = File.OpenRead(appletPath))
                            {

                                var package = AppletPackage.Load(fs);
                                retVal.m_tracer.TraceInfo("Loading {0} v{1}", package.Meta.Id, package.Meta.Version);

                                // Is this applet in the allowed applets
                                appService.LoadApplet(package.Unpack());
                            }
                        }
                        catch (Exception e)
                        {
                            retVal.m_tracer.TraceError("Loading applet {0} failed: {1}", appletInfo, e.ToString());
                            throw;
                        }
                }

                // Ensure data migration exists
                try
                {
                    // If the DB File doesn't exist we have to clear the migrations
                    if (!File.Exists(retVal.ConfigurationManager.GetConnectionString(retVal.Configuration.GetSection<DcDataConfigurationSection>().MainDataSourceConnectionStringName).GetComponent("dbfile")))
                    {
                        retVal.m_tracer.TraceWarning("Can't find the SanteDB database, will re-install all migrations");
                        retVal.Configuration.GetSection<DcDataConfigurationSection>().MigrationLog.Entry.Clear();
                    }
                    retVal.SetProgress("Migrating databases", 0.6f);

                    ConfigurationMigrator migrator = new ConfigurationMigrator();
                    migrator.Ensure(true);

                    // Set the entity source
                    EntitySource.Current = new EntitySource(retVal.GetService<IEntitySourceProvider>());

                    // Prepare clinical protocols
                    //retVal.GetService<ICarePlanService>().Repository = retVal.GetService<IClinicalProtocolRepositoryService>();
                    ApplicationServiceContext.Current = ApplicationContext.Current;

                }
                catch (Exception e)
                {
                    retVal.m_tracer.TraceError(e.ToString());
                    throw;
                }
                finally
                {
                }

                // Set the tracer writers for the PCL goodness!
                foreach (var itm in retVal.Configuration.GetSection<DiagnosticsConfigurationSection>().TraceWriter)
                {
                    SanteDB.Core.Diagnostics.Tracer.AddWriter(Activator.CreateInstance(itm.TraceWriter, itm.Filter, itm.InitializationData, new Dictionary<String, EventLevel>()) as TraceWriter, itm.Filter);
                }
                // Start daemons
                retVal.GetService<IThreadPoolService>().QueueUserWorkItem(o => { retVal.Start(); });

                //retVal.Start();

            }
            catch (Exception e)
            {
                retVal.m_tracer?.TraceError(e.ToString());
                throw;
            }
            return true;
        }

        /// <summary>
        /// Exit the application
        /// </summary>
        public override void Exit()
        {
            Environment.Exit(0);
        }

        /// <summary>
        /// Confirmation
        /// </summary>
        public override bool Confirm(string confirmText)
        {
            return true;
        }

        /// <summary>
        /// Show an alert
        /// </summary>
        public override void Alert(string alertText)
        {
            Console.WriteLine("ALERT >>> {0}", alertText);
        }

        /// <summary>
        /// Get current context key -- Since miniims is debuggable this is not needed
        /// </summary>
        public override byte[] GetCurrentContextSecurityKey()
        {
            return null;
        }

    }
}
