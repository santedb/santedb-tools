﻿/*
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
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(ApplicationServiceContext));

        /// <summary>
        /// The configuration
        /// </summary>
        private Parameters.ConsoleParameters m_configuration;

        /// <summary>
        /// Services
        /// </summary>
        private List<Object> m_services = new List<object>();

        // Token auth
        private IDisposable m_tokenAuth = null;

        /// <summary>
        /// Rest clients
        /// </summary>
        private Dictionary<ServiceEndpointType, IRestClient> m_restClients = new Dictionary<ServiceEndpointType, IRestClient>();

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

        /// <summary>
        /// Creates a new application context
        /// </summary>
        private ApplicationContext(Parameters.ConsoleParameters configuration)
        {
            this.ApplicationName = configuration.AppId ?? "org.santedb.sdbac";
            this.ApplicationSecret = configuration.AppSecret ?? "sdbac-default-secret";
            this.m_configuration = configuration;
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
        public string RealmId
        { get { return this.m_configuration.RealmId; } }

        public bool IsRunning => true;

        /// <summary>
        /// Get the operating system
        /// </summary>
        public OperatingSystemID OperatingSystem => OperatingSystemID.Other;

        /// <summary>
        /// Represents the client host
        /// </summary>
        public SanteDBHostType HostType => SanteDBHostType.Client;

        /// <summary>
        /// Start the application context
        /// </summary>
        public void Start()
        {
            this.m_tracer.TraceInfo("Starting mini-context");

            String scheme = this.m_configuration.UseTls ? "https" : "http",
                host = $"{scheme}://{this.m_configuration.RealmId}:{this.m_configuration.Port}/";

            this.m_tracer.TraceInfo("Contacting {0}", host);
            try
            {
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
                    Accept = "application/xml"
                };

                if (!String.IsNullOrEmpty(this.m_configuration.Proxy))
                {
                    this.m_tracer.TraceVerbose("Setting proxy to : {0}", this.m_configuration.Proxy);
                    WebRequest.DefaultWebProxy = new WebProxy(this.m_configuration.Proxy);
                }

                this.m_tracer.TraceVerbose("Setting up endpoint : {0}/ami", host);

                optionDescription.Endpoint.Add(new RestClientEndpointConfiguration($"{host}/ami"));
                var amiServiceClient = new AmiServiceClient(new RestClient(optionDescription));

                // get options
                var amiOptions = amiServiceClient.Options();

                // Server version
                if (new Version(amiOptions.InterfaceVersion.Substring(0, amiOptions.InterfaceVersion.LastIndexOf(".")) + ".0") > typeof(AmiServiceClient).Assembly.GetName().Version)
                {
                    throw new InvalidOperationException($"Server version of AMI is too new for this version of console. Expected {typeof(AmiServiceClient).Assembly.GetName().Version} got {amiOptions.InterfaceVersion}");
                }

                foreach (var itm in amiOptions.Endpoints)
                {
                    this.m_tracer.TraceInfo("Server supports {0} at {1}", itm.ServiceType, String.Join(",", itm.BaseUrl).Replace("0.0.0.0", this.m_configuration.RealmId));

                    var config = new RestClientDescriptionConfiguration()
                    {
                        Accept = "application/xml",
                        Binding = new RestClientBindingConfiguration()
                        {

                        }
                    };

                    if (itm.Capabilities.HasFlag(ServiceEndpointCapabilities.Compression))
                    {
                        config.Binding.Optimize = true;
                    }

                    if (itm.Capabilities.HasFlag(ServiceEndpointCapabilities.BearerAuth))
                    {
                        config.Binding.Security = new RestClientSecurityConfiguration()
                        {
                            CredentialProvider = new TokenCredentialProvider(),
                            Mode = SecurityScheme.Bearer,
                            PreemptiveAuthentication = true,
                            CertificateValidatorXml = new SanteDB.Core.Configuration.TypeReferenceConfiguration(typeof(ConsoleCertificateValidator))

                        };
                    }
                    else if (itm.Capabilities.HasFlag(ServiceEndpointCapabilities.BasicAuth))
                    {
                        if (itm.ServiceType == ServiceEndpointType.AuthenticationService)
                        {
                            config.Binding.Security = new RestClientSecurityConfiguration()
                            {
                                CredentialProvider = new OAuth2CredentialProvider(),
                                Mode = this.m_configuration.OAuthBasic ? SecurityScheme.Basic : SecurityScheme.None,
                                PreemptiveAuthentication = true,
                                CertificateValidatorXml = new SanteDB.Core.Configuration.TypeReferenceConfiguration(typeof(ConsoleCertificateValidator))
                            };
                        }
                        else
                        {
                            config.Binding.Security = new RestClientSecurityConfiguration()
                            {
                                CredentialProvider = new HttpBasicTokenCredentialProvider(),
                                Mode = SecurityScheme.Basic,
                                PreemptiveAuthentication = true,
                                CertificateValidatorXml = new SanteDB.Core.Configuration.TypeReferenceConfiguration(typeof(ConsoleCertificateValidator))
                            };
                        }
                    }

                    config.Endpoint.AddRange(itm.BaseUrl.Select(o => new RestClientEndpointConfiguration(o.Replace("0.0.0.0", this.m_configuration.RealmId)))); //  new   new AdminClientEndpointDescription(o.Replace("0.0.0.0", this.m_configuration.RealmId))));

                    // Add client
                    if (!this.m_restClients.ContainsKey(itm.ServiceType))
                    {
                        this.m_restClients.Add(itm.ServiceType, new RestClient(
                            config
                        ));
                    }
                }

                // Attempt to get server time from clinical interface which should challenge
                var data = this.GetRestClient(ServiceEndpointType.HealthDataService)?.Get("/time");

            }
            catch (Exception ex)
            {
#if DEBUG
                this.m_tracer.TraceError("Cannot start services: {0}", ex);
#else
                this.m_tracer.TraceError("Cannot start services: {0}", ex);
#endif
                throw;
            }
        }

        /// <summary>
        /// Authenticate using the authentication provider
        /// </summary>
        internal bool Authenticate(IIdentityProviderService authenticationProvider, IRestClient context)
        {
            bool retVal = false;
            while (!retVal)
            {
                Console.WriteLine("Access denied, authentication required.");
                if (String.IsNullOrEmpty(this.m_configuration.User))
                {
                    Console.Write("Username:");
                    this.m_configuration.User = Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("Username:{0}", this.m_configuration.User);
                }

                if (String.IsNullOrEmpty(this.m_configuration.Password))
                {
                    this.m_configuration.Password = DisplayUtil.PasswordPrompt("Password:");
                    if (String.IsNullOrEmpty(this.m_configuration.Password))
                    {
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("Password:{0}", new String('*', this.m_configuration.Password.Length * 2));
                }

                // Now authenticate
                try
                {
                    var principal = (authenticationProvider as AdminConsole.Security.OAuthIdentityProvider)?.Authenticate(
                        new SanteDBClaimsPrincipal(new SanteDBClaimsIdentity(this.m_configuration.User, false, "OAUTH2")), this.m_configuration.Password) ??
                        authenticationProvider.Authenticate(this.m_configuration.User, this.m_configuration.Password);
                    if (principal != null)
                    {
                        retVal = true;
                        this.m_tokenAuth = AuthenticationContext.EnterContext(principal);
                    }
                    else
                    {
                        this.m_configuration.Password = null;
                    }
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Authentication error: {0}", e.Message);
                    this.m_configuration.Password = null;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Get the named REST client
        /// </summary>
        public IRestClient GetRestClient(ServiceEndpointType type)
        {
            IRestClient retVal = null;
            this.m_restClients.TryGetValue(type, out retVal);
            return retVal;
        }

        /// <summary>
        /// Stop the service context
        /// </summary>
        public void Stop()
        {
        }
    }
}