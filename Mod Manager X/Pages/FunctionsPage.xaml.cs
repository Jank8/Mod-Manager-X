using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Media;
using System;
using System.Text.Json;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Mod_Manager_X.Pages
{
    public sealed partial class FunctionsPage : Page
    {
        public class FunctionInfo
        {
            public string FileName { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public bool Enabled { get; set; }
        }

        private ObservableCollection<FunctionInfo> _functionInfos = new();
        
        // GameBanana Author Update fields
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly Regex[] _authorPatterns = new[]
        {
            new Regex("<a class=\"Uploader[^\"]*\" href=\"[^\"]+\">([^<]+)</a>", RegexOptions.Compiled),
            new Regex("<span class=\"UserName[^\"]*\">([^<]+)</span>", RegexOptions.Compiled),
            new Regex("<a class=\"UserName[^\"]*\" href=\"[^\"]+\">([^<]+)</a>", RegexOptions.Compiled),
            new Regex("<meta name=\"author\" content=\"([^\"]+)\"", RegexOptions.Compiled),
            new Regex("\\\"author\\\":\\\"([^\\\"]+)\\\"", RegexOptions.Compiled)
        };
        private static readonly Regex _jsonLdPattern = new("<script type=\"application/ld\\+json\">(.*?)</script>", RegexOptions.Compiled | RegexOptions.Singleline);
        private static bool _isUpdatingAuthors = false;
        private static CancellationTokenSource? _cancellationTokenSource;

        // Hotkey Finder fields
        private CancellationTokenSource? _hotkeyFinderCancellationTokenSource;
        private bool _isHotkeyProcessing = false;
        private static readonly Regex _hotkeyPattern = new(@"key\s*=\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Status Keeper fields
        private bool _isStatusKeeperProcessing = false;
        private FileSystemWatcher? _d3dxFileWatcher;

        // Win32 API declarations for folder picker
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHBrowseForFolder(ref BROWSEINFO bi);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);

        private const int MAX_PATH = 260;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct BROWSEINFO
        {
            public nint hwndOwner;
            public nint pidlRoot;
            public IntPtr pszDisplayName;
            public string lpszTitle;
            public uint ulFlags;
            public nint lpfn;
            public nint lParam;
            public int iImage;
        }

        public FunctionsPage()
        {
            this.InitializeComponent();
            UpdateTexts();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoadFunctionsList();
            UpdateTexts();
            
            // Initialize SelectorBar to show first function by default
            FunctionsSelectorBar.SelectedItem = GBAuthorUpdateSelectorItem;
            ShowGBAuthorUpdatePanel();
        }

        private void UpdateTexts()
        {
            // Update selector bar item text
            GBAuthorUpdateSelectorText.Text = GetGameBananaFunctionName();
            HotkeyFinderSelectorText.Text = GetHotkeyFinderFunctionName();
            StatusKeeperSelectorText.Text = GetStatusKeeperFunctionName();
            ModInfoBackupSelectorText.Text = GetModInfoBackupFunctionName();
            
            // Update GameBanana Author Update texts
            UpdateAuthorsLabel.Text = GetGBTranslation("UpdateAuthorsLabel");
            SmartUpdateLabel.Text = GetGBTranslation("SmartUpdateLabel");
            SafetyLockLabel.Text = GetGBTranslation("SafetyLockLabel");
            UpdateButtonText.Text = GetGBTranslation("UpdateButton");
            
            // Update Hotkey Finder texts
            AutoDetectLabel.Text = GetHKTranslation("AutoDetectLabel");
            RefreshAllLabel.Text = GetHKTranslation("RefreshAllLabel");
            RefreshButtonText.Text = GetHKTranslation("RefreshButton");
            
            // Update Status Keeper texts
            StatusKeeperSyncSelectorItem.Text = GetSKTranslation("StatusKeeper_Tab_Synchronizacja");
            StatusKeeperBackupSelectorItem.Text = GetSKTranslation("StatusKeeper_Tab_KopiaZapasowa");
            StatusKeeperLogsSelectorItem.Text = GetSKTranslation("StatusKeeper_Tab_Logi");
            
            // Update Status Keeper Sync texts
            D3dxFilePathLabel.Text = GetSKTranslation("StatusKeeper_D3dxFilePath_Label");
            BackupConfirmationLabel.Text = GetSKTranslation("StatusKeeper_BackupConfirmation_Label");
            DynamicSyncLabel.Text = GetSKTranslation("StatusKeeper_DynamicSync_Label");
            ManualSyncLabel.Text = GetSKTranslation("StatusKeeper_ManualSync_Label");
            ManualSyncButtonText.Text = GetSKTranslation("StatusKeeper_ManualSync_Button");
            
            // Update Status Keeper Backup texts
            CreateBackupLabel.Text = GetSKTranslation("StatusKeeper_CreateBackup_Label");
            CreateBackupButtonText.Text = GetSKTranslation("StatusKeeper_CreateBackup_Button");
            CheckBackupButtonText.Text = GetSKTranslation("StatusKeeper_CheckBackups_Button");
            SafetyOverrideLabel.Text = GetSKTranslation("StatusKeeper_SafetyOverride_Label");
            RestoreBackupLabel.Text = GetSKTranslation("StatusKeeper_RestoreBackup_Label");
            RestoreBackupButtonText.Text = GetSKTranslation("StatusKeeper_RestoreBackup_Button");
            DeleteBackupsLabel.Text = GetSKTranslation("StatusKeeper_DeleteBackups_Label");
            DeleteBackupsButtonText.Text = GetSKTranslation("StatusKeeper_DeleteBackups_Button");
            
            // Update Status Keeper Logs texts
            LoggingLabel.Text = GetSKTranslation("StatusKeeper_Logging_Label");
            OpenLogButtonText.Text = GetSKTranslation("StatusKeeper_OpenLog_Button");
            ClearLogButtonText.Text = GetSKTranslation("StatusKeeper_ClearLog_Button");
            
            // Update Mod Info Backup texts
            CreateBackupsText.Text = GetMIBTranslation("ModInfoBackup_BackupAll");
            RestoreBackup1Text.Text = GetMIBTranslation("ModInfoBackup_Restore1");
            RestoreBackup2Text.Text = GetMIBTranslation("ModInfoBackup_Restore2");
            RestoreBackup3Text.Text = GetMIBTranslation("ModInfoBackup_Restore3");
        }

        // Helper methods for getting translations from different language files
        private string GetGBTranslation(string key)
        {
            var langFile = SettingsManager.Current.LanguageFile ?? "en.json";
            var langPath = Path.Combine(System.AppContext.BaseDirectory, "Language", "GBAuthorUpdate", langFile);
            if (!File.Exists(langPath))
                langPath = Path.Combine(System.AppContext.BaseDirectory, "Language", "GBAuthorUpdate", "en.json");
            
            if (File.Exists(langPath))
            {
                try
                {
                    var json = File.ReadAllText(langPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict != null && dict.TryGetValue(key, out var value))
                        return value;
                }
                catch { }
            }
            return key;
        }

        private string GetHKTranslation(string key)
        {
            var langFile = SettingsManager.Current.LanguageFile ?? "en.json";
            var langPath = Path.Combine(System.AppContext.BaseDirectory, "Language", "HotkeyFinder", langFile);
            if (!File.Exists(langPath))
                langPath = Path.Combine(System.AppContext.BaseDirectory, "Language", "HotkeyFinder", "en.json");
            
            if (File.Exists(langPath))
            {
                try
                {
                    var json = File.ReadAllText(langPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict != null && dict.TryGetValue(key, out var value))
                        return value;
                }
                catch { }
            }
            return key;
        }

        private string GetSKTranslation(string key)
        {
            var langFile = SettingsManager.Current.LanguageFile ?? "en.json";
            var langPath = Path.Combine(System.AppContext.BaseDirectory, "Language", "StatusKeeper", langFile);
            if (!File.Exists(langPath))
                langPath = Path.Combine(System.AppContext.BaseDirectory, "Language", "StatusKeeper", "en.json");
            
            if (File.Exists(langPath))
            {
                try
                {
                    var json = File.ReadAllText(langPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict != null && dict.TryGetValue(key, out var value))
                        return value;
                }
                catch { }
            }
            return key;
        }

        private string GetMIBTranslation(string key)
        {
            var langFile = SettingsManager.Current.LanguageFile ?? "en.json";
            var langPath = Path.Combine(System.AppContext.BaseDirectory, "Language", "ModInfoBackup", langFile);
            if (!File.Exists(langPath))
                langPath = Path.Combine(System.AppContext.BaseDirectory, "Language", "ModInfoBackup", "en.json");
            
            if (File.Exists(langPath))
            {
                try
                {
                    var json = File.ReadAllText(langPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict != null && dict.TryGetValue(key, out var value))
                        return value;
                }
                catch { }
            }
            return key;
        }

        private void SaveFunctionSettings(FunctionInfo function)
        {
            string settingsDir = Path.Combine(System.AppContext.BaseDirectory, "Settings", "Functions");
            if (!Directory.Exists(settingsDir))
                Directory.CreateDirectory(settingsDir);
            string jsonPath = Path.Combine(settingsDir, function.FileName + ".json");
            var json = JsonSerializer.Serialize(new { function.Name, function.Enabled });
            File.WriteAllText(jsonPath, json);
        }

        private void LoadFunctionsList()
        {
            _functionInfos.Clear();
            string settingsDir = Path.Combine(System.AppContext.BaseDirectory, "Settings", "Functions");

            // Add GameBanana author update function
            var gbAuthorUpdateFunction = new FunctionInfo
            {
                FileName = "GBAuthorUpdate",
                Name = GetGameBananaFunctionName(),
                Enabled = true
            };
            string gbJsonPath = Path.Combine(settingsDir, gbAuthorUpdateFunction.FileName + ".json");
            if (File.Exists(gbJsonPath))
            {
                try
                {
                    var json = File.ReadAllText(gbJsonPath);
                    var loaded = JsonSerializer.Deserialize<FunctionInfo>(json);
                    if (loaded != null)
                    {
                        gbAuthorUpdateFunction.Enabled = loaded.Enabled;
                        gbAuthorUpdateFunction.Name = loaded.Name;
                    }
                }
                catch { }
            }
            _functionInfos.Add(gbAuthorUpdateFunction);

            // Add Hotkey Finder function
            var hotkeyFinderFunction = new FunctionInfo
            {
                FileName = "HotkeyFinder",
                Name = GetHotkeyFinderFunctionName(),
                Enabled = true
            };
            string hkJsonPath = Path.Combine(settingsDir, hotkeyFinderFunction.FileName + ".json");
            if (File.Exists(hkJsonPath))
            {
                try
                {
                    var json = File.ReadAllText(hkJsonPath);
                    var loaded = JsonSerializer.Deserialize<FunctionInfo>(json);
                    if (loaded != null)
                    {
                        hotkeyFinderFunction.Enabled = loaded.Enabled;
                    }
                }
                catch { /* Settings loading failed - use defaults */ }
            }
            _functionInfos.Add(hotkeyFinderFunction);

            // Add StatusKeeper function
            var statusKeeperFunction = new FunctionInfo
            {
                FileName = "StatusKeeperPage",
                Name = GetStatusKeeperFunctionName(),
                Enabled = true
            };
            string skJsonPath = Path.Combine(settingsDir, statusKeeperFunction.FileName + ".json");
            if (File.Exists(skJsonPath))
            {
                try
                {
                    var json = File.ReadAllText(skJsonPath);
                    var loaded = JsonSerializer.Deserialize<FunctionInfo>(json);
                    if (loaded != null)
                    {
                        statusKeeperFunction.Enabled = loaded.Enabled;
                        statusKeeperFunction.Name = loaded.Name;
                    }
                }
                catch { }
            }
            _functionInfos.Add(statusKeeperFunction);

            // Add ModInfoBackup function
            var modInfoBackupFunction = new FunctionInfo
            {
                FileName = "ModInfoBackup",
                Name = GetModInfoBackupFunctionName(),
                Enabled = true
            };
            string mibJsonPath = Path.Combine(settingsDir, modInfoBackupFunction.FileName + ".json");
            if (File.Exists(mibJsonPath))
            {
                try
                {
                    var json = File.ReadAllText(mibJsonPath);
                    var loaded = JsonSerializer.Deserialize<FunctionInfo>(json);
                    if (loaded != null)
                    {
                        modInfoBackupFunction.Enabled = loaded.Enabled;
                        modInfoBackupFunction.Name = loaded.Name;
                    }
                }
                catch { }
            }
            _functionInfos.Add(modInfoBackupFunction);
        }

        private void GameSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle the game selection ComboBox (inside content area)
            // For now, this does nothing as requested
        }

        private void FunctionsSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            if (sender.SelectedItem is SelectorBarItem item && item.Tag is string section)
            {
                // Hide all panels first
                GameSelectorPanel.Visibility = Visibility.Collapsed;
                GBAuthorUpdatePanel.Visibility = Visibility.Collapsed;
                HotkeyFinderPanel.Visibility = Visibility.Collapsed;
                StatusKeeperPanel.Visibility = Visibility.Collapsed;
                ModInfoBackupPanel.Visibility = Visibility.Collapsed;
                
                // Show the selected panel
                switch (section)
                {
                    case "GBAuthorUpdate":
                        ShowGBAuthorUpdatePanel();
                        break;
                    case "HotkeyFinder":
                        ShowHotkeyFinderPanel();
                        break;
                    case "StatusKeeperPage":
                        ShowStatusKeeperPanel();
                        break;
                    case "ModInfoBackup":
                        ShowModInfoBackupPanel();
                        break;
                }
            }
        }

        private void ShowGBAuthorUpdatePanel()
        {
            GBAuthorUpdatePanel.Visibility = Visibility.Visible;
            // Initialize GameBanana Author Update UI
            LoadGBAuthorUpdateSettings();
        }

        private void ShowHotkeyFinderPanel()
        {
            HotkeyFinderPanel.Visibility = Visibility.Visible;
            LoadHotkeyFinderSettings();
        }

        private void ShowStatusKeeperPanel()
        {
            StatusKeeperPanel.Visibility = Visibility.Visible;
            LoadStatusKeeperSettings();
        }

        private void ShowModInfoBackupPanel()
        {
            ModInfoBackupPanel.Visibility = Visibility.Visible;
            LoadModInfoBackupSettings();
        }

        private void LoadHotkeyFinderSettings()
        {
            try
            {
                var settingsPath = Path.Combine(AppContext.BaseDirectory, "Settings", "Functions", "HotkeyFinder.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    if (settings != null)
                    {
                        if (settings.TryGetValue("AutoDetectEnabled", out var autoDetect) && autoDetect is JsonElement autoElement)
                            AutoDetectToggle.IsOn = autoElement.GetBoolean();
                        else
                            AutoDetectToggle.IsOn = true; // Default: enabled
                            
                        if (settings.TryGetValue("RefreshAllEnabled", out var refreshAll) && refreshAll is JsonElement refreshElement)
                            RefreshAllToggle.IsOn = refreshElement.GetBoolean();
                        else
                            RefreshAllToggle.IsOn = false; // Default: disabled
                    }
                }
                else
                {
                    // Default values when no settings file exists
                    AutoDetectToggle.IsOn = true;  // Auto detect enabled by default
                    RefreshAllToggle.IsOn = false; // Refresh all disabled by default
                }
            }
            catch
            {
                // Default values on error
                AutoDetectToggle.IsOn = true;  // Auto detect enabled by default
                RefreshAllToggle.IsOn = false; // Refresh all disabled by default
            }
            
            // Update text labels
            UpdateTexts();
        }

        private void LoadStatusKeeperSettings()
        {
            // TODO: Load actual settings from StatusKeeperPage business logic
            
            // Initialize the internal SelectorBar to show first tab
            StatusKeeperSyncSelectorItem.IsSelected = true;
            StatusKeeperSyncPanel.Visibility = Visibility.Visible;
            StatusKeeperBackupPanel.Visibility = Visibility.Collapsed;
            StatusKeeperLogsPanel.Visibility = Visibility.Collapsed;
            
            // Initialize Sync toggle states (placeholder values for now)
            BackupConfirmationToggle.IsOn = false;
            DynamicSyncToggle.IsOn = false;
            
            // Initialize breadcrumb (placeholder path)
            SetBreadcrumbBar(D3dxFilePathBreadcrumb, "XXMI/ZZMI/Mods");
            
            // Initialize Backup toggle states (placeholder values for now)
            SafetyOverride1Toggle.IsOn = false;
            SafetyOverride2Toggle.IsOn = false;
            SafetyOverride3Toggle.IsOn = false;
            
            // Initialize Logs toggle states (placeholder values for now)
            LoggingToggle.IsOn = false;
            LogsTextBlock.Text = GetSKTranslation("StatusKeeper_Log_Empty");
            
            // Update text labels
            UpdateTexts();
        }

        private void LoadModInfoBackupSettings()
        {
            // Update backup info from actual backup files
            UpdateModInfoBackupInfo();
            
            // Update text labels
            UpdateTexts();
        }

        private void LoadGBAuthorUpdateSettings()
        {
            try
            {
                var settingsPath = Path.Combine(AppContext.BaseDirectory, "Settings", "Functions", "GBAuthorUpdate.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    if (settings != null)
                    {
                        if (settings.TryGetValue("UpdateAuthorsEnabled", out var updateAuthors) && updateAuthors is JsonElement updateElement)
                            UpdateAuthorsSwitch.IsOn = updateElement.GetBoolean();
                        if (settings.TryGetValue("SmartUpdateEnabled", out var smartUpdate) && smartUpdate is JsonElement smartElement)
                            SmartUpdateSwitch.IsOn = smartElement.GetBoolean();
                        if (settings.TryGetValue("SafetyLockEnabled", out var safetyLock) && safetyLock is JsonElement safetyElement)
                            SafetyLockSwitch.IsOn = safetyElement.GetBoolean();
                    }
                }
                else
                {
                    // Default values
                    UpdateAuthorsSwitch.IsOn = true;
                    SmartUpdateSwitch.IsOn = false;
                    SafetyLockSwitch.IsOn = false;
                }
            }
            catch
            {
                // Default values on error
                UpdateAuthorsSwitch.IsOn = true;
                SmartUpdateSwitch.IsOn = false;
                SafetyLockSwitch.IsOn = false;
            }
            
            // Update all text labels
            UpdateTexts();
        }

        // Removed old navigation methods - no longer needed with SelectorBar

        private void FunctionToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggle && toggle.DataContext is FunctionInfo function)
            {
                function.Enabled = toggle.IsOn;
                SaveFunctionSettings(function);
            }
        }

        private string GetGameBananaFunctionName()
        {
            // Get translation from GBAuthorUpdate language file
            var langFile = Mod_Manager_X.SettingsManager.Current.LanguageFile ?? "en.json";
            var langPath = Path.Combine(System.AppContext.BaseDirectory, "Language", "GBAuthorUpdate", langFile);
            if (!File.Exists(langPath))
                langPath = Path.Combine(System.AppContext.BaseDirectory, "Language", "GBAuthorUpdate", "en.json");
            
            if (File.Exists(langPath))
            {
                try
                {
                    var json = File.ReadAllText(langPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict != null && dict.TryGetValue("Title", out var gbTitle))
                    {
                        return gbTitle;
                    }
                }
                catch { /* Language file parsing failed - use fallback */ }
            }
            
            // Fallback to main language manager
            return LanguageManager.Instance.T("GameBananaAuthorUpdate_Function");
        }

        private string GetHotkeyFinderFunctionName()
        {
            // Get translation from HotkeyFinder language file
            var langFile = Mod_Manager_X.SettingsManager.Current.LanguageFile ?? "en.json";
            var langPath = Path.Combine(System.AppContext.BaseDirectory, "Language", "HotkeyFinder", langFile);
            if (!File.Exists(langPath))
                langPath = Path.Combine(System.AppContext.BaseDirectory, "Language", "HotkeyFinder", "en.json");
            
            if (File.Exists(langPath))
            {
                try
                {
                    var json = File.ReadAllText(langPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict != null && dict.TryGetValue("Title", out var hkName))
                    {
                        return hkName;
                    }
                }
                catch { /* Language file parsing failed - use fallback */ }
            }
            
            // Fallback to default
            return "Hotkey Finder";
        }

        private string GetStatusKeeperFunctionName()
        {
            // Get translation from StatusKeeper language file
            var langFile = Mod_Manager_X.SettingsManager.Current.LanguageFile ?? "en.json";
            var langPath = Path.Combine(System.AppContext.BaseDirectory, "Language", "StatusKeeper", langFile);
            if (!File.Exists(langPath))
                langPath = Path.Combine(System.AppContext.BaseDirectory, "Language", "StatusKeeper", "en.json");
            if (File.Exists(langPath))
            {
                try
                {
                    var json = File.ReadAllText(langPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict != null && dict.TryGetValue("Title", out var skName))
                    {
                        return skName;
                    }
                }
                catch { /* Language file parsing failed - use fallback */ }
            }
            // Fallback to default
            return "Status Keeper";
        }
        
        private string GetModInfoBackupFunctionName()
        {
            // Get translation from ModInfoBackup language file
            var langFile = Mod_Manager_X.SettingsManager.Current.LanguageFile ?? "en.json";
            var langPath = Path.Combine(System.AppContext.BaseDirectory, "Language", "ModInfoBackup", langFile);
            if (!File.Exists(langPath))
                langPath = Path.Combine(System.AppContext.BaseDirectory, "Language", "ModInfoBackup", "en.json");
            
            if (File.Exists(langPath))
            {
                try
                {
                    var json = File.ReadAllText(langPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict != null && dict.TryGetValue("Title", out var mibName))
                    {
                        return mibName;
                    }
                }
                catch { /* Language file parsing failed - use fallback */ }
            }
            
            // Fallback to default
            return "ModInfo Backup";
        }

        // Event handlers for GameBanana Author Update
        private void UpdateAuthorsSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            SaveGBAuthorUpdateSettings();
        }

        private void SmartUpdateSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            SaveGBAuthorUpdateSettings();
        }

        private void SafetyLockSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            SaveGBAuthorUpdateSettings();
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingAuthors)
            {
                // Cancel operation
                _cancellationTokenSource?.Cancel();
                return;
            }

            // Check safety lock
            if (!SafetyLockSwitch.IsOn)
            {
                var dialog = new ContentDialog
                {
                    Title = GetGBTranslation("SafetyLockTitle"),
                    Content = GetGBTranslation("SafetyLockContent"),
                    CloseButtonText = GetGBTranslation("OK"),
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            await StartAuthorUpdate();
        }

        // Event handlers for Hotkey Finder
        private void AutoDetectToggle_Toggled(object sender, RoutedEventArgs e)
        {
            SaveHotkeyFinderSettings();
        }

        private void RefreshAllToggle_Toggled(object sender, RoutedEventArgs e)
        {
            SaveHotkeyFinderSettings();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isHotkeyProcessing)
            {
                // Cancel operation
                _hotkeyFinderCancellationTokenSource?.Cancel();
                return;
            }

            if (!RefreshAllToggle.IsOn)
            {
                var dialog = new ContentDialog
                {
                    Title = GetHKTranslation("Warning"),
                    Content = GetHKTranslation("EnableConfirmFirst"),
                    CloseButtonText = GetHKTranslation("OK"),
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            await StartHotkeyRefresh();
        }

        // Event handlers for Status Keeper
        private void StatusKeeperSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            if (sender.SelectedItem is SelectorBarItem item)
            {
                // Hide all Status Keeper sub-panels
                StatusKeeperSyncPanel.Visibility = Visibility.Collapsed;
                StatusKeeperBackupPanel.Visibility = Visibility.Collapsed;
                StatusKeeperLogsPanel.Visibility = Visibility.Collapsed;
                
                // Show the selected sub-panel
                if (item == StatusKeeperSyncSelectorItem)
                {
                    StatusKeeperSyncPanel.Visibility = Visibility.Visible;
                }
                else if (item == StatusKeeperBackupSelectorItem)
                {
                    StatusKeeperBackupPanel.Visibility = Visibility.Visible;
                }
                else if (item == StatusKeeperLogsSelectorItem)
                {
                    StatusKeeperLogsPanel.Visibility = Visibility.Visible;
                }
            }
        }

        // Status Keeper Sync event handlers
        private async void D3dxFilePathPickButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle((App.Current as App)?.MainWindow);
                var folderPath = await PickFolderWin32DialogSTA(hwnd);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    var iniPath = Path.Combine(folderPath, "d3dx_user.ini");
                    if (File.Exists(iniPath))
                    {
                        SetBreadcrumbBar(D3dxFilePathBreadcrumb, iniPath);
                        SaveStatusKeeperSettings();

                        // Restart watcher if dynamic sync is enabled
                        if (DynamicSyncToggle.IsOn)
                        {
                            SetupFileWatcher();
                        }
                    }
                    else
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "Error",
                            Content = "d3dx_user.ini not found in the selected directory.",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await dialog.ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowStatusKeeperDialog("Error", ex.Message);
            }
        }

        private void D3dxFilePathDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            var defaultPath = Path.Combine(SettingsManager.Current.XXMIModsDirectory ?? "XXMI/ZZMI/Mods");
            SetBreadcrumbBar(D3dxFilePathBreadcrumb, defaultPath);
            SaveStatusKeeperSettings();
        }

        private void BackupConfirmationToggle_Toggled(object sender, RoutedEventArgs e)
        {
            SaveStatusKeeperSettings();
        }

        private void DynamicSyncToggle_Toggled(object sender, RoutedEventArgs e)
        {
            SaveStatusKeeperSettings();
            SetupFileWatcher();
        }

        private async void ManualSyncButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isStatusKeeperProcessing)
                return;

            _isStatusKeeperProcessing = true;
            ManualSyncProgressBar.Visibility = Visibility.Visible;
            ManualSyncButton.IsEnabled = false;
            
            try
            {
                await PerformStatusKeeperSync();
                await ShowStatusKeeperDialog("Success", GetSKTranslation("StatusKeeper_ManualSync_Complete_Title"));
            }
            catch (Exception ex)
            {
                await ShowStatusKeeperDialog("Error", ex.Message);
            }
            finally
            {
                _isStatusKeeperProcessing = false;
                ManualSyncProgressBar.Visibility = Visibility.Collapsed;
                ManualSyncButton.IsEnabled = true;
            }
        }

        // Status Keeper Backup event handlers
        private async void CreateBackupButton_Click(object sender, RoutedEventArgs e)
        {
            CreateBackupProgressBar.Visibility = Visibility.Visible;
            CreateBackupButton.IsEnabled = false;
            
            try
            {
                var result = await CreateStatusKeeperBackup();
                await ShowStatusKeeperDialog("Success", string.Format(GetSKTranslation("StatusKeeper_CreateBackup_Dialog_Message"), result.newBackups, result.totalBackups));
            }
            catch (Exception ex)
            {
                await ShowStatusKeeperDialog("Error", ex.Message);
            }
            finally
            {
                CreateBackupProgressBar.Visibility = Visibility.Collapsed;
                CreateBackupButton.IsEnabled = true;
            }
        }

        private async void CheckBackupButton_Click(object sender, RoutedEventArgs e)
        {
            CheckBackupProgressBar.Visibility = Visibility.Visible;
            CheckBackupButton.IsEnabled = false;
            
            try
            {
                var result = await CheckStatusKeeperBackup();
                await ShowStatusKeeperDialog("Info", string.Format(GetSKTranslation("StatusKeeper_CheckBackup_Dialog_Message"), result.iniFiles, result.backups, result.missing));
            }
            catch (Exception ex)
            {
                await ShowStatusKeeperDialog("Error", ex.Message);
            }
            finally
            {
                CheckBackupProgressBar.Visibility = Visibility.Collapsed;
                CheckBackupButton.IsEnabled = true;
            }
        }

        private void SafetyOverride1Toggle_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateSafetyOverrideButtons();
        }

        private void SafetyOverride2Toggle_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateSafetyOverrideButtons();
        }

        private void SafetyOverride3Toggle_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateSafetyOverrideButtons();
        }

        private async void StatusKeeperRestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (!AllSafetyOverridesEnabled())
            {
                await ShowStatusKeeperDialog("Warning", GetSKTranslation("BackupOverride_Warning_Content"));
                return;
            }

            try
            {
                var result = await RestoreStatusKeeperBackup();
                await ShowStatusKeeperDialog("Success", string.Format(GetSKTranslation("StatusKeeper_RestoreBackup_Dialog_Message"), result.restored, result.failed));
            }
            catch (Exception ex)
            {
                await ShowStatusKeeperDialog("Error", ex.Message);
            }
        }

        private async void DeleteBackupsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!AllSafetyOverridesEnabled())
            {
                await ShowStatusKeeperDialog("Warning", GetSKTranslation("BackupOverride_Warning_Content"));
                return;
            }

            try
            {
                var deletedCount = await DeleteStatusKeeperBackups();
                await ShowStatusKeeperDialog("Success", string.Format(GetSKTranslation("StatusKeeper_DeleteBackups_Dialog_Message"), deletedCount));
            }
            catch (Exception ex)
            {
                await ShowStatusKeeperDialog("Error", ex.Message);
            }
        }

        // Status Keeper Logs event handlers
        private void LoggingToggle_Toggled(object sender, RoutedEventArgs e)
        {
            SaveStatusKeeperSettings();
        }

        private async void OpenLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "Logs", "StatusKeeper.log");
                if (File.Exists(logPath))
                {
                    await Windows.System.Launcher.LaunchUriAsync(new Uri($"file:///{logPath}"));
                }
                else
                {
                    await ShowStatusKeeperDialog(GetSKTranslation("StatusKeeper_LogNotFound_Title"), GetSKTranslation("StatusKeeper_LogNotFound_Message"));
                }
            }
            catch (Exception ex)
            {
                await ShowStatusKeeperDialog("Error", ex.Message);
            }
        }

        private async void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "Logs", "StatusKeeper.log");
                if (File.Exists(logPath))
                {
                    File.WriteAllText(logPath, "");
                    LogsTextBlock.Text = GetSKTranslation("StatusKeeper_Log_Empty");
                    await ShowStatusKeeperDialog(GetSKTranslation("StatusKeeper_Success"), GetSKTranslation("StatusKeeper_LogCleared_Success"));
                }
                else
                {
                    await ShowStatusKeeperDialog(GetSKTranslation("StatusKeeper_Info"), GetSKTranslation("StatusKeeper_LogNotFound_Clear"));
                }
            }
            catch (Exception ex)
            {
                await ShowStatusKeeperDialog("Error", ex.Message);
            }
        }

        // Event handlers for Mod Info Backup
        private async void CreateBackupsButton_Click(object sender, RoutedEventArgs e)
        {
            CreateBackupsProgressBar.Visibility = Visibility.Visible;
            CreateBackupsButton.IsEnabled = false;
            
            try
            {
                var modLibraryPath = SettingsManager.Current.ModLibraryDirectory ?? Path.Combine(AppContext.BaseDirectory, "ModLibrary");
                if (!Directory.Exists(modLibraryPath))
                {
                    await ShowModInfoBackupDialog("Error", "Mod library directory not found.");
                    return;
                }

                var backupCount = await CreateModInfoBackups(modLibraryPath);
                await ShowModInfoBackupDialog("Success", string.Format(GetMIBTranslation("ModInfoBackup_BackupComplete"), backupCount));
                UpdateModInfoBackupInfo();
            }
            catch (Exception ex)
            {
                await ShowModInfoBackupDialog("Error", ex.Message);
            }
            finally
            {
                CreateBackupsProgressBar.Visibility = Visibility.Collapsed;
                CreateBackupsButton.IsEnabled = true;
            }
        }

        private async void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string backupSet)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = GetMIBTranslation("ModInfoBackup_RestoreConfirm_Title"),
                    Content = string.Format(GetMIBTranslation("ModInfoBackup_RestoreConfirm_Message"), backupSet),
                    PrimaryButtonText = GetMIBTranslation("Yes"),
                    CloseButtonText = GetMIBTranslation("No"),
                    XamlRoot = this.XamlRoot
                };

                if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    try
                    {
                        var restoredCount = await RestoreModInfoBackups(backupSet);
                        await ShowModInfoBackupDialog("Success", string.Format(GetMIBTranslation("ModInfoBackup_RestoreComplete"), restoredCount, backupSet));
                    }
                    catch (Exception ex)
                    {
                        await ShowModInfoBackupDialog("Error", ex.Message);
                    }
                }
            }
        }

        private async void DeleteBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string backupSet)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = GetMIBTranslation("ModInfoBackup_DeleteConfirm_Title"),
                    Content = string.Format(GetMIBTranslation("ModInfoBackup_DeleteConfirm_Message"), backupSet),
                    PrimaryButtonText = GetMIBTranslation("Yes"),
                    CloseButtonText = GetMIBTranslation("No"),
                    XamlRoot = this.XamlRoot
                };

                if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    try
                    {
                        var deletedCount = await DeleteModInfoBackups(backupSet);
                        await ShowModInfoBackupDialog("Success", string.Format(GetMIBTranslation("ModInfoBackup_DeleteComplete"), backupSet, deletedCount));
                        UpdateModInfoBackupInfo();
                    }
                    catch (Exception ex)
                    {
                        await ShowModInfoBackupDialog("Error", ex.Message);
                    }
                }
            }
        }

        // Helper method for setting breadcrumb bar
        private void SetBreadcrumbBar(BreadcrumbBar bar, string path)
        {
            var items = new List<object>();
            // Default path: only dot or empty string
            if (path == "." || string.IsNullOrWhiteSpace(path))
            {
                items.Add(new FontIcon { Glyph = "\uE80F" });
            }
            else
            {
                items.Add(new FontIcon { Glyph = "\uE80F" });
                var segments = path.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var seg in segments)
                    items.Add(seg);
            }
            bar.ItemsSource = items;
        }

        // GameBanana Author Update Logic
        private void SaveGBAuthorUpdateSettings()
        {
            try
            {
                var settings = new
                {
                    UpdateAuthorsEnabled = UpdateAuthorsSwitch.IsOn,
                    SmartUpdateEnabled = SmartUpdateSwitch.IsOn,
                    SafetyLockEnabled = SafetyLockSwitch.IsOn
                };
                
                var settingsDir = Path.Combine(AppContext.BaseDirectory, "Settings", "Functions");
                if (!Directory.Exists(settingsDir))
                    Directory.CreateDirectory(settingsDir);
                
                var settingsPath = Path.Combine(settingsDir, "GBAuthorUpdate.json");
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch { }
        }
        private async Task StartAuthorUpdate()
        {
            _isUpdatingAuthors = true;
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Update UI
            UpdateButtonText.Text = GetGBTranslation("CancelButton");
            UpdateIcon.Visibility = Visibility.Collapsed;
            CancelIcon.Visibility = Visibility.Visible;
            UpdateProgressBar.Visibility = Visibility.Visible;
            
            try
            {
                var modLibraryPath = SettingsManager.Current.ModLibraryDirectory ?? Path.Combine(AppContext.BaseDirectory, "ModLibrary");
                if (!Directory.Exists(modLibraryPath))
                {
                    await ShowErrorDialog("Error", "Mod library directory not found.");
                    return;
                }

                var modDirs = Directory.GetDirectories(modLibraryPath);
                int successCount = 0;
                var skippedMods = new List<string>();
                var errors = new List<string>();

                foreach (var modDir in modDirs)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    var modJsonPath = Path.Combine(modDir, "mod.json");
                    if (!File.Exists(modJsonPath))
                    {
                        skippedMods.Add($"{Path.GetFileName(modDir)}: {GetGBTranslation("NoModJson")}");
                        continue;
                    }

                    try
                    {
                        var jsonContent = await File.ReadAllTextAsync(modJsonPath);
                        var modData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
                        
                        if (modData == null) continue;

                        // Check if we should update (smart update logic)
                        if (SmartUpdateSwitch.IsOn && modData.ContainsKey("author") && modData["author"] != null && !string.IsNullOrWhiteSpace(modData["author"].ToString()))
                        {
                            continue; // Skip if author already exists and smart update is enabled
                        }

                        // Get URL from mod.json
                        if (!modData.ContainsKey("url") || modData["url"] == null || string.IsNullOrWhiteSpace(modData["url"].ToString()))
                        {
                            skippedMods.Add($"{Path.GetFileName(modDir)}: {GetGBTranslation("InvalidUrl")}");
                            continue;
                        }

                        var url = modData["url"].ToString();
                        if (string.IsNullOrEmpty(url) || !IsGameBananaUrl(url))
                        {
                            skippedMods.Add($"{Path.GetFileName(modDir)}: {GetGBTranslation("InvalidUrl")}");
                            continue;
                        }

                        // Fetch author from GameBanana
                        var author = await FetchAuthorFromGameBanana(url, _cancellationTokenSource.Token);
                        if (!string.IsNullOrWhiteSpace(author))
                        {
                            modData["author"] = author;
                            var updatedJson = JsonSerializer.Serialize(modData, new JsonSerializerOptions { WriteIndented = true });
                            await File.WriteAllTextAsync(modJsonPath, updatedJson);
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"{Path.GetFileName(modDir)}: {GetGBTranslation("AuthorFetchError")}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{Path.GetFileName(modDir)}: {ex.Message}");
                    }
                }

                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await ShowSummaryDialog(successCount, skippedMods, errors, modDirs.Length);
                }
                else
                {
                    await ShowErrorDialog(GetGBTranslation("CancelledTitle"), GetGBTranslation("CancelledContent"));
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog(GetGBTranslation("ErrorTitle"), ex.Message);
            }
            finally
            {
                _isUpdatingAuthors = false;
                UpdateButtonText.Text = GetGBTranslation("UpdateButton");
                UpdateIcon.Visibility = Visibility.Visible;
                CancelIcon.Visibility = Visibility.Collapsed;
                UpdateProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private bool IsGameBananaUrl(string url)
        {
            return url.Contains("gamebanana.com", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string?> FetchAuthorFromGameBanana(string url, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.GetStringAsync(url, cancellationToken);
                
                // Try different patterns to extract author
                foreach (var pattern in _authorPatterns)
                {
                    var match = pattern.Match(response);
                    if (match.Success)
                    {
                        return System.Net.WebUtility.HtmlDecode(match.Groups[1].Value.Trim());
                    }
                }

                // Try JSON-LD pattern
                var jsonLdMatch = _jsonLdPattern.Match(response);
                if (jsonLdMatch.Success)
                {
                    try
                    {
                        var jsonLd = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonLdMatch.Groups[1].Value);
                        if (jsonLd?.ContainsKey("author") == true)
                        {
                            var authorObj = jsonLd["author"];
                            if (authorObj is JsonElement element && element.ValueKind == JsonValueKind.Object)
                            {
                                if (element.TryGetProperty("name", out var nameElement))
                                {
                                    return nameElement.GetString();
                                }
                            }
                        }
                    }
                    catch { }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task ShowSummaryDialog(int successCount, List<string> skippedMods, List<string> errors, int totalChecked)
        {
            var content = $"{GetGBTranslation("TotalChecked")}: {totalChecked}\n";
            content += $"{GetGBTranslation("SuccessCount")}: {successCount}\n\n";
            
            if (skippedMods.Any())
            {
                content += $"{GetGBTranslation("SkippedMods")}\n";
                content += string.Join("\n", skippedMods.Take(10));
                if (skippedMods.Count > 10) content += $"\n... and {skippedMods.Count - 10} more";
                content += "\n\n";
            }
            
            if (errors.Any())
            {
                content += $"{GetGBTranslation("Errors")}\n";
                content += string.Join("\n", errors.Take(10));
                if (errors.Count > 10) content += $"\n... and {errors.Count - 10} more";
            }

            var dialog = new ContentDialog
            {
                Title = GetGBTranslation("SummaryTitle"),
                Content = new ScrollViewer 
                { 
                    Content = new TextBlock { Text = content, TextWrapping = TextWrapping.Wrap },
                    MaxHeight = 400
                },
                CloseButtonText = GetGBTranslation("OK"),
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = GetGBTranslation("OK"),
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        // Hotkey Finder Logic
        private void SaveHotkeyFinderSettings()
        {
            try
            {
                var settings = new
                {
                    AutoDetectEnabled = AutoDetectToggle.IsOn,
                    RefreshAllEnabled = RefreshAllToggle.IsOn
                };
                
                var settingsDir = Path.Combine(AppContext.BaseDirectory, "Settings", "Functions");
                if (!Directory.Exists(settingsDir))
                    Directory.CreateDirectory(settingsDir);
                
                var settingsPath = Path.Combine(settingsDir, "HotkeyFinder.json");
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch { }
        }

        private async Task StartHotkeyRefresh()
        {
            _isHotkeyProcessing = true;
            _hotkeyFinderCancellationTokenSource = new CancellationTokenSource();
            
            // Update UI
            RefreshButtonText.Text = GetHKTranslation("CancelButton");
            RefreshIcon.Visibility = Visibility.Collapsed;
            CancelIcon2.Visibility = Visibility.Visible;
            RefreshProgressBar.Visibility = Visibility.Visible;
            
            try
            {
                var modLibraryPath = SettingsManager.Current.ModLibraryDirectory ?? Path.Combine(AppContext.BaseDirectory, "ModLibrary");
                if (!Directory.Exists(modLibraryPath))
                {
                    await ShowHotkeyErrorDialog(GetHKTranslation("FatalError"), GetHKTranslation("ModLibraryNotFound"));
                    return;
                }

                var modDirs = Directory.GetDirectories(modLibraryPath);
                int processedCount = 0;
                int successCount = 0;
                var errors = new List<string>();

                foreach (var modDir in modDirs)
                {
                    if (_hotkeyFinderCancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    try
                    {
                        var iniFiles = Directory.GetFiles(modDir, "*.ini", SearchOption.AllDirectories);
                        foreach (var iniFile in iniFiles)
                        {
                            if (_hotkeyFinderCancellationTokenSource.Token.IsCancellationRequested)
                                break;

                            var content = await File.ReadAllTextAsync(iniFile);
                            var matches = _hotkeyPattern.Matches(content);
                            
                            if (matches.Count > 0)
                            {
                                // Update mod.json with hotkey information
                                var modJsonPath = Path.Combine(modDir, "mod.json");
                                if (File.Exists(modJsonPath))
                                {
                                    var jsonContent = await File.ReadAllTextAsync(modJsonPath);
                                    var modData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
                                    if (modData != null)
                                    {
                                        var hotkeys = matches.Cast<Match>().Select(m => m.Groups[1].Value).ToArray();
                                        modData["hotkeys"] = string.Join(", ", hotkeys);
                                        
                                        var updatedJson = JsonSerializer.Serialize(modData, new JsonSerializerOptions { WriteIndented = true });
                                        await File.WriteAllTextAsync(modJsonPath, updatedJson);
                                        successCount++;
                                    }
                                }
                            }
                        }
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{Path.GetFileName(modDir)}: {ex.Message}");
                    }
                }

                if (!_hotkeyFinderCancellationTokenSource.Token.IsCancellationRequested)
                {
                    var message = $"{GetHKTranslation("Processed")}: {processedCount}\n{GetHKTranslation("Success")}: {successCount}";
                    if (errors.Any())
                    {
                        message += $"\n{GetHKTranslation("Errors")}: {errors.Count}";
                    }
                    
                    await ShowHotkeyErrorDialog(GetHKTranslation("RefreshCompleteMessage"), message);
                }
                else
                {
                    await ShowHotkeyErrorDialog(GetHKTranslation("Info"), GetHKTranslation("RefreshCancelledMessage"));
                }
            }
            catch (Exception ex)
            {
                await ShowHotkeyErrorDialog(GetHKTranslation("FatalError"), ex.Message);
            }
            finally
            {
                _isHotkeyProcessing = false;
                RefreshButtonText.Text = GetHKTranslation("RefreshButton");
                RefreshIcon.Visibility = Visibility.Visible;
                CancelIcon2.Visibility = Visibility.Collapsed;
                RefreshProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private async Task ShowHotkeyErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = GetHKTranslation("OK"),
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        // Mod Info Backup Logic
        private async Task<int> CreateModInfoBackups(string modLibraryPath)
        {
            var backupDir = Path.Combine(AppContext.BaseDirectory, "Backups", "ModInfo");
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            // Shift existing backups
            ShiftBackups(backupDir);

            var backup1Dir = Path.Combine(backupDir, "1");
            if (!Directory.Exists(backup1Dir))
                Directory.CreateDirectory(backup1Dir);

            int backupCount = 0;
            var modDirs = Directory.GetDirectories(modLibraryPath);
            
            foreach (var modDir in modDirs)
            {
                var modJsonPath = Path.Combine(modDir, "mod.json");
                if (File.Exists(modJsonPath))
                {
                    var backupPath = Path.Combine(backup1Dir, Path.GetFileName(modDir) + ".json");
                    File.Copy(modJsonPath, backupPath, true);
                    backupCount++;
                }
            }

            // Save backup info
            var backupInfo = new
            {
                Created = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Files = backupCount
            };
            var infoPath = Path.Combine(backup1Dir, "info.json");
            var infoJson = JsonSerializer.Serialize(backupInfo, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(infoPath, infoJson);

            return backupCount;
        }

        private void ShiftBackups(string backupDir)
        {
            // Delete backup 3 if exists
            var backup3Dir = Path.Combine(backupDir, "3");
            if (Directory.Exists(backup3Dir))
                Directory.Delete(backup3Dir, true);

            // Move backup 2 to 3
            var backup2Dir = Path.Combine(backupDir, "2");
            if (Directory.Exists(backup2Dir))
                Directory.Move(backup2Dir, backup3Dir);

            // Move backup 1 to 2
            var backup1Dir = Path.Combine(backupDir, "1");
            if (Directory.Exists(backup1Dir))
                Directory.Move(backup1Dir, backup2Dir);
        }

        private Task<int> RestoreModInfoBackups(string backupSet)
        {
            var backupDir = Path.Combine(AppContext.BaseDirectory, "Backups", "ModInfo", backupSet);
            if (!Directory.Exists(backupDir))
                throw new DirectoryNotFoundException($"Backup set {backupSet} not found.");

            var modLibraryPath = SettingsManager.Current.ModLibraryDirectory ?? Path.Combine(AppContext.BaseDirectory, "ModLibrary");
            var backupFiles = Directory.GetFiles(backupDir, "*.json").Where(f => !f.EndsWith("info.json"));
            
            int restoredCount = 0;
            foreach (var backupFile in backupFiles)
            {
                var modName = Path.GetFileNameWithoutExtension(backupFile);
                var modDir = Path.Combine(modLibraryPath, modName);
                if (Directory.Exists(modDir))
                {
                    var modJsonPath = Path.Combine(modDir, "mod.json");
                    File.Copy(backupFile, modJsonPath, true);
                    restoredCount++;
                }
            }

            return Task.FromResult(restoredCount);
        }

        private Task<int> DeleteModInfoBackups(string backupSet)
        {
            var backupDir = Path.Combine(AppContext.BaseDirectory, "Backups", "ModInfo", backupSet);
            if (!Directory.Exists(backupDir))
                return Task.FromResult(0);

            var files = Directory.GetFiles(backupDir);
            var fileCount = files.Length;
            Directory.Delete(backupDir, true);
            return Task.FromResult(fileCount);
        }

        private void UpdateModInfoBackupInfo()
        {
            var backupDir = Path.Combine(AppContext.BaseDirectory, "Backups", "ModInfo");
            
            UpdateBackupInfo(Backup1Info, Path.Combine(backupDir, "1"));
            UpdateBackupInfo(Backup2Info, Path.Combine(backupDir, "2"));
            UpdateBackupInfo(Backup3Info, Path.Combine(backupDir, "3"));
        }

        private void UpdateBackupInfo(TextBlock infoBlock, string backupPath)
        {
            try
            {
                var infoPath = Path.Combine(backupPath, "info.json");
                if (File.Exists(infoPath))
                {
                    var json = File.ReadAllText(infoPath);
                    var info = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    if (info != null)
                    {
                        var created = info.TryGetValue("Created", out var createdObj) ? createdObj.ToString() : "Unknown";
                        var files = info.TryGetValue("Files", out var filesObj) ? filesObj.ToString() : "0";
                        infoBlock.Text = string.Format(GetMIBTranslation("ModInfoBackup_BackupInfo"), created, files);
                        return;
                    }
                }
            }
            catch { }
            
            infoBlock.Text = "No backup available";
        }

        private async Task ShowModInfoBackupDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = GetMIBTranslation("OK"),
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        // Status Keeper Logic
        private void SaveStatusKeeperSettings()
        {
            try
            {
                var settings = new
                {
                    D3dxFilePath = GetBreadcrumbPath(D3dxFilePathBreadcrumb),
                    BackupConfirmation = BackupConfirmationToggle.IsOn,
                    DynamicSync = DynamicSyncToggle.IsOn,
                    LoggingEnabled = LoggingToggle.IsOn
                };
                
                var settingsDir = Path.Combine(AppContext.BaseDirectory, "Settings", "Functions");
                if (!Directory.Exists(settingsDir))
                    Directory.CreateDirectory(settingsDir);
                
                var settingsPath = Path.Combine(settingsDir, "StatusKeeper.json");
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch { }
        }

        private string GetBreadcrumbPath(BreadcrumbBar bar)
        {
            if (bar.ItemsSource is List<object> items && items.Count > 1)
            {
                return string.Join(Path.DirectorySeparatorChar.ToString(), items.Skip(1));
            }
            return "";
        }

        private void SetupFileWatcher()
        {
            _d3dxFileWatcher?.Dispose();
            
            if (DynamicSyncToggle.IsOn)
            {
                try
                {
                    var d3dxPath = GetBreadcrumbPath(D3dxFilePathBreadcrumb);
                    if (!string.IsNullOrEmpty(d3dxPath) && Directory.Exists(d3dxPath))
                    {
                        _d3dxFileWatcher = new FileSystemWatcher(d3dxPath, "d3dx_user.ini");
                        _d3dxFileWatcher.Changed += async (s, e) => await PerformStatusKeeperSync();
                        _d3dxFileWatcher.EnableRaisingEvents = true;
                    }
                }
                catch { }
            }
        }

        private Task PerformStatusKeeperSync()
        {
            // Basic sync implementation - this would be more complex in the real implementation
            return Task.CompletedTask;
        }

        private Task<(int newBackups, int totalBackups)> CreateStatusKeeperBackup()
        {
            // Placeholder implementation - would create actual backups in real implementation
            return Task.FromResult((5, 15));
        }

        private Task<(int iniFiles, int backups, int missing)> CheckStatusKeeperBackup()
        {
            // Placeholder implementation - would check actual backups in real implementation
            return Task.FromResult((10, 8, 2));
        }

        private Task<(int restored, int failed)> RestoreStatusKeeperBackup()
        {
            // Placeholder implementation - would restore actual backups in real implementation
            return Task.FromResult((8, 0));
        }

        private Task<int> DeleteStatusKeeperBackups()
        {
            // Placeholder implementation - would delete actual backups in real implementation
            return Task.FromResult(15);
        }

        private bool AllSafetyOverridesEnabled()
        {
            return SafetyOverride1Toggle.IsOn && SafetyOverride2Toggle.IsOn && SafetyOverride3Toggle.IsOn;
        }

        private void UpdateSafetyOverrideButtons()
        {
            var allEnabled = AllSafetyOverridesEnabled();
            RestoreBackupButton.IsEnabled = allEnabled;
            DeleteBackupsButton.IsEnabled = allEnabled;
        }

        private async Task ShowStatusKeeperDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = GetSKTranslation("OK"),
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        // Win32 folder picker implementation
        private string? PickFolderWin32Dialog(nint hwnd)
        {
            var bi = new BROWSEINFO
            {
                hwndOwner = hwnd,
                lpszTitle = GetSKTranslation("PickFolderDialog_Title"),
                ulFlags = 0x00000040 // BIF_NEWDIALOGSTYLE
            };
            IntPtr pidl = SHBrowseForFolder(ref bi);
            if (pidl == IntPtr.Zero)
                return null;
            var sb = new StringBuilder(MAX_PATH);
            if (SHGetPathFromIDList(pidl, sb))
                return sb.ToString();
            return null;
        }

        private Task<string?> PickFolderWin32DialogSTA(nint hwnd)
        {
            var tcs = new TaskCompletionSource<string?>();
            var thread = new Thread(() =>
            {
                try
                {
                    var result = PickFolderWin32Dialog(hwnd);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }
    }
}
