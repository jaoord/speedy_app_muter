using SpeedyAppMuter.Models;
using SpeedyAppMuter.Services;
using SpeedyAppMuter.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SpeedyAppMuter.UI
{
    public partial class SettingsForm : Form
    {
        private readonly ProcessDetectionService _processDetectionService;
        private readonly ConfigurationService _configurationService;
        private AppConfig _currentConfig;
        private List<ProcessInfo> _availableProcesses = new();
        private bool _isLoading = false;

        private RadioButton _radioRunningApps = null!;
        private RadioButton _radioAllApps = null!;
        private RadioButton _radioManualEntry = null!;
        private ComboBox _comboApps = null!;
        private Button _buttonRefresh = null!;
        private Panel _panelAppDropdown = null!;
        private Panel _panelManualEntry = null!;
        private TextBox _textProcessName = null!;
        private TextBox _textDisplayName = null!;
        private Label _labelSelectedApp = null!;
        private Label _labelProcessInfo = null!;
        private TextBox _textHotkey = null!;
        private Button _buttonClearHotkey = null!;


        private Button _buttonApply = null!;
        private Button _buttonSave = null!;
        private Button _buttonCancel = null!;
        private GroupBox _targetApplicationGroup = null!;
        private TableLayoutPanel _mainLayout = null!;

        private readonly List<string> _pressedModifiers = new();
        private string _pressedKey = string.Empty;
        private bool _capturingHotkey = false;
        private bool _hasUnsavedChanges = false;

        public event EventHandler<AppConfig>? ConfigurationChanged;

        public SettingsForm(AppConfig currentConfig)
        {
            _processDetectionService = new ProcessDetectionService();
            _configurationService = new ConfigurationService();
            _currentConfig = currentConfig;

            InitializeComponent();
            LoadCurrentConfiguration();
            RefreshProcessList();
            
            this.FormClosing += OnFormClosing;
            this.Load += OnFormLoad;
        }

        private void InitializeComponent()
        {
            this.Text = "Speedy App Muter - Settings";
            this.MinimumSize = new Size(650, 480);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            
            RestoreWindowState();

            _mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(20)
            };
            var mainLayout = _mainLayout;

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 240F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var appGroup = CreateTargetApplicationGroup();
            var hotkeyGroup = CreateHotkeyGroup();
            var buttonPanel = CreateButtonPanel();

            mainLayout.Controls.Add(appGroup, 0, 0);
            mainLayout.Controls.Add(hotkeyGroup, 0, 1);
            mainLayout.Controls.Add(buttonPanel, 0, 2);

            this.Controls.Add(mainLayout);
        }

        private void RestoreWindowState()
        {
            var windowState = _currentConfig.Settings.WindowState;
            
            this.Size = new Size(windowState.Width, windowState.Height);
            if (windowState.X >= 0 && windowState.Y >= 0)
            {
                var screen = Screen.FromPoint(new Point(windowState.X, windowState.Y));
                if (screen.WorkingArea.Contains(windowState.X, windowState.Y))
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = new Point(windowState.X, windowState.Y);
                }
                else
                {
                    this.StartPosition = FormStartPosition.CenterScreen;
                }
            }
            else
            {
                this.StartPosition = FormStartPosition.CenterScreen;
            }
            
            if (windowState.IsMaximized)
            {
                this.WindowState = FormWindowState.Maximized;
            }
        }

        private void SaveWindowState()
        {
            var windowState = _currentConfig.Settings.WindowState;
            
            if (this.WindowState == FormWindowState.Normal)
            {
                windowState.Width = this.Size.Width;
                windowState.Height = this.Size.Height;
                windowState.X = this.Location.X;
                windowState.Y = this.Location.Y;
                windowState.IsMaximized = false;
            }
            else if (this.WindowState == FormWindowState.Maximized)
            {
                windowState.IsMaximized = true;
            }
        }

        private GroupBox CreateTargetApplicationGroup()
        {
            _targetApplicationGroup = new GroupBox
            {
                Text = "Target Application",
                Height = 220,
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };
            var group = _targetApplicationGroup;

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 1
            };

            _radioRunningApps = new RadioButton
            {
                Text = "From running applications with audio",
                Checked = true,
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 5)
            };
            _radioRunningApps.CheckedChanged += OnSelectionMethodChanged;

            _radioAllApps = new RadioButton
            {
                Text = "From all running applications",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 5)
            };
            _radioAllApps.CheckedChanged += OnSelectionMethodChanged;

            _radioManualEntry = new RadioButton
            {
                Text = "Manual entry",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            _radioManualEntry.CheckedChanged += OnSelectionMethodChanged;

            _panelAppDropdown = new Panel { Height = 30, Dock = DockStyle.Top };
            _comboApps = new ComboBox
            {
                DisplayMember = "DisplayName",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 400,
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };
            _comboApps.SelectionChangeCommitted += OnAppSelectionChanged;

            _buttonRefresh = new Button
            {
                Text = "🔄",
                Width = 30,
                Height = 23,
                Anchor = AnchorStyles.Right
            };
            _buttonRefresh.Location = new Point(_comboApps.Right + 5, 0);
            _buttonRefresh.Click += OnRefreshClick;

            _panelAppDropdown.Controls.Add(_comboApps);
            _panelAppDropdown.Controls.Add(_buttonRefresh);

            _panelManualEntry = new Panel { Height = 60, Dock = DockStyle.Top, Visible = false };
            var processLabel = new Label { Text = "Process Name:", AutoSize = true };
            _textProcessName = new TextBox { Width = 200, Top = 20 };
            _textProcessName.TextChanged += OnProcessNameChanged;

            var displayLabel = new Label { Text = "Display Name:", AutoSize = true, Left = 220 };
            _textDisplayName = new TextBox { Width = 200, Top = 20, Left = 220 };
            _textDisplayName.TextChanged += OnDisplayNameChanged;

            _panelManualEntry.Controls.AddRange(new Control[] { processLabel, _textProcessName, displayLabel, _textDisplayName });

            var infoPanel = new Panel
            {
                Height = 55,
                Dock = DockStyle.Top,
                BorderStyle = BorderStyle.FixedSingle
            };

            _labelSelectedApp = new Label
            {
                Text = "Selected Application: None",
                Font = new Font(this.Font, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(8, 8)
            };

            _labelProcessInfo = new Label
            {
                Text = "",
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(8, 30)
            };

            infoPanel.Controls.Add(_labelSelectedApp);
            infoPanel.Controls.Add(_labelProcessInfo);

            panel.Controls.Add(_radioRunningApps, 0, 0);
            panel.Controls.Add(_radioAllApps, 0, 1);
            panel.Controls.Add(_radioManualEntry, 0, 2);
            panel.Controls.Add(_panelAppDropdown, 0, 3);
            panel.Controls.Add(_panelManualEntry, 0, 4);
            panel.Controls.Add(infoPanel, 0, 5);

            group.Controls.Add(panel);
            return group;
        }

        private GroupBox CreateHotkeyGroup()
        {
            var group = new GroupBox
            {
                Text = "Hotkey Configuration",
                Height = 110,
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };

            var hotkeyLabel = new Label
            {
                Text = "Hotkey Combination:",
                Font = new Font(this.Font, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 25)
            };

            _textHotkey = new TextBox
            {
                ReadOnly = true,
                Text = "Press keys...",
                Width = 300,
                Location = new Point(10, 50)
            };
            _textHotkey.KeyDown += OnHotkeyKeyDown;
            _textHotkey.KeyUp += OnHotkeyKeyUp;
            _textHotkey.Enter += OnHotkeyEnter;
            _textHotkey.Leave += OnHotkeyLeave;

            _buttonClearHotkey = new Button
            {
                Text = "Clear",
                Width = 60,
                Location = new Point(320, 50)
            };
            _buttonClearHotkey.Click += OnClearHotkeyClick;

            var helpLabel = new Label
            {
                Text = "Click in the field above and press your desired key combination.",
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(10, 75)
            };

            group.Controls.AddRange(new Control[] { hotkeyLabel, _textHotkey, _buttonClearHotkey, helpLabel });
            return group;
        }



        private Panel CreateButtonPanel()
        {
            var panel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Fill
            };



            _buttonApply = new Button
            {
                Text = "Apply",
                Width = 80,
                Height = 30,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _buttonApply.Click += OnApplyClick;

            _buttonSave = new Button
            {
                Text = "OK",
                Width = 80,
                Height = 30,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _buttonSave.Click += OnSaveClick;

            _buttonCancel = new Button
            {
                Text = "Close",
                Width = 80,
                Height = 30,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _buttonCancel.Click += OnCancelClick;

            this.Resize += (s, e) => PositionButtons();

            panel.Controls.AddRange(new Control[] { _buttonApply, _buttonSave, _buttonCancel });
            return panel;
        }

        private void LoadCurrentConfiguration()
        {
            _isLoading = true;

            try
            {
                UpdateSelectedAppInfo(_currentConfig.TargetApplication.Name, _currentConfig.TargetApplication.ProcessNames);
                if (_currentConfig.TargetApplication.Hotkey?.Modifiers?.Length > 0 && 
                    !string.IsNullOrEmpty(_currentConfig.TargetApplication.Hotkey.Key))
                {
                    var hotkeyText = string.Join(" + ", _currentConfig.TargetApplication.Hotkey.Modifiers!) + 
                                    " + " + _currentConfig.TargetApplication.Hotkey.Key;
                    _textHotkey.Text = hotkeyText;
                }


            }
            finally
            {
                _isLoading = false;
            }
        }

        private void RefreshProcessList()
        {
            try
            {
                if (_radioRunningApps.Checked)
                {
                    _availableProcesses = _processDetectionService.GetProcessesWithAudio();
                }
                else if (_radioAllApps.Checked)
                {
                    _availableProcesses = _processDetectionService.GetAllRunningProcesses();
                }
                else
                {
                    _availableProcesses.Clear();
                }

                _comboApps.DataSource = null;
                _comboApps.DataSource = _availableProcesses;

                var currentApp = _availableProcesses.FirstOrDefault(p => 
                    _currentConfig.TargetApplication.ProcessNames?.Contains(p.ProcessName, StringComparer.OrdinalIgnoreCase) == true);

                if (currentApp != null)
                {
                    _comboApps.SelectedItem = currentApp;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing process list: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnSelectionMethodChanged(object? sender, EventArgs e)
        {
            if (_isLoading) return;

            if (_radioManualEntry.Checked)
            {
                _panelAppDropdown.Visible = false;
                _panelManualEntry.Visible = true;
                _targetApplicationGroup.Height = 260;
                _mainLayout.RowStyles[0] = new RowStyle(SizeType.Absolute, 260F);
                
                _textProcessName.Text = _currentConfig.TargetApplication.ProcessNames?.FirstOrDefault() ?? "";
                _textDisplayName.Text = _currentConfig.TargetApplication.Name;
            }
            else
            {
                _panelAppDropdown.Visible = true;
                _panelManualEntry.Visible = false;
                _targetApplicationGroup.Height = 220;
                _mainLayout.RowStyles[0] = new RowStyle(SizeType.Absolute, 240F);
                RefreshProcessList();
            }
            
            MarkAsChanged();
        }

        private void OnAppSelectionChanged(object? sender, EventArgs e)
        {
            if (_isLoading || _comboApps.SelectedItem is not ProcessInfo selectedProcess) 
                return;

            UpdateSelectedAppInfo(selectedProcess.DisplayName.Replace(" 🔊", ""), 
                new[] { selectedProcess.ProcessName });
            MarkAsChanged();
        }

        private void OnProcessNameChanged(object? sender, EventArgs e)
        {
            if (_isLoading) return;

            var processName = _textProcessName.Text.Trim();
            var displayName = _textDisplayName.Text.Trim();

            if (string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(processName))
            {
                var commonNames = ProcessDetectionService.GetCommonAppNames();
                if (commonNames.TryGetValue(processName, out var commonName))
                {
                    _textDisplayName.Text = commonName;
                    displayName = commonName;
                }
                else
                {
                    displayName = processName;
                }
            }

            if (!string.IsNullOrEmpty(processName))
            {
                var processNames = processName.Contains(',') 
                    ? processName.Split(',').Select(s => s.Trim()).ToArray()
                    : new[] { processName };

                UpdateSelectedAppInfo(displayName, processNames);
                MarkAsChanged();
            }
        }

        private void OnDisplayNameChanged(object? sender, EventArgs e)
        {
            if (_isLoading) return;

            var processName = _textProcessName.Text.Trim();
            var displayName = _textDisplayName.Text.Trim();

            if (!string.IsNullOrEmpty(processName) && !string.IsNullOrEmpty(displayName))
            {
                var processNames = processName.Contains(',') 
                    ? processName.Split(',').Select(s => s.Trim()).ToArray()
                    : new[] { processName };

                UpdateSelectedAppInfo(displayName, processNames);
                MarkAsChanged();
            }
        }

        private void UpdateSelectedAppInfo(string displayName, string[] processNames)
        {
            _labelSelectedApp.Text = $"Selected Application: {displayName}";
            _labelProcessInfo.Text = $"Process: {string.Join(", ", processNames)}";

            _currentConfig.TargetApplication.Name = displayName;
            _currentConfig.TargetApplication.ProcessNames = processNames;
        }

        private void OnRefreshClick(object? sender, EventArgs e)
        {
            RefreshProcessList();
        }

        private void OnHotkeyEnter(object? sender, EventArgs e)
        {
            _textHotkey.Text = "Press keys...";
            _pressedModifiers.Clear();
            _pressedKey = string.Empty;
            _capturingHotkey = true;
        }

        private void OnHotkeyLeave(object? sender, EventArgs e)
        {
            _capturingHotkey = false;
            UpdateHotkeyDisplay();
        }

        private void OnHotkeyKeyDown(object? sender, KeyEventArgs e)
        {
            if (!_capturingHotkey) return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            if (IsModifierKey(e.KeyCode))
            {
                var modifierName = GetModifierName(e.KeyCode);
                if (!string.IsNullOrEmpty(modifierName) && !_pressedModifiers.Contains(modifierName))
                {
                    _pressedModifiers.Add(modifierName);
                }
            }
            else
            {
                _pressedKey = GetKeyName(e.KeyCode);
            }

            UpdateCurrentHotkeyDisplay();
        }

        private void OnHotkeyKeyUp(object? sender, KeyEventArgs e)
        {
            if (!_capturingHotkey) return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            // If we have both modifiers and a key, save the combination
            if (_pressedModifiers.Count > 0 && !string.IsNullOrEmpty(_pressedKey))
            {
                _currentConfig.TargetApplication.Hotkey = new HotkeyConfig
                {
                    Modifiers = _pressedModifiers.ToArray(),
                    Key = _pressedKey
                };

                _capturingHotkey = false;
                UpdateHotkeyDisplay();
                MarkAsChanged();
                
                _buttonSave.Focus();
            }
        }

        private void OnClearHotkeyClick(object? sender, EventArgs e)
        {
            _currentConfig.TargetApplication.Hotkey = new HotkeyConfig { Modifiers = Array.Empty<string>(), Key = string.Empty };
            _textHotkey.Text = "None";
            _pressedModifiers.Clear();
            _pressedKey = string.Empty;
            MarkAsChanged();
        }

        private void UpdateCurrentHotkeyDisplay()
        {
            var parts = new List<string>(_pressedModifiers);
            if (!string.IsNullOrEmpty(_pressedKey))
            {
                parts.Add(_pressedKey);
            }

            _textHotkey.Text = parts.Count > 0 ? string.Join(" + ", parts) : "Press keys...";
        }

        private void UpdateHotkeyDisplay()
        {
            if (_currentConfig.TargetApplication.Hotkey?.Modifiers?.Length > 0 || 
                !string.IsNullOrEmpty(_currentConfig.TargetApplication.Hotkey?.Key))
            {
                var parts = new List<string>();
                if (_currentConfig.TargetApplication.Hotkey.Modifiers != null)
                {
                    parts.AddRange(_currentConfig.TargetApplication.Hotkey.Modifiers);
                }
                if (!string.IsNullOrEmpty(_currentConfig.TargetApplication.Hotkey.Key))
                {
                    parts.Add(_currentConfig.TargetApplication.Hotkey.Key);
                }
                _textHotkey.Text = string.Join(" + ", parts);
            }
            else
            {
                _textHotkey.Text = "None";
            }
        }





        private void OnApplyClick(object? sender, EventArgs e)
        {
            if (!ValidateConfiguration())
                return;

            try
            {
                SaveWindowState();
                _configurationService.SaveConfiguration(_currentConfig);
                ConfigurationChanged?.Invoke(this, _currentConfig);

                MessageBox.Show("Settings applied successfully!", "Settings Applied", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                _hasUnsavedChanges = false;
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Save Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSaveClick(object? sender, EventArgs e)
        {
            if (!ValidateConfiguration())
                return;

            try
            {
                SaveWindowState();
                _configurationService.SaveConfiguration(_currentConfig);
                ConfigurationChanged?.Invoke(this, _currentConfig);
                
                _hasUnsavedChanges = false;
                UpdateButtonStates();

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Save Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCancelClick(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveWindowState();
        }

        private void OnFormLoad(object? sender, EventArgs e)
        {
            PositionButtons();
            UpdateButtonStates();
        }

        private void PositionButtons()
        {
            const int buttonSpacing = 10;
            
            _buttonApply.Location = new Point(0, 10);
            _buttonSave.Location = new Point(_buttonApply.Right + buttonSpacing, 10);
            _buttonCancel.Location = new Point(_buttonSave.Right + buttonSpacing, 10);
        }

        private void UpdateButtonStates()
        {
            _buttonApply.Enabled = _hasUnsavedChanges;
            _buttonSave.Enabled = _hasUnsavedChanges;
        }

        private void MarkAsChanged()
        {
            if (!_isLoading)
            {
                _hasUnsavedChanges = true;
                UpdateButtonStates();
            }
        }

        private bool ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(_currentConfig.TargetApplication.Name))
            {
                MessageBox.Show("Please select or enter a target application.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (_currentConfig.TargetApplication.ProcessNames?.Length == 0)
            {
                MessageBox.Show("Please specify at least one process name.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (_currentConfig.TargetApplication.Hotkey?.Modifiers?.Length == 0 || 
                string.IsNullOrEmpty(_currentConfig.TargetApplication.Hotkey?.Key))
            {
                MessageBox.Show("Please configure a hotkey combination.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private static bool IsModifierKey(Keys key)
        {
            return key == Keys.ControlKey || key == Keys.LControlKey || key == Keys.RControlKey ||
                   key == Keys.Menu || key == Keys.LMenu || key == Keys.RMenu ||
                   key == Keys.ShiftKey || key == Keys.LShiftKey || key == Keys.RShiftKey ||
                   key == Keys.LWin || key == Keys.RWin;
        }

        private static string GetModifierName(Keys key)
        {
            return key switch
            {
                Keys.ControlKey or Keys.LControlKey or Keys.RControlKey => "Ctrl",
                Keys.Menu or Keys.LMenu or Keys.RMenu => "Alt",
                Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey => "Shift",
                Keys.LWin or Keys.RWin => "Win",
                _ => string.Empty
            };
        }

        private static string GetKeyName(Keys key)
        {
            return key switch
            {
                Keys.F1 => "F1", Keys.F2 => "F2", Keys.F3 => "F3", Keys.F4 => "F4",
                Keys.F5 => "F5", Keys.F6 => "F6", Keys.F7 => "F7", Keys.F8 => "F8",
                Keys.F9 => "F9", Keys.F10 => "F10", Keys.F11 => "F11", Keys.F12 => "F12",
                Keys.F13 => "F13", Keys.F14 => "F14", Keys.F15 => "F15", Keys.F16 => "F16",
                Keys.F17 => "F17", Keys.F18 => "F18", Keys.F19 => "F19", Keys.F20 => "F20",
                Keys.F21 => "F21", Keys.F22 => "F22", Keys.F23 => "F23", Keys.F24 => "F24",
                
                Keys.A => "A", Keys.B => "B", Keys.C => "C", Keys.D => "D", Keys.E => "E",
                Keys.F => "F", Keys.G => "G", Keys.H => "H", Keys.I => "I", Keys.J => "J",
                Keys.K => "K", Keys.L => "L", Keys.M => "M", Keys.N => "N", Keys.O => "O",
                Keys.P => "P", Keys.Q => "Q", Keys.R => "R", Keys.S => "S", Keys.T => "T",
                Keys.U => "U", Keys.V => "V", Keys.W => "W", Keys.X => "X", Keys.Y => "Y",
                Keys.Z => "Z",
                Keys.D0 => "0", Keys.D1 => "1", Keys.D2 => "2", Keys.D3 => "3", Keys.D4 => "4",
                Keys.D5 => "5", Keys.D6 => "6", Keys.D7 => "7", Keys.D8 => "8", Keys.D9 => "9",
                
                Keys.Space => "Space",
                Keys.Enter => "Enter",
                Keys.Escape => "Escape",
                Keys.Tab => "Tab",
                Keys.Back => "Backspace",
                Keys.Delete => "Delete",
                Keys.Insert => "Insert",
                Keys.Home => "Home",
                Keys.End => "End",
                Keys.PageUp => "PageUp",
                Keys.PageDown => "PageDown",
                Keys.Up => "Up",
                Keys.Down => "Down",
                Keys.Left => "Left",
                Keys.Right => "Right",
                
                _ => key.ToString()
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _processDetectionService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
} 