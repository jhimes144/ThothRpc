using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ThothRpc.Attributes;
using ThothRpc.Models.Dto;

namespace ThothRpc.Utility
{
    internal static class ReflectionHelper
    {
        static readonly SimplifyConst _simplifyConst = new SimplifyConst();

        public static (string methodName, object?[] arguments) EvaluateMethodCall<T>(Expression<Action<T>> expression)
        {
            return EvaluateMethodCall((LambdaExpression)expression);
        }

        public static IEnumerable<MethodInfo> GetThothMethods(Type type)
        {
            return type.GetMethods()
                .Where(m => m.GetCustomAttribute<ThothMethodAttribute>() != null);
        }

        /// <summary>
        /// IMPORTANT: MethodCallData that is returned in a thread static field to save on GC
        /// and should not be used across threads, and used quickly
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static (string MethodName, object?[] Arguments) EvaluateMethodCall(LambdaExpression expression)
        {
            if (expression.Body is MethodCallExpression methodCall)
            {
                var args = methodCall.Arguments;
                var rArgs = new object?[args.Count];

                var i = 0;
                foreach (var arg in args)
                {
                    if (_simplifyConst.Visit(arg) is ConstantExpression consta)
                    {
                        rArgs[i] = consta.Value;
                    }
                    else
                    {
                        throw new Exception("Expression arguments must be either from variables, fields, or properties.");
                    }

                    i++;
                }

                return (methodCall.Method.Name, rArgs);
            }
            else
            {
                throw new Exception("Expression must be a method call");
            }
        }

        private class SimplifyConst : ExpressionVisitor
        {
            protected override Expression VisitMember(System.Linq.Expressions.MemberExpression node)
            {
                var expr = Visit(node.Expression);
                if (expr is ConstantExpression c)
                {
                    if (node.Member is PropertyInfo prop)
                        return Expression.Constant(prop.GetValue(c.Value), prop.PropertyType);
                    if (node.Member is FieldInfo field)
                        return Expression.Constant(field.GetValue(c.Value), field.FieldType);
                }
                return node.Update(expr);
            }
        }
    }
}
