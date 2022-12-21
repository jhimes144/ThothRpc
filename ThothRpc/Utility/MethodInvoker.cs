using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThothRpc.Exceptions;
using ThothRpc.Models;
using ThothRpc.Models.Dto;

namespace ThothRpc.Utility
{
    internal static class MethodInvoker
    {
        static readonly ReaderWriterLockSlim _parameterCacheLock = new ReaderWriterLockSlim();

        static readonly ConditionalWeakTable<MethodInfo, Type[]> _parameterTypesByMethodInfo
            = new ConditionalWeakTable<MethodInfo, Type[]>();

        public static async ValueTask<object?> InvokeMethodAsync(TargetRegistration target,
            string methodName, IReadOnlyList<ReadOnlyMemory<byte>> parameterData, DeserializeObject objectDeserializer)
        {
            var method = target.Methods
                .FirstOrDefault(m => m.Name == methodName);

            if (method == null)
            {
                throw new InvalidCallException($"Method {methodName} was not found.");
            }

            try
            {
                _parameterCacheLock.EnterUpgradeableReadLock();

                _parameterTypesByMethodInfo.TryGetValue(method, out var paramTypes);

                if (paramTypes == null)
                {
                    _parameterCacheLock.EnterWriteLock();
                    paramTypes = method.GetParameters()
                        .Select(p => p.ParameterType)
                        .ToArray();

                    _parameterTypesByMethodInfo.Add(method, paramTypes);
                    _parameterCacheLock.ExitWriteLock();
                }

                _parameterCacheLock.ExitUpgradeableReadLock();

                if (paramTypes.Length != parameterData.Count)
                {
                    throw new InvalidCallException
                        ($"Parameter count mismatch. Peer required {paramTypes.Length}" +
                        $" but {parameterData.Count} were supplied.");
                }

                var suppliedParams = new object[paramTypes.Length];

                for (int i = 0; i < paramTypes.Length; i++)
                {
                    suppliedParams[i] = objectDeserializer(paramTypes[i], parameterData[i]);
                }

                var result = method.Invoke(target.Instance, suppliedParams);

                if (typeof(Task).IsAssignableFrom(method.ReturnType)
                    && result is Task task)
                {
                    await task.ConfigureAwait(false);

                    var genericTaskType = method.ReturnType.GenericTypeArguments
                        .FirstOrDefault();

                    if (genericTaskType == null)
                    {
                        return null;
                    }
                    else
                    {
                        return method.ReturnType
                            .GetProperty("Result")?.GetValue(task);
                    }
                }
                else if (typeof(ValueTask).IsAssignableFrom(method.ReturnType)
                        && result is ValueTask valueTask)
                {
                    await valueTask.ConfigureAwait(false);
                    return null;
                }
                else
                {
                    return result;
                }
            }
            catch (InvalidCallException e)
            {
                throw e;
            }
            catch (CallFailedException e)
            {
                throw e;
            }
            catch (TargetException e)
            {
                throw new InvalidCallException(e.Message, e);
            }
            catch (TargetParameterCountException e)
            {
                throw new InvalidCallException(e.Message, e);
            }
            catch (MethodAccessException e)
            {
                throw new InvalidCallException(e.Message, e);
            }
            catch (TargetInvocationException e)
            {
                throw new CallFailedException(e.InnerException?.Message ?? "Unknown exception occurred..");
            }
            catch (Exception e)
            {
                throw new CallFailedException(e.Message, e);
            }
        }
    }
}
