using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Attributes;
using SanteDB.Core.Model.Interfaces;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.SDK.JsProxy
{
    internal class DeepCloneFactory
    {

        private static readonly CodePrimitiveExpression s_true = new CodePrimitiveExpression(true);
        private static readonly CodePrimitiveExpression s_false = new CodePrimitiveExpression(false);
        private static readonly CodePrimitiveExpression s_null = new CodePrimitiveExpression(null);
        private static readonly CodeThisReferenceExpression s_this = new CodeThisReferenceExpression();

        /// <summary>
        /// Create a code namespace for the specified assembly
        /// </summary>
        public CodeNamespace CreateCodeNamespace(String name, Assembly asm)
        {
            CodeNamespace retVal = new CodeNamespace(name);
            retVal.Imports.Add(new CodeNamespaceImport("SanteDB.Core.Model"));
            // Generate the type definition
            var ct = new CodeTypeDeclaration("DeepCloner");
            retVal.Types.Add(ct);

            var cloneTypes = asm.GetTypes().Where(o => o.GetCustomAttribute<JsonObjectAttribute>() != null && !o.IsAbstract && !o.IsGenericTypeDefinition && typeof(IdentifiedData).IsAssignableFrom(o));
            foreach (var t in cloneTypes)
            {
                var ctdecl = this.CreateDeepCloneMethod(t);
                if (ctdecl != null)
                {
                    ct.Members.Add(ctdecl);
                }
            }

            // Add a generic method which calls others
            ct.Members.Add(this.CreateGenericDeepCloneMethod(cloneTypes));
            return retVal;
        }

        /// <summary>
        /// Create a generic deep clone method
        /// </summary>
        private CodeTypeMember CreateGenericDeepCloneMethod(IEnumerable<Type> cloneTypes)
        {
            var retVal = new CodeMemberMethod()
            {
                Attributes = MemberAttributes.Static | MemberAttributes.Public,
                ReturnType = new CodeTypeReference(typeof(IdentifiedData)),
                Name = "CloneDeep"
            };

            retVal.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(IdentifiedData)), "clonee"));
            var _clonee = new CodeVariableReferenceExpression("clonee");

            // If/else statements
            CodeConditionStatement currentIfElse = null;
            int castType = 0;
            retVal.Statements.Add(new CodeSnippetStatement("switch(clonee) {"));
            foreach(var itm in cloneTypes)
            {
                retVal.Statements.Add(new CodeSnippetStatement($"case {itm.FullName} cl{++castType}:"));
                retVal.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeTypeReferenceExpression("DeepCloner"), "CloneDeep"), new CodeSnippetExpression($"cl{castType}"))));
            }
            retVal.Statements.Add(new CodeSnippetStatement("}"));
            new CodeMethodReturnStatement(_clonee);

            return retVal;
        }

        /// <summary>
        /// Create a deep clone method
        /// </summary>
        public CodeMemberMethod CreateDeepCloneMethod(Type forType)
        {

            var retVal = new CodeMemberMethod()
            {
                Attributes = MemberAttributes.Static | MemberAttributes.Public,
                ReturnType = new CodeTypeReference(forType),
                Name = "CloneDeep"
            };

            retVal.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(forType), "clonee"));
            var _clonee = new CodeVariableReferenceExpression("clonee");
            var _retVal = new CodeVariableReferenceExpression("_retVal");

            retVal.Statements.Add(new CodeConditionStatement(new CodeBinaryOperatorExpression(_clonee, CodeBinaryOperatorType.IdentityEquality, s_null), new CodeMethodReturnStatement(s_null)));

            retVal.Statements.Add(new CodeVariableDeclarationStatement(forType, "_retVal", new CodeObjectCreateExpression(forType)));

            foreach(var property in forType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite  && !p.HasCustomAttribute<SerializationMetadataAttribute>()))
            {
                retVal.Statements.Add(new CodeCommentStatement($"Clone {property.Name}"));

                var _retValProp = new CodePropertyReferenceExpression(_retVal, property.Name);
                var _cloneeProp = new CodePropertyReferenceExpression(_clonee, property.Name);
                var nullCheck = new CodeConditionStatement(new CodeBinaryOperatorExpression(_cloneeProp, CodeBinaryOperatorType.IdentityInequality, s_null));
                retVal.Statements.Add(nullCheck);
                if (typeof(IList).IsAssignableFrom(property.PropertyType) && !property.PropertyType.IsArray)
                {
                    if (typeof(IdentifiedData).IsAssignableFrom(property.PropertyType.GetGenericArguments()[0]))
                    {
                        nullCheck.TrueStatements.Add(new CodeAssignStatement(_retValProp, new CodeObjectCreateExpression(property.PropertyType)));
                        nullCheck.TrueStatements.Add(new CodeVariableDeclarationStatement(typeof(int), $"i{property.Name}"));
                        var _iterator = new CodeVariableReferenceExpression($"i{property.Name}");
                        var iterator = new CodeIterationStatement(
                                new CodeAssignStatement(_iterator, new CodePrimitiveExpression(0)),
                                new CodeBinaryOperatorExpression(_iterator, CodeBinaryOperatorType.LessThan, new CodePropertyReferenceExpression(_cloneeProp, "Count")),
                                new CodeAssignStatement(_iterator, new CodeBinaryOperatorExpression(_iterator, CodeBinaryOperatorType.Add, new CodePrimitiveExpression(1)))
                            );
                        iterator.Statements.Add(new CodeMethodInvokeExpression(_retValProp, "Add", new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeTypeReferenceExpression("DeepCloner"), "CloneDeep"), new CodeArrayIndexerExpression(_cloneeProp, _iterator))));
                        nullCheck.TrueStatements.Add(iterator);
                    }
                    else
                    {
                        nullCheck.TrueStatements.Add(new CodeAssignStatement(_retValProp, new CodeObjectCreateExpression(property.PropertyType, _cloneeProp)));
                    }
                }
                else if(typeof(IdentifiedData).IsAssignableFrom(property.PropertyType))
                {
                    nullCheck.TrueStatements.Add(new CodeAssignStatement(_retValProp, new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeTypeReferenceExpression("DeepCloner"), "CloneDeep"), _cloneeProp)));
                }
                else
                {
                    nullCheck.TrueStatements.Add(new CodeAssignStatement(_retValProp, _cloneeProp));
                }
            }

            retVal.Statements.Add(new CodeMethodReturnStatement(_retVal));
            return retVal;
        }

    }
}
