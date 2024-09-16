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
using SanteDB.Core.Applets.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;

namespace SanteDB.PakMan
{
    /// <summary>
    /// Inspect a package
    /// </summary>
    internal class Inspector
    {
        private readonly AppletPackage m_applet;

        public Inspector(PakManParameters parameters)
        {
            if (!File.Exists(parameters.Source))
            {
                throw new FileNotFoundException(parameters.Source);
            }
            using (var fs = File.OpenRead(parameters.Source))
            {
                this.m_applet = AppletPackage.Load(fs);
            }
        }

        private Inspector(AppletPackage package)
        {
            this.m_applet = package;
        }

        /// <summary>
        /// Print data as columns
        /// </summary>
        public static void TablePrint<T>(IEnumerable<T> data, params Expression<Func<T, Object>>[] columns)
        {


            // Column width
            int defaultWidth = (Console.WindowWidth - columns.Length) / columns.Length,
                c = 0;
            int[] cWidths = columns.Select(o => defaultWidth - 2).ToArray();

            foreach (var col in columns)
            {


                var body = (col as LambdaExpression).Body;
                if (body.NodeType == ExpressionType.Convert)
                {
                    body = (body as UnaryExpression).Operand;
                }

                var member = (body as MemberExpression)?.Member;
                string colName = member?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? member?.Name ?? "?";
                if (colName.Length > cWidths[c])
                {
                    Console.Write("{0}... ", colName.Substring(0, cWidths[c] - 3));
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
                        if (value is Byte[] b)
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
        /// Dump the inforation for the package
        /// </summary>
        internal int Dump()
        {
            Console.WriteLine("Package Type: {0}", this.m_applet.GetType().Name);
            Console.WriteLine("Tooling Version: {0}", this.m_applet.Version);
            Console.WriteLine("ID: {0}", this.m_applet.Meta.Id);
            Console.WriteLine("Version: {0}", this.m_applet.Meta.Version);
            Console.WriteLine("Author: {0}", this.m_applet.Meta.Author);
            Console.WriteLine("Name(s): {0}", string.Join("\r\n\t", this.m_applet.Meta.Names.Select(o => o.Value)));
            Console.WriteLine("Public Key ID: {0}", this.m_applet.Meta.PublicKeyToken);
            Console.WriteLine("Content Hash: {0}", this.m_applet.Meta.Hash.HexEncode());
            Console.WriteLine("Timestamp: {0}", this.m_applet.Meta.TimeStamp?.ToString("o") ?? "none");
            if (this.m_applet.PublicKey != null)
            {
                var cert = new X509Certificate2(this.m_applet.PublicKey);
                Console.WriteLine("=== Embedded Publisher Information ===");
                Console.WriteLine("SN: {0}", cert.Subject);
                Console.WriteLine("TUMB: {0}", cert.Thumbprint);
                Console.WriteLine("ISSUER: {0}", cert.Issuer);
                Console.WriteLine("VALIDITY: {0} THRU {1}", cert.NotBefore, cert.NotAfter);
            }

            if (this.m_applet is AppletSolution sln)
            {
                Console.WriteLine("-- INCLUDES --");
                var includes = sln.Include.Select(o => new
                {
                    Id = o.Meta.Id,
                    Version = o.Meta.Version,
                    PublicKey = o.PublicKey != null ? new X509Certificate2(o.PublicKey).Subject : null
                });
                TablePrint(includes, o => o.Id, o => o.Version, o => o.PublicKey);
            }
            else
            {
                var mfst = this.m_applet.Unpack();

                Console.WriteLine("MENUS: {0}", String.Join(" , ", mfst.Menus.OrderBy(o => o.Order).Select(o => $"{o.Text[0].Value} ({o.Context} - {o.Menus.Count()} sub-items)")));
                Console.WriteLine("TEMPLATES: {0}", String.Join(" , ", mfst.Templates.Select(o => o.Oid)));
                Console.WriteLine("LOCALES: {0}", String.Join(" , ", mfst.Locales.Select(o => o.Code)));
                Console.WriteLine("I18N STRINGS: {0}", String.Join(" , ", mfst.Strings.Select(o => $"{o.Language} ({o.String.Count()} strings - Refer: {o.Reference})")));
                Console.WriteLine("-- CONTENTS --");
                var contents = mfst.Assets.Select(itm =>
                {

                    long szContent = 0;
                    string typeName = String.Empty,
                        mimeType = itm.MimeType,
                        name = itm.Name;
                    switch (itm.Content)
                    {
                        case AppletWidget w:
                            typeName = "WIDGET";
                            szContent = Encoding.UTF8.GetByteCount(w.Html.ToString());
                            break;
                        case AppletAssetHtml h:
                            typeName = "HTML";
                            szContent = Encoding.UTF8.GetByteCount(h.Html.ToString());
                            break;
                        case byte[] b:
                            typeName = "BINARY";
                            szContent = b.Length;
                            break;
                        case AppletAssetVirtual v:
                            typeName = "VIRTUAL";
                            szContent = 0;
                            break;
                        case XElement x:
                            typeName = "XML";
                            szContent = Encoding.UTF8.GetByteCount(x.ToString());
                            break;
                        case string s:
                            typeName = "TEXT";
                            szContent = Encoding.UTF8.GetByteCount(s);
                            break;
                    }
                    return new
                    {
                        Type = typeName,
                        MimeType = mimeType,
                        Name = name,
                        Size = szContent
                    };
                });
                TablePrint(contents, o => o.Type, o => o.MimeType, o => o.Name, o => $"{o.Size / 1024f:#,##0.#} kb");
                Console.WriteLine("Total: {0} assets ({1:#,##0.#} kb)", contents.Count(), contents.Sum(o => o.Size) / 1024f);
            }
            return 0;

        }
    }
}