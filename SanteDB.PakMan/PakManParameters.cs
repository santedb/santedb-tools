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
using MohawkCollege.Util.Console.Parameters;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace SanteDB.PakMan
{
    /// <summary>
    /// Console parameters
    /// </summary>
    public class PakManParameters
    {


        /// <summary>
        /// Clean
        /// </summary>
        [Parameter("c")]
        [Description("Instructs the compiler to clean the output directory")]
        public bool Clean { get; set; }

        /// <summary>
        /// Source files
        /// </summary>
        [Parameter("s")]
        [Parameter("source")]
        [Description("Identifies the source files to include in the applet")]
        public String Source { get; set; }

        /// <summary>
        /// Gets or sets the output
        /// </summary>
        [Description("The output applet file")]
        [Parameter("o")]
        [Parameter("output")]
        public String Output { get; set; }

        /// <summary>
        /// Display information about the package
        /// </summary>
        [Description("Show information for file")]
        [Parameter("info")]
        public bool Info { get; set; }

        /// <summary>
        /// Gets or sets the indicator for showing help
        /// </summary>
        [Parameter("?")]
        [Parameter("help")]
        [Description("Shows this help and exits")]
        public bool Help { get; set; }

        /// <summary>
        /// Optimize the output files
        /// </summary>
        [Parameter("optimize")]
        [Description("When true, optimize (minify) javascript and css")]
        public bool Optimize { get; set; }

        /// <summary>
        /// The key that should be used to sign the applet
        /// </summary>
        [Parameter("pfxFile")]
        [Parameter("keyFile")]
        [Description("The RSA key used to sign the applet")]
        public String SignKeyFile { get; set; }

        /// <summary>
        /// The key used to sign the applet
        /// </summary>
        [Parameter("pfxPassword")]
        [Parameter("keyPassword")]
        [Description("The password for the applet signing key")]
        public String SignPassword { get; set; }

        /// <summary>
        /// Compile instruction
        /// </summary>
        [Parameter("compile")]
        [Description("Initiates a compilation of the specified applet source directories")]
        public bool Compile { get; internal set; }

        /// <summary>
        /// Signing instruction
        /// </summary>
        [Parameter("sign")]
        [Description("Signs an already existing applet pak file")]
        public bool Sign { get; internal set; }

        [Parameter("certHash")]
        [Description("The thumprint of the key to use for signing (in your user's personal store)")]
        public string SignKeyHash { get; set; }

        /// <summary>
        /// Embed certificate into the manifest
        /// </summary>
        [Parameter("embedCert")]
        [Description("Embeds the certificate used to sign the package in the applet (recommended for wide-publishing)")]
        public bool EmbedCertificate { get; set; }

        /// <summary>
        /// Compose
        /// </summary>
        [Parameter("compose")]
        [Description("Indicates the source files are PAK files that should be composed into a solution")]
        public bool Compose { get; set; }

        /// <summary>
        /// Install the package
        /// </summary>
        [Parameter("install")]
        [Description("Install the output package into the local repository")]
        public bool Install { get; set; }

        /// <summary>
        /// Gets or sets the version to apply to the applet (overrides the version)
        /// </summary>
        [Parameter("version")]
        [Description("The version to apply to the package")]
        public string Version { get; set; }

        /// <summary>
        /// When true, indicates an installation package should be created
        /// </summary>
        [Parameter("dcdr")]
        [Description("Build dCDR assets (android,gateway,web,windows)")]
        public StringCollection DcdrAssets { get; set; }

        /// <summary>
        /// When specified, the branch of the dcdr asset to use
        /// </summary>
        [Parameter("dcdr-branch")]
        [Description("Use the specified branch as a basis for building DCDR assets")]
        public String SourceBranch { get; set; }

        /// <summary>
        /// MSBuild Tooling
        /// </summary>
        [Parameter("msbuild")]
        [Description("The path to the MSBUILD tooling")]
        public String MsBuild { get; set; }

        /// <summary>
        /// Generate internationalization file
        /// </summary>
        [Parameter("i18n")]
        [Description("Generate an internationalization file from the specified manifests")]
        public String InternationalizationFile { get; set; }

        /// <summary>
        /// Output directory
        /// </summary>
        [Parameter("dcdr-output")]
        [Description("Output the DCDR generated assets to the specified directory")]
        public string DcdrAssetOutput { get; set; }

        /// <summary>
        /// Publish the package
        /// </summary>
        [Parameter("publish")]
        [Description("Publish the specified output")]
        public bool Publish { get; set; }

        /// <summary>
        /// The URL to publish to
        /// </summary>
        [Parameter("publish-server")]
        [Description("Publish to the specified URI")]
        public String PublishServer { get; set; }


        /// <summary>
        /// Get the signing certificate
        /// </summary>
        public X509Certificate2 GetSigningCert()
        {
            if (!String.IsNullOrEmpty(this.SignKeyFile))
            {
                if (String.IsNullOrEmpty(this.SignPassword))
                {
                    using (var frmKey = new frmKeyPassword(this.SignKeyFile))
                        if (frmKey.ShowDialog() == DialogResult.OK)
                            this.SignPassword = frmKey.Password;
                }
                else if (File.Exists(this.SignPassword))
                    this.SignPassword = File.ReadAllText(this.SignPassword);
                return new X509Certificate2(this.SignKeyFile, this.SignPassword);
            }
            else if (!String.IsNullOrEmpty(this.SignKeyHash))
            {
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.OpenExistingOnly);
                    var candidates = store.Certificates.Find(X509FindType.FindByThumbprint, this.SignKeyHash, false);
                    if (candidates.Count == 0)
                    {
                        throw new InvalidOperationException("Cannot find specified certificate");
                    }
                    if (candidates[0].NotAfter < DateTime.Now)
                    {
                        throw new InvalidOperationException("Certificate is expired");
                    }
                    return candidates[0];
                }
            }
            else
            {
                throw new InvalidOperationException("Signing a package requires either --keyHash or --keyFile");
            }

        }
    }
}
