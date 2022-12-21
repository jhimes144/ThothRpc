using Microsoft.IO;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using ThothRpc.Exceptions;
using ThothRpc.Models.Dto;
using ThothRpc.Optimizer;
using ThothRpc.Utility;

namespace ThothRpc.Base
{
    internal class PacketAnalyzer
    {
        RecyclableMemoryStreamManager _memManager;

        [ThreadStatic]
        static byte[] _rBuffer; // small reusable buffer.

        readonly IInternalThothOptimizer _optimizer;
        readonly Func<MethodCallDto> _methodCallDtoFactory;
        readonly Func<MethodResponseDto> _methodResponseDtoFactory;

        public PacketAnalyzer(IInternalThothOptimizer optimizer,
            Func<MethodCallDto> methodCallDtoFactory,
            Func<MethodResponseDto> methodResponseDtoFactory)
        {
            _optimizer = optimizer;
            _methodCallDtoFactory = methodCallDtoFactory;
            _methodResponseDtoFactory = methodResponseDtoFactory;

            // these values may need to be tweaked.
            var blockSize = 1024;
            var largeBufferMultiple = 1024 * 1024;
            var maximumBufferSize = 16 * largeBufferMultiple;
            var maximumFreeLargePoolBytes = maximumBufferSize * 4;
            var maximumFreeSmallPoolBytes = 250 * blockSize;

            _memManager = new RecyclableMemoryStreamManager
                (blockSize, largeBufferMultiple, maximumBufferSize);

            _memManager.AggressiveBufferReturn = true;
            _memManager.GenerateCallStacks = false;
            _memManager.MaximumFreeLargePoolBytes = maximumFreeLargePoolBytes;
            _memManager.MaximumFreeSmallPoolBytes = maximumFreeSmallPoolBytes;
        }

        public IThothDto DeserializePacket(byte[] data)
        {
            MethodResponseDto? methodResponse;
            MethodCallDto? methodCall;

            var packetType = (PacketType)data[0];
            var pos = 1;

            if (containsFlag(packetType, PacketType.IsMethodCall)
                && !containsFlag(packetType, PacketType.IsMethodCall_Optimized))
            {
                methodCall = _methodCallDtoFactory();

                if (!packetType.HasFlag(PacketType.IsMethodCall_NoCallId))
                {
                    methodCall.CallId = BitConversion.ReadUInt(data, pos);
                    pos += sizeof(uint);
                }

                var cTL = data[pos];
                pos++;

                methodCall.ClassTarget = Encoding.UTF8.GetString(data, pos, cTL);
                pos += cTL;

                var mL = data[pos];
                pos++;

                methodCall.Method = Encoding.UTF8.GetString(data, pos, mL);
                pos += mL;

                while (pos < data.Length)
                {
                    var argLength = BitConversion.ReadInt(data, pos);
                    pos += sizeof(int);

                    methodCall.ArgumentsData.Add(new ReadOnlyMemory<byte>(data, pos, argLength));
                    pos += argLength;
                }

                return methodCall;
            }

            if (containsFlag(packetType, PacketType.IsMethodCall_Optimized))
            {
                if (!_optimizer.IsOptimized)
                {
                    throw new InvalidCallException("The target peer does not have optimization enabled.");
                }

                methodCall = _methodCallDtoFactory();

                if (!packetType.HasFlag(PacketType.IsMethodCall_NoCallId))
                {
                    methodCall.CallId = BitConversion.ReadUInt(data, pos);
                    pos += sizeof(uint);
                }

                var targetId = BitConversion.ReadUShort(data, pos);
                pos += sizeof(ushort);

                var rec = _optimizer.GetRecFromId(targetId);
                methodCall.ClassTarget = rec.TargetName;
                methodCall.Method = rec.MethodName;

                while (pos < data.Length)
                {
                    var argLength = BitConversion.ReadInt(data, pos);
                    pos += sizeof(int);

                    methodCall.ArgumentsData.Add(new ReadOnlyMemory<byte>(data, pos, argLength));
                    pos += argLength;
                }

                return methodCall;
            }

            if (containsFlag(packetType, PacketType.IsMethodResponse | PacketType.IsMethodResponse_NoException))
            {
                methodResponse = _methodResponseDtoFactory();
                populateMethodResponse(packetType, data, methodResponse, null, ref pos);

                return methodResponse;
            }

            if (containsFlag(packetType, PacketType.IsMethodResponse | PacketType.IsMethodResponse_Exception_InvalidCall))
            {
                methodResponse = _methodResponseDtoFactory();
                populateMethodResponse(packetType, data, methodResponse, ExceptionType.InvalidCall, ref pos);

                return methodResponse;
            }

            if (containsFlag(packetType, PacketType.IsMethodResponse | PacketType.IsMethodResponse_Exception_CallFailed))
            {
                methodResponse = _methodResponseDtoFactory();
                populateMethodResponse(packetType, data, methodResponse, ExceptionType.CallFailed, ref pos);

                return methodResponse;
            }

            throw new NotSupportedException();
        }

        static void populateMethodResponse(PacketType packetType,
            byte[] data, MethodResponseDto methodResponse, ExceptionType? exceptionType,
            ref int pos)
        {
            methodResponse.CallId = BitConversion.ReadUInt(data, pos);
            pos += sizeof(uint);

            if (!packetType.HasFlag(PacketType.IsMethodResponse_NoResult))
            {
                var rL = BitConversion.ReadInt(data, pos);
                pos += sizeof(int);

                methodResponse.ResultData = new ReadOnlyMemory<byte>(data, pos, rL);
                pos += rL;
            }

            if (exceptionType.HasValue)
            {
                var ml = BitConversion.ReadInt(data, pos);
                pos += sizeof(int);

                methodResponse.Exception = new ExceptionContainer
                {
                    Type = exceptionType.Value,
                    Message = Encoding.UTF8.GetString(data, pos, ml)
                };
            }
        }

        static bool containsFlag(PacketType packetType, PacketType flag)
        {
            return (packetType & flag) == flag;
        }

        public byte[] SerializePacket(IThothDto dto)
        {
            using (var mStream = _memManager.GetStream())
            {
                _rBuffer ??= new byte[4];
                PacketType packetType;

                switch (dto)
                {
                    case MethodCallDto methodCallDto:
                        packetType = PacketType.IsMethodCall;
                        var optimized = _optimizer.IsOptimized;
                        var hasCallId = methodCallDto.CallId.HasValue;

                        if (optimized)
                        {
                            packetType |= PacketType.IsMethodCall_Optimized;
                        }

                        if (!hasCallId)
                        {
                            packetType |= PacketType.IsMethodCall_NoCallId;
                        }

                        mStream.WriteByte((byte)packetType);

                        if (hasCallId)
                        {
                            BitConversion.WriteUInt(_rBuffer, methodCallDto.CallId.Value!);
                            mStream.Write(_rBuffer, 0, sizeof(uint));
                        }

                        if (optimized)
                        {
                            BitConversion.WriteUShort(_rBuffer, _optimizer
                                .GetIdFromTargetMethod(methodCallDto.ClassTarget, methodCallDto.Method));
                            mStream.Write(_rBuffer, 0, sizeof(ushort));
                        }
                        else
                        {
                            var classTargetBytes = Encoding.UTF8.GetBytes(methodCallDto.ClassTarget);
                            mStream.WriteByte((byte)classTargetBytes.Length);
                            mStream.Write(classTargetBytes);

                            var methodBytes = Encoding.UTF8.GetBytes(methodCallDto.Method);
                            mStream.WriteByte((byte)methodBytes.Length);
                            mStream.Write(methodBytes);
                        }

                        for (int i = 0; i < methodCallDto.ArgumentsData.Count; i++)
                        {
                            var argData = methodCallDto.ArgumentsData[i];
                            BitConversion.WriteInt(_rBuffer, argData.Length);

                            mStream.Write(_rBuffer, 0, sizeof(int));
                            mStream.Write(argData.Span);
                        }
                        break;
                    case MethodResponseDto methodResponseDto:
                        packetType = PacketType.IsMethodResponse;

                        var hasNoResult = methodResponseDto.ResultData == null;
                        var hasNoException = methodResponseDto.Exception == null;
                        var invalidCall = !hasNoException && methodResponseDto.Exception.Type == ExceptionType.InvalidCall;
                        var callFailed = !hasNoException && methodResponseDto.Exception.Type == ExceptionType.CallFailed;

                        if (hasNoException)
                        {
                            packetType |= PacketType.IsMethodResponse_NoException;
                        }
                        else if (invalidCall)
                        {
                            packetType |= PacketType.IsMethodResponse_Exception_InvalidCall;
                        }
                        else if (callFailed)
                        {
                            packetType |= PacketType.IsMethodResponse_Exception_CallFailed;
                        }

                        if (hasNoResult)
                        {
                            packetType |= PacketType.IsMethodResponse_NoResult;
                        }

                        mStream.WriteByte((byte)packetType);

                        BitConversion.WriteUInt(_rBuffer, methodResponseDto.CallId);
                        mStream.Write(_rBuffer, 0, sizeof(uint));

                        if (!hasNoResult)
                        {
                            BitConversion.WriteInt(_rBuffer, methodResponseDto.ResultData.Value.Length);
                            mStream.Write(_rBuffer, 0, sizeof(int));
                            mStream.Write(methodResponseDto.ResultData.Value.Span);
                        }

                        if (!hasNoException)
                        {
                            var messageBytes = Encoding.UTF8.GetBytes(methodResponseDto.Exception.Message);
                            BitConversion.WriteInt(_rBuffer, messageBytes.Length);
                            mStream.Write(_rBuffer, 0, sizeof(int));
                            mStream.Write(messageBytes);
                        }
                        break;
                    default:
                        throw new NotSupportedException();
                }
                
                // todo: Memory allocation here
                var result = new byte[mStream.Position];
                mStream.Position = 0;
                mStream.Read(result, 0, result.Length);
                return result;
            }
        }
    }
}
