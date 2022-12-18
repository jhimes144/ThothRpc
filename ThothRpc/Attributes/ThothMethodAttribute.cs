using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc.Attributes
{
    /// <summary>
    /// Indicates that a method is used in ThothRpc.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ThothMethodAttribute : Attribute { }
}
