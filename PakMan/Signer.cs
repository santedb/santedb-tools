﻿/*
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
using SanteDB.Core.Applets.Model;
using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace SanteDB.PakMan
{
    /// <summary>
    /// Tool for signing packages and data
    /// </summary>
    internal class Signer
    {

        // Parameters
        private PakManParameters m_parms;

        /// <summary>
        /// Create a new signer
        /// </summary>
        /// <param name="parameters"></param>
        public Signer(PakManParameters parameters)
        {
            this.m_parms = parameters;
        }

        /// <summary>
        /// Sign an existing package
        /// </summary>
        public int Sign()
        {
            try
            {
                AppletPackage pkg = null;
                using (FileStream fs = File.OpenRead(this.m_parms.Source))
                    pkg = AppletPackage.Load(fs);

                Emit.Message("INFO", "Will sign package {0}", pkg.Meta);
                pkg = this.CreateSignedPackage(pkg.Unpack());
                using (FileStream fs = File.Create(this.m_parms.Output ?? Path.ChangeExtension(this.m_parms.Source, ".signed.pak")))
                    pkg.Save(fs);
                return 0;
            }
            catch (Exception e)
            {
                Emit.Message("ERROR", "Cannot sign package: {0}", e);
                return -0232;
            }
        }


        /// <summary>
        /// Create a signed package
        /// </summary>
        public AppletPackage CreateSignedPackage(AppletManifest mfst)
        {
            try
            {
                X509Certificate2 signCert = this.m_parms.GetSigningCert();

                if (!signCert.HasPrivateKey)
                {
                    throw new InvalidOperationException($"You do not have the private key for certificiate {signCert.Subject}");
                }

                mfst.Info.TimeStamp = DateTime.Now; // timestamp
                mfst.Info.PublicKeyToken = signCert.Thumbprint;
                var retVal = mfst.CreatePackage();

                retVal.Meta.Hash = SHA256.Create().ComputeHash(retVal.Manifest);
                retVal.Meta.PublicKeyToken = signCert.Thumbprint;

                if (this.m_parms.EmbedCertificate)
                    retVal.PublicKey = signCert.Export(X509ContentType.Cert);

                if (!signCert.HasPrivateKey)
                    throw new SecurityException($"Provided key {this.m_parms.SignKeyFile} has no private key");
                RSACryptoServiceProvider rsa = signCert.PrivateKey as RSACryptoServiceProvider;
                retVal.Meta.Signature = rsa.SignData(retVal.Manifest, CryptoConfig.MapNameToOID("SHA1"));
                return retVal;
            }
            catch (Exception e)
            {
                Emit.Message("ERROR", "Error signing package: {0}", e);
                return null;
            }
        }

        /// <summary>
        /// Create a signed package
        /// </summary>
        public AppletPackage CreateSignedSolution(AppletSolution sln)
        {
            try
            {
                X509Certificate2 signCert = this.m_parms.GetSigningCert();

                if (!signCert.HasPrivateKey)
                {
                    throw new InvalidOperationException($"You do not have the private key for certificiate {signCert.Subject}");
                }

                // Combine all the manifests
                sln.Meta.Hash = SHA256.Create().ComputeHash(sln.Include.SelectMany(o => o.Manifest).ToArray());
                sln.Meta.PublicKeyToken = signCert.Thumbprint;

                if (this.m_parms.EmbedCertificate)
                    sln.PublicKey = signCert.Export(X509ContentType.Cert);

                if (!signCert.HasPrivateKey)
                    throw new SecurityException($"Provided key {this.m_parms.SignKeyFile} has no private key");
                RSACryptoServiceProvider rsa = signCert.PrivateKey as RSACryptoServiceProvider;
                sln.Meta.Signature = rsa.SignData(sln.Include.SelectMany(o => o.Manifest).ToArray(), CryptoConfig.MapNameToOID("SHA1"));
                return sln;
            }
            catch (Exception e)
            {
                Emit.Message("ERROR", "Error signing package: {0}", e);
                return null;
            }
        }

    }
}
