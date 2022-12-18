using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThothRpc.Exceptions;
using ThothRpc.Utility;

namespace ThothRpc.Optimizer
{
    /// <summary>
    /// Allows optimization of Thoth RPC calls.
    /// </summary>
    public class ThothOptimizer : IInternalThothOptimizer
    {
        static readonly Lazy<ThothOptimizer> _instance = new Lazy<ThothOptimizer>
            (() => new ThothOptimizer(), true);

        /// <summary>
        /// Returns the single <see cref="ThothOptimizer"/> instance.
        /// </summary>
        public static ThothOptimizer Instance
        {
            get => _instance.Value;
        }

        ReaderWriterLockSlim _recordsLock = new ReaderWriterLockSlim();

        readonly Dictionary<ushort, MethodTargetOptRec> _targetRecsByKey
            = new Dictionary<ushort, MethodTargetOptRec>();

        readonly Dictionary<string, ushort> _idsByTargetMethodStr
            = new Dictionary<string, ushort>();

        bool _isOptimized;


        /// <summary>
        /// Returns an indication if the Optimize method was called.
        /// </summary>
        public bool IsOptimized
        {
            get
            {
                bool value;

                _recordsLock.EnterReadLock();
                value = _isOptimized;
                _recordsLock.ExitReadLock();

                return value;
            }
        }

        internal ThothOptimizer() { }

        /// <summary>
        /// This method will generate an auto target/method map of all Thoth methods found in all loaded assemblies.
        /// This will decrease the packet size of RPC calls.
        /// In order for optimization to work, the peer codebase MUST contain an identical number of Thoth methods with identical names
        /// from types of an identical fully qualified name as the current codebase. 
        /// In addition, the peer application must also call <see cref="Optimize()"/>.
        /// This method is best called at app startup.
        /// </summary>
        public void Optimize()
        {
            Optimize(null);
        }

        /// <summary>
        /// This method will generate an auto target/method map of all Thoth methods found in the given assemblies.
        /// This will decrease the packet size of RPC calls.
        /// In order for optimization to work, the peer codebase MUST contain an identical number of Thoth methods with identical names
        /// from types of an identical fully qualified name as the current codebase. 
        /// In addition, the peer application must also call <see cref="Optimize(IEnumerable{Assembly}?)"/>.
        /// This method is best called at app startup.
        /// </summary>
        /// <param name="assemblies">The assemblies to search for Thoth methods.</param>
        public void Optimize(IEnumerable<Assembly>? assemblies)
        {
            _recordsLock.EnterWriteLock();

            if (assemblies == null)
            {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }

            var types = assemblies.SelectMany(a => a.GetTypes())
                .OrderBy(t => t.FullName);

            ushort cId = 1;

            foreach (var type in types)
            {
                var methods = ReflectionHelper
                    .GetThothMethods(type)
                    .OrderBy(m => m.Name)
                    .ToList();

                if (methods.Select(m => m.Name).Count()
                    != methods.Select(m => m.Name).Distinct().Count())
                {
                    throw new NotSupportedException
                        ("Overload methods annotated with ThothMethodAttribute are not supported.");
                }

                if (methods.Count > 0)
                {
                    Logging.LogInfo($"Optimizer - Added target {type.FullName}");

                    foreach (var method in methods)
                    {
                        if (cId == ushort.MaxValue - 1)
                        {
                            throw new NotSupportedException($"When using optimization the maximum " +
                                $"allowed number of Thoth methods is {ushort.MaxValue - 1}. Wow!");
                        }

                        _targetRecsByKey.Add(cId, new MethodTargetOptRec(type.FullName, method.Name));
                        _idsByTargetMethodStr.Add(type.FullName + method.Name, cId);
                        cId++;

                        Logging.LogInfo($"Optimizer - Added method {type.FullName} -> {method.Name}");
                    }
                }
            }

            Logging.LogInfo("Optimizer - Done optimizing");
            _isOptimized = true;
            _recordsLock.ExitWriteLock();
        }

        MethodTargetOptRec IInternalThothOptimizer.GetRecFromId(ushort id)
        {
            _recordsLock.EnterReadLock();
            _targetRecsByKey.TryGetValue(id, out MethodTargetOptRec? value);
            _recordsLock.ExitReadLock();

            if (value == null)
            {
                throw new InvalidCallException($"Cannot find method by id {id}.");
            }

            return value;
        }

        ushort IInternalThothOptimizer.GetIdFromTargetMethod(string target, string method)
        {
            _recordsLock.EnterReadLock();
            _idsByTargetMethodStr.TryGetValue(target + method, out var value);
            _recordsLock.ExitReadLock();

            if (value == 0)
            {
                throw new InvalidCallException($"Cannot find id for {target} + {method}.");
            }

            return value;
        }
    }

    internal class MethodTargetOptRec
    {
        public string TargetName { get; }

        public string MethodName { get; }

        public MethodTargetOptRec(string targetName, string methodName)
        {
            TargetName = targetName;
            MethodName = methodName;
        }
    }
}
