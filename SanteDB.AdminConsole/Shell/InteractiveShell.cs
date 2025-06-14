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
using DocumentFormat.OpenXml.Drawing.Charts;
using MohawkCollege.Util.Console.Parameters;
using SanteDB.AdminConsole.Attributes;
using SanteDB.AdminConsole.Parameters;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Http;
using SanteDB.Core.Security;
using SanteDB.Rest.Common.Fault;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SanteDB.AdminConsole.Shell
{
    /// <summary>
    /// Represents a base class for interactive shells
    /// </summary>
    [AdminCommandlet]
    [ExcludeFromCodeCoverage]
    public class InteractiveShell
    {
        // Exit debugger
        private bool m_exitRequested = false;

        private Regex m_extractParmsRegex = new Regex(@"((?:-\w\s|--\w+=)(\""[^\""]+\""|\'[^\']+\'|\w+)|([^\s]+))");
        protected string m_prompt = "> ";
        private ConsoleColor m_promptColor = Console.ForegroundColor;
        // Commandlets
        private Dictionary<AdminCommandAttribute, MethodInfo> m_commandlets = new Dictionary<AdminCommandAttribute, MethodInfo>();

        /// <summary>
        /// Sets the root path
        /// </summary>
        public InteractiveShell()
        {
            foreach (var t in AppDomain.CurrentDomain.GetAllTypes().Where(t => t.HasCustomAttribute<AdminCommandletAttribute>()))
            {
                foreach (var me in t.GetRuntimeMethods().Where(o => o.GetCustomAttribute<AdminCommandAttribute>() != null))
                {
                    this.m_commandlets.Add(me.GetCustomAttribute<AdminCommandAttribute>(), me);
                }

                if (t.GetRuntimeMethod("Init", new Type[0]) != null)
                {
                    t.GetRuntimeMethod("Init", new Type[0]).Invoke(null, null);
                }
            }
        }

        /// <summary>
        /// Get response color
        /// </summary>
        protected ConsoleColor GetResponseColor()
        {
            if (Console.BackgroundColor == ConsoleColor.Black)
            {
                return Console.ForegroundColor != ConsoleColor.Cyan ? ConsoleColor.Cyan : ConsoleColor.Magenta;
            }

            if (Console.BackgroundColor == ConsoleColor.Blue)
            {
                return Console.ForegroundColor != ConsoleColor.White ? ConsoleColor.Yellow : ConsoleColor.White;
            }
            else
            {
                return Console.ForegroundColor != ConsoleColor.Blue ? ConsoleColor.Blue : ConsoleColor.Red;
            }
        }

        /// <summary>
        /// Prompt
        /// </summary>
        protected void Prompt()
        {
            Console.ForegroundColor = m_promptColor;
            Console.Write(this.m_prompt);
        }

        /// <summary>
        /// Print stack
        /// </summary>
        protected virtual void PrintStack(Exception e)
        {
            Console.Error.WriteLine("ERR: {0}", e.Message);
            var i = e.InnerException; int l = 1;
            while (i != null)
            {
                Console.WriteLine("\t{0}:{1}", l++, i.Message);

                if (i is RestClientException<Object> rso && rso.Result is RestServiceFault rsf)
                {
                    Console.WriteLine("\t\tREMOTE: {0}", rsf.Message);
                    foreach (var itm in rsf.Rules ?? new List<DetectedIssue>())
                    {
                        Console.WriteLine("\t\tREMOTE: RULE: {0} {1}", itm.Priority.ToString(), itm.Text);
                    }

                    rsf = rsf.CausedBy;
                    while (rsf != null)
                    {
                        Console.WriteLine("\t\t\tREMOTE [CAUSE]: {0}", rsf.Message);
                        foreach (var itm in rsf.Rules ?? new List<DetectedIssue>())
                        {
                            Console.WriteLine("\t\t\tREMOTE [CAUSE]: RULE: {0} {1}", itm.Priority.ToString(), itm.Text);
                        }
                        rsf = rsf.CausedBy;

                    }

                }
                i = i.InnerException;
            }
        }

        /// <summary>
        /// Run a script
        /// </summary>
        /// <param name="scriptFile"></param>
        public void RunScript(String scriptFile)
        {
            try
            {
                using (var tr = File.OpenText(scriptFile))
                {
                    while (!tr.EndOfStream) {
                        var cmd = tr.ReadLine();
                        if (cmd.StartsWith("#"))
                        {
                            continue;
                        }
                        else
                        {
                            Console.WriteLine("EXEC: {0}", cmd);
                            this.RunCmd(cmd);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.PrintStack(e);
            }
        }

        /// <summary>
        /// Perform debugger
        /// </summary>
        public void Exec(ConsoleParameters parameters = null)
        {

            Console.CursorVisible = true;
            Console.WriteLine("Ready...");

            if (parameters.ScriptFiles?.Count > 0)
            {
                foreach(var itm in parameters.ScriptFiles)
                {
                    try
                    {
                        if(File.Exists(itm))
                        {
                            Console.WriteLine("Running {0}...", itm);
                            using(var fr = File.OpenText(itm))
                            {
                                while(!fr.EndOfStream)
                                {
                                    var cmd = fr.ReadLine();
                                    if(!cmd.StartsWith("#"))
                                    {
                                        Console.WriteLine(cmd);
                                        this.RunCmd(cmd);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("ERROR: {0} not found", itm);
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Error Executing Scripts: {0}", e.ToHumanReadableString());
                    }
                }
            }
            else
            {
                // Now drop to a command prompt
                while (!m_exitRequested)
                {
                    try
                    {
                        var col = Console.ForegroundColor;
                        this.Prompt();
                        var cmd = Console.ReadLine();
                        this.RunCmd(cmd);
                        Console.ForegroundColor = col;
                    }
                    catch
                    {

                    }
                }
            }
        }

        /// <summary>
        /// Run command
        /// </summary>
        /// <param name="cmd">The command text to run</param>
        public void RunCmd(String cmd)
        {

            Console.ForegroundColor = this.GetResponseColor();
            if (String.IsNullOrEmpty(cmd))
            {
                return;
            }

            var redir = cmd.Split('>');
            cmd = redir[0];
            string[] tokens = m_extractParmsRegex.Matches(cmd).OfType<Match>().SelectMany(o => {
                if (o.Groups[0].Value.StartsWith("--") || !o.Groups[0].Value.StartsWith("-"))
                {
                    return new string[] { o.Groups[0].Value };
                }
                else {
                    return new string[] { o.Groups[0].Value.Substring(0, 2), o.Groups[0].Value.Substring(3) };
                }
            }).ToArray();
            

            // Set output
            TextWriter tw = null, orig = Console.Out;
            if (redir.Length > 1)
            {
                if (redir.Length == 2)
                {
                    tw = File.CreateText(redir[1].Trim());
                }
                else if (redir.Length == 3)
                {
                    tw = File.AppendText(redir[2].Trim());
                }

                Console.SetOut(tw);
            }

            // Get tokens
            if (!this.m_commandlets.Keys.Any(o => o.Command == tokens[0]))
            {
                Console.Error.WriteLine("ERR: Command {0} with {1} parms not found", tokens[0], tokens.Length - 1);
            }
            else
            {
                var parmValues = tokens.Length > 1 ? tokens.OfType<String>().Skip(1).ToArray() : new string[0];

                try
                {
                    // Find the matches
                    var candidates = this.m_commandlets.Where(o => o.Key.Command == tokens[0]);
                    if (candidates.Count() == 1)
                    {
                        candidates.First().Value.Invoke(this, this.CreateParameters(parmValues, candidates.First().Value.GetParameters()));
                    }
                    else
                    {
                        var candidate = candidates.FirstOrDefault(o => parmValues.Length == o.Value.GetParameters().Length);
                        candidate.Value?.Invoke(this, this.CreateParameters(parmValues, candidate.Value?.GetParameters()));
                    }
                }
                catch (Exception e)
                {
                    this.PrintStack(e);
                }
            }

            if (tw != null)
            {
                tw.Close();
                Console.SetOut(orig);
            }
        }

        /// <summary>
        /// Create a parameter list
        /// </summary>
        private object[] CreateParameters(string[] args, ParameterInfo[] argTypes)
        {
            if (args == null)
            {
                return null;
            }

            object[] argVals = new object[argTypes.Length];
            for (int i = 0; i < argTypes.Length; i++)
            {
                if (argTypes[i].ParameterType == typeof(String))
                {
                    argVals[i] = args[i];
                }
                else
                {
                    var ppt = typeof(ParameterParser<>).MakeGenericType(argTypes[i].ParameterType);
                    var pp = Activator.CreateInstance(ppt);
                    argVals[i] = ppt.GetMethod("Parse").Invoke(pp, new object[] { args.Select(o=>o.Replace("\"", "").Replace("\'", "")).ToArray() });
                }
            }
            return argVals;
        }

        /// <summary>
        /// Show current version of console
        /// </summary>
        [AdminCommand("ver", "Shows current Admin Console Version")]
        public void Version()
        {
            Program.PrintHeader();
        }

        /// <summary>
        /// Clear the console
        /// </summary>
        [AdminCommand("clear", "Clears the screen")]
        public void Clear()
        {
            Console.Clear();
        }

        /// <summary>
        /// Get help
        /// </summary>
        [AdminCommand("help", "Shows help")]
        public void Help()
        {

            List<String> hlp = new List<string>();
            foreach (var mi in this.m_commandlets.OrderBy(o => o.Key.Command))
            {
                var itm = mi.Value.GetCustomAttribute<AdminCommandAttribute>();
                if (itm == null || String.IsNullOrEmpty(itm.Description))
                {
                    continue;
                }

                if (!hlp.Contains(itm.Command))
                {
                    hlp.Add(itm.Command);
                    Console.Write("{0:2}", itm.Command, String.Join(" ", mi.Value.GetParameters().Select(o => $"[{o.Name}]")));
                    Console.WriteLine("{0}{1}", new String(' ', 20 - Console.CursorLeft), itm.Description);
                }
            }
            Console.WriteLine("Use:\r\n\thelp cmd\r\nFor command specific help");
        }

        /// <summary>
        /// Get help
        /// </summary>
        [AdminCommand("help", "Show help for specific method")]
        public void Help([Description("The command for which help should be shown")] String cmd)
        {

            var cmdlets = this.m_commandlets.Where(o => o.Key.Command == cmd).Select(o => o.Value);
            foreach (var cmdlet in cmdlets)
            {
                var itm = cmdlet.GetCustomAttribute<AdminCommandAttribute>();
                if (itm == null || String.IsNullOrEmpty(itm.Description))
                {
                    return;
                }

                Console.WriteLine("{0} {1} - {2}", itm.Command, String.Join(" ", cmdlet.GetParameters().Select(o => o.Name)), itm.Description);

                var descr = cmdlet.GetCustomAttribute<DescriptionAttribute>();
                if (descr != null)
                {
                    Console.WriteLine("{0} + {1}", new String(' ', itm.Command.Length + 1), descr.Description);
                }

                foreach (var p in cmdlet.GetParameters())
                {
                    if (p.ParameterType == typeof(String))
                    {
                        Console.Write("{0}{1} - ", new String(' ', itm.Command.Length + 1), p.Name);
                        descr = p.GetCustomAttribute<DescriptionAttribute>();
                        if (descr != null)
                        {
                            Console.WriteLine("{0}", descr.Description);
                        }
                        else
                        {
                            Console.WriteLine();
                        }
                    }
                    else
                    {
                        var ppt = typeof(ParameterParser<>).MakeGenericType(p.ParameterType);
                        var pp = Activator.CreateInstance(ppt);
                        ppt.GetMethod("WriteHelp").Invoke(pp, new object[] { Console.Out });
                    }
                }
            }

        }

        /// <summary>
        /// Identifies current authentication principal
        /// </summary>
        [AdminCommand("whoami", "Identifies the current authentication principal")]
        public void Whoami()
        {
            Console.WriteLine(AuthenticationContext.Current.Principal.Identity.Name);
        }

        /// <summary>
        /// Exit the debugger
        /// </summary>
        [AdminCommand("exit", "Quits the administrative shell")]
        public virtual void Exit()
        {
            Console.ResetColor();
            Console.Clear();
            this.m_exitRequested = true;
            Environment.Exit(0);
        }


    }
}
