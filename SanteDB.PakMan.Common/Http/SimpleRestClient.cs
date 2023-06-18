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
using Newtonsoft.Json;
using SanteDB.PakMan.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace SanteDB.PakMan.Http
{
    /// <summary>
    /// Represents an ADGMT client
    /// </summary>
    public class SimpleRestClient
    {
        // Base URI
        private Uri m_baseUri;

        // Proxy address
        private Uri m_proxyUri;

        // User
        private ICredentials m_credential;

        // Serializer
        private JsonSerializer m_serializer;

        /// <summary>
        /// Represents a simple rest client ctor
        /// </summary>
        /// <param name="baseUri">The base URI of the service to execute</param>
        public SimpleRestClient(Uri baseUri)
        {
            this.m_serializer = new JsonSerializer()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.Auto
            };
            this.m_baseUri = baseUri;

            if (!this.m_baseUri.LocalPath.EndsWith("/"))
                this.m_baseUri = new Uri(baseUri.ToString() + "/");
        }

        /// <summary>
        /// Represents a rest client ctor with proxy
        /// </summary>
        public SimpleRestClient(Uri baseUri, Uri proxyUri) : this(baseUri)
        {
            this.m_proxyUri = proxyUri;
        }

        /// <summary>
        /// Sets the credentials to be used for this service call
        /// </summary>
        public void SetCredentials(string username, string password)
        {
            this.m_credential = new NetworkCredential(username, password);
        }

        /// <summary>
        /// Gets the specified resource identifier
        /// </summary>
        /// <typeparam name="TReturn">The type of resource to get</typeparam>
        /// <param name="id">The id of the resource</param>
        /// <returns>The retrieved resource or null if not found</returns>
        public TReturn Get<TReturn>(String path)
            where TReturn : class, new()
        {
            return this.Invoke<Object, TReturn>("GET", path, null);
        }

        /// <summary>
        /// Get the return values
        /// </summary>
        public TReturn Get<TReturn>(String path, params KeyValuePair<String, String>[] query)
            where TReturn : class, new()
        {
            return this.Invoke<Object, TReturn>("GET", path, null, query);
        }

        /// <summary>
        /// Creates a new specified resource
        /// </summary>
        /// <typeparam name="TResource">The type of resource</typeparam>
        /// <param name="resource">The resource to create</param>
        /// <returns>The created resource</returns>
        public TReturn Post<TResource, TReturn>(String path, TResource body)
            where TResource : class, new()
            where TReturn : class, new()
        {
            return this.Invoke<TResource, TReturn>("POST", path, body);
        }

        /// <summary>
        /// Updates the specified resource
        /// </summary>
        /// <typeparam name="TResource">The type of resource</typeparam>
        /// <param name="resource">The resource to create</param>
        /// <returns>The created resource</returns>
        public TReturn Put<TResource, TReturn>(String path, TResource body)
            where TResource : class, new()
            where TReturn : class, new()
        {
            return this.Invoke<TResource, TReturn>("POST", path, body);
        }

        /// <summary>
        /// Deletes the specified resource
        /// </summary>
        /// <typeparam name="TResource">The type of resource</typeparam>
        /// <param name="id">The ID of the resource to delete</param>
        /// <returns>The deleted resource</returns>
        public TReturn Delete<TReturn>(string path)
            where TReturn : class, new()
        {
            return this.Invoke<Object, TReturn>("DELETE", path, null);
        }

        /// <summary>
        /// Invoke the specified operation
        /// </summary>
        /// <typeparam name="TBody">The type of body to send</typeparam>
        /// <typeparam name="TReturn">The type of return expected</typeparam>
        /// <param name="verb">The verb to invoke</param>
        /// <param name="resourcePath">The path to the resource</param>
        /// <param name="body">The body to be invoked</param>
        /// <param name="parms">The parameters</param>
        /// <returns>The response</returns>
        public TReturn Invoke<TBody, TReturn>(String verb, String resourcePath, TBody body, params KeyValuePair<String, String>[] parms)
            where TBody : class, new()
            where TReturn : class, new()
        {
            NameValueCollection parmCollection = new NameValueCollection();
            if (parms != null)
                foreach (var kv in parms)
                    parmCollection[kv.Key] = kv.Value;
            return this.Invoke<TBody, TReturn>(verb, new Uri(this.m_baseUri, resourcePath), body, parmCollection);
        }

        /// <summary>
        /// Create the query string from a list of query parameters
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private String CreateQueryString(NameValueCollection query)
        {
            String queryString = String.Empty;
            foreach (var kv in query.AllKeys)
                queryString += String.Format("{0}={1}&", kv, Uri.EscapeDataString(query[kv]));
            if (queryString.Length > 0)
                return queryString.Substring(0, queryString.Length - 1);
            else
                return queryString;
        }

        /// <summary>
        /// Invoke the specified operation
        /// </summary>
        /// <typeparam name="TBody">The type of return body</typeparam>
        /// <typeparam name="TReturn">The return type</typeparam>
        /// <param name="verb">The HTTP verb to invoke</param>
        /// <param name="requestUri">The URL to send the request to</param>
        /// <param name="body">The body of the request</param>
        /// <param name="parms">The parmaeters of the request</param>
        /// <returns>The response of the request</returns>
        /// <exception cref="T:Adgmt.Client.Exceptions.RestClientException">When the REST call failed</exception>
        private TReturn Invoke<TBody, TReturn>(String verb, Uri requestUri, TBody body, NameValueCollection parms)
            where TBody : class, new()
            where TReturn : class, new()
        {
            try
            {
                if (String.IsNullOrEmpty(requestUri.Query))
                    requestUri = new Uri($"{requestUri}?{this.CreateQueryString(parms)}");
                var client = (HttpWebRequest)WebRequest.Create(requestUri);

                if (this.m_proxyUri != null)
                    client.Proxy = new WebProxy(this.m_proxyUri, false) { UseDefaultCredentials = true };

                client.Method = verb;

                if (this.m_credential == null)
                {
                    client.UseDefaultCredentials = true;
                    client.Credentials = CredentialCache.DefaultNetworkCredentials;
                }
                else
                {
                    client.Credentials = this.m_credential;
                }

                if (body != null)
                {
                    if (body is Stream stream)
                    {
                        client.ContentType = "application/octet-stream";
                        stream.CopyTo(client.GetRequestStream());
                    }
                    else
                    {
                        client.ContentType = "application/json";
                        using (var tw = new StreamWriter(client.GetRequestStream()))
                        using (var jw = new JsonTextWriter(tw))
                            this.m_serializer.Serialize(jw, body);
                    }
                }

                client.Accept = "application/json";
                // Response
                using (var response = client.GetResponse())
                {
                    if (response.ContentLength > 0)
                    {
                        if (typeof(Stream).IsAssignableFrom(typeof(TReturn)))
                        {
                            var ms = new MemoryStream();
                            response.GetResponseStream().CopyTo(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                            return (TReturn)(object)ms;
                        }
                        else
                        {
                            using (var tr = new StreamReader(response.GetResponseStream()))
                            using (var jr = new JsonTextReader(tr))
                                return this.m_serializer.Deserialize<TReturn>(jr);
                        }
                    }
                    else
                        return default(TReturn);
                }
            }
            catch (WebException e)
            {
                var httpResponse = e.Response as HttpWebResponse;

                // Response

                if (httpResponse?.ContentLength > 0)
                {
                    ErrorResult result = null;
                    try
                    {
                        using (var tr = new StreamReader(httpResponse.GetResponseStream()))
                        using (var jr = new JsonTextReader(tr))
                            result = this.m_serializer.Deserialize<ErrorResult>(jr);
                    }
                    catch { }
                    throw new RestClientException(verb, requestUri, parms, httpResponse.StatusCode, result, e);
                }

                throw new RestClientException(verb, requestUri, parms, httpResponse?.StatusCode ?? (HttpStatusCode)400, e);
            }
            catch (System.Exception e)
            {
                throw new RestClientException(verb, requestUri, parms, e);
            }
        }
    }
}