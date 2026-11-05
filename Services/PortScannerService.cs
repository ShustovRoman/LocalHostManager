using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LocalhostManager.Models;

namespace LocalhostManager.Services
{
    public class PortScannerService
    {
        private static readonly HashSet<string> DevProcesses = new(StringComparer.OrdinalIgnoreCase)
        {
            "node", "deno", "bun", "python", "python3", "pythonw",
            "uvicorn", "gunicorn", "flask", "streamlit", "fastapi",
            "dotnet", "java", "javaw", "cargo", "rust", "ruby", "php", "go",
            "next", "vite", "webpack", "live-server", "http-server",
            "electron", "code", "devenv", "rider64", "idea64"
        };

        private static readonly HashSet<int> CommonDevPorts = new()
        {
            3000, 3001, 3002, 3003, 5173, 5174, 5175, 8000, 8080, 8081, 
            4200, 5000, 5001, 4000, 9000, 8088, 8888, 1234, 4321
        };

        private static readonly HashSet<string> DatabaseProcesses = new(StringComparer.OrdinalIgnoreCase)
        {
            "postgres", "mysqld", "mariadb", "mongod", "redis-server", "memcached",
            "docker", "dockerd", "com.docker.backend", "wslhost", "vpnkit"
        };

        private static readonly HashSet<int> CommonDatabasePorts = new()
        {
            5432, 3306, 6379, 27017, 9200, 1433, 1521, 5672, 15672
        };

        private static readonly HashSet<string> SystemProcesses = new(StringComparer.OrdinalIgnoreCase)
        {
            "system", "system idle process", "svchost", "lsass", "services", 
            "spoolsv", "wininit", "csrss", "smss", "winlogon", "taskhostw"
        };

        public List<PortEntry> ScanPorts()
        {
            var rawSockets = NativeTcpHelper.GetListeningSockets();

            // Group by Port + PID to avoid duplicate entries for dual-stack IPv4/IPv6 listeners
            var grouped = rawSockets
                .GroupBy(s => (s.Port, s.Pid))
                .Select(g => g.First())
                .ToList();

            var result = new List<PortEntry>();

            foreach (var socket in grouped)
            {
                var entry = new PortEntry
                {
                    Port = socket.Port,
                    LocalAddress = socket.LocalAddress,
                    ProcessId = socket.Pid
                };

                ResolveProcessInfo(entry);
                ClassifyPort(entry);

                result.Add(entry);
            }

            // Order: Dev first, then Database, then Other, then System. Sub-ordered by Port.
            return result
                .OrderBy(e => GetCategoryWeight(e.Category))
                .ThenBy(e => e.Port)
                .ToList();
        }

        private static int GetCategoryWeight(PortType type) => type switch
        {
            PortType.Dev => 0,
            PortType.Database => 1,
            PortType.Other => 2,
            PortType.System => 3,
            _ => 4
        };

        private static void ResolveProcessInfo(PortEntry entry)
        {
            if (entry.ProcessId <= 4)
            {
                entry.ProcessName = entry.ProcessId == 0 ? "System Idle" : "System";
                entry.Category = PortType.System;
                return;
            }

            try
            {
                using var proc = Process.GetProcessById(entry.ProcessId);
                entry.ProcessName = proc.ProcessName;
                entry.WindowTitle = proc.MainWindowTitle;

                try
                {
                    entry.ProcessPath = proc.MainModule?.FileName ?? string.Empty;
                }
                catch
                {
                    // Access denied for elevated or system processes
                    entry.ProcessPath = string.Empty;
                }
            }
            catch
            {
                entry.ProcessName = "[Завершен / Защищен]";
            }
        }

        private static void ClassifyPort(PortEntry entry)
        {
            if (entry.ProcessId <= 4)
            {
                entry.Category = PortType.System;
                return;
            }

            string name = entry.ProcessName.ToLowerInvariant();

            // 1. Check System
            if (SystemProcesses.Contains(name))
            {
                entry.Category = PortType.System;
                return;
            }

            // 2. Check Database / Services
            if (DatabaseProcesses.Contains(name) || CommonDatabasePorts.Contains(entry.Port))
            {
                entry.Category = PortType.Database;
                return;
            }

            // 3. Check Dev
            if (DevProcesses.Contains(name) || CommonDevPorts.Contains(entry.Port))
            {
                entry.Category = PortType.Dev;
                return;
            }

            // 4. Fallback checks for system ports < 1024
            if (entry.Port < 1024)
            {
                entry.Category = PortType.System;
                return;
            }

            entry.Category = PortType.Other;
        }
    }
}
