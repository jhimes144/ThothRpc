using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc
{
    /// <summary>
    /// Contains callbacks which can be used to obtain Thoth log output as well as publish log entries.
    /// This logging system is just a basic callback invoker.
    /// </summary>
    public static class Logging
    {
        /// <summary>
        /// Callback that if specified, will fire for info log entries.
        /// Logic for this callback should run as quick as possible as to not block internal processing.
        /// </summary>
        public static Action<string>? InfoCallback { get; set; }

        /// <summary>
        /// Callback that if specified, will fire for warning log entries.
        /// Logic for this callback should run as quick as possible as to not block internal processing.
        /// </summary>
        public static Action<string>? WarnCallback { get; set; }

        /// <summary>
        /// Callback that if specified, will fire for error log entries.
        /// Logic for this callback should run as quick as possible as to not block internal processing.
        /// </summary>
        public static Action<string>? ErrorCallback { get; set; }

        /// <summary>
        /// Logs an info entry to the logging system.
        /// Logic for this callback should run as quick as possible as to not block internal processing.
        /// </summary>
        /// <param name="entry">Entry to log.</param>
        public static void LogInfo(string entry)
        {
            InfoCallback?.Invoke(entry);
        }

        /// <summary>
        /// Logs a warning entry to the logging system.
        /// </summary>
        /// <param name="entry">Entry to log.</param>
        public static void LogWarn(string entry)
        {
            WarnCallback?.Invoke(entry);
        }

        /// <summary>
        /// Logs a error entry to the logging system.
        /// </summary>
        /// <param name="entry">Entry to log.</param>
        public static void LogError(string entry)
        {
            ErrorCallback?.Invoke(entry);
        }
    }
}
