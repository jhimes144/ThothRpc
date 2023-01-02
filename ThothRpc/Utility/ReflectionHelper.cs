using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            // Algorithm from StackOverflow answer here:
            // https://stackoverflow.com/questions/7889228/how-to-prevent-reflectiontypeloadexception-when-calling-assembly-gettypes
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            try
            {
                return assembly.DefinedTypes.Select(t => t.AsType());
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null)!;
            }
        }

        public static IEnumerable<MethodInfo> GetThothMethods(Type type)
        {
            var methods = new List<MethodInfo>();

            methods.AddRange(type.GetMethods()
                .Where(m => m.GetCustomAttribute<ThothMethodAttribute>() != null));

            foreach (var @interface in type.GetInterfaces())
            {
                methods.AddRange(GetThothMethods(@interface));
            }

            if (type.BaseType != null)
            {
                methods.AddRange(GetThothMethods(type.BaseType));
            }

            return methods;
        }

        [ThreadStatic]
        static Stopwatch _sw;

        /// <summary>
        /// IMPORTANT: MethodCallData that is returned in a thread static field to save on GC
        /// and should not be used across threads, and used quickly
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static (string MethodName, object?[] Arguments) EvaluateMethodCall(LambdaExpression expression)
        {
            if (_sw == null)
            {
                _sw = Stopwatch.StartNew();
            }
            else
            {
                _sw.Restart();
            }

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

                Debug.WriteLine($"Parsing expression for the call took {_sw.ElapsedMilliseconds} ms");
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
