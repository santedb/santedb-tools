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
using Newtonsoft.Json;
using RestSrvr;
using RestSrvr.Message;
using SanteDB.PakMan.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security;
using System.Text;

namespace SanteDB.PakSrv
{
    public class PakSrvErrorHandler : IServiceBehavior, IServiceErrorHandler
    {
        /// <summary>
        /// Add the service behavior
        /// </summary>
        public void ApplyServiceBehavior(RestService service, ServiceDispatcher dispatcher)
        {
            dispatcher.ErrorHandlers.Clear();
            dispatcher.ErrorHandlers.Add(this);
        }

        /// <summary>
        /// Handle the specified error
        /// </summary>
        public bool HandleError(Exception error) => true;

        /// <summary>
        /// Provide the actual fault message
        /// </summary>
        public bool ProvideFault(Exception error, RestResponseMessage response)
        {
            if (error is SecurityException)
            {
                response.StatusCode = System.Net.HttpStatusCode.Forbidden;
            }
            else if (error is FileNotFoundException || error is KeyNotFoundException)
            {
                response.StatusCode = System.Net.HttpStatusCode.NotFound;
            }
            else if (error is DuplicateNameException)
            {
                response.StatusCode = System.Net.HttpStatusCode.Conflict;
            }
            else
            {
                response.StatusCode = System.Net.HttpStatusCode.InternalServerError;
            }

            var errorResponse = new ErrorResult(error);
            response.Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(errorResponse)));
            response.ContentType = "application/json";
            return true;
        }
    }
}
