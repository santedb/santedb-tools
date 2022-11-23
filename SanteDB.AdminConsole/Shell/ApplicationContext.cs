/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using SanteDB.Core;
using SanteDB.Core.Configuration.Http;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Http;
using SanteDB.Core.Http.Description;
using SanteDB.Core.Interop;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Messaging.AMI.Client;
using SanteDB.AdminConsole.Security;
using SanteDB.AdminConsole.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;

namespace SanteDB.AdminConsole.Shell
{
    /// <summary>
    /// Represents a basic application context based on configuration
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ApplicationContext : IServiceProvider, IApplicationServiceContext
    {
        // Tracer
        internal readonly Tracer Tracer = Tracer.GetTracer(typeof(ApplicationServiceContext));

        /// <summary>
        /// The configuration
        /// </summary>
        internal Parameters.ConsoleParameters Configuration { get; private set; }

        /// <summary>
        /// Services
        /// </summary>
        private List<Object> m_services = new List<object>();

        // Token auth
        internal IDisposable TokenAuthContext { get; set; }

        public Guid ActivityUuid => Guid.NewGuid();

        /// <summary>
        /// Rest clients
        /// </summary>
        private Dictionary<string, IRestClient> m_restClients = new Dictionary<string, IRestClient>();

        public event EventHandler Starting;

        public event EventHandler Started;

        public event EventHandler Stopping;

        public event EventHandler Stopped;

        /// <summary>
        /// Gets the time that the application context was started
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Initialize the application context
        /// </summary>
        public static void Initialize(Parameters.ConsoleParameters configuration)
        {
            ApplicationContext.Current = new ApplicationContext(configuration);
            ServiceUtil.Start(Guid.Empty, ApplicationContext.Current);

        }

        /// <summary>
        /// Get the services provided
        /// </summary>
        public object GetService(Type serviceType)
        {
            return this.m_services.FirstOrDefault(o => serviceType.IsAssignableFrom(o.GetType()));
        }

        public T GetService<T>() => (T)GetService(typeof(T));

        /// <summary>
        /// Creates a new application context
        /// </summary>
        private ApplicationContext(Parameters.ConsoleParameters configuration)
        {
            if (string.IsNullOrEmpty(configuration?.AppId))
            {
                throw new ArgumentNullException(nameof(configuration.AppId), "AppId is missing.");
            }

            if (string.IsNullOrEmpty(configuration?.AppSecret))
            {
                throw new ArgumentNullException(nameof(configuration.AppSecret), "App Secret is missing.");
            }

            this.ApplicationName = configuration.AppId;
            this.ApplicationSecret = configuration.AppSecret;
            this.Configuration = configuration;
            //this.m_services.Add(new FileConfigurationService(String.Empty));
        }

        /// <summary>
        /// Gets the current application context
        /// </summary>
        public static ApplicationContext Current
        {
            get; private set;
        }

        /// <summary>
        /// Gets the application name
        /// </summary>
        public string ApplicationName { get; private set; }

        /// <summary>
        /// Gets the application secret
        /// </summary>
        public string ApplicationSecret { get; private set; }

        /// <summary>
        /// Get realm identifier
        /// </summary>
        public string RealmId => Configuration.RealmId;

        public bool IsRunning { get; private set; }


        private void AddServices()
        {
            m_services.Add(this);
            m_services.Add(new AdminConsoleRestClientFactory());
            m_services.Add(new ApplicationCredentialProvider());
            m_services.Add(new OAuthBearerCredentialProvider());
            m_services.Add(new HttpBasicCredentialProvider());
            m_services.Add(new Client.OAuth.OAuthClientCore(GetService<IRestClientFactory>()) { ClientId = Configuration.AppId });
        }

        /// <summary>
        /// Represents the client host
        /// </summary>
        public SanteDBHostType HostType => SanteDBHostType.Client;

        /// <summary>
        /// Start the application context
        /// </summary>
        public void Start()
        {
            try
            {
                Tracer.TraceInfo("Initializing Services");
                AddServices();

                this.Tracer.TraceInfo("Starting mini-context");

                String scheme = this.Configuration.UseTls ? "https" : "http",
                    host = $"{scheme}://{this.Configuration.RealmId}:{this.Configuration.Port}/";

                this.Tracer.TraceInfo("Contacting {0}", host);

                // Options on AMI
                var optionDescription = new RestClientDescriptionConfiguration()
                {
                    Binding = new RestClientBindingConfiguration()
                    {
                        Security = new RestClientSecurityConfiguration()
                        {
                            CertificateValidatorXml = new SanteDB.Core.Configuration.TypeReferenceConfiguration(typeof(ConsoleCertificateValidator)),
                        }
                    },
                    Accept = "application/xml",
                    ProxyAddress = Configuration.Proxy
                };

                if (!String.IsNullOrEmpty(this.Configuration.Proxy))
                {
                    this.Tracer.TraceVerbose("Setting proxy to : {0}", this.Configuration.Proxy);
                    WebRequest.DefaultWebProxy = new WebProxy(this.Configuration.Proxy);
                }

                this.Tracer.TraceVerbose("Setting up endpoint : {0}ami", host);

                optionDescription.Endpoint.Add(new RestClientEndpointConfiguration($"{host}ami"));
                var boostrapamiclient = new AmiServiceClient(new RestClient(optionDescription));

                // get options
                var amiOptions = boostrapamiclient.Options();

                // Server version
                if (new Version(amiOptions.InterfaceVersion.Substring(0, amiOptions.InterfaceVersion.LastIndexOf(".")) + ".0") > typeof(AmiServiceClient).Assembly.GetName().Version)
                {
                    throw new InvalidOperationException($"Server version of AMI is too new for this version of console. Expected {typeof(AmiServiceClient).Assembly.GetName().Version} got {amiOptions.InterfaceVersion}");
                }

                foreach (var endpoint in amiOptions.Endpoints)
                {
                    this.Tracer.TraceInfo("Server supports {0} at {1}", endpoint.ServiceType, String.Join(",", endpoint.BaseUrl).Replace("0.0.0.0", this.Configuration.RealmId));
                    RestClientDescriptionConfiguration config = BuildRestClientEndpointDescription(endpoint);

                    // Add client
                    if (!this.m_restClients.ContainsKey(endpoint.ServiceType.ToString()))
                    {
                        this.m_restClients.Add(endpoint.ServiceType.ToString(), new RestClient(
                            config
                        ));
                    }
                }

                // Attempt to get server time from clinical interface which should challenge
                var data = this.GetRestClient(ServiceEndpointType.HealthDataService)?.Get("/time");

                IsRunning = true;

            }
            catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
            {
                this.Tracer.TraceError("Cannot start services: {0}", ex);
                throw;
            }
        }

        private RestClientDescriptionConfiguration BuildRestClientEndpointDescription(ServiceEndpointOptions endpoint)
        {
            var config = new RestClientDescriptionConfiguration()
            {
                Accept = "application/xml",
                Binding = new RestClientBindingConfiguration()
                {

                }
            };

            if (endpoint.Capabilities.HasFlag(ServiceEndpointCapabilities.Compression))
            {
                config.Binding.CompressRequests = true;
            }

            if (endpoint.Capabilities.HasFlag(ServiceEndpointCapabilities.BearerAuth))
            {
                config.Binding.Security = new RestClientSecurityConfiguration()
                {
                    CredentialProvider = GetService<OAuthBearerCredentialProvider>(),
                    Mode = SecurityScheme.Bearer,
                    PreemptiveAuthentication = true,
                    CertificateValidatorXml = new SanteDB.Core.Configuration.TypeReferenceConfiguration(typeof(ConsoleCertificateValidator))

                };
            }
            else if (endpoint.Capabilities.HasFlag(ServiceEndpointCapabilities.BasicAuth))
            {
                if (endpoint.ServiceType == ServiceEndpointType.AuthenticationService)
                {
                    config.Binding.Security = new RestClientSecurityConfiguration()
                    {
                        CredentialProvider = GetService<ApplicationCredentialProvider>(),
                        Mode = this.Configuration.OAuthBasic ? SecurityScheme.Basic : SecurityScheme.None,
                        PreemptiveAuthentication = true,
                        CertificateValidatorXml = new SanteDB.Core.Configuration.TypeReferenceConfiguration(typeof(ConsoleCertificateValidator))
                    };
                }
                else
                {
                    config.Binding.Security = new RestClientSecurityConfiguration()
                    {
                        CredentialProvider = GetService<HttpBasicCredentialProvider>(),
                        Mode = SecurityScheme.Basic,
                        PreemptiveAuthentication = true,
                        CertificateValidatorXml = new SanteDB.Core.Configuration.TypeReferenceConfiguration(typeof(ConsoleCertificateValidator))
                    };
                }
            }

            config.Endpoint.AddRange(endpoint.BaseUrl.Select(o => new RestClientEndpointConfiguration(o.Replace("0.0.0.0", this.Configuration.RealmId)))); //  new   new AdminClientEndpointDescription(o.Replace("0.0.0.0", this.m_configuration.RealmId))));
            return config;
        }


        /// <summary>
        /// Get the named REST client
        /// </summary>
        public IRestClient GetRestClient(string clientName)
        {
            IRestClient retVal = null;
            if (!TryGetRestClient(clientName, out retVal))
            {
                throw new KeyNotFoundException(clientName);
            }
            return retVal;
        }

        public IRestClient GetRestClient(ServiceEndpointType type) => GetRestClient(type.ToString());

        public bool TryGetRestClient(string clientName, out IRestClient restClient)
        {
            return m_restClients.TryGetValue(clientName, out restClient);
        }

        /// <summary>
        /// Stop the service context
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
        }
    }
}