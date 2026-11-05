using System;

namespace LocalhostManager.Models
{
    public class PortEntry
    {
        public int Port { get; set; }
        public string Protocol { get; set; } = "TCP";
        public string LocalAddress { get; set; } = "127.0.0.1";
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = "Unknown";
        public string ProcessPath { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public PortType Category { get; set; } = PortType.Other;

        public string Url => $"http://localhost:{Port}";

        public bool IsSystem => Category == PortType.System;
        public bool IsDev => Category == PortType.Dev;
        public bool IsDatabase => Category == PortType.Database;

        public string CategoryDisplayName => Category switch
        {
            PortType.Dev => Services.LocalizationService.Get("Loc_CatDev"),
            PortType.Database => Services.LocalizationService.Get("Loc_CatDb"),
            PortType.System => Services.LocalizationService.Get("Loc_CatSystem"),
            _ => Services.LocalizationService.Get("Loc_CatOther")
        };

        // Colors for Dark Theme Pills / Badges matching Windows 11
        public string BadgeBackground => Category switch
        {
            PortType.Dev => "#1E3A2F",       // Soft dark emerald
            PortType.Database => "#1A324B",  // Soft dark blue
            PortType.System => "#3D2424",    // Soft dark red / caution
            _ => "#2D2D2D"
        };

        public string BadgeForeground => Category switch
        {
            PortType.Dev => "#4ADE80",       // Bright mint green
            PortType.Database => "#60A5FA",  // Crisp sky blue
            PortType.System => "#F87171",    // Soft salmon red
            _ => "#A3A3A3"
        };

        public string DisplayName => string.IsNullOrWhiteSpace(WindowTitle) 
            ? ProcessName 
            : $"{ProcessName} ({WindowTitle})";
    }
}
