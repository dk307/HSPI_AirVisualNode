using SharpCifs.Smb;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Hspi.Utils
{
    internal static class ExceptionHelper
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals")]
        public static string GetFullMessage(this Exception ex)
        {
            switch (ex)
            {
                case AggregateException aggregationException:
                    {
                        var stb = new StringBuilder();

                        foreach (var innerException in aggregationException.InnerExceptions)
                        {
                            stb.AppendLine(GetFullMessage(innerException));
                        }

                        return stb.ToString();
                    }

                case SmbException smbException:
                    {
                        var stb = new StringBuilder();

                        stb.AppendLine(smbException.Message);
                        var rootCause = smbException.GetRootCause();
                        if (rootCause != null)
                        {
                            stb.AppendLine("Root Cause:");
                            stb.AppendLine(rootCause.ToString());
                            return stb.ToString();
                        }

                        return stb.ToString();
                    }
                default:
                    return ex.Message;
            }
        }

        public static bool IsCancelException(this Exception ex)
        {
            return (ex is TaskCanceledException) ||
                   (ex is OperationCanceledException) ||
                   (ex is ObjectDisposedException);
        }
    };
}