using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;

namespace LocalhostManager.Services
{
    public record LanguageItem(string Code, string DisplayName);

    public static class LocalizationService
    {
        public static event Action? LanguageChanged;

        public static readonly List<LanguageItem> SupportedLanguages = new()
        {
            new LanguageItem("ru", "Русский"),
            new LanguageItem("en", "English"),
            new LanguageItem("es", "Español"),
            new LanguageItem("de", "Deutsch"),
            new LanguageItem("fr", "Français"),
            new LanguageItem("zh", "中文")
        };

        public static string CurrentLanguage { get; private set; } = "ru";

        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
        {
            ["ru"] = new()
            {
                ["Loc_AppTitle"] = "Localhost Manager",
                ["Loc_SearchPlaceholder"] = "Поиск хоста или процесса...",
                ["Loc_Refresh"] = "Обновить",
                ["Loc_TooltipRefresh"] = "Обновить список портов (F5)",
                ["Loc_FilterAll"] = "Все",
                ["Loc_FilterDev"] = "Dev-хосты",
                ["Loc_FilterDb"] = "Базы данных",
                ["Loc_FilterSystem"] = "Системные",
                ["Loc_ColPort"] = "Порт",
                ["Loc_ColCategory"] = "Категория",
                ["Loc_ColProcess"] = "Процесс",
                ["Loc_ColPid"] = "PID",
                ["Loc_ColActions"] = "Действия",
                ["Loc_BtnOpen"] = "Открыть",
                ["Loc_TooltipOpen"] = "Открыть хост в браузере",
                ["Loc_TooltipKill"] = "Завершить процесс",
                ["Loc_EmptyTitle"] = "Хосты не найдены",
                ["Loc_EmptySubtitle"] = "Запусти локальный сервер и нажми 'Обновить'",
                ["Loc_FooterHint"] = "Двойной клик — открыть в браузере • ПКМ — меню • F5 — обновить",
                ["Loc_StatusReady"] = "Готов к работе",
                ["Loc_StatusFound"] = "Найдено портов: {0} (Dev: {1}, Базы: {2}, Системные: {3}) • {4}",
                ["Loc_StatusOpened"] = "Открыт {0} в браузере",
                ["Loc_StatusCopiedUrl"] = "URL скопирован в буфер обмена",
                ["Loc_StatusCopiedPid"] = "PID скопирован в буфер обмена",
                ["Loc_TrayShow"] = "Открыть окно",
                ["Loc_TrayRefresh"] = "Обновить порты",
                ["Loc_TrayExit"] = "Выход",
                ["Loc_ModalTitle"] = "Завершение системного процесса",
                ["Loc_ModalSubtitle"] = "Требуется подтверждение безопасности",
                ["Loc_ModalMsg"] = "Процесс '{0}' (PID {1}, порт {2}) является системной службой Windows.\n\nЗавершение может повлиять на стабильность системы. Всё равно завершить?",
                ["Loc_ModalCancel"] = "Отмена",
                ["Loc_ModalConfirm"] = "Завершить принудительно",
                ["Loc_CatDev"] = "Dev Хост",
                ["Loc_CatDb"] = "База данных",
                ["Loc_CatSystem"] = "Системный",
                ["Loc_CatOther"] = "Прочее",
                ["Loc_MenuOpen"] = "Открыть в браузере",
                ["Loc_MenuCopyUrl"] = "Копировать URL",
                ["Loc_MenuCopyPid"] = "Копировать PID",
                ["Loc_MenuOpenFolder"] = "Открыть расположение файла",
                ["Loc_MenuKill"] = "Завершить процесс",
                ["Loc_AdminKilled"] = "Процесс '{0}' (PID {1}) завершён с правами администратора.",
                ["Loc_Killed"] = "Процесс '{0}' (PID {1}) завершён.",
                ["Loc_AccessDenied"] = "Отказано в доступе (PID {0}). Процесс защищён системой."
            },
            ["en"] = new()
            {
                ["Loc_AppTitle"] = "Localhost Manager",
                ["Loc_SearchPlaceholder"] = "Search host or process...",
                ["Loc_Refresh"] = "Refresh",
                ["Loc_TooltipRefresh"] = "Refresh active ports (F5)",
                ["Loc_FilterAll"] = "All",
                ["Loc_FilterDev"] = "Dev Hosts",
                ["Loc_FilterDb"] = "Databases",
                ["Loc_FilterSystem"] = "System",
                ["Loc_ColPort"] = "Port",
                ["Loc_ColCategory"] = "Category",
                ["Loc_ColProcess"] = "Process",
                ["Loc_ColPid"] = "PID",
                ["Loc_ColActions"] = "Actions",
                ["Loc_BtnOpen"] = "Open",
                ["Loc_TooltipOpen"] = "Open host in browser",
                ["Loc_TooltipKill"] = "Kill process",
                ["Loc_EmptyTitle"] = "No active hosts found",
                ["Loc_EmptySubtitle"] = "Start a local server and click 'Refresh'",
                ["Loc_FooterHint"] = "Double-click to open • Right-click for menu • F5 to refresh",
                ["Loc_StatusReady"] = "Ready",
                ["Loc_StatusFound"] = "Found ports: {0} (Dev: {1}, DB: {2}, System: {3}) • {4}",
                ["Loc_StatusOpened"] = "Opened {0} in browser",
                ["Loc_StatusCopiedUrl"] = "URL copied to clipboard",
                ["Loc_StatusCopiedPid"] = "PID copied to clipboard",
                ["Loc_TrayShow"] = "Show Window",
                ["Loc_TrayRefresh"] = "Refresh Ports",
                ["Loc_TrayExit"] = "Exit",
                ["Loc_ModalTitle"] = "Kill System Process",
                ["Loc_ModalSubtitle"] = "Security confirmation required",
                ["Loc_ModalMsg"] = "Process '{0}' (PID {1}, port {2}) is a Windows system service.\n\nTerminating it may affect system stability. Force kill anyway?",
                ["Loc_ModalCancel"] = "Cancel",
                ["Loc_ModalConfirm"] = "Force Kill",
                ["Loc_CatDev"] = "Dev Host",
                ["Loc_CatDb"] = "Database",
                ["Loc_CatSystem"] = "System",
                ["Loc_CatOther"] = "Other",
                ["Loc_MenuOpen"] = "Open in Browser",
                ["Loc_MenuCopyUrl"] = "Copy URL",
                ["Loc_MenuCopyPid"] = "Copy PID",
                ["Loc_MenuOpenFolder"] = "Open File Location",
                ["Loc_MenuKill"] = "Kill Process",
                ["Loc_AdminKilled"] = "Process '{0}' (PID {1}) killed with administrator privileges.",
                ["Loc_Killed"] = "Process '{0}' (PID {1}) terminated.",
                ["Loc_AccessDenied"] = "Access Denied (PID {0}). Protected by system."
            },
            ["es"] = new()
            {
                ["Loc_AppTitle"] = "Localhost Manager",
                ["Loc_SearchPlaceholder"] = "Buscar host o proceso...",
                ["Loc_Refresh"] = "Actualizar",
                ["Loc_TooltipRefresh"] = "Actualizar lista de puertos (F5)",
                ["Loc_FilterAll"] = "Todos",
                ["Loc_FilterDev"] = "Hosts Dev",
                ["Loc_FilterDb"] = "Bases de datos",
                ["Loc_FilterSystem"] = "Sistema",
                ["Loc_ColPort"] = "Puerto",
                ["Loc_ColCategory"] = "Categoría",
                ["Loc_ColProcess"] = "Proceso",
                ["Loc_ColPid"] = "PID",
                ["Loc_ColActions"] = "Acciones",
                ["Loc_BtnOpen"] = "Abrir",
                ["Loc_TooltipOpen"] = "Abrir host en navegador",
                ["Loc_TooltipKill"] = "Terminar proceso",
                ["Loc_EmptyTitle"] = "No se encontraron hosts",
                ["Loc_EmptySubtitle"] = "Inicia un servidor local y pulsa 'Actualizar'",
                ["Loc_FooterHint"] = "Doble clic para abrir • Clic derecho para menú • F5 para actualizar",
                ["Loc_StatusReady"] = "Listo",
                ["Loc_StatusFound"] = "Puertos encontrados: {0} (Dev: {1}, BD: {2}, Sistema: {3}) • {4}",
                ["Loc_StatusOpened"] = "Se abrió {0} en el navegador",
                ["Loc_StatusCopiedUrl"] = "URL copiada al portapapeles",
                ["Loc_StatusCopiedPid"] = "PID copiado al portapapeles",
                ["Loc_TrayShow"] = "Mostrar ventana",
                ["Loc_TrayRefresh"] = "Actualizar puertos",
                ["Loc_TrayExit"] = "Salir",
                ["Loc_ModalTitle"] = "Terminar proceso del sistema",
                ["Loc_ModalSubtitle"] = "Confirmación de seguridad requerida",
                ["Loc_ModalMsg"] = "El proceso '{0}' (PID {1}, puerto {2}) es un servicio del sistema.\n\nTerminarlo puede afectar la estabilidad. ¿Terminar de todos modos?",
                ["Loc_ModalCancel"] = "Cancelar",
                ["Loc_ModalConfirm"] = "Forzar terminación",
                ["Loc_CatDev"] = "Host Dev",
                ["Loc_CatDb"] = "Base de datos",
                ["Loc_CatSystem"] = "Sistema",
                ["Loc_CatOther"] = "Otro",
                ["Loc_MenuOpen"] = "Abrir en navegador",
                ["Loc_MenuCopyUrl"] = "Copiar URL",
                ["Loc_MenuCopyPid"] = "Copiar PID",
                ["Loc_MenuOpenFolder"] = "Abrir ubicación de archivo",
                ["Loc_MenuKill"] = "Terminar proceso",
                ["Loc_AdminKilled"] = "Proceso '{0}' (PID {1}) terminado con permisos de administrador.",
                ["Loc_Killed"] = "Proceso '{0}' (PID {1}) terminado.",
                ["Loc_AccessDenied"] = "Acceso denegado (PID {0}). Protegido por el sistema."
            },
            ["de"] = new()
            {
                ["Loc_AppTitle"] = "Localhost Manager",
                ["Loc_SearchPlaceholder"] = "Host oder Prozess suchen...",
                ["Loc_Refresh"] = "Aktualisieren",
                ["Loc_TooltipRefresh"] = "Portliste aktualisieren (F5)",
                ["Loc_FilterAll"] = "Alle",
                ["Loc_FilterDev"] = "Dev-Hosts",
                ["Loc_FilterDb"] = "Datenbanken",
                ["Loc_FilterSystem"] = "System",
                ["Loc_ColPort"] = "Port",
                ["Loc_ColCategory"] = "Kategorie",
                ["Loc_ColProcess"] = "Prozess",
                ["Loc_ColPid"] = "PID",
                ["Loc_ColActions"] = "Aktionen",
                ["Loc_BtnOpen"] = "Öffnen",
                ["Loc_TooltipOpen"] = "Host im Browser öffnen",
                ["Loc_TooltipKill"] = "Prozess beenden",
                ["Loc_EmptyTitle"] = "Keine Hosts gefunden",
                ["Loc_EmptySubtitle"] = "Starte einen Server und klicke auf 'Aktualisieren'",
                ["Loc_FooterHint"] = "Doppelklick zum Öffnen • Rechtsklick für Menü • F5 zum Aktualisieren",
                ["Loc_StatusReady"] = "Bereit",
                ["Loc_StatusFound"] = "Gefundene Ports: {0} (Dev: {1}, DB: {2}, System: {3}) • {4}",
                ["Loc_StatusOpened"] = "{0} im Browser geöffnet",
                ["Loc_StatusCopiedUrl"] = "URL in Zwischenablage kopiert",
                ["Loc_StatusCopiedPid"] = "PID in Zwischenablage kopiert",
                ["Loc_TrayShow"] = "Fenster anzeigen",
                ["Loc_TrayRefresh"] = "Ports aktualisieren",
                ["Loc_TrayExit"] = "Beenden",
                ["Loc_ModalTitle"] = "Systemprozess beenden",
                ["Loc_ModalSubtitle"] = "Sicherheitsbestätigung erforderlich",
                ["Loc_ModalMsg"] = "Der Prozess '{0}' (PID {1}, Port {2}) ist ein Windows-Systemdienst.\n\nDas Beenden kann die Systemstabilität beeinträchtigen. Trotzdem beenden?",
                ["Loc_ModalCancel"] = "Abbrechen",
                ["Loc_ModalConfirm"] = "Erzwingen",
                ["Loc_CatDev"] = "Dev Host",
                ["Loc_CatDb"] = "Datenbank",
                ["Loc_CatSystem"] = "System",
                ["Loc_CatOther"] = "Sonstige",
                ["Loc_MenuOpen"] = "Im Browser öffnen",
                ["Loc_MenuCopyUrl"] = "URL kopieren",
                ["Loc_MenuCopyPid"] = "PID kopieren",
                ["Loc_MenuOpenFolder"] = "Dateipfad öffnen",
                ["Loc_MenuKill"] = "Prozess beenden",
                ["Loc_AdminKilled"] = "Prozess '{0}' (PID {1}) mit Administratorrechten beendet.",
                ["Loc_Killed"] = "Prozess '{0}' (PID {1}) beendet.",
                ["Loc_AccessDenied"] = "Zugriff verweigert (PID {0}). Vom System geschützt."
            },
            ["fr"] = new()
            {
                ["Loc_AppTitle"] = "Localhost Manager",
                ["Loc_SearchPlaceholder"] = "Rechercher hôte ou processus...",
                ["Loc_Refresh"] = "Actualiser",
                ["Loc_TooltipRefresh"] = "Actualiser la liste des ports (F5)",
                ["Loc_FilterAll"] = "Tous",
                ["Loc_FilterDev"] = "Hôtes Dev",
                ["Loc_FilterDb"] = "Bases de données",
                ["Loc_FilterSystem"] = "Système",
                ["Loc_ColPort"] = "Port",
                ["Loc_ColCategory"] = "Catégorie",
                ["Loc_ColProcess"] = "Processus",
                ["Loc_ColPid"] = "PID",
                ["Loc_ColActions"] = "Actions",
                ["Loc_BtnOpen"] = "Ouvrir",
                ["Loc_TooltipOpen"] = "Ouvrir l'hôte dans le navigateur",
                ["Loc_TooltipKill"] = "Arrêter le processus",
                ["Loc_EmptyTitle"] = "Aucun hôte trouvé",
                ["Loc_EmptySubtitle"] = "Démarrez un serveur local et cliquez sur 'Actualiser'",
                ["Loc_FooterHint"] = "Double-clic pour ouvrir • Clic droit pour menu • F5 pour actualiser",
                ["Loc_StatusReady"] = "Prêt",
                ["Loc_StatusFound"] = "Ports trouvés: {0} (Dev: {1}, BD: {2}, Système: {3}) • {4}",
                ["Loc_StatusOpened"] = "{0} ouvert dans le navigateur",
                ["Loc_StatusCopiedUrl"] = "URL copiée dans le presse-papiers",
                ["Loc_StatusCopiedPid"] = "PID copié dans le presse-papiers",
                ["Loc_TrayShow"] = "Afficher la fenêtre",
                ["Loc_TrayRefresh"] = "Actualiser les ports",
                ["Loc_TrayExit"] = "Quitter",
                ["Loc_ModalTitle"] = "Arrêter le processus système",
                ["Loc_ModalSubtitle"] = "Confirmation de sécurité requise",
                ["Loc_ModalMsg"] = "Le processus '{0}' (PID {1}, port {2}) est un service système Windows.\n\nSon arrêt peut affecter la stabilité. Arrêter quand même?",
                ["Loc_ModalCancel"] = "Annuler",
                ["Loc_ModalConfirm"] = "Forcer l'arrêt",
                ["Loc_CatDev"] = "Hôte Dev",
                ["Loc_CatDb"] = "Base de données",
                ["Loc_CatSystem"] = "Système",
                ["Loc_CatOther"] = "Autre",
                ["Loc_MenuOpen"] = "Ouvrir dans le navigateur",
                ["Loc_MenuCopyUrl"] = "Copier l'URL",
                ["Loc_MenuCopyPid"] = "Copier le PID",
                ["Loc_MenuOpenFolder"] = "Ouvrir l'emplacement du fichier",
                ["Loc_MenuKill"] = "Arrêter le processus",
                ["Loc_AdminKilled"] = "Processus '{0}' (PID {1}) arrêté avec privilèges administrateur.",
                ["Loc_Killed"] = "Processus '{0}' (PID {1}) arrêté.",
                ["Loc_AccessDenied"] = "Accès refusé (PID {0}). Protégé par le système."
            },
            ["zh"] = new()
            {
                ["Loc_AppTitle"] = "Localhost Manager",
                ["Loc_SearchPlaceholder"] = "搜索端口或进程...",
                ["Loc_Refresh"] = "刷新",
                ["Loc_TooltipRefresh"] = "刷新端口列表 (F5)",
                ["Loc_FilterAll"] = "全部",
                ["Loc_FilterDev"] = "开发服务",
                ["Loc_FilterDb"] = "数据库",
                ["Loc_FilterSystem"] = "系统服务",
                ["Loc_ColPort"] = "端口",
                ["Loc_ColCategory"] = "类别",
                ["Loc_ColProcess"] = "进程",
                ["Loc_ColPid"] = "PID",
                ["Loc_ColActions"] = "操作",
                ["Loc_BtnOpen"] = "打开",
                ["Loc_TooltipOpen"] = "在浏览器中打开",
                ["Loc_TooltipKill"] = "结束进程",
                ["Loc_EmptyTitle"] = "未发现活动主机",
                ["Loc_EmptySubtitle"] = "启动本地服务并点击“刷新”",
                ["Loc_FooterHint"] = "双击打开 • 右击打开菜单 • F5 刷新",
                ["Loc_StatusReady"] = "就绪",
                ["Loc_StatusFound"] = "发现端口: {0} (开发: {1}, 数据库: {2}, 系统: {3}) • {4}",
                ["Loc_StatusOpened"] = "已在浏览器中打开 {0}",
                ["Loc_StatusCopiedUrl"] = "URL 已复制到剪贴板",
                ["Loc_StatusCopiedPid"] = "PID 已复制到剪贴板",
                ["Loc_TrayShow"] = "显示窗口",
                ["Loc_TrayRefresh"] = "刷新端口",
                ["Loc_TrayExit"] = "退出",
                ["Loc_ModalTitle"] = "结束系统进程",
                ["Loc_ModalSubtitle"] = "需要安全确认",
                ["Loc_ModalMsg"] = "进程 '{0}' (PID {1}, 端口 {2}) 是 Windows 系统服务。\n\n强行结束可能影响系统稳定性。仍要结束吗？",
                ["Loc_ModalCancel"] = "取消",
                ["Loc_ModalConfirm"] = "强制结束",
                ["Loc_CatDev"] = "开发服务",
                ["Loc_CatDb"] = "数据库",
                ["Loc_CatSystem"] = "系统",
                ["Loc_CatOther"] = "其他",
                ["Loc_MenuOpen"] = "在浏览器中打开",
                ["Loc_MenuCopyUrl"] = "复制 URL",
                ["Loc_MenuCopyPid"] = "复制 PID",
                ["Loc_MenuOpenFolder"] = "打开文件位置",
                ["Loc_MenuKill"] = "结束进程",
                ["Loc_AdminKilled"] = "已通过管理员权限结束进程 '{0}' (PID {1})。",
                ["Loc_Killed"] = "进程 '{0}' (PID {1}) 已终止。",
                ["Loc_AccessDenied"] = "访问被拒绝 (PID {0})。系统关键进程保护。"
            }
        };

        public static void Initialize()
        {
            string lang = LoadLanguagePreference();
            if (string.IsNullOrEmpty(lang))
            {
                var sysLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
                lang = Translations.ContainsKey(sysLang) ? sysLang : "ru";
            }
            SetLanguage(lang);
        }

        public static void SetLanguage(string langCode)
        {
            if (!Translations.ContainsKey(langCode)) langCode = "ru";

            CurrentLanguage = langCode;
            var dict = Translations[langCode];

            foreach (var kvp in dict)
            {
                Application.Current.Resources[kvp.Key] = kvp.Value;
            }

            SaveLanguagePreference(langCode);
            LanguageChanged?.Invoke();
        }

        public static string Get(string key, params object[] args)
        {
            if (!Translations.TryGetValue(CurrentLanguage, out var dict) || !dict.TryGetValue(key, out var val))
            {
                if (Translations["en"].TryGetValue(key, out val))
                {
                    // Fallback to English
                }
                else
                {
                    return key;
                }
            }

            if (args != null && args.Length > 0)
            {
                try { return string.Format(val, args); } catch { return val; }
            }
            return val;
        }

        private static string GetConfigPath() =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lang.cfg");

        private static string LoadLanguagePreference()
        {
            try
            {
                string path = GetConfigPath();
                if (File.Exists(path))
                {
                    string saved = File.ReadAllText(path).Trim();
                    if (Translations.ContainsKey(saved)) return saved;
                }
            }
            catch { }
            return string.Empty;
        }

        private static void SaveLanguagePreference(string langCode)
        {
            try
            {
                File.WriteAllText(GetConfigPath(), langCode);
            }
            catch { }
        }
    }
}
