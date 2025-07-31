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

        private void FunctionsSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            if (sender.SelectedItem is SelectorBarItem item && item.Tag is string section)
            {
                // Hide all panels first
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
            // TODO: Load actual settings from HotkeyFinderPage business logic
            
            // Initialize toggle states (placeholder values for now)
            AutoDetectToggle.IsOn = false;
            RefreshAllToggle.IsOn = false;
            
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
            // TODO: Load actual settings from ModInfoBackupPage business logic
            
            // Update backup info labels (placeholder for now)
            Backup1Info.Text = "No backup available";
            Backup2Info.Text = "No backup available";
            Backup3Info.Text = "No backup available";
            
            // Update text labels
            UpdateTexts();
        }

        private void LoadGBAuthorUpdateSettings()
        {
            // Load settings for GameBanana Author Update
            // TODO: Load actual settings from GBAuthorUpdatePage business logic
            
            // Initialize toggle states (placeholder values for now)
            UpdateAuthorsSwitch.IsOn = true;
            SmartUpdateSwitch.IsOn = false;
            SafetyLockSwitch.IsOn = false;
            
            // Update all text labels
            UpdateTexts();
        }

        private void GBAuthorUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToFunction("GBAuthorUpdate");
        }

        private void HotkeyFinderButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToFunction("HotkeyFinder");
        }

        private void StatusKeeperButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToFunction("StatusKeeperPage");
        }

        private void ModInfoBackupButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToFunction("ModInfoBackup");
        }

        private void NavigateToFunction(string functionFileName)
        {
            var function = _functionInfos.FirstOrDefault(f => f.FileName == functionFileName);
            if (function != null)
            {
                if (function.FileName == "StatusKeeperPage")
                {
                    Frame.Navigate(typeof(StatusKeeperPage));
                    return;
                }
                if (function.FileName == "ModInfoBackup")
                {
                    Frame.Navigate(typeof(ModInfoBackupPage));
                    return;
                }
                if (!string.IsNullOrEmpty(function.FileName))
                {
                    var pageTypeName = $"Mod_Manager_X.Pages.{function.FileName.First().ToString().ToUpper() + function.FileName.Substring(1)}Page";
                    var pageType = Type.GetType(pageTypeName);
                    if (pageType != null)
                    {
                        Frame.Navigate(pageType);
                    }
                }
            }
            {
                if (function.FileName == "StatusKeeperPage")
                {
                    Frame.Navigate(typeof(StatusKeeperPage));
                    return;
                }
                if (function.FileName == "ModInfoBackup")
                {
                    Frame.Navigate(typeof(ModInfoBackupPage));
                    return;
                }
                var pageTypeName = $"Mod_Manager_X.Pages.{function.FileName.First().ToString().ToUpper() + function.FileName.Substring(1)}Page";
                var pageType = Type.GetType(pageTypeName);
                if (pageType != null)
                {
                    Frame.Navigate(pageType); // Navigate to page
                }
                else
                {
                    var dialog = new ContentDialog
                    {
                        Title = "No Interface",
                        Content = $"No XAML interface for function: {function.Name}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    _ = dialog.ShowAsync();
                }
            }
        }

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
            // TODO: Call into GBAuthorUpdatePage business logic
        }

        private void SmartUpdateSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            // TODO: Call into GBAuthorUpdatePage business logic
        }

        private void SafetyLockSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            // TODO: Call into GBAuthorUpdatePage business logic
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Call into GBAuthorUpdatePage business logic
        }

        // Event handlers for Hotkey Finder
        private void AutoDetectToggle_Toggled(object sender, RoutedEventArgs e)
        {
            // TODO: Call into HotkeyFinderPage business logic
        }

        private void RefreshAllToggle_Toggled(object sender, RoutedEventArgs e)
        {
            // TODO: Call into HotkeyFinderPage business logic
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Call into HotkeyFinderPage business logic
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
        private void D3dxFilePathPickButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Call into StatusKeeperSyncPage business logic
        }

        private void D3dxFilePathDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Call into StatusKeeperSyncPage business logic
        }

        private void BackupConfirmationToggle_Toggled(object sender, RoutedEventArgs e)
        {
            // TODO: Call into StatusKeeperSyncPage business logic
        }

        private void DynamicSyncToggle_Toggled(object sender, RoutedEventArgs e)
        {
            // TODO: Call into StatusKeeperSyncPage business logic
        }

        private void ManualSyncButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Call into StatusKeeperSyncPage business logic
        }

        // Status Keeper Backup event handlers
        private void CreateBackupButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Call into StatusKeeperBackupPage business logic
        }

        private void CheckBackupButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Call into StatusKeeperBackupPage business logic
        }

        private void SafetyOverride1Toggle_Toggled(object sender, RoutedEventArgs e)
        {
            // TODO: Call into StatusKeeperBackupPage business logic
        }

        private void SafetyOverride2Toggle_Toggled(object sender, RoutedEventArgs e)
        {
            // TODO: Call into StatusKeeperBackupPage business logic
        }

        private void SafetyOverride3Toggle_Toggled(object sender, RoutedEventArgs e)
        {
            // TODO: Call into StatusKeeperBackupPage business logic
        }

        private void StatusKeeperRestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Call into StatusKeeperBackupPage business logic
        }

        private void DeleteBackupsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Call into StatusKeeperBackupPage business logic
        }

        // Status Keeper Logs event handlers
        private void LoggingToggle_Toggled(object sender, RoutedEventArgs e)
        {
            // TODO: Call into StatusKeeperLogsPage business logic
        }

        private void OpenLogButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Call into StatusKeeperLogsPage business logic
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Call into StatusKeeperLogsPage business logic
        }

        // Event handlers for Mod Info Backup
        private void CreateBackupsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Call into ModInfoBackupPage business logic
        }

        private void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Call into ModInfoBackupPage business logic
        }

        private void DeleteBackupButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Call into ModInfoBackupPage business logic
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
    }
}
