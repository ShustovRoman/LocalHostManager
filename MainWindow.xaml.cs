using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using LocalhostManager.Models;
using LocalhostManager.Services;
using Forms = System.Windows.Forms;

namespace LocalhostManager
{
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        private static void TrimWorkingSet()
        {
            try
            {
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
            }
            catch
            {
                // Silently ignore if permission restricted
            }
        }

        private readonly PortScannerService _scanner = new();
        private readonly ProcessKillerService _killer = new();

        private List<PortEntry> _allEntries = new();
        private PortEntry? _pendingEntryToKill;

        private Forms.NotifyIcon? _notifyIcon;
        private bool _isExplicitExit = false;

        public MainWindow()
        {
            AppLogger.Log("MainWindow .ctor started");
            InitializeComponent();
            AppLogger.Log("InitializeComponent finished");
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            PreviewKeyDown += MainWindow_PreviewKeyDown;

            PortsListView.SizeChanged += (s, e) => AdjustColumnWidths();
            LocalizationService.LanguageChanged += OnLanguageChanged;

            InitializeTrayIcon();
            AppLogger.Log("InitializeTrayIcon finished");
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AppLogger.Log("MainWindow_Loaded started");
            EnableImmersiveDarkMode();
            SyncLanguageSelector();
            LoadPorts();
            AdjustColumnWidths();
            UpdateContextMenuStrings();
            
            // Bring to front on launch
            Topmost = true;
            Activate();
            Topmost = false;
            Focus();

            AppLogger.Log("MainWindow_Loaded finished");
        }

        private void AdjustColumnWidths()
        {
            if (PortsListView.View is GridView gridView && PortsListView.ActualWidth > 0)
            {
                // Fixed widths: 95 (Port) + 115 (Category) + 80 (PID) + 140 (Actions) + 25 (margin/scrollbar) = 455
                double fixedWidth = 95 + 115 + 80 + 140 + 25;
                double remaining = PortsListView.ActualWidth - fixedWidth;
                if (remaining > 160 && gridView.Columns.Count >= 3)
                {
                    gridView.Columns[2].Width = remaining; // Process column stretches to fill entire table!
                }
            }
        }

        private void EnableImmersiveDarkMode()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                int trueValue = 1;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueValue, sizeof(int));
            }
            catch
            {
                // Fallback for older OS versions
            }
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                LoadPorts();
                e.Handled = true;
            }
        }

        #region System Tray Integration

        private Forms.ToolStripMenuItem? _trayItemShow;
        private Forms.ToolStripMenuItem? _trayItemRefresh;
        private Forms.ToolStripMenuItem? _trayItemExit;

        private void InitializeTrayIcon()
        {
            System.Drawing.Icon trayIcon;
            try
            {
                var icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
                if (File.Exists(icoPath))
                {
                    trayIcon = new System.Drawing.Icon(icoPath);
                }
                else
                {
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                    trayIcon = !string.IsNullOrEmpty(exePath) ? System.Drawing.Icon.ExtractAssociatedIcon(exePath) ?? CreateMinimalistTrayIcon() : CreateMinimalistTrayIcon();
                }
            }
            catch
            {
                trayIcon = CreateMinimalistTrayIcon();
            }

            _notifyIcon = new Forms.NotifyIcon
            {
                Text = "Localhost Manager",
                Visible = true,
                Icon = trayIcon
            };

            var contextMenu = new Forms.ContextMenuStrip();
            _trayItemShow = new Forms.ToolStripMenuItem(LocalizationService.Get("Loc_TrayShow"), null, (s, e) => ShowAndRestoreWindow());
            _trayItemRefresh = new Forms.ToolStripMenuItem(LocalizationService.Get("Loc_TrayRefresh"), null, (s, e) => LoadPorts());
            _trayItemExit = new Forms.ToolStripMenuItem(LocalizationService.Get("Loc_TrayExit"), null, (s, e) => ExitApplication());

            contextMenu.Items.Add(_trayItemShow);
            contextMenu.Items.Add(_trayItemRefresh);
            contextMenu.Items.Add(new Forms.ToolStripSeparator());
            contextMenu.Items.Add(_trayItemExit);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowAndRestoreWindow();
        }

        private void UpdateTrayMenuStrings()
        {
            if (_trayItemShow != null) _trayItemShow.Text = LocalizationService.Get("Loc_TrayShow");
            if (_trayItemRefresh != null) _trayItemRefresh.Text = LocalizationService.Get("Loc_TrayRefresh");
            if (_trayItemExit != null) _trayItemExit.Text = LocalizationService.Get("Loc_TrayExit");
        }

        private static System.Drawing.Icon CreateMinimalistTrayIcon()
        {
            // Dynamically generate a clean 2-color Windows 11 style socket icon (16x16)
            using var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Two-color palette: Windows Blue (#0078D4) and Silver White (#F0F0F0)
                using var blueBrush = new SolidBrush(Color.FromArgb(0, 120, 212));
                using var whiteBrush = new SolidBrush(Color.FromArgb(240, 240, 240));

                g.FillRectangle(blueBrush, 1, 1, 6, 6);
                g.FillRectangle(whiteBrush, 9, 1, 6, 6);
                g.FillRectangle(whiteBrush, 1, 9, 6, 6);
                g.FillRectangle(blueBrush, 9, 9, 6, 6);
            }

            var hIcon = bitmap.GetHicon();
            return System.Drawing.Icon.FromHandle(hIcon);
        }

        private void ShowAndRestoreWindow()
        {
            Show();
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Activate();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExplicitExit)
            {
                // Minimize to system tray instead of closing
                e.Cancel = true;
                Hide();
                TrimWorkingSet();
                _notifyIcon?.ShowBalloonTip(1500, "Localhost Manager", "Приложение свернуто в трей и готово к работе.", Forms.ToolTipIcon.Info);
            }
        }

        private void ExitApplication()
        {
            _isExplicitExit = true;
            _notifyIcon?.Dispose();
            Application.Current.Shutdown();
        }

        #endregion

        #region Language Selection

        private bool _isSyncingLang = false;

        private void SyncLanguageSelector()
        {
            if (LanguageComboBox == null) return;
            _isSyncingLang = true;
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag?.ToString() == LocalizationService.CurrentLanguage)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }
            _isSyncingLang = false;
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSyncingLang || LanguageComboBox.SelectedItem is not ComboBoxItem item) return;
            string langCode = item.Tag?.ToString() ?? "ru";
            LocalizationService.SetLanguage(langCode);
        }

        private void OnLanguageChanged()
        {
            UpdatePillCounters();
            ApplyCurrentFilter();
            UpdateContextMenuStrings();
            UpdateTrayMenuStrings();

            if (_allEntries != null && _allEntries.Count > 0)
            {
                var devCount = _allEntries.Count(x => x.Category == PortType.Dev);
                var dbCount = _allEntries.Count(x => x.Category == PortType.Database);
                var sysCount = _allEntries.Count(x => x.Category == PortType.System);
                StatusMessageText.Text = LocalizationService.Get("Loc_StatusFound", _allEntries.Count, devCount, dbCount, sysCount, DateTime.Now.ToString("HH:mm:ss"));
            }
            else
            {
                StatusMessageText.Text = LocalizationService.Get("Loc_StatusReady");
            }
        }

        private void UpdateContextMenuStrings()
        {
            if (PortsListView?.ContextMenu is not ContextMenu cm) return;

            if (cm.Items.Count >= 7)
            {
                if (cm.Items[0] is MenuItem m0) m0.Header = LocalizationService.Get("Loc_MenuOpen");
                if (cm.Items[1] is MenuItem m1) m1.Header = LocalizationService.Get("Loc_MenuCopyUrl");
                if (cm.Items[2] is MenuItem m2) m2.Header = LocalizationService.Get("Loc_MenuCopyPid");
                if (cm.Items[4] is MenuItem m4) m4.Header = LocalizationService.Get("Loc_MenuOpenFolder");
                if (cm.Items[6] is MenuItem m6) m6.Header = LocalizationService.Get("Loc_MenuKill");
            }
        }

        #endregion

        #region Port Loading & Filtering

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadPorts();
        }

        private void LoadPorts()
        {
            try
            {
                _allEntries = _scanner.ScanPorts();
                UpdatePillCounters();
                ApplyCurrentFilter();

                var devCount = _allEntries.Count(x => x.Category == PortType.Dev);
                var dbCount = _allEntries.Count(x => x.Category == PortType.Database);
                var sysCount = _allEntries.Count(x => x.Category == PortType.System);

                StatusMessageText.Text = LocalizationService.Get("Loc_StatusFound", _allEntries.Count, devCount, dbCount, sysCount, DateTime.Now.ToString("HH:mm:ss"));
            }
            catch (Exception ex)
            {
                StatusMessageText.Text = $"Error: {ex.Message}";
            }
        }

        private void UpdatePillCounters()
        {
            if (PillAll == null || PillDev == null || PillDb == null || PillSystem == null) return;

            int allCount = _allEntries.Count;
            int devCount = _allEntries.Count(x => x.Category == PortType.Dev);
            int dbCount = _allEntries.Count(x => x.Category == PortType.Database);
            int sysCount = _allEntries.Count(x => x.Category == PortType.System);

            PillAll.Content = $"{LocalizationService.Get("Loc_FilterAll")} ({allCount})";
            PillDev.Content = $"{LocalizationService.Get("Loc_FilterDev")} ({devCount})";
            PillDb.Content = $"{LocalizationService.Get("Loc_FilterDb")} ({dbCount})";
            PillSystem.Content = $"{LocalizationService.Get("Loc_FilterSystem")} ({sysCount})";
        }

        private void FilterPill_Checked(object sender, RoutedEventArgs e)
        {
            ApplyCurrentFilter();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchBox.Text.Trim();
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(query) ? Visibility.Visible : Visibility.Collapsed;
            ClearSearchButton.Visibility = string.IsNullOrEmpty(query) ? Visibility.Collapsed : Visibility.Visible;
            ApplyCurrentFilter();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
        }

        private void ApplyCurrentFilter()
        {
            if (PortsListView == null || SearchBox == null || PillDev == null || PillDb == null || PillSystem == null)
                return;

            var filtered = _allEntries.AsEnumerable();

            // 1. Pill Filter
            if (PillDev.IsChecked == true)
            {
                filtered = filtered.Where(x => x.Category == PortType.Dev);
            }
            else if (PillDb.IsChecked == true)
            {
                filtered = filtered.Where(x => x.Category == PortType.Database);
            }
            else if (PillSystem.IsChecked == true)
            {
                filtered = filtered.Where(x => x.Category == PortType.System);
            }

            // 2. Search Query Filter
            string query = SearchBox.Text.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(x =>
                    x.Port.ToString().Contains(query) ||
                    x.ProcessName.ToLowerInvariant().Contains(query) ||
                    x.ProcessId.ToString().Contains(query) ||
                    x.WindowTitle.ToLowerInvariant().Contains(query) ||
                    x.ProcessPath.ToLowerInvariant().Contains(query));
            }

            var list = filtered.ToList();
            PortsListView.ItemsSource = list;

            EmptyStatePanel.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            PortsListView.Visibility = list.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        #endregion

        #region Actions (Open Browser & Kill Process)

        private void OpenBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is PortEntry entry)
            {
                OpenInBrowser(entry);
            }
        }

        private void PortsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PortsListView.SelectedItem is PortEntry entry)
            {
                OpenInBrowser(entry);
            }
        }

        private void OpenInBrowser(PortEntry entry)
        {
            try
            {
                Process.Start(new ProcessStartInfo(entry.Url) { UseShellExecute = true });
                StatusMessageText.Text = LocalizationService.Get("Loc_StatusOpened", entry.Url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open browser: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void KillProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is PortEntry entry)
            {
                TryKillEntry(entry);
            }
        }

        private void TryKillEntry(PortEntry entry)
        {
            if (entry.IsSystem)
            {
                // Show in-app confirmation modal for system services
                _pendingEntryToKill = entry;
                ModalMessageText.Text = LocalizationService.Get("Loc_ModalMsg", entry.ProcessName, entry.ProcessId, entry.Port);
                ConfirmationModal.Visibility = Visibility.Visible;
                return;
            }

            ExecuteKill(entry, false);
        }

        private void ExecuteKill(PortEntry entry, bool confirmed)
        {
            var result = _killer.KillProcess(entry, confirmed);

            if (result.Success)
            {
                StatusMessageText.Text = result.Message;
                LoadPorts();
            }
            else
            {
                MessageBox.Show(result.Message, "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusMessageText.Text = result.Message;
            }
        }

        private void ModalCancel_Click(object sender, RoutedEventArgs e)
        {
            _pendingEntryToKill = null;
            ConfirmationModal.Visibility = Visibility.Collapsed;
        }

        private void ModalConfirm_Click(object sender, RoutedEventArgs e)
        {
            ConfirmationModal.Visibility = Visibility.Collapsed;
            if (_pendingEntryToKill != null)
            {
                ExecuteKill(_pendingEntryToKill, true);
                _pendingEntryToKill = null;
            }
        }

        #endregion

        #region Context Menu

        private void MenuOpenBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (PortsListView.SelectedItem is PortEntry entry)
            {
                OpenInBrowser(entry);
            }
        }

        private void MenuCopyUrl_Click(object sender, RoutedEventArgs e)
        {
            if (PortsListView.SelectedItem is PortEntry entry)
            {
                Clipboard.SetText(entry.Url);
                StatusMessageText.Text = LocalizationService.Get("Loc_StatusCopiedUrl");
            }
        }

        private void MenuCopyPid_Click(object sender, RoutedEventArgs e)
        {
            if (PortsListView.SelectedItem is PortEntry entry)
            {
                Clipboard.SetText(entry.ProcessId.ToString());
                StatusMessageText.Text = LocalizationService.Get("Loc_StatusCopiedPid");
            }
        }

        private void MenuOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (PortsListView.SelectedItem is PortEntry entry && !string.IsNullOrEmpty(entry.ProcessPath) && File.Exists(entry.ProcessPath))
            {
                try
                {
                    Process.Start("explorer.exe", $"/select,\"{entry.ProcessPath}\"");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось открыть папку: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Путь к исполняемому файлу недоступен для этого процесса.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MenuKillProcess_Click(object sender, RoutedEventArgs e)
        {
            if (PortsListView.SelectedItem is PortEntry entry)
            {
                TryKillEntry(entry);
            }
        }

        #endregion
    }
}
