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
 * DatERROR: 2021-9-2
 */
using ClosedXML.Excel;
using MohawkCollege.Util.Console.Parameters;
using SanteDB.Core.Data.Initialization;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace SanteDB.VocabImport
{
    internal class Program
    {
        private const int COL_TERM = 1;
        private const int COL_LANG = 2;
        private const int COL_DISPLAY = 3;
        private const int COL_CONCEPT = 4;
        private const int COL_MNEMONIC = 5;
        private const int COL_CS_URI = 6;
        private const int COL_CS_OID = 7;
        private const int COL_CS_AUTH = 8;
        private const int COL_CS_NAME = 9;
        private const int COL_CS_UUID = 10;

        private static Regex camelCaser = new Regex(@"^(.*?)[^\w](\w)?(.*?)$");
        // Code system mapping
        private static Dictionary<String, CodeSystem> m_codeSystemMap = new Dictionary<string, CodeSystem>();

        /// <summary>
        /// Make <paramref name="id"/> an ID
        /// </summary>
        private static String CamelCase(string id)
        {
            var retVal = id;
            while (camelCaser.IsMatch(retVal))
            {
                retVal = camelCaser.Replace(retVal, (o) => $"{o.Groups[1].Value}{o.Groups[2]?.Value.ToUpper()}{o.Groups[3].Value}");
            }
            return retVal;
        }

        /// <summary>
        /// Process the specified excel file into a dataset file
        /// </summary>
        private static void Main(string[] args)
        {
            try
            {

                Console.WriteLine("SanteDB Vocabulary Importer v{0} ({1})", Assembly.GetEntryAssembly().GetName().Version, Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
                Console.WriteLine("Copyright (C) 2015-2022 See NOTICE for contributors");
                var parms = new ParameterParser<ConsoleParameters>().Parse(args);
                Dataset retVal = new Dataset()
                {
                    Id = parms.Name ?? "Imported Dataset",
                    Action = new List<DataInstallAction>()
                };

                // Process a FHIR
                if (parms.Fhir || parms.Csv)
                {
                    if (!File.Exists(parms.SourceFile))
                    {
                        throw new FileNotFoundException($"{parms.SourceFile} not found");
                    }
                    if (parms.Fhir)
                    {
                        using (var fhirFileStream = File.OpenRead(parms.SourceFile))
                        {
                            using (var xreader = XmlReader.Create(fhirFileStream))
                            {
                                var fsz = new Hl7.Fhir.Serialization.FhirXmlParser();
                                var cs = fsz.Parse<Hl7.Fhir.Model.CodeSystem>(xreader);

                                retVal.Id = $"Import FHIR Code System {cs.Id}";
                                var csId = Guid.NewGuid();
                                retVal.Action.Add(new DataUpdate()
                                {
                                    InsertIfNotExists = true,
                                    Element = new CodeSystem()
                                    {
                                        Key = csId,
                                        Authority = CamelCase(cs.Name),
                                        Url = cs.Url,
                                        Oid = cs.Identifier.First(i => i.System == "urn:ietf:rfc:3986").Value,
                                        VersionText = cs.Version,
                                        Name = cs.Title
                                    }
                                });

                                if (parms.CreateConcept)
                                {
                                    var setId = Guid.NewGuid();
                                    retVal.Action.Add(new DataUpdate()
                                    {
                                        InsertIfNotExists = true,
                                        Element = new ConceptSet()
                                        {
                                            Key = setId,
                                            Name = cs.Name,
                                            Mnemonic = CamelCase(cs.Name),
                                            Url = cs.Url,
                                            Oid = cs.Identifier.First(i => i.System == "urn:ietf:rfc:3986").Value
                                        }
                                    });
                                    retVal.Action.AddRange(cs.Concept.SelectMany(o => ConvertToReferenceTerm(o, csId, setId, cs.Name)));

                                }
                                else
                                {
                                    retVal.Action.AddRange(cs.Concept.SelectMany(o => ConvertToReferenceTerm(o, csId, null, null)));
                                }
                            }
                        }
                    }
                    else if (parms.Csv)
                    {
                        // Open excel file stream
                        using (var excelFileStream = File.OpenRead(parms.SourceFile))
                        {
                            using (var importWkb = new XLWorkbook(excelFileStream, new LoadOptions()
                            {
                                RecalculateAllFormulas = false
                            }))
                            {
                                retVal.Action = importWkb.Worksheets.SelectMany(o => o.Rows()).SelectMany(o => CreateReferenceTermInstruction(o, parms)).ToList();
                            }
                        }
                    }

                    if (parms.CreateConcept)
                        retVal.Action.Add(new DataUpdate()
                        {
                            InsertIfNotExists = true,
                            Element = new ConceptSet()
                            {
                                ConceptsXml = retVal.Action.Where(o => o.Element is Concept).Select(o => o.Element.Key.Value).ToList(),
                                Mnemonic = parms.Prefix,
                                Name = "A new Code System",
                                Oid = ""
                            }
                        });
                    if (parms.OutputFile == "-")
                        new XmlSerializer(typeof(Dataset)).Serialize(Console.Out, retVal);
                    else
                        using (var fs = File.Create(parms.OutputFile))
                        {
                            new XmlSerializer(typeof(Dataset)).Serialize(fs, retVal);
                        }
                }
                else
                {
                    new ParameterParser<ConsoleParameters>().WriteHelp(Console.Out);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error processing file: {0}", e);
            }

        }

        /// <summary>
        /// convert the specified HL7 FHIR concept to a SanteDB reference term
        /// </summary>
        private static IEnumerable<DataInstallAction> ConvertToReferenceTerm(Hl7.Fhir.Model.CodeSystem.ConceptDefinitionComponent conceptDefinition, Guid codeSystem, Guid? conceptSet, String prefix)
        {
            var rtId = Guid.NewGuid();
            yield return new DataUpdate()
            {
                InsertIfNotExists = true,
                Element = new ReferenceTerm()
                {
                    Key = rtId,
                    CodeSystemKey = codeSystem,
                    DisplayNames = new List<ReferenceTermName>() { new ReferenceTermName("en", conceptDefinition.Display) },
                    Mnemonic = conceptDefinition.Code
                }
            };

            if (conceptSet.HasValue)
            {
                yield return new DataUpdate()
                {
                    InsertIfNotExists = true,
                    Element = new Concept()
                    {
                        Key = Guid.NewGuid(),
                        ClassKey = ConceptClassKeys.Other,
                        ConceptNames = new List<ConceptName>() { new ConceptName("en", conceptDefinition.Definition ?? conceptDefinition.Display) },
                        Mnemonic = $"{prefix}-{CamelCase(conceptDefinition.Display)}",
                        StatusConceptKey = StatusKeys.Active,
                        ReferenceTerms = new List<ConceptReferenceTerm>()
                        {
                            new ConceptReferenceTerm() { ReferenceTermKey = rtId, RelationshipTypeKey = ConceptRelationshipTypeKeys.SameAs }
                        },
                        ConceptSetsXml = new List<Guid>() { conceptSet.Value }
                    }
                };
            }
        }

        /// <summary>
        /// Create reference term instructions
        /// </summary>
        private static IEnumerable<DataInstallAction> CreateReferenceTermInstruction(IXLRow row, ConsoleParameters parms)
        {
            // if we are on the first row and the file contains a header row
            // we want to skip the header row
            if (row.RowNumber() == 1 && parms.SourceFileHasHeaderRow)
            {
                yield break;
            }

            // if the row is empty, we are going to assume we are at the end of the rows containing data to be processed
            // therefore we want to exit
            if (row.IsEmpty())
            {
                yield break;
            }

            if (row.Cell(COL_TERM).GetString() == "Reference Term")
                yield break;

            // Create an instruction for the concept
            if (parms.CreateConcept)
            {
                yield return new DataUpdate()
                {
                    InsertIfNotExists = true,
                    IgnoreErrors = true,
                    Element = new Concept()
                    {
                        Key = Guid.Parse(row.Cell(COL_CONCEPT).GetValue<String>()),
                        Mnemonic = $"{parms.Prefix}-{CamelCase(row.Cell(COL_MNEMONIC).GetValue<String>())}",
                        ConceptNames = new List<ConceptName>()
                       {
                           new ConceptName(row.Cell(COL_LANG).GetValue<String>(), row.Cell(COL_DISPLAY).GetValue<String>())
                       }
                    }
                };
            }

            if (!m_codeSystemMap.TryGetValue(row.Cell(COL_CS_AUTH).GetValue<String>(), out CodeSystem cs))
            {
                cs = new CodeSystem()
                {
                    Authority = row.Cell(COL_CS_AUTH).GetValue<String>(),
                    Name = row.Cell(COL_CS_NAME).GetValue<String>(),
                    Oid = row.Cell(COL_CS_OID).GetValue<String>(),
                    Url = row.Cell(COL_CS_URI).GetValue<String>(),
                    Key = Guid.Parse(row.Cell(COL_CS_UUID).GetValue<String>())
                };
                m_codeSystemMap.Add(cs.Authority, cs);
                yield return new DataUpdate()
                {
                    InsertIfNotExists = true,
                    IgnoreErrors = false,
                    Element = cs
                };
            }

            var uuid = Guid.NewGuid();

            yield return new DataUpdate()
            {
                InsertIfNotExists = true,
                IgnoreErrors = true,
                Element = new ReferenceTerm()
                {
                    Mnemonic = row.Cell(COL_TERM).GetValue<String>(),
                    DisplayNames = new List<ReferenceTermName>()
                      {
                          new ReferenceTermName(row.Cell(COL_LANG).GetValue<String>(), row.Cell(COL_DISPLAY).GetValue<String>())
                      },
                    Key = uuid,
                    CodeSystem = cs
                }
            };

            // Link to term
            yield return new DataUpdate()
            {
                InsertIfNotExists = true,
                Element = new ConceptReferenceTerm()
                {
                    Key = Guid.NewGuid(),
                    SourceEntityKey = Guid.Parse(row.Cell(COL_CONCEPT).GetValue<String>()),
                    ReferenceTermKey = uuid,
                    RelationshipTypeKey = ConceptRelationshipTypeKeys.SameAs
                }
            };
        }
    }
}