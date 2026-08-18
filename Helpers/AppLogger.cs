using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Web;

/// <summary>
/// Task 9, Requirement 8: lightweight, dependency-free file logger used to record every
/// Email API attempt (success or failure) plus any other significant application events.
///
/// Writes rolling daily log files under the folder configured by the "LogFolderPath"
/// appSetting (defaults to ~/App_Data/Logs/). Falls back to System.Diagnostics.Trace if the
/// file system is not writable (e.g. locked-down hosting), so a logging failure can never
/// itself take the application down.
/// </summary>
public static class AppLogger
{
    private static readonly object SyncRoot = new object();

    public static void Info(string category, string message)
    {
        Write("INFO", category, message);
    }

    public static void Warn(string category, string message)
    {
        Write("WARN", category, message);
    }

    public static void Error(string category, string message, Exception ex = null)
    {
        string full = ex == null ? message : message + " | Exception: " + ex;
        Write("ERROR", category, full);
    }

    private static void Write(string level, string category, string message)
    {
        string line = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff}\t{1}\t{2}\t{3}",
            DateTime.Now, level, category, message == null ? "" : message.Replace("\r", " ").Replace("\n", " "));

        try
        {
            string folder = ResolveLogFolder();
            string file = Path.Combine(folder, "app_" + DateTime.Now.ToString("yyyyMMdd") + ".log");

            lock (SyncRoot)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                File.AppendAllText(file, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never throw. Fall back to Trace so the event is not lost entirely.
            System.Diagnostics.Trace.WriteLine(line);
        }
    }

    private static string ResolveLogFolder()
    {
        string configured = ConfigurationManager.AppSettings["LogFolderPath"];
        if (string.IsNullOrWhiteSpace(configured))
        {
            configured = "~/App_Data/Logs/";
        }

        if (HttpContext.Current != null)
        {
            return HttpContext.Current.Server.MapPath(configured);
        }

        // No HttpContext (e.g. called from a background thread) — fall back to a relative path.
        return configured.Replace("~/", AppDomain.CurrentDomain.BaseDirectory + "/");
    }
}
