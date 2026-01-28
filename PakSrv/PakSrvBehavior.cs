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
using RestSrvr;
using RestSrvr.Attributes;
using RestSrvr.Exceptions;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Model.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace SanteDB.PakSrv
{
    /// <summary>
    /// Package service behavior
    /// </summary>
    [ServiceBehavior(InstanceMode = ServiceInstanceMode.Singleton)]
    public class PakSrvBehavior : IPakSrvContract
    {

        /// <summary>
        /// Configuration
        /// </summary>
        private PakSrvConfiguration m_configuration;

        /// <summary>
        /// Configuration
        /// </summary>
        public PakSrvBehavior()
        {
            this.m_configuration = PakSrvHost.m_configuration;
        }

        /// <summary>
        /// Delete the specified package
        /// </summary>
        public AppletInfo Delete(string id)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Delete a specific version of the package
        /// </summary>
        public AppletInfo Delete(string id, string version)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Find a package
        /// </summary>
        public List<AppletInfo> Find()
        {
            var filter = QueryExpressionParser.BuildLinqExpression<AppletInfo>(RestOperationContext.Current.IncomingRequest.Url.Query.ParseQueryString());
            string offset = RestOperationContext.Current.IncomingRequest.QueryString["_offset"] ?? "0",
                count = RestOperationContext.Current.IncomingRequest.QueryString["_count"] ?? "10";

            return this.m_configuration.Repository.GetRepository()
                .Find(filter, Int32.Parse(offset), Int32.Parse(count), out int _)
                .OrderByDescending(applet => applet.Version)
                .ThenBy(applet => $"{applet.GetName(null)}")
                .ThenBy(applet => applet.Author)
                .ThenBy(applet => applet.Id)
                .ToList();
        }

        /// <summary>
        /// Get a specific package
        /// </summary>
        public Stream Get(string id)
        {
            MemoryStream retVal = new MemoryStream();
            var pkg = this.m_configuration.Repository.GetRepository().Get(id, null);
            if (pkg == null)
            {
                throw new KeyNotFoundException($"Pakcage {id} not found");
            }

            this.AddHeaders(pkg);
            pkg.Save(retVal);
            retVal.Seek(0, SeekOrigin.Begin);
            return retVal;
        }

        /// <summary>
        /// Add specified headers to the response stream
        /// </summary>
        private void AddHeaders(AppletPackage pkg)
        {
            RestOperationContext.Current.OutgoingResponse.AppendHeader("ETag", pkg.Meta.Version);
            RestOperationContext.Current.OutgoingResponse.AppendHeader("Location", $"/pkg/{pkg.Meta.Id}/{pkg.Meta.Version}");
            RestOperationContext.Current.OutgoingResponse.AppendHeader("Last-Modified", pkg.Meta.TimeStamp.GetValueOrDefault().ToUniversalTime().ToString("WWW, dd MMM yyyy HH:mm:ss GMT"));
        }

        /// <summary>
        /// Gets a specific version of the package
        /// </summary>
        public Stream Get(string id, string version)
        {
            MemoryStream retVal = new MemoryStream();
            var pkg = this.m_configuration.Repository.GetRepository().Get(id, new System.Version(version), true);
            if (pkg == null)
            {
                throw new KeyNotFoundException($"Package {id} verison {version} not found");
            }

            this.AddHeaders(pkg);
            pkg.Save(retVal);
            retVal.Seek(0, SeekOrigin.Begin);
            RestOperationContext.Current.OutgoingResponse.AddHeader("Content-Disposition", $"attachment; filename={id}-{version}.pak");
            return retVal;
        }

        /// <summary>
        /// Return only headers from the specifed version
        /// </summary>
        /// <param name="id"></param>
        public void Head(string id)
        {
            var pkg = this.m_configuration.Repository.GetRepository().Get(id, null);
            if (pkg == null)
            {
                throw new KeyNotFoundException($"Package {id} not found");
            }

            this.AddHeaders(pkg);
        }

        /// <summary>
        /// Put the application into the file repository
        /// </summary>
        public AppletInfo Put(Stream body)
        {
            var package = AppletPackage.Load(body);

            try
            {
                this.m_configuration.Repository.GetRepository().Get(package.Meta.Id, package.Meta.Version.ParseVersion(out _), true);
                throw new FaultException(HttpStatusCode.Conflict, $"Package {package.Meta.Id} version {package.Meta.Version} already exists");
            }
            catch (KeyNotFoundException)
            {
                return this.m_configuration.Repository.GetRepository().Put(package);
            }
            finally
            {
            }
        }


        /// <summary>
        /// Get the content type of the file
        /// </summary>
        private string GetContentType(string filename)
        {
            string extension = Path.GetExtension(filename);
            switch (extension.Substring(1).ToLower())
            {
                case "htm":
                case "html":
                    return "text/html";
                case "js":
                    return "application/javascript";
                case "css":
                    return "text/css";
                case "svg":
                    return "image/svg+xml";
                case "ttf":
                    return "application/x-font-ttf";
                case "eot":
                    return "application/vnd.ms-fontobject";
                case "woff":
                    return "application/font-woff";
                case "woff2":
                    return "application/font-woff2";
                case "gif":
                    return "image/gif";
                case "ico":
                    return "image/x-icon";
                case "png":
                    return "image/png";
                case "yaml":
                    return "application/x-yaml";
                default:
                    return "application/x-octet-stream";
            }
        }

        /// <summary>
        /// Get the specified index file
        /// </summary>
        public Stream Serve(String content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    content = "index.html";
                }

                string filename = content.Contains("?")
                    ? content.Substring(0, content.IndexOf("?", StringComparison.Ordinal))
                    : content;

                // Get the query tool stream
                var contentPath = Path.Combine(Path.GetDirectoryName(typeof(PakSrvBehavior).Assembly.Location), "www", filename);

                if (!File.Exists(contentPath))
                {
                    throw new FileNotFoundException(content);
                }
                else
                {

                    RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.OK;
                    RestOperationContext.Current.OutgoingResponse.ContentLength64 = new FileInfo(contentPath).Length;
                    RestOperationContext.Current.OutgoingResponse.ContentType = GetContentType(contentPath);
                    using (var fs = File.OpenRead(contentPath))
                    {
                        var ms = new MemoryStream();
                        fs.CopyTo(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        return ms;
                    }
                }
            }
            catch (Exception e)
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                Trace.TraceError(e.ToString());
                return null;
            }

        }

        /// <summary>
        /// Get the specified asset from the pak file
        /// </summary>
        public Stream GetAsset(string id, string assetPath)
        {
            var pkg = this.m_configuration.Repository.GetRepository().Get(id, null);
            if (pkg == null)
            {
                throw new KeyNotFoundException($"Package {id} not found");
            }

            // Open the package
            var unpack = pkg.Unpack();
            var assetObject = unpack.Assets.FirstOrDefault(o => o.Name == $"{assetPath}");

            if (assetObject == null)
            {
                throw new FileNotFoundException();
            }

            RestOperationContext.Current.OutgoingResponse.ContentType = assetObject.MimeType;
            byte[] content;
            if (assetObject.Content is byte[] bytea)
            {
                content = bytea;
            }
            else if (assetObject.Content is String stra)
            {
                content = System.Text.Encoding.UTF8.GetBytes(stra);
            }
            else if (assetObject.Content is XElement xela)
            {
                content = System.Text.Encoding.UTF8.GetBytes(xela.ToString());
            }
            else if (assetObject.Content is AppletAssetHtml html)
            {
                content = System.Text.Encoding.UTF8.GetBytes(html.Html.ToString());
            }
            else
            {
                throw new InvalidOperationException("Cannot render this type of data");
            }

            if (Encoding.UTF8.GetString(content as byte[], 0, 4) == "LZIP")
            {
                using (var ms = new MemoryStream(content as byte[]))
                using (var ls = new SharpCompress.Compressors.LZMA.LZipStream(ms, SharpCompress.Compressors.CompressionMode.Decompress))
                {
                    var oms = new MemoryStream();
                    ls.CopyTo(oms);
                    oms.Seek(0, SeekOrigin.Begin);
                    return oms;
                }
            }
            else
            {
                return new MemoryStream(content);
            }
        }
    }
}