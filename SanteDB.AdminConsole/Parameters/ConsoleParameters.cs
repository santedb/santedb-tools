﻿/*
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
using MohawkCollege.Util.Console.Parameters;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SanteDB.AdminConsole.Parameters
{
    /// <summary>
    /// Console parameters
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConsoleParameters
    {

        /// <summary>
        /// Console parameters
        /// </summary>
        public ConsoleParameters()
        {
            this.RealmId = "localhost";
            this.UseTls = false;
            this.AppId = "org.santedb.administration";
            this.AppSecret = "Mohawk123";
            this.Port = "8080";
        }

        /// <summary>
        /// Realm identifier
        /// </summary>
        [Parameter("realm")]
        [Parameter("r")]
        [Description("Sets the realm to administer (default: localhost)")]
        public String RealmId { get; set; }

        /// <summary>
        /// Application identifier
        /// </summary>
        [Parameter("appId")]
        [Parameter("a")]
        [Description("Sets the application identifier (default: org.santedb.sdbac)")]
        public String AppId { get; set; }

        /// <summary>
        /// Application secret
        /// </summary>
        [Parameter("secret")]
        [Parameter("s")]
        [Description("Sets the application secret")]
        public String AppSecret { get; set; }

        /// <summary>
        /// Sets the port for the ims
        /// </summary>
        [Parameter("port")]
        [Description("Sets the IMS port number (default: 8080 non-tls, 8443 tls)")]
        public String Port { get; set; }

        /// <summary>
        /// Use TLS setting
        /// </summary>
        [Parameter("tls")]
        [Parameter("t")]
        [Description("When true execute with TLS (default: false)")]
        public bool UseTls { get; set; }

        /// <summary>
        /// User setting
        /// </summary>
        [Parameter("user")]
        [Parameter("u")]
        [Description("Log into the IMS with the specified user (default: administrator)")]
        public string User { get; set; }

        /// <summary>
        /// Set password
        /// </summary>
        [Parameter("password")]
        [Parameter("p")]
        [Description("Set the password")]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the verbosity level
        /// </summary>
        [Description("Set the verbosity of output (Verbose, Error, Warning) (default: Error)")]
        [Parameter("v")]
        [Parameter("verbose")]
        public string Verbosity { get; set; }

        /// <summary>
        /// Gets or sets the proxy
        /// </summary>
        [Parameter("x")]
        [Parameter("proxy")]
        [Description("Sets the HTTP proxy address")]
        public string Proxy { get; set; }

        /// <summary>
        /// Show help and exit
        /// </summary>
        [Parameter("help")]
        [Parameter("?")]
        [Description("Show help and exit")]
        public bool Help { get; internal set; }

        /// <summary>
        /// Basic oauth
        /// </summary>
        [Parameter("b")]
        [Parameter("oauth_basic")]
        [Description("Instructs the console to use basic authentication against the oauth token service")]
        public bool OAuthBasic { get; internal set; }


    }
}
