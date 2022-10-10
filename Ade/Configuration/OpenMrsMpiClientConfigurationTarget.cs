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
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.DisconnectedClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace SanteDB.SDK.AppletDebugger.Configuration
{
    /// <summary>
    /// OpenMRS MPI Client configuration target
    /// </summary>
    public class OpenMrsMpiClientConfigurationTarget : IConfigurationTarget
    {

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(OpenMrsMpiClientConfigurationTarget));

        /// <summary>
        /// Gets the invariant of this
        /// </summary>
        public string Invariant => "openmrs";

        /// <summary>
        /// Push the configuration to the specified target
        /// </summary>
        public List<Uri> PushConfiguration(Uri target, string user, string password, IDictionary<string, object> configuration)
        {
            try
            {
                this.m_tracer.TraceInfo("Will attempt to configure OpenMRS at {0}...", target);

                // Now we want to establish a URI target
                List<Uri> targets = new List<Uri>();
                if (target.Host == "0.0.0.0") // All on local network
                {
                    // Scan local network
                    var netService = ApplicationServiceContext.Current.GetService<INetworkInformationService>();
                    var interfaces = netService.GetInterfaces();
                    foreach (var iface in interfaces.Where(o => o.IpAddress != "127.0.0.1"))
                    {
                        try
                        {
                            this.m_tracer.TraceVerbose("Will scan on interface {0} with range {1}/24", iface.Name, iface.IpAddress);
                            var network = IPNetwork.Parse(iface.IpAddress, 24);
                            foreach (var addr in network.ListIPAddress(FilterEnum.Usable))
                            {
                                try
                                {
                                    // Should return 302 found and redirect
                                    var scanUri = new Uri($"http{(target.Port == 8443 ? "s" : "")}://{addr}:{target.Port}/openmrs/module/santedb-mpiclient/mpiConfig.form");
                                    var client = HttpWebRequest.Create(scanUri) as HttpWebRequest;
                                    client.Method = "GET";
                                    client.AllowAutoRedirect = false;
                                    client.Timeout = 50;
                                    var response = client.GetResponse();
                                    if (response is HttpWebResponse httpResponse && httpResponse.StatusCode == HttpStatusCode.Found)
                                    {
                                        this.m_tracer.TraceVerbose("Found MPI Module on OpenMRS at {0}", addr);
                                        targets.Add(new Uri($"openmrs://{addr}:{target.Port}"));
                                    }
                                    client = null; // de-init
                                }
                                catch (Exception)
                                {
                                    this.m_tracer.TraceVerbose("Scan of {0} failed", addr);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            this.m_tracer.TraceVerbose("Scan of {0} failed", iface.Name);

                        }
                    }
                }
                else
                    targets.Add(target);

                if (targets.Count == 0)
                    throw new InvalidOperationException("Could not locate any OpenMRS instances on your network");

                var configured = new List<Uri>(targets.Count);
                // OpenMRS targets can now be configured
                foreach (var rawUri in targets)
                {
                    try
                    {
                        // First, we want to login to OpenMRS
                        var openMrsUri = new Uri($"{(rawUri.Port == 8443 ? "https" : "http")}://{rawUri.Host}:{rawUri.Port}/openmrs");
                        var request = HttpWebRequest.Create($"{openMrsUri}/loginServlet") as HttpWebRequest;
                        request.Method = "POST";
                        request.AllowAutoRedirect = false;
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.Timeout = 5000;
                        var formData = new NameValueCollection();
                        formData.Add("uname", user);
                        formData.Add("pw", password);
                        var formBytes = Encoding.UTF8.GetBytes(formData.ToString());
                        request.GetRequestStream().Write(formBytes, 0, formBytes.Length);
                        var response = request.GetResponse() as HttpWebResponse;
                        var authCookie = response.Headers[HttpResponseHeader.SetCookie];
                        if (String.IsNullOrEmpty(authCookie))
                            throw new InvalidOperationException("Could not get session from OpenMRS");
                        var authParts = authCookie.Split(';').Select(o => o.Split('=')).ToDictionary(o => o[0], o => o[1]);

                        // Is there a login cookie?
                        request = HttpWebRequest.Create($"{openMrsUri}/module/santedb-mpiclient/mpiConfig.form") as HttpWebRequest;
                        request.Headers.Add(HttpRequestHeader.Cookie, $"JSESSIONID={authParts["JSESSIONID"]}");
                        request.Method = "POST";
                        request.Timeout = 5000;
                        request.AllowAutoRedirect = false;
                        request.ContentType = "application/x-www-form-urlencoded";
                        formData = new NameValueCollection(configuration.ToArray());
                        formBytes = Encoding.UTF8.GetBytes(formData.ToString());
                        request.GetRequestStream().Write(formBytes, 0, formBytes.Length);
                        response = request.GetResponse() as HttpWebResponse;
                        if (response.StatusCode == HttpStatusCode.OK)
                            configured.Add(rawUri);

                    }
                    catch (WebException e)
                    {
                        throw new InvalidOperationException($"OpenMRS instance at {rawUri} indicated an error", e);
                    }
                    catch (Exception e)
                    {
                        this.m_tracer.TraceError("Skipping {0} due to configuration error - {1}", rawUri, e);
                    }
                }

                if (configured.Count == 0)
                    throw new InvalidOperationException("Was not able to configure any remote OpenMRS instances. Please check your settings and try again");
                return configured;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Could not configure OpenMRS {0} - {1}", target, e);
                throw new Exception($"Could not configure OpenMRS {target}", e);
            }
        }
    }
}
