﻿using System.Collections.Generic;
using System.Reflection;

namespace ThothRpc.Optimizer
{
    internal interface IInternalThothOptimizer
    {
        bool IsOptimized { get; }

        void Optimize();
        void Optimize(IEnumerable<Assembly>? assemblies);
        MethodTargetOptRec GetRecFromId(ushort id);
        ushort GetIdFromTargetMethod(string target, string method);
    }
}