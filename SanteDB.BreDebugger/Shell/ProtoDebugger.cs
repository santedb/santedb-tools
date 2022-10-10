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
using SanteDB.BusinessRules.JavaScript;
using SanteDB.BusinessRules.JavaScript.JNI;
using SanteDB.Cdss.Xml;
using SanteDB.Cdss.Xml.Model;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Protocol;
using SanteDB.Core.Services;
using SanteDB.DisconnectedClient;
using SdbDebug.Core;
using SdbDebug.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.SDL.BreDebugger.Shell
{
    /// <summary>
    /// Business Rules debugger
    /// </summary>
    [Description("Care Plan Debugger")]
    public class ProtoDebugger : DebuggerBase
    {

        // Loaded files
        private Dictionary<String, String> m_loadedFiles = new Dictionary<string, string>();


        public class ConsoleTraceWriter : TraceWriter
        {
            /// <summary>
            /// Write
            /// </summary>
            public ConsoleTraceWriter(EventLevel filter, string initializationData, IDictionary<String, EventLevel> settings) : base(filter, initializationData, settings)
            {
            }

            protected override void WriteTrace(EventLevel level, string source, string format, params object[] args)
            {
                if (source == typeof(JsConsoleProvider).FullName)
                    Console.WriteLine(format, args);
            }

        }


        /// <summary>
        /// File system resolver
        /// </summary>
        private class FileSystemResolver : IDataReferenceResolver
        {
            public String RootDirectory { get; set; }

            public FileSystemResolver()
            {
                this.RootDirectory = Environment.CurrentDirectory;
            }

            /// <summary>
            /// Resolve specified reference
            /// </summary>
            public Stream Resolve(string reference)
            {
                reference = reference.Replace("~", this.RootDirectory);
                if (File.Exists(reference))
                    return File.OpenRead(reference);
                else
                {
                    Console.Error.WriteLine("ERR: {0}", reference);
                    return null;
                }
            }
        }

        /// <summary>
        /// Class for protocol debugging
        /// </summary>
        private class DebugProtocolRepository : IClinicalProtocolRepositoryService
        {
            /// <summary>
            /// Get the service name
            /// </summary>
            public String ServiceName => "Protocol Debugging Repository";

            // Protocols
            private List<Protocol> m_protocols = new List<Protocol>();

            /// <summary>
            /// Find a protocol
            /// </summary>
            public IEnumerable<Protocol> FindProtocol(Expression<Func<Protocol, bool>> predicate, int offset, int? count, out int totalResults)
            {
                totalResults = 0;
                return this.m_protocols.Where(predicate.Compile()).Skip(offset).Take(count ?? 100);
            }

            /// <summary>
            /// Insert protocol into the provider
            /// </summary>
            public Protocol InsertProtocol(Protocol data)
            {
                this.m_protocols.Add(data);
                return data;
            }
        }

        /// <summary>
        /// BRE debugger
        /// </summary>
        /// <param name="sources"></param>
        public ProtoDebugger(DebuggerParameters parms) : base(parms.WorkingDirectory)
        {
            Console.WriteLine("Starting debugger...");

            DebugApplicationContext.Start(parms);
            ApplicationServiceContext.Current = ApplicationContext.Current;
            try
            {
                ApplicationServiceContext.Current.GetService<IServiceManager>().RemoveServiceProvider(typeof(IClinicalProtocolRepositoryService));
            }
            catch { }

            ApplicationServiceContext.Current.GetService<IServiceManager>().AddServiceProvider(typeof(FileSystemResolver));
            ApplicationServiceContext.Current.GetService<IServiceManager>().AddServiceProvider(typeof(DebugProtocolRepository));
            Tracer.AddWriter(new ConsoleTraceWriter(EventLevel.LogAlways, "dbg", null), EventLevel.LogAlways);

            if (!String.IsNullOrEmpty(parms.WorkingDirectory))
                ApplicationContext.Current.GetService<FileSystemResolver>().RootDirectory = parms.WorkingDirectory;
            var rootPath = ApplicationContext.Current.GetService<FileSystemResolver>().RootDirectory;

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
                ApplicationContext.Current.GetService<IClinicalProtocolRepositoryService>().InsertProtocol(proto.GetProtocolData());
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

                        ApplicationContext.Current.GetService<IClinicalProtocolRepositoryService>().InsertProtocol(proto.GetProtocolData());
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
            foreach (var itm in ApplicationContext.Current.GetService<ICarePlanService>().Protocols)
                Console.WriteLine("{0}    {1}", itm.Id, itm.Name);
        }

        /// <summary>
        /// Set a breakpoint
        /// </summary>
        [Command("go", "Runs the clinical protocols to construct a care plan")]
        public object Run()
        {

            var cpService = ApplicationContext.Current.GetService<ICarePlanService>();
            if (cpService == null)
                throw new InvalidOperationException("No care plan service is registered");
            else if (this.m_scopeObject is Patient)
            {
                Console.WriteLine("Running care planner...");
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var cp = cpService.CreateCarePlan(this.m_scopeObject as Patient);
                sw.Stop();
                Console.WriteLine("Care plan generated in {0}", sw.Elapsed);
                return cp;
            }
            else
                throw new InvalidOperationException("Scope must be a patient object");
        }


        /// <summary>
        /// Set a breakpoint
        /// </summary>
        [Command("go.encounter", "Runs the clinical protocols to construct a care plan as appointments")]
        public object RunEncounter()
        {

            var cpService = ApplicationContext.Current.GetService<ICarePlanService>();
            if (cpService == null)
                throw new InvalidOperationException("No care plan service is registered");
            else if (this.m_scopeObject is Patient)
            {
                Console.WriteLine("Running care planner...");
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var cp = cpService.CreateCarePlan(this.m_scopeObject as Patient);
                sw.Stop();
                Console.WriteLine("Care plan generated in {0}", sw.Elapsed);
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
