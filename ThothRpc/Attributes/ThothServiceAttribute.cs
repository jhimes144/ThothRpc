using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThothRpc.Optimizer;

namespace ThothRpc.Attributes
{
    /// <summary>
    /// Indicates that a class or interface is used for ThothRpc calls. This attribute is optional but may be required
    /// in use with <see cref="ThothOptimizer.Optimize(IEnumerable{System.Reflection.Assembly}?, bool)"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class ThothServiceAttribute : Attribute { }
}
