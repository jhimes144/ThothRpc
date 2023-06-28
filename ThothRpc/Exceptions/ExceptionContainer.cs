using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS8604 // Possible null reference argument.

namespace ThothRpc.Exceptions
{
    internal enum ExceptionType
    {
        CallFailed,
        InvalidCall
    }

    internal class ExceptionContainer
    {
        public ExceptionType Type { get; set; }

        public string? Message { get; set; }

        public Exception Unpack()
        {
            return Type switch
            {
                ExceptionType.CallFailed => new CallFailedException(Message),
                ExceptionType.InvalidCall => new InvalidCallException(Message),
                _ => throw new NotSupportedException(),
            };
        }

        public static ExceptionContainer Pack(Exception exception, bool genericErrorMessages)
        {
            var message = genericErrorMessages 
                ? "An error occurred on the remote peer." :
                exception.Message;

            // ide will say unnessary assignment, do not listen will break .net standard build

            return exception switch
            {
                CallFailedException callFailedException => new ExceptionContainer
                {
                    Message = message,
                    Type = ExceptionType.CallFailed
                },
                InvalidCallException invalidCallException => new ExceptionContainer
                {
                    Message = message,
                    Type = ExceptionType.InvalidCall
                },
                _ => throw new NotSupportedException(),
            };
        }
    }
}

#pragma warning restore CS8604 // Possible null reference argument.
