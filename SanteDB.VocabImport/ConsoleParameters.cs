/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using MohawkCollege.Util.Console.Parameters;
using System.ComponentModel;

namespace SanteDB.VocabImport
{
    /// <summary>
    /// The parameters to be passed to the program via the command line
    /// </summary>
    public class ConsoleParameters
    {
        /// <summary>
        /// Process CSV
        /// </summary>
        [Parameter("csv")]
        [Description("When specified, the source is a CSV using the vocabulary template")]
        public bool Csv { get; set; }

        /// <summary>
        /// Process FHIR
        /// </summary>
        [Parameter("fhir")]
        [Description("When specified, the source is a FHIR resource bundle in XML")]
        public bool Fhir { get; set; }

        /// <summary>
        /// Gets or sets the source file to be processed
        /// </summary>
        [Parameter("source")]
        [Parameter("s")]
        [Description("The source excel file to process")]
        public string SourceFile { get; set; }

        /// <summary>
        /// Gets or sets the indicator as to whether or not the the XLSX file has a header row.
        /// Defaults to False.
        /// </summary>
        [Parameter("header")]
        [Description("The flag to indicate that the source excel file has a header row.")]
        public bool SourceFileHasHeaderRow { get; set; }

        /// <summary>
        /// Create the specified concept.
        /// </summary>
        [Parameter("create-concepts")]
        [Description("Create the necessary instructions to create the concept")]
        public bool CreateConcept { get; set; }

        /// <summary>
        /// Gets or sets the name of the dataset being created
        /// </summary>
        [Parameter("name")]
        [Description("The name of the emitted dataset")]
        public string Name { get; set; }

        /// <summary>
        /// Prefix of the mnemonic
        /// </summary>
        [Parameter("prefix")]
        [Description("The prefix to add to each mnemonic")]
        public string Prefix { get; set; }

        /// <summary>
        /// Gets or sets the output file
        /// </summary>
        [Parameter("output")]
        [Parameter("o")]
        [Description("The output dataset file to emit")]
        public string OutputFile { get; set; }

    }
}
