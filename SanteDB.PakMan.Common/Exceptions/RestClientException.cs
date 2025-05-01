/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using System;
using System.Collections.Specialized;
using System.Net;

namespace SanteDB.PakMan.Exceptions
{
    /// <summary>
    /// An exception which indicates that a REST operation has failed
    /// </summary>
    [Serializable]
    public class RestClientException : System.Exception
    {

        /// <summary>
        /// Gets the VERB that caused the exception
        /// </summary>
        public String Verb { get; }
        /// <summary>
        /// Gets the Request URI
        /// </summary>
        public Uri RequestUri { get; }
        /// <summary>
        /// Gets the parameters (if any)
        /// </summary>
        public NameValueCollection Parms { get; }
        /// <summary>
        /// Gets the HTTP status
        /// </summary>
        public HttpStatusCode Status { get; }

        /// <summary>
        /// Gets the Error Result
        /// </summary>
        public ErrorResult Result { get; }

        /// <summary>
        /// Default CTOR
        /// </summary>
        public RestClientException()
        {
        }

        /// <summary>
        /// Rest client exception with message
        /// </summary>
        public RestClientException(string message) : base(message)
        {
        }

        /// <summary>
        /// Rest client exception with message and cause
        /// </summary>
        public RestClientException(string message, System.Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// REST client exception 
        /// </summary>
        /// <param name="verb">The HTTP verb that caused the exception</param>
        /// <param name="requestUri">The URI which caused the exception</param>
        /// <param name="parms">The parameters </param>
        /// <param name="innerException">The cause of this exception</param>
        public RestClientException(string verb, Uri requestUri, NameValueCollection parms, System.Exception innerException) : this($"REST Exception: {verb} {requestUri}", innerException)
        {
            this.Verb = verb;
            this.RequestUri = requestUri;
            this.Parms = parms;
        }

        /// <summary>
        /// REST client exception 
        /// </summary>
        /// <param name="verb">The HTTP verb that caused the exception</param>
        /// <param name="requestUri">The URI which caused the exception</param>
        /// <param name="parms">The parameters </param>
        /// <param name="innerException">The cause of this exception</param>
        public RestClientException(string verb, Uri requestUri, NameValueCollection parms, HttpStatusCode status, System.Exception innerException) : this(verb, requestUri, parms, innerException)
        {
            this.Status = status;
        }

        /// <summary>
        /// REST client exception 
        /// </summary>
        /// <param name="verb">The HTTP verb that caused the exception</param>
        /// <param name="requestUri">The URI which caused the exception</param>
        /// <param name="parms">The parameters </param>
        /// <param name="innerException">The cause of this exception</param>
        public RestClientException(string verb, Uri requestUri, NameValueCollection parms, HttpStatusCode status, ErrorResult result, System.Exception innerException) : this(verb, requestUri, parms, status, innerException)
        {
            this.Result = result;
        }

    }
}
