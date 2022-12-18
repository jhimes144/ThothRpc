using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ThothRpc.Attributes;

namespace ThothRpc.Models
{
    internal class TargetRegistration
    {
        public object? Instance { get; set; }

        public List<MethodInfo> Methods { get; set; }
            = new List<MethodInfo>();
    }
}
