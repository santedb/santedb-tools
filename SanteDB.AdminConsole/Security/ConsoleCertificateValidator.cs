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
 * Date: 2023-3-10
 */
using SanteDB.Core.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

namespace SanteDB.AdminConsole.Security
{

    /// <summary>
    /// Certificate validator
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class ConsoleCertificateValidator : ICertificateValidator
    {

        private static HashSet<object> m_trustedCerts = new HashSet<object>();

        /// <summary>
        /// Validate certificate
        /// </summary>
        public bool ValidateCertificate(X509Certificate2 certificate, X509Chain chain)
        {
            if (m_trustedCerts.Contains(certificate.ToString()))
            {
                return true;
            }

            String response = String.Empty;
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                while (response != "y" && response != "n" && response != "s")
                {
                    Console.WriteLine("Certificate {0} presented by server is invalid.", certificate.ToString());
                    Console.Write("Trust this certificate? ([y]es/[n]o/[s]ession):");
                    response = Console.ReadLine();
                }
            }
            finally
            {
                Console.ResetColor();
            }

            if (response == "s")
            {
                m_trustedCerts.Add(certificate.ToString());
                return true;
            }
            else
            {
                return response == "y";
            }
        }
    }
}
