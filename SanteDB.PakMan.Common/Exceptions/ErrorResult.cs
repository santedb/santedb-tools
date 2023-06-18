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
using System;

namespace SanteDB.PakMan.Exceptions
{
    /// <summary>
    /// Represents an error result from the ADGMT server
    /// </summary>
    [JsonObject]
    public class ErrorResult
    {


        /// <summary>
        /// Default ctor
        /// </summary>
        public ErrorResult()
        {

        }

        /// <summary>
        /// Create error result from exception
        /// </summary>
        public ErrorResult(System.Exception exception, bool includeStack = true)
        {
            this.Message = exception.Message;
            if (includeStack)
                this.Stack = exception.StackTrace;
            if (exception.InnerException != null)
                this.CausedBy = new ErrorResult(exception.InnerException, includeStack);
        }

        /// <summary>
        /// Message for the event
        /// </summary>
        [JsonProperty("message")]
        public String Message { get; set; }

        /// <summary>
        /// Cause of the event
        /// </summary>
        [JsonProperty("cause")]
        public ErrorResult CausedBy { get; set; }

        /// <summary>
        /// Stack trace
        /// </summary>
#if DEBUG
        [JsonProperty("stack")]
#else
        [JsonIgnore]
#endif
        public String Stack { get; set; }
    }
}
