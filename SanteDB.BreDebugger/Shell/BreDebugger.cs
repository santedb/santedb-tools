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
using Jint;
using Jint.Runtime.Debugger;
using Jint.Runtime.Interop;
using Newtonsoft.Json;
using SanteDB.BusinessRules.JavaScript;
using SanteDB.Core;
using SanteDB.Core.Applets.ViewModel.Json;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Model;
using SanteDB.Core.Services;
using SanteDB.SDK.BreDebugger.Options;
using SanteDB.SDK.BreDebugger.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SanteDB.SDK.BreDebugger.Shell
{
    /// <summary>
    /// Business Rules debugger
    /// </summary>
    [Description("Business Rules Debugger")]
    public class BreDebugger : DebuggerBase
    {

        private bool m_blocPrint = false;
        private bool m_isStepRegistered = false;

        // Running thread
        private Thread m_runThread = null;

        // Loaded files
        private Dictionary<String, String> m_loadedFiles = new Dictionary<string, string>();

        /// <summary>
        /// Thread event
        /// </summary>
        private ManualResetEventSlim m_resetEvent = new ManualResetEventSlim(false);

        /// <summary>
        /// Step mode
        /// </summary>
        private StepMode? m_stepMode;

        /// <summary>
        /// Current debug information
        /// </summary>
        private DebugInformation m_currentDebug;

        /// <summary>
        /// Rule file
        /// </summary>
        private String m_loadFile = String.Empty;

        // Parameters
        private DebuggerParameters m_parms;

        /// <summary>
        /// BRE debugger
        /// </summary>
        /// <param name="sources"></param>
        public BreDebugger(DebuggerParameters parms) : base(parms)
        {

            this.m_parms = parms;
            Console.WriteLine("Starting debugger...");
            //Tracer.AddWriter(new ConsoleTraceWriter(EventLevel.LogAlways, "dbg", null), EventLevel.LogAlways);

            if (!String.IsNullOrEmpty(parms.WorkingDirectory))
            {
                ApplicationServiceContext.Current.GetService<FileSystemResolver>().RootDirectory = parms.WorkingDirectory;
            }

            var rootPath = ApplicationServiceContext.Current.GetService<FileSystemResolver>().RootDirectory;

            // Load debug targets
            Console.WriteLine("Loading debuggees...");
            JavascriptExecutorPool.Current.ExecuteGlobal(o => o.Engine.DebugHandler.Step += JreStep);

            if (parms.Extensions != null)
            {
                foreach (var ext in parms.Extensions)
                {
                    var extParts = ext.Split(':');
                    if (extParts.Length != 2)
                    {
                        throw new InvalidOperationException($"Extension parameters must include alias and type such as --MyExtension:MyNamespace.MyClass");
                    }
                    var extType = Type.GetType(extParts[1]);
                    if (extType == null)
                    {
                        throw new InvalidOperationException($"Cannot find extension type {extParts[1]} - use the --assembly=X to force loading your assembly");
                    }
                    JavascriptExecutorPool.Current.ExecuteGlobal(j => j.AddExposedObject(extParts[0], Activator.CreateInstance(extType)));
                }
            }

            if (parms.Sources != null)
            {
                foreach (var rf in parms.Sources)
                {
                    var f = rf.Replace("~", rootPath);
                    if (!File.Exists(f))
                    {
                        Console.Error.WriteLine("Can't find file {0}", f);
                    }
                    else
                    {
                        this.Execute(f);
                    }
                }
            }
        }

        /// <summary>
        /// Terminate the thread
        /// </summary>
        [Command("reset", "Reset the environment")]
        public void ResetEvironment()
        {
            JavascriptExecutorPool.Current.Dispose();
            JavascriptExecutorPool.Current.ExecuteGlobal(o => o.Engine.DebugHandler.Step += JreStep);
            this.m_loadedFiles.Clear();
            // Load debug targets
            Console.WriteLine("Reloading debuggees...");
            var rootPath = ApplicationServiceContext.Current.GetService<FileSystemResolver>().RootDirectory;
            if (this.m_parms.Sources != null)
            {
                foreach (var rf in this.m_parms.Sources)
                {
                    var f = rf.Replace("~", rootPath);
                    if (!File.Exists(f))
                    {
                        Console.Error.WriteLine("Can't find file {0}", f);
                    }
                    else
                    {
                        this.Execute(f);
                    }
                }
            }
        }

        /// <summary>
        /// Register for step
        /// </summary>
        public void StepRegister(bool state)
        {

            this.m_isStepRegistered = state;
        }

        /// <summary>
        /// Step is called
        /// </summary>
        private Jint.Runtime.Debugger.StepMode JreStep(object sender, Jint.Runtime.Debugger.DebugInformation e)
        {
            if (this.m_stepMode.HasValue && (this.m_stepMode == StepMode.Over || this.m_stepMode == StepMode.Into) || this.m_isStepRegistered || this.Breakpoints.Contains(e.CurrentNode.Location.Start.Line))
            {
                var col = Console.ForegroundColor;
                Console.ForegroundColor = this.GetResponseColor();
                this.m_prompt = $"{e.CurrentCallFrame.FunctionName ?? this.m_loadFile} @ {e.CurrentNode.Location.Start.Line} (step) >";
                this.m_currentDebug = e;
                int l = Console.CursorLeft;
                Console.CursorLeft = 0;
                Console.Write(new string(' ', l));
                Console.CursorLeft = 0;
                if (this.m_blocPrint)
                {
                    this.PrintBlock();
                }
                else
                {
                    this.PrintLoc();
                }

                Console.ForegroundColor = col;
                this.Prompt();
                this.m_resetEvent.Wait();
                this.m_resetEvent.Reset();
                this.m_currentDebug = null;
                return this.m_stepMode ?? StepMode.Into;
            }
            else
            {
                return StepMode.Into;
            }
        }

        [Command("ob", "Sets the break output to block mode")]
        public void OutputBlock() => this.m_blocPrint = true;

        [Command("ol", "Sets the break output to line mode")]
        public void OutputLine() => this.m_blocPrint = false;

        /// <summary>
        /// Loads a script to be debugged
        /// </summary>
        [Command("o", "Open a script file to be debugged")]
        public void LoadScript(String file)
        {
            if (!Path.IsPathRooted(file))
            {
                file = Path.Combine(this.m_workingDirectory, file);
            }

            if (!File.Exists(file))
            {
                throw new FileNotFoundException(file);
            }

            if (!String.IsNullOrEmpty(this.m_loadFile))
            {
                Console.WriteLine("WARN: The file you're attempting to load is already loaded but not executed. Reloading.");
            }

            String key = Path.GetFileName(file);
            if (this.m_loadedFiles.ContainsKey(key))
            {
                throw new InvalidOperationException($"File {key} already loaded and executed (or executing)");
            }

            this.m_loadedFiles.Add(key, file);
            this.m_loadFile = file;
            this.m_prompt = key + " (idle) >";

        }

        /// <summary>
        /// Terminate the thread
        /// </summary>
        [Command("t", "Terminates the current execution")]
        public void Terminate()
        {
            if (this.m_runThread != null)
            {
                this.m_runThread.Abort();
            }

            this.m_runThread = null;
        }


        /// <summary>
        /// Loads a script to be debugged
        /// </summary>
        [Command("x", "Executes the loaded file")]
        public void Execute()
        {
            // Exec = continue execution
            if (this.m_currentDebug != null)
            {
                this.StepRegister(false);
                this.m_resetEvent.Set();
                return;
            }

            if (String.IsNullOrEmpty(this.m_loadFile))
            {
                throw new InvalidOperationException("No file in buffer");
            }
            else if (this.m_runThread != null)
            {
                throw new InvalidOperationException("Script is already running...");
            }

            this.m_prompt = Path.GetFileName(this.m_loadFile) + " (run) >";

            this.m_runThread = new Thread(() =>
            {
                using (var sr = File.OpenText(this.m_loadFile))
                {
                    JavascriptExecutorPool.Current.ExecuteGlobal(e => e.ExecuteScript(Path.GetFileName(this.m_loadFile), sr.ReadToEnd()));
                }

                this.m_prompt = Path.GetFileName(this.m_loadFile) + " (idle) >";
                this.m_loadFile = Path.GetFileName(this.m_loadFile);
                Console.WriteLine("\r\nExecution Finished");
                this.Prompt();

            });
            this.m_runThread.Start();
        }

        /// <summary>
        /// Loads a script to be debugged
        /// </summary>
        [Command("x", "Loads and executes the specified file")]
        public void Execute(String file)
        {
            this.LoadScript(file);
            this.Execute();
        }

        /// <summary>
        /// Set a breakpoint
        /// </summary>
        [Command("gn", "Go until next breakpoint")]
        public void GoNext()
        {
            this.StepRegister(false);

            if (this.m_currentDebug != null)
            {

                this.m_stepMode = null;
                this.m_resetEvent.Set();

            }
            else
            {
                throw new InvalidOperationException("Step-in can only be done when loaded but not executing");
            }
        }

        /// <summary>
        /// Set a breakpoint
        /// </summary>
        [Command("gi", "Steps into the current line number")]
        public void GoIn()
        {
            this.StepRegister(true);

            if (this.m_currentDebug != null)
            {
                this.m_stepMode = StepMode.Into;
                this.m_resetEvent.Set();
            }
            else if (!String.IsNullOrEmpty(this.m_loadFile))
            {
                this.Execute();
            }
            else
            {
                throw new InvalidOperationException("Step-in can only be done when loaded but not executing");
            }
        }

        /// <summary>
        /// Set a breakpoint
        /// </summary>
        [Command("go", "Steps over the current line number")]
        public void GoOver()
        {
            if (this.m_currentDebug != null)
            {
                this.m_stepMode = StepMode.Over;
                this.m_resetEvent.Set();
            }
            else if (!String.IsNullOrEmpty(this.m_loadFile))
            {
                this.StepRegister(false);

                this.Execute();
            }
            else
            {
                throw new InvalidOperationException("Step-over can only be done when loaded but not executing");
            }
        }

        /// <summary>
        /// Set a breakpoint
        /// </summary>
        [Command("gu", "Steps out of the current scope ")]
        public void GoOut()
        {
            if (this.m_currentDebug != null)
            {
                this.m_stepMode = StepMode.Out;
                this.m_resetEvent.Set();
            }
            else if (!String.IsNullOrEmpty(this.m_loadFile))
            {
                this.StepRegister(false);

                this.Execute();
            }
            else
            {
                throw new InvalidOperationException("Step-over can only be done when loaded but not executing");
            }
        }

        /// <summary>
        /// Dump locals
        /// </summary>
        [Command("dl", "Dumps local variables to console")]
        public void DumpLocals()
        {
            this.DumpLocals(null);
        }

        /// <summary>
        /// Dump locals
        /// </summary>
        [Command("dl", "Dumps specified local variable to console")]
        public void DumpLocals(String id)
        {
            this.DumpLocals(id, null);
        }

        /// <summary>
        /// Dump locals
        /// </summary>
        [Command("dl", "Dumps specified local variable path to console")]
        public void DumpLocals(String id, String path)
        {
            this.ThrowIfNotDebugging();
            // Locals?
            var locals = this.m_currentDebug.CurrentScopeChain.First(o => o.ScopeType == DebugScopeType.Local);
            if (id == null)
            {
                this.DumpObject(locals.BindingNames.ToDictionary(o => o, o => locals.GetBindingValue(o)), path);
            }
            else
            {
                var kobj = locals.GetBindingValue(id);
                try
                {
                    if (kobj.IsObject())
                    {
                        this.DumpObject((kobj.AsObject() as ObjectWrapper).Target, path);
                    }
                    else
                    {
                        this.DumpObject(kobj?.AsObject()?.GetOwnProperties(), path);
                    }
                }
                catch
                {
                    if (kobj.IsObject())
                    {
                        this.DumpObject(kobj?.AsObject()?.GetOwnProperties(), path);
                    }
                    else
                    {
                        Console.WriteLine("{0} = {1}", id, kobj);
                    }
                }
            }
        }

        /// <summary>
        /// Dump scope as json
        /// </summary>
        /// <param name="path"></param>
        [Command("dlv", "Dumps local as ViewModel JSON to screen")]
        public void DumpLocalsJson(String id)
        {
            this.DumpLocalsJson(id, null);
        }

        /// <summary>
        /// Dump scope
        /// </summary>
        /// <param name="path"></param>
        [Command("dlv", "Dumps local as ViewModel JSON to screen")]
        public void DumpLocalsJson(String id, String path)
        {

            this.ThrowIfNotDebugging();
            var locals = this.m_currentDebug.CurrentScopeChain.First(o => o.ScopeType == DebugScopeType.Local);

            // Locals?
            if (id == null)
            {
                this.DumpObject(locals.BindingNames.ToDictionary(o => o, o => locals.GetBindingValue(o)), path);
            }
            else
            {
                var kobj = locals.GetBindingValue(id);
                try
                {
                    if (kobj.IsObject())
                    {
                        Object obj = new Dictionary<String, Object>((kobj.AsObject() as ObjectWrapper).Target as ExpandoObject);
                        obj = SanteDB.BusinessRules.JavaScript.Util.JavascriptUtils.ToModel(this.GetScopeObject(obj, path));
                        JsonViewModelSerializer xsz = new JsonViewModelSerializer();
                        using (var jv = new JsonTextWriter(Console.Out) { Formatting = Newtonsoft.Json.Formatting.Indented })
                        {
                            xsz.Serialize(jv, obj as IdentifiedData);
                        }
                        Console.WriteLine();
                    }
                    else
                    {
                        this.DumpObject(kobj?.AsArray() ?? kobj?.AsObject(), path);
                    }
                }
                catch
                {
                    this.DumpObject(kobj?.AsObject()?.GetOwnProperties(), path);
                }
            }



        }
        /// <summary>
        /// Dump locals
        /// </summary>
        [Command("dg", "Dumps global variables to console")]
        public void DumpGlobals()
        {
            this.DumpGlobals(null);
        }

        /// <summary>
        /// Dump locals
        /// </summary>
        [Command("dg", "Dumps specified global variable to console")]
        public void DumpGlobals(String id)
        {
            this.DumpGlobals(id, null);
        }

        /// <summary>
        /// Dump locals
        /// </summary>
        [Command("dg", "Dumps specified global variable path to console")]
        public void DumpGlobals(String id, String path)
        {
            this.ThrowIfNotDebugging();
            // Locals?
            var globals = this.m_currentDebug.CurrentScopeChain.First(o => o.ScopeType == DebugScopeType.Global);
            if (id == null)
            {
                this.DumpObject(globals.BindingNames.ToDictionary(o => o, o => globals.GetBindingValue(o)), path);
            }
            else
            {
                var kobj = globals.GetBindingValue(id);
                try
                {
                    if (kobj.IsObject())
                    {
                        this.DumpObject((kobj.AsObject() as ObjectWrapper).Target, path);
                    }
                    else
                    {
                        this.DumpObject(kobj?.AsObject()?.GetOwnProperties(), path);
                    }
                }
                catch
                {
                    this.DumpObject(kobj?.AsObject()?.GetOwnProperties(), path);
                }
            }

        }

        /// <summary>
        /// Dump locals
        /// </summary>
        [Command("cs", "Shows the current callstack")]
        public void CallStack()
        {
            this.ThrowIfNotDebugging();
            int i = 0;
            foreach (var itm in this.m_currentDebug.CallStack)
            {
                Console.WriteLine("{0}:{1}", i++, itm);
            }
        }

        /// <summary>
        /// Check in debug mode
        /// </summary>
        private void ThrowIfNotDebugging()
        {
            if (this.m_currentDebug == null)
            {
                throw new InvalidOperationException("Not in break / debug mode");
            }
        }

        /// <summary>
        /// Print line of code
        /// </summary>
        [Command("pl", "Prints the current line of code")]
        public void PrintLoc()
        {
            this.ThrowIfNotDebugging();
            this.PrintFile(this.m_currentDebug.CurrentNode.Location.Start.Line.ToString(), this.m_currentDebug.CurrentNode.Location.Start.Line.ToString());
        }

        /// <summary>
        /// Print block of code
        /// </summary>
        [Command("pb", "Prints the current block of code")]
        public void PrintBlock()
        {
            this.ThrowIfNotDebugging();
            this.PrintFile((this.m_currentDebug.CurrentNode.Location.Start.Line - 5).ToString(), (this.m_currentDebug.CurrentNode.Location.End.Line + 5).ToString());

        }

        /// <summary>
        /// Print block of code
        /// </summary>
        [Command("pf", "Prints the current file in memory")]
        public void PrintFile()
        {
            this.PrintFile(null, null);
        }

        /// <summary>
        /// Print file
        /// </summary>
        [Command("pf", "Prints the current file in memory from the start-end lines")]
        public void PrintFile(string start, string end)
        {
            String fileName = null;
            if (this.m_currentDebug != null)
            {
                if (!this.m_loadedFiles.TryGetValue(this.m_currentDebug.CurrentCallFrame.FunctionName ?? this.m_loadFile, out fileName))
                {
                    throw new InvalidOperationException($"Source for {this.m_currentDebug.CurrentCallFrame.FunctionName} not found");
                }
            }
            else if (!this.m_loadedFiles.TryGetValue(Path.GetFileName(this.m_loadFile), out fileName))
            {
                throw new InvalidOperationException($"Source for '{this.m_loadFile}' not found");
            }

            int ln = 1,
                startInt = String.IsNullOrEmpty(start) ? 0 : Int32.Parse(start),
                endInt = String.IsNullOrEmpty(end) ? Int32.MaxValue : Int32.Parse(end);

            using (var sr = File.OpenText(fileName))
            {
                while (!sr.EndOfStream)
                {
                    if (ln >= startInt && ln <= endInt)
                    {
                        Console.WriteLine($"[{ln}]{(this.m_currentDebug?.CurrentNode.Location.Start.Line <= ln && this.m_currentDebug?.CurrentNode.Location.End.Line >= ln ? "++>" : this.Breakpoints.Contains(ln) ? "***" : "   ")}{sr.ReadLine()}");
                    }
                    else if (ln >= endInt)
                    {
                        break;
                    }
                    else
                    {
                        sr.ReadLine();
                    }

                    ln++;
                }

                if (sr.EndOfStream)
                {
                    if (this.m_currentDebug != null)
                    {
                        Console.WriteLine("Source not avaialble - (built-in function - showing disassembly)");
                        Console.WriteLine("{0}: {1}", this.m_currentDebug.CurrentNode.Location.Start.Line, this.m_currentDebug.CurrentNode.Type);
                        foreach (var itm in this.m_currentDebug.CurrentNode.ChildNodes.Where(o => o != null))
                        {
                            Console.WriteLine("\t{0}: {1}", itm.Location.Start.Line, itm.Type);
                        }
                        return;
                    }
                    else
                    {
                        Console.WriteLine("<<EOF>>");
                    }
                }

            }
        }

        /// <summary>
        /// Executes a validator
        /// </summary>
        [Command("go.validate", "Executes the validator for the current scope object")]
        public void ExecuteValidator()
        {
            this.ExecuteValidator(false, this.m_scopeObject?.GetType());
        }

        /// <summary>
        /// Executes a validator
        /// </summary>
        [Command("gi.validate", "Steps into the validator for the current scope object casting it before running")]
        public void ExecuteStepValidator(string cast)
        {
            var t = Type.GetType(cast);
            if (t == null)
            {
                t = new SanteDB.Core.Model.Serialization.ModelSerializationBinder().BindToType(typeof(IdentifiedData).Assembly.FullName, cast);
            }

            if (t == null)
            {
                throw new ArgumentOutOfRangeException(nameof(cast));
            }
            this.ExecuteValidator(true, t);
        }


        /// <summary>
        /// Executes a validator
        /// </summary>
        [Command("go.validate", "Executes the validator for the current scope object casting it before running")]
        public void ExecuteValidator(string cast)
        {
            var t = Type.GetType(cast);
            if (t == null)
            {
                t = new SanteDB.Core.Model.Serialization.ModelSerializationBinder().BindToType(typeof(IdentifiedData).Assembly.FullName, cast);
            }

            if (t == null)
            {
                throw new ArgumentOutOfRangeException(nameof(cast));
            }
            this.ExecuteValidator(false, t);
        }

        /// <summary>
        /// Executes a validator
        /// </summary>
        [Command("gi.validate", "Steps into the validator for the current scope object")]
        public void ExecuteStepValidator()
        {
            this.ExecuteValidator(true, this.m_scopeObject?.GetType());
        }

        /// <summary>
        /// Execute a validator
        /// </summary>
        private void ExecuteValidator(bool stepIn, Type asType)
        {
            this.StepRegister(stepIn);

            if (this.m_scopeObject == null)
            {
                throw new InvalidOperationException("Cannot validate a null scope object");
            }
            else if (this.m_currentDebug != null)
            {
                throw new InvalidOperationException("Cannot execute validator at this time");
            }

            if (asType == null)
            {
                asType = this.m_scopeObject.GetType();
            }

            var rdb = typeof(IBusinessRulesService<>).MakeGenericType(asType);
            var rds = ApplicationServiceContext.Current.GetService(rdb);
            if (rds == null)
            {
                throw new InvalidOperationException($"Cannot find business rule registered for {this.m_scopeObject.GetType().Name}");
            }
            else
            {
                this.m_prompt = Path.GetFileName(this.m_loadFile) + " (run) >";

                this.m_runThread = new Thread(() =>
                {
                    try
                    {
                        var mi = rdb.GetMethod("Validate");
                        foreach (var itm in mi.Invoke(rds, new object[] { this.m_scopeObject }) as List<DetectedIssue>)
                        {
                            Console.WriteLine("{0}\t{1}", itm.Priority, itm.Text);
                        }
                        this.m_prompt = Path.GetFileName(this.m_loadFile) + " (idle) >";
                        Console.WriteLine("\r\nExecution Complete");
                        this.Prompt();

                    }
                    catch (Exception e)
                    {
                        this.PrintStack(e);
                        this.Prompt();

                    }

                });
                this.m_runThread.Start();
            }
        }
        /// <summary>
        /// Executes a validator
        /// </summary>
        [Command("go.rule", "Executes the validator for the current scope object")]
        public void ExecuteRule(String @event)
        {
            this.ExecuteRule(@event, false, this.m_scopeObject?.GetType());
        }

        /// <summary>
        /// Executes a validator
        /// </summary>
        [Command("gi.rule", "Steps into the validator for the current scope object casting it before running")]
        public void ExecuteRule(String @event, string cast)
        {
            var t = Type.GetType(cast);
            if (t == null)
            {
                t = new SanteDB.Core.Model.Serialization.ModelSerializationBinder().BindToType(typeof(IdentifiedData).Assembly.FullName, cast);
            }

            if (t == null)
            {
                throw new ArgumentOutOfRangeException(nameof(cast));
            }
            this.ExecuteRule(@event, true, t);
        }


        /// <summary>
        /// Executes a validator
        /// </summary>
        [Command("go.rule", "Executes the validator for the current scope object casting it before running")]
        public void ExecuteStepRule(String @event, string cast)
        {
            var t = Type.GetType(cast);
            if (t == null)
            {
                t = new SanteDB.Core.Model.Serialization.ModelSerializationBinder().BindToType(typeof(IdentifiedData).Assembly.FullName, cast);
            }

            if (t == null)
            {
                throw new ArgumentOutOfRangeException(nameof(cast));
            }
            this.ExecuteRule(@event, false, t);
        }

        /// <summary>
        /// Executes a validator
        /// </summary>
        [Command("gi.rule", "Steps into the validator for the current scope object")]
        public void ExecuteStepRule(String @event)
        {
            this.ExecuteRule(@event, true, this.m_scopeObject?.GetType());
        }

        /// <summary>
        /// Execute a validator
        /// </summary>
        private void ExecuteRule(String @event, bool stepIn, Type asType)
        {
            this.StepRegister(stepIn);

            if (this.m_scopeObject == null)
            {
                throw new InvalidOperationException("Cannot validate a null scope object");
            }
            else if (this.m_currentDebug != null)
            {
                throw new InvalidOperationException("Cannot execute validator at this time");
            }

            if (asType == null)
            {
                asType = this.m_scopeObject.GetType();
            }

            var rdb = typeof(IBusinessRulesService<>).MakeGenericType(asType);
            var rds = ApplicationServiceContext.Current.GetService(rdb);
            if (rds == null)
            {
                throw new InvalidOperationException($"Cannot find business rule registered for {this.m_scopeObject.GetType().Name}");
            }
            else
            {
                this.m_prompt = Path.GetFileName(this.m_loadFile) + " (run) >";

                this.m_runThread = new Thread(() =>
                {
                    try
                    {
                        var mi = rdb.GetMethod(@event);
                        this.m_scopeObject = mi.Invoke(rds, new object[] { this.m_scopeObject });
                        this.m_prompt = Path.GetFileName(this.m_loadFile) + " (idle) >";
                        Console.WriteLine("\r\nExecution Complete, result set to scope");
                        this.Prompt();
                    }
                    catch (Exception e)
                    {
                        this.PrintStack(e);
                        this.Prompt();

                    }
                });
                this.m_runThread.Start();
            }
        }

        /// <summary>
        /// Exit the specified context
        /// </summary>
        public override void Exit()
        {
            this.m_runThread?.Abort();
            base.Exit();
        }
    }

}
