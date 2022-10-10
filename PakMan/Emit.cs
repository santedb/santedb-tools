/*
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
using System;

namespace SanteDB.PakMan
{
    public static class Emit
    {

        /// <summary>
        /// Emit message
        /// </summary>
        public static void Message(String category, String message, params object[] args)
        {
            switch (category)
            {
                case "INFO":
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case "WARN":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case "ERROR":
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
            }

            if (args.Length > 0)
                Console.WriteLine("{0}: {1}", category, String.Format(message ?? "", args));
            else
                Console.WriteLine("{0}: {1}", category, message);
            Console.ResetColor();

        }
    }
}
