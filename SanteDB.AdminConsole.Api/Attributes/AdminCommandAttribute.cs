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
using System;
using System.Diagnostics.CodeAnalysis;

namespace SanteDB.AdminConsole.Attributes
{
    /// <summary>
    /// Identifies the method can be invoked from the admin console
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AdminCommandAttribute : System.Attribute
    {



        /// <summary>
        /// Administrative command attribute
        /// </summary>
        public AdminCommandAttribute(String command, String description)
        {
            this.Description = description;
            this.Command = command;
        }

        /// <summary>
        /// Commands
        /// </summary>
        public String Command { get; set; }

        /// <summary>
        /// Description for commands
        /// </summary>
        public String Description { get; set; }


    }
}
