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
 * Date: 2023-3-10
 */
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Security;
using SanteDB.Messaging.AMI.Client;
using SanteDB.AdminConsole.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace SanteDB.AdminConsole.Util
{
    /// <summary>
    /// Display utilities
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class DisplayUtil
    {
        // Ami client
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.AdministrationIntegrationService));

        /// <summary>
        /// Print table 
        /// </summary>
        public static void TablePrint<T>(IEnumerable<T> data, params Expression<Func<T, Object>>[] columns)
        {
            TablePrint(data, null, null, columns);
        }

        /// <summary>
        /// Print data as columns
        /// </summary>
        public static void TablePrint<T>(IEnumerable<T> data, String[] colNames, int[] colWidths, params Expression<Func<T, Object>>[] columns)
        {

            if (colNames != null && colNames.Length != columns.Length)
            {
                throw new ArgumentException("When specified, colNames must match columns");
            }

            // Column width
            int defaultWidth = (Console.WindowWidth - columns.Length) / columns.Length,
                c = 0;
            int[] cWidths = colWidths ?? columns.Select(o => defaultWidth - 2).ToArray();

            foreach (var col in columns)
            {
                // Only process lambdas
                if (colNames != null)
                {
                    if (col.NodeType != ExpressionType.Lambda)
                    {
                        continue;
                    }
                }

                var body = (col as LambdaExpression).Body;
                if (body.NodeType == ExpressionType.Convert)
                {
                    body = (body as UnaryExpression).Operand;
                }

                var member = (body as MemberExpression)?.Member;
                string colName = colNames?[c] ?? member?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? member?.Name ?? "??";
                if (colName.Length > cWidths[c])
                {
                    Console.Write("{0}... ", colName.Substring(0, colWidths[c] - 3));
                }
                else
                {
                    Console.Write("{0}{1} ", colName, new String(' ', cWidths[c] - colName.Length));
                }

                c++;
            }

            Console.WriteLine();

            // Now output data
            foreach (var tuple in data)
            {
                c = 0;
                foreach (var col in columns)
                {
                    try
                    {
                        Object value = col.Compile().DynamicInvoke(tuple);
                        if(value is Byte[] b)
                        {
                            value = b.HexEncode();
                        }
                        else if (value is DateTime dt)
                        {
                            value = dt.ToLocalTime();
                        }
                        String stringValue = value?.ToString();
                        if (stringValue == null)
                        {
                            Console.Write(new string(' ', cWidths[c] + 1));
                        }
                        else if (stringValue.Length > cWidths[c])
                        {
                            Console.Write("{0}... ", stringValue.Substring(0, cWidths[c] - 3));
                        }
                        else
                        {
                            Console.Write("{0}{1} ", stringValue, new String(' ', cWidths[c] - stringValue.Length));
                        }
                    }
                    catch
                    {
                        Console.Write(new string(' ', cWidths[c] + 1));
                    }
                    finally
                    {
                        c++;
                    }
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Print policy information
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="user"></param>
        public static void PrintPolicies<T>(ISecurityEntityInfo<T> user, String[] dataLabels, params Expression<Func<T, object>>[] data)
            where T : NonVersionedEntityData
        {

            int d = 0;
            foreach (var dat in data)
            {
                try
                {
                    Console.WriteLine("{0}: {1}", dataLabels[d], dat.Compile().DynamicInvoke(user.Entity));
                }
                catch
                {
                }
                finally
                {
                    d++;
                }
            }

            List<SecurityPolicyInfo> policies = m_client.GetPolicies(o => o.ObsoletionTime == null).CollectionItem.OfType<SecurityPolicy>().OrderBy(o => o.Oid).Select(o => new SecurityPolicyInfo(o)).ToList();
            policies.ForEach(o => o.Grant = (PolicyGrantType)10);
            foreach (var pol in user.Policies)
            {
                var existing = policies.FirstOrDefault(o => o.Oid == pol.Oid);
                if (pol.Grant < existing.Grant)
                {
                    existing.Grant = pol.Grant;
                }
            }

            Console.WriteLine("\tEffective Policies:");
            foreach (var itm in policies)
            {
                Console.Write("\t\t{0} [{1}] : ", itm.Name, itm.Oid);
                if (itm.Grant == (PolicyGrantType)10) // Lookup parent
                {
                    var parent = policies.LastOrDefault(o => itm.Oid.StartsWith(o.Oid + ".") && itm.Oid != o.Oid);
                    if (parent != null && parent.Grant <= PolicyGrantType.Grant)
                    {
                        Console.WriteLine("{0} (inherited from {1})", parent.Grant, parent.Name);
                    }
                    else
                    {
                        Console.WriteLine("--- (default DENY)");
                    }
                }
                else
                {
                    Console.WriteLine("{0} (explicit)", itm.Grant);
                }
            }
        }


        /// <summary>
        /// Read a masked line input
        /// </summary>
        public static String PasswordPrompt(String prompt)
        {
            Console.Write(prompt);
            StringBuilder input = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) break;
                if (key.Key == ConsoleKey.Backspace && input.Length > 0) input.Remove(input.Length - 1, 1);
                else if (key.Key != ConsoleKey.Backspace) input.Append(key.KeyChar);
            }
            return input.ToString();
        }

    }
}
