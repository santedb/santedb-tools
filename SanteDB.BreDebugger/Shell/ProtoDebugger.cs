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
 * Date: 2023-5-19
 */
using SanteDB.Cdss.Xml;
using SanteDB.Cdss.Xml.Model;
using SanteDB.Core;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Protocol;
using SanteDB.Core.Services;
using SanteDB.SDK.BreDebugger.Options;
using SanteDB.SDK.BreDebugger.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SanteDB.SDK.BreDebugger.Shell
{
    /// <summary>
    /// Business Rules debugger
    /// </summary>
    [Description("Care Plan Debugger")]
    public class ProtoDebugger : DebuggerBase
    {

        // Loaded files
        private Dictionary<String, String> m_loadedFiles = new Dictionary<string, string>();




        /// <summary>
        /// BRE debugger
        /// </summary>
        /// <param name="sources"></param>
        public ProtoDebugger(DebuggerParameters parms) : base(parms)
        {
            Console.WriteLine("Starting debugger...");

            if (!String.IsNullOrEmpty(parms.WorkingDirectory))
                ApplicationServiceContext.Current.GetService<FileSystemResolver>().RootDirectory = parms.WorkingDirectory;
            var rootPath = ApplicationServiceContext.Current.GetService<FileSystemResolver>().RootDirectory;

            // Load debug targets
            Console.WriteLine("Loading debuggees...");
            if (parms.Sources != null)
                foreach (var rf in parms.Sources)
                {
                    var f = rf.Replace("~", rootPath);
                    if (!File.Exists(f))
                        Console.Error.WriteLine("Can't find file {0}", f);
                    else
                        this.Add(f);
                }

        }

        /// <summary>
        /// Loads a script to be debugged
        /// </summary>
        [Command("o", "Adds a protocol file to the planner")]
        public void Add(String file)
        {
            if (!Path.IsPathRooted(file))
                file = Path.Combine(this.m_workingDirectory, file);
            if (!File.Exists(file))
                throw new FileNotFoundException(file);

            // Add
            using (var fs = File.OpenRead(file))
            {
                var protoSource = ProtocolDefinition.Load(fs);
                var proto = new XmlClinicalProtocol(protoSource);
                ApplicationServiceContext.Current.GetService<IClinicalProtocolRepositoryService>().InsertProtocol(proto);
            }
        }

        /// <summary>
        /// Clear protocol repository
        /// </summary>
        [Command("c", "Clear the protocol repository")]
        public void Clear()
        {
            ApplicationServiceContext.Current.GetService<IServiceManager>().RemoveServiceProvider(typeof(ICarePlanService));
            ApplicationServiceContext.Current.GetService<IServiceManager>().RemoveServiceProvider(typeof(IClinicalProtocolRepositoryService));
            ApplicationServiceContext.Current.GetService<IServiceManager>().AddServiceProvider(typeof(DebugProtocolRepository));
            ApplicationServiceContext.Current.GetService<IServiceManager>().AddServiceProvider(typeof(SimpleCarePlanService));

        }

        /// <summary>
        /// Loads a script to be debugged
        /// </summary>
        [Command("od", "Adds all files dir to care planner")]
        public void AddDir(String dir)
        {
            if (!Path.IsPathRooted(dir))
                dir = Path.Combine(this.m_workingDirectory, dir);
            if (!Directory.Exists(dir))
                throw new FileNotFoundException(dir);

            // Add
            foreach (var file in Directory.GetFiles(dir))
            {
                Console.WriteLine("Add {0}", Path.GetFileName(file));
                try
                {
                    using (var fs = File.OpenRead(file))
                    {
                        var protoSource = ProtocolDefinition.Load(fs);
                        var proto = new XmlClinicalProtocol(protoSource);

                        ApplicationServiceContext.Current.GetService<IClinicalProtocolRepositoryService>().InsertProtocol(proto);
                    }
                }
                catch (Exception e)
                {
                    base.PrintStack(e);
                }
            }
        }


        /// <summary>
        /// List all protocols
        /// </summary>
        [Command("pl", "Displays a list of clinical protocols that have been loaded in this session")]
        public void ListProtocols()
        {
            Console.WriteLine("ID#{0}NAME", new String(' ', 38));
            foreach (var itm in ApplicationServiceContext.Current.GetService<IClinicalProtocolRepositoryService>().FindProtocol())
                Console.WriteLine("{0}    {1}", itm.Id, itm.Name);
        }

        /// <summary>
        /// Set a breakpoint
        /// </summary>
        [Command("go", "Runs the clinical protocols to construct a care plan")]
        public object Run()
        {

            var cpService = ApplicationServiceContext.Current.GetService<ICarePlanService>();
            if (cpService == null)
                throw new InvalidOperationException("No care plan service is registered");
            else if (this.m_scopeObject is Patient)
            {
                Console.WriteLine("Running care planner...");
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var cp = cpService.CreateCarePlan(this.m_scopeObject as Patient);
                sw.Stop();
                Console.WriteLine("Care plan generated in {0} and set to scope (use dj to dump)", sw.Elapsed);
                return cp;
            }
            else
                throw new InvalidOperationException("Scope must be a patient object");
        }


        /// <summary>
        /// Unload all protocols
        /// </summary>
        [Command("u", "Unload all protocols (reset the environment)")]
        public void Reset()
        {
            var protoRepo = ApplicationServiceContext.Current.GetService<IClinicalProtocolRepositoryService>();
            foreach (var p in protoRepo.FindProtocol().ToArray())
            {
                protoRepo.RemoveProtocol(p.Id);
            }
        }

        /// <summary>
        /// Set a breakpoint
        /// </summary>
        [Command("go.encounter", "Runs the clinical protocols to construct a care plan as appointments")]
        public object RunEncounter()
        {

            var cpService = ApplicationServiceContext.Current.GetService<ICarePlanService>();
            if (cpService == null)
                throw new InvalidOperationException("No care plan service is registered");
            else if (this.m_scopeObject is Patient)
            {
                Console.WriteLine("Running care planner...");
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var cp = cpService.CreateCarePlan(this.m_scopeObject as Patient);
                sw.Stop();
                Console.WriteLine("Care plan generated in {0} and set to scope (use dj to dump)", sw.Elapsed);
                return cp;
            }
            else
                throw new InvalidOperationException("Current scope must be a patient");
        }


        /// <summary>
        /// Exit the specified context
        /// </summary>
        public override void Exit()
        {
            base.Exit();
        }
    }

}
