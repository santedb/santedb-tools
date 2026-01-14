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
            var ct = new CodeTypeDeclaration("DeepCloner")
            {
                Attributes = MemberAttributes.Assembly | MemberAttributes.Static
            };
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
            //ct.Members.Add(this.CreateGenericDeepCloneMethod(cloneTypes));
            return retVal;
        }

        /// <summary>
        /// Create a generic deep clone method
        /// </summary>
        private CodeTypeMember CreateGenericDeepCloneMethod(IEnumerable<Type> cloneTypes)
        {
            var retVal = new CodeMemberMethod()
            {
                Attributes = MemberAttributes.Static | MemberAttributes.Assembly,
                ReturnType = new CodeTypeReference(typeof(IdentifiedData)),
                Name = "CloneDeep"
            };
            retVal.Comments.Add(new CodeCommentStatement("<inheritdoc />"));

            retVal.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(IdentifiedData)), "clonee"));
            var _clonee = new CodeVariableReferenceExpression("clonee");

            // Sort the clone types by their inheritence depth descending
            var inheritenceDepths = cloneTypes.ToDictionary(t => t, t =>
            {
                int depth = 0;
                while (t.BaseType != typeof(Object))
                {
                    depth++;
                    t = t.BaseType;
                };
                return depth;
            });

            // If/else statements

            CodeConditionStatement currentIfElse = null;
            int castType = 0;
            retVal.Statements.Add(new CodeSnippetStatement("switch(clonee) {"));
            foreach(var itm in inheritenceDepths.OrderByDescending(o=>o.Value).Select(o=>o.Key))
            {
                retVal.Statements.Add(new CodeSnippetStatement($"case {itm.FullName} cl{++castType}:"));
                retVal.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeSnippetExpression($"cl{castType}"), "DeepCopy"))));
            }
            retVal.Statements.Add(new CodeSnippetStatement("}"));
            retVal.Statements.Add(new CodeMethodReturnStatement(_clonee));

            return retVal;
        }

        /// <summary>
        /// Create a deep clone method
        /// </summary>
        public CodeMemberMethod CreateDeepCloneMethod(Type forType)
        {

            var retVal = new CodeMemberMethod()
            {
                Attributes = MemberAttributes.Static | MemberAttributes.Assembly,
                ReturnType = new CodeTypeReference(forType),
                Name = "CloneDeep"
            };
            retVal.Comments.Add(new CodeCommentStatement("<inheritdoc />", true));

            retVal.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(forType), "clonee"));
            var _clonee = new CodeVariableReferenceExpression("clonee");
            var _retVal = new CodeVariableReferenceExpression("_retVal");
            var _iterator = new CodeVariableReferenceExpression($"_iterator");
            bool hasIterator = false;

            retVal.Statements.Add(new CodeConditionStatement(new CodeBinaryOperatorExpression(_clonee, CodeBinaryOperatorType.IdentityEquality, s_null), new CodeMethodReturnStatement(s_null)));
            retVal.Statements.Add(new CodeVariableDeclarationStatement(forType, "_retVal", new CodeObjectCreateExpression(forType)));

            foreach(var property in forType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.CanWrite  && (!p.HasCustomAttribute<SerializationMetadataAttribute>())))
            {
                retVal.Statements.Add(new CodeCommentStatement($"Clone {property.Name}"));

                var _retValProp = new CodePropertyReferenceExpression(_retVal, property.Name);
                var _cloneeProp = new CodePropertyReferenceExpression(_clonee, property.Name);
                var targetStatementCollection = retVal.Statements;
                if (property.PropertyType.IsClass && !typeof(string).Equals(property.PropertyType))
                {
                    var nullCheck = new CodeConditionStatement(new CodeBinaryOperatorExpression(_cloneeProp, CodeBinaryOperatorType.IdentityInequality, s_null));
                    retVal.Statements.Add(nullCheck);
                    targetStatementCollection = nullCheck.TrueStatements;

                }
                if (typeof(IList).IsAssignableFrom(property.PropertyType) && !property.PropertyType.IsArray)
                {
                    if (typeof(IdentifiedData).IsAssignableFrom(property.PropertyType.GetGenericArguments()[0]))
                    {
                        targetStatementCollection.Add(new CodeAssignStatement(_retValProp, new CodeObjectCreateExpression(property.PropertyType)));
                        if(!hasIterator)
                        {
                            retVal.Statements.Insert(2, new CodeVariableDeclarationStatement(typeof(int), $"_iterator"));
                            hasIterator = true;
                        }

                        var iterator = new CodeIterationStatement(
                                new CodeAssignStatement(_iterator, new CodePrimitiveExpression(0)),
                                new CodeBinaryOperatorExpression(_iterator, CodeBinaryOperatorType.LessThan, new CodePropertyReferenceExpression(_cloneeProp, "Count")),
                                new CodeAssignStatement(_iterator, new CodeBinaryOperatorExpression(_iterator, CodeBinaryOperatorType.Add, new CodePrimitiveExpression(1)))
                            );
                        iterator.Statements.Add(new CodeMethodInvokeExpression(_retValProp, "Add", new CodeCastExpression(new CodeTypeReference(property.PropertyType.StripGeneric()), new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeArrayIndexerExpression(_cloneeProp, _iterator), "DeepCopy")))));
                        targetStatementCollection.Add(iterator);
                    }
                    else
                    {
                        targetStatementCollection.Add(new CodeAssignStatement(_retValProp, new CodeObjectCreateExpression(property.PropertyType, _cloneeProp)));
                    }
                }
                else if(typeof(IdentifiedData).IsAssignableFrom(property.PropertyType))
                {
                    targetStatementCollection.Add(new CodeAssignStatement(_retValProp, new CodeCastExpression(new CodeTypeReference(property.PropertyType), new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(_cloneeProp, "DeepCopy")))));
                }
                else
                {
                    targetStatementCollection.Add(new CodeAssignStatement(_retValProp, _cloneeProp));
                }
            }

            retVal.Statements.Add(new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(_retVal, "CopyDelayLoadIndicators"), _clonee));
            retVal.Statements.Add(new CodeMethodReturnStatement(_retVal));
            return retVal;
        }

    }
}
