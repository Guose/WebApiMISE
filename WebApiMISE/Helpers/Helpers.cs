using Microsoft.Win32;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace WebApiMISE.Helpers
{
    public static class Helpers
    {
        public static readonly HashSet<char> LocalFileSystemSpecialChars = new HashSet<char>() { '\\', '/', '.', '?', '*', ':' };

        [ExcludeFromCodeCoverage]
        public static void WaitForDebuggerAttach()
        {
            string valueName = @"SOFTWARE\Samarkand\Debug";
            WaitForDebuggerAttach(valueName);
        }

        [ExcludeFromCodeCoverage]
        public static void WaitForDebuggerAttach(string valueName)
        {
            try
            {
                if (string.IsNullOrEmpty(valueName))
                {
                    // Dont hook up debugger on bogus input.
                    return;
                }

                int sleepIntervalInSeconds = 5;
                int remainingTimeInSeconds = 2 * 60;
                int? specifiedWaitTime = null;

                if (System.Diagnostics.Debugger.IsAttached)
                {
                    return;
                }

                while (true)
                {
                    // Note we look in Current user first. This wont work for apps started as System.
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(valueName, false);

                    // If key is null lets look in hklm. This works for apps started as System.
                    if (key == null)
                    {
                        key = Registry.LocalMachine.OpenSubKey(valueName, false);
                    }

                    if (key == null)
                    {
                        break; // Run without Debugger
                    }

                    if (specifiedWaitTime.HasValue == false)
                    {
                        try
                        {
                            specifiedWaitTime = (int)key.GetValue("WaitForDebugger");

                            // Caution!! Caution!! Caution!!
                            // Be careful here SF seems to have a 'restart your process if service type not registered'
                            // timeout of 1 minute.
                            // So *if* you wait for debugger attach in Main before you've registered the service type
                            // and you set up your time out to be > 1 minute, then you will not start the process until
                            // the debugger is connected. Found this out the hard way :) 
                            // This is because you'll be sitting in this loop when SF kills you, you will restart and
                            // enter this loop.
                            remainingTimeInSeconds = specifiedWaitTime.Value & 0xFFFF;   // Latch the remaining time.
                            sleepIntervalInSeconds = (int)(specifiedWaitTime.Value & 0xFFFF0000) >> 16; // and sleep interval
                            if (sleepIntervalInSeconds <= 0)
                            {
                                sleepIntervalInSeconds = 5; // At least 5 second wait between checking.
                            }
                        }
                        catch
                        {
                            return; // Key does not exist don't wait.
                        }
                    }

                    if (remainingTimeInSeconds <= 0 || System.Diagnostics.Debugger.IsAttached)
                    {
                        return;             // Wait is done. return.
                    }

                    System.Threading.Thread.Sleep(sleepIntervalInSeconds * 1000);
                    remainingTimeInSeconds -= sleepIntervalInSeconds;
                }
            }
            catch (Exception e)
            {
                // Record exception in Registry, but swallow it and keep the process up.
                var excpkey = Registry.LocalMachine.OpenSubKey(valueName, true);
                excpkey.SetValue("Exception:", $"{ParsePossibleAggregateException(e, true)}", RegistryValueKind.String);
            }
        }

        public static string ParsePossibleAggregateException(Exception e, bool includeType = false)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                if (includeType)
                {
                    sb.Append($"{e.GetType().Name}, ");
                }

                sb.Append($"{e.Message}");
                if (e is AggregateException)
                {
                    AggregateException? ae = e as AggregateException;
                    sb.AppendLine();
                    if ((ae.InnerExceptions != null) && ae.InnerExceptions.Any())
                    {
                        foreach (var ex in ae.InnerExceptions)
                        {
                            if (ex is AggregateException)
                            {
                                // Its possible to blow the call stack here if exception isnt
                                // well formed.
                                // its either this or we ignore embedded AggregateException
                                sb.AppendLine(ParsePossibleAggregateException(ex, includeType));
                            }
                            else
                            {
                                if (includeType)
                                {
                                    sb.AppendLine($"{ex.GetType().Name}, ");
                                }

                                sb.AppendLine($"{ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        sb.AppendLine("InnerExceptions is Empty.");
                    }
                }

                sb.AppendLine(e.StackTrace);
            }
            catch (Exception oops)
            {
                // We might get here because we blew the call stack above
                sb.AppendLine($"Exception in ParsePossibleAggregateException: {oops}");
            }

            return sb.ToString();
        }

        public static string SanitizeString(HashSet<char> disallowedChars, string inputString, char replacementChar)
        {
            char[] buffer = new char[inputString.Length];
            for (int i = 0; i < inputString.Length; i++)
            {
                buffer[i] = disallowedChars.Contains(inputString[i]) ? replacementChar : inputString[i];
            }

            return new string(buffer);
        }

        /// <summary>
        /// Helper to indent for formatting strings.
        /// </summary>
        /// <param name="spaceCount">No of spaces</param>
        /// <returns>Padded String</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.StyleCop.CSharp.ReadabilityRules", "SA1122:UseStringEmptyForEmptyStrings", Justification = "Reviewed")]
        public static string Indent(int spaceCount)
        {
            return "".PadLeft(spaceCount);
        }

        /// <summary>
        /// Converts a hostname into ip-address
        /// </summary>
        /// <param name="host">hostname that needs to be converted</param>
        /// <returns>an ip-address in s tring format</returns>
        public static string GetHostIp(string host)
        {
            IPAddress? ip;
            if (IPAddress.TryParse(host, out ip))
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork)
                {
                    ip = null;
                }
            }
            else
            {
                ip = Dns.GetHostAddresses(host).FirstOrDefault(ipAddress => ipAddress.AddressFamily == AddressFamily.InterNetwork);
            }

            return ip!.ToString();
        }
    }
}
