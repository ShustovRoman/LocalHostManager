using System;
using System.ComponentModel;
using System.Diagnostics;
using LocalhostManager.Models;

namespace LocalhostManager.Services
{
    public record KillResult(bool Success, string Message);

    public class ProcessKillerService
    {
        public KillResult KillProcess(PortEntry entry, bool userConfirmedSystem = false)
        {
            if (entry.ProcessId <= 4)
            {
                return new KillResult(false, $"Невозможно завершить системный процесс ядра Windows (PID {entry.ProcessId}).");
            }

            if (entry.IsSystem && !userConfirmedSystem)
            {
                return new KillResult(false, "Требуется явное подтверждение для завершения системного процесса.");
            }

            try
            {
                using var proc = Process.GetProcessById(entry.ProcessId);
                
                // Kill process and all its child processes (e.g., cmd -> npm -> node.exe)
                proc.Kill(entireProcessTree: true);
                proc.WaitForExit(1500);

                return new KillResult(true, LocalizationService.Get("Loc_Killed", entry.ProcessName, entry.ProcessId));
            }
            catch (ArgumentException)
            {
                return new KillResult(true, LocalizationService.Get("Loc_Killed", entry.ProcessName, entry.ProcessId));
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 5) // Access Denied
            {
                // Target process is running elevated or under another account.
                // Elevate taskkill on-demand so the user can kill it seamlessly!
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "taskkill.exe",
                        Arguments = $"/F /T /PID {entry.ProcessId}",
                        Verb = "runas",
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(3000);

                    // Check if process was killed
                    try
                    {
                        using var check = Process.GetProcessById(entry.ProcessId);
                        if (check.HasExited)
                        {
                            return new KillResult(true, LocalizationService.Get("Loc_AdminKilled", entry.ProcessName, entry.ProcessId));
                        }
                    }
                    catch (ArgumentException)
                    {
                        return new KillResult(true, LocalizationService.Get("Loc_AdminKilled", entry.ProcessName, entry.ProcessId));
                    }
                }
                catch
                {
                    // User declined UAC prompt
                }

                return new KillResult(false, LocalizationService.Get("Loc_AccessDenied", entry.ProcessId));
            }
            catch (Exception ex)
            {
                return new KillResult(false, $"Ошибка при завершении процесса: {ex.Message}");
            }
        }
    }
}
