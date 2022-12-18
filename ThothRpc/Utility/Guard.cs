using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc.Utility
{
    internal static class Guard
    {
        public static void AgainstNullOrWhiteSpaceString(string? argument, string paramName)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentNullException(paramName);
            }
        }

        public static void AgainstNull(string? argumentName, object obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }
    }
}
