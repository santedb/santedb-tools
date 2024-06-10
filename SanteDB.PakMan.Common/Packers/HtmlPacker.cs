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
 * User: fyfej
 * Date: 2023-6-21
 */
using SanteDB.Core.Applets.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace SanteDB.PakMan.Packers
{

    /// <summary>
    /// HTML file packager
    /// </summary>
    public class HtmlPacker : IFilePacker
    {
        /// <summary>
        /// Gets the extensions 
        /// </summary>
        public string[] Extensions => new string[] { ".htm", ".html", ".xhtml" };

        /// <summary>
        /// Process the specified file
        /// </summary>
        public AppletAsset Process(string file, bool optimize)
        {
            try
            {
                XElement xe = XElement.Load(file);

                if (xe.Name.Namespace != PakManTool.XS_HTML)
                {
                    xe.Name = (XNamespace)PakManTool.XS_HTML + xe.Name.LocalName;
                }

                // Optimizing?
                if (optimize)
                {
                    xe.DescendantNodesAndSelf().OfType<XComment>().Where(o => !o.Value.Contains("#include")).Remove();
                }

                // Now we have to iterate throuh and add the asset\
                AppletAssetHtml htmlAsset = null;
                XElement viewElement = null;

                if (xe.Elements().OfType<XElement>().Any(o => o.Name == (XNamespace)PakManTool.XS_APPLET + "widget"))
                {
                    viewElement = xe.Elements().OfType<XElement>().FirstOrDefault(o => o.Name == (XNamespace)PakManTool.XS_APPLET + "widget"); ;
                    htmlAsset = new AppletWidget()
                    {
                        Icon = viewElement.Element((XNamespace)PakManTool.XS_APPLET + "icon")?.Value,
                        Type = (AppletWidgetType)Enum.Parse(typeof(AppletWidgetType), viewElement.Attribute("type")?.Value),
                        Size = (AppletWidgetSize)Enum.Parse(typeof(AppletWidgetSize), viewElement.Attribute("size")?.Value ?? "Medium"),
                        AlternateViews = viewElement.Element((XNamespace)PakManTool.XS_APPLET + "views")?.Elements().Where(o => o.Name == (XNamespace)PakManTool.XS_APPLET + "view").Select(o => new AppletWidgetView()
                        {
                            ViewType = (AppletWidgetViewType)Enum.Parse(typeof(AppletWidgetViewType), o.Attribute("type")?.Value ?? "None"),
                            Policies = o.Elements().Where(d => d.Name == (XNamespace)PakManTool.XS_APPLET + "demand")?.Select(d => d?.Value).ToList()
                        }).ToList(),
                        ColorClass = viewElement.Attribute("headerClass")?.Value,
                        Priority = Int32.Parse(viewElement.Attribute("priority")?.Value ?? "0"),
                        MaxStack = Int32.Parse(viewElement.Attribute("maxStack")?.Value ?? "2"),
                        Order = Int32.Parse(viewElement.Attribute("order")?.Value ?? "0"),
                        Context = viewElement.Attribute("context")?.Value,
                        Description = viewElement.Elements().Where(o => o.Name == (XNamespace)PakManTool.XS_APPLET + "description").Select(o => new LocaleString() { Value = o.Value, Language = o.Attribute("lang")?.Value }).ToList(),
                        Name = viewElement.Attribute("name")?.Value,
                        Controller = viewElement.Element((XNamespace)PakManTool.XS_APPLET + "controller")?.Value,
                        Guard = viewElement.Elements().Where(o => o.Name == (XNamespace)PakManTool.XS_APPLET + "guard").Select(o => o.Value).ToList()
                    };
                }
                else
                {
                    htmlAsset = new AppletAssetHtml();
                    viewElement = xe.Elements().OfType<XElement>().Where(o => o.Name == (XNamespace)PakManTool.XS_APPLET + "state").FirstOrDefault();
                    // View state data
                    if (viewElement != null)
                    {
                        htmlAsset.ViewState = new AppletViewState()
                        {
                            Priority = Int32.Parse(viewElement.Attribute("priority")?.Value ?? "0"),
                            Name = viewElement.Attribute("name")?.Value,
                            Route = viewElement.Elements().OfType<XElement>().FirstOrDefault(r => r.Name == (XNamespace)PakManTool.XS_APPLET + "url" || r.Name == (XNamespace)PakManTool.XS_APPLET + "route")?.Value,
                            IsAbstract = Boolean.Parse(viewElement.Attribute("abstract")?.Value ?? "False"),
                            View = viewElement.Elements().OfType<XElement>().Where(v => v.Name == (XNamespace)PakManTool.XS_APPLET + "view")?.Select(v => new AppletView()
                            {
                                Priority = Int32.Parse(viewElement.Attribute("priority")?.Value ?? "0"),

                                Name = v.Attribute("name")?.Value,
                                Controller = v.Element((XNamespace)PakManTool.XS_APPLET + "controller")?.Value
                            }).ToList()
                        };
                        htmlAsset.Titles = xe.Elements().OfType<XElement>().Where(t => t.Name == (XNamespace)PakManTool.XS_APPLET + "title")?.Select(t => new LocaleString()
                        {
                            Language = t.Attribute("lang")?.Value,
                            Value = t?.Value
                        }).ToList();
                    }
                    htmlAsset.Static = xe.Attribute((XNamespace)PakManTool.XS_APPLET + "static")?.Value == "true";
                }

                htmlAsset.Titles = new List<LocaleString>(xe.Descendants().OfType<XElement>().Where(o => o.Name == (XNamespace)PakManTool.XS_APPLET + "title").Select(o => new LocaleString() { Language = o.Attribute("lang")?.Value, Value = o.Value }));
                htmlAsset.Bundle = new List<string>(xe.Descendants().OfType<XElement>().Where(o => o.Name == (XNamespace)PakManTool.XS_APPLET + "bundle").Select(o => PakManTool.TranslatePath(o.Value)));
                htmlAsset.Script = new List<AssetScriptReference>(xe.Descendants().OfType<XElement>().Where(o => o.Name == (XNamespace)PakManTool.XS_APPLET + "script").Select(o => new AssetScriptReference()
                {
                    Reference = PakManTool.TranslatePath(o.Value),
                    IsStatic = Boolean.Parse(o.Attribute("static")?.Value ?? "true")
                }));
                htmlAsset.Style = new List<string>(xe.Descendants().OfType<XElement>().Where(o => o.Name == (XNamespace)PakManTool.XS_APPLET + "style").Select(o => PakManTool.TranslatePath(o.Value)));

                var demand = viewElement?.Elements().OfType<XElement>().Where(o => o.Name == (XNamespace)PakManTool.XS_APPLET + "demand").Select(o => o.Value).ToList();

                var includes = xe.DescendantNodes().OfType<XComment>().Where(o => o?.Value?.Trim().StartsWith("#include virtual=\"") == true).ToList();
                foreach (var inc in includes)
                {
                    String assetName = inc.Value.Trim().Substring(18); // HACK: Should be a REGEX
                    if (assetName.EndsWith("\""))
                    {
                        assetName = assetName.Substring(0, assetName.Length - 1);
                    }

                    if (assetName == "content")
                    {
                        continue;
                    }

                    var includeAsset = PakManTool.TranslatePath(assetName);
                    inc.AddAfterSelf(new XComment(String.Format("#include virtual=\"{0}\"", includeAsset)));
                    inc.Remove();
                }

                var xel = xe.Descendants().OfType<XElement>().Where(o => o.Name.Namespace == (XNamespace)PakManTool.XS_APPLET).ToList();
                if (xel != null)
                {
                    foreach (var x in xel)
                    {
                        x.Remove();
                    }
                }

                htmlAsset.Html = xe;

                return new AppletAsset()
                {
                    MimeType = "text/html",
                    Content = htmlAsset,
                    Policies = demand
                };
            }
            catch (XmlException e)
            {
                throw new XmlException($"{file} is not well formed @ {e.LineNumber}:{e.LinePosition}", e);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Cannot process HTML {file}", e);
                throw;
            }
        }
    }
}
