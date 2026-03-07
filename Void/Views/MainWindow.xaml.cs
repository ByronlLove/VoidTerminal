using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using VoidTerminal.Models;
using VoidTerminal.Services;

namespace VoidTerminal.Views
{
    public partial class MainWindow : Window
    {
        private readonly VoidData _data;
        private readonly string _password;

        private bool _isSidebarOpen = false;
        private string _currentNoteKey = null;
        private string _noteToRename = null;
        private bool _isSearchDictOpen = false;
        private bool _isSearchPossOpen = false;
        private List<string> _fullPossibilities = new();

        private Rect _restoreRect;
        private bool _isCustomMaximized = false;
        private bool _isFullScreen = false;

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_INVALID_STATE = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public uint GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        public MainWindow(VoidData data, string password)
        {
            InitializeComponent();
            _data = data;
            if (_data.UserAddedWords == null) _data.UserAddedWords = new HashSet<string>();

            if (_data.EngineConfig != null)
            {
                Radio1.Content = _data.EngineConfig.Mode1Name;
                RadioVoid.Content = _data.EngineConfig.Mode2Name;
            }

            _password = password;
            RefreshNotesList();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2) ToggleMaximize();
                else
                {
                    if (_isCustomMaximized || _isFullScreen)
                    {
                        var point = PointToScreen(e.GetPosition(this));
                        double percentHorizontal = e.GetPosition(this).X / ActualWidth;
                        _isCustomMaximized = false; _isFullScreen = false;
                        this.Width = _restoreRect.Width > 0 ? _restoreRect.Width : 1050;
                        this.Height = _restoreRect.Height > 0 ? _restoreRect.Height : 750;
                        this.Left = point.X - (this.Width * percentHorizontal);
                        this.Top = point.Y - 20;
                        BtnMax.Content = "☐";
                        if (this.WindowState == WindowState.Maximized) this.WindowState = WindowState.Normal;
                    }
                    this.DragMove();
                }
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11 || ((e.Key == Key.Return || e.SystemKey == Key.Return) && (Keyboard.Modifiers == ModifierKeys.Alt)))
            {
                ToggleFullScreen();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                if (CustomAlertOverlay.Visibility == Visibility.Visible) BtnCloseAlert_Click(null, null);
                else if (AddWordsOverlay.Visibility == Visibility.Visible) BtnCancelAddWords_Click(null, null);
                else if (PossibilityOverlay.Visibility == Visibility.Visible) BtnClosePossibility_Click(null, null);
                else if (CryptoForgeOverlay.Visibility == Visibility.Visible) BtnCancelForge_Click(null, null);
                else if (CryptoAuthOverlay.Visibility == Visibility.Visible) BtnCancelCryptoAuth_Click(null, null);
                else if (DictManagerOverlay.Visibility == Visibility.Visible) BtnCloseDictManager_Click(null, null);
                else if (DictAuthOverlay.Visibility == Visibility.Visible) BtnCancelDictAuth_Click(null, null);
                else if (NoteEditorOverlay.Visibility == Visibility.Visible) BtnCloseNote_Click(null, null);
                else if (RenameOverlay.Visibility == Visibility.Visible) BtnCancelRename_Click(null, null);
                e.Handled = true;
            }
        }
        private void ToggleMaximize() { if (_isFullScreen) ToggleFullScreen(); if (_isCustomMaximized) { this.Left = _restoreRect.Left; this.Top = _restoreRect.Top; this.Width = _restoreRect.Width; this.Height = _restoreRect.Height; _isCustomMaximized = false; BtnMax.Content = "☐"; } else { _restoreRect = new Rect(this.Left, this.Top, this.Width, this.Height); this.Left = SystemParameters.WorkArea.Left; this.Top = SystemParameters.WorkArea.Top; this.Width = SystemParameters.WorkArea.Width; this.Height = SystemParameters.WorkArea.Height; _isCustomMaximized = true; BtnMax.Content = "❐"; } }
        private void ToggleFullScreen() { if (_isFullScreen) { this.WindowState = WindowState.Normal; this.Left = _restoreRect.Left; this.Top = _restoreRect.Top; this.Width = _restoreRect.Width; this.Height = _restoreRect.Height; _isFullScreen = false; _isCustomMaximized = false; BtnMax.Content = "☐"; } else { if (!_isCustomMaximized) _restoreRect = new Rect(this.Left, this.Top, this.Width, this.Height); this.Left = 0; this.Top = 0; this.Width = SystemParameters.PrimaryScreenWidth; this.Height = SystemParameters.PrimaryScreenHeight; if (this.WindowState == WindowState.Maximized) this.WindowState = WindowState.Normal; _isFullScreen = true; _isCustomMaximized = false; } }
        private void BtnMaximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void BtnExit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void BtnLockApp_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();

            this.Close();
        }

        private void ToggleGhost_Click(object sender, RoutedEventArgs e)
        {
            if (SwitchGhost.IsChecked == true)
            {
                if (SwitchGlass.IsChecked == true)
                {
                    SwitchGlass.IsChecked = false;
                    DisableGlassEffect();
                }
                this.Opacity = 0.85;
            }
            else
            {
                this.Opacity = 1.0;
            }
        }

        private void ToggleGlass_Click(object sender, RoutedEventArgs e)
        {
            if (SwitchGlass.IsChecked == true)
            {
                if (SwitchGhost.IsChecked == true)
                {
                    SwitchGhost.IsChecked = false;
                    this.Opacity = 1.0;
                }
                EnableGlassEffect();
            }
            else
            {
                DisableGlassEffect();
            }
        }

        private void EnableGlassEffect()
        {
            var windowHelper = new WindowInteropHelper(this);
            IntPtr mainWindowPtr = windowHelper.Handle;
            var accent = new AccentPolicy();

            accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
            accent.GradientColor = 0x10000000;

            var solidGhostBg = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
            this.Background = solidGhostBg;
            MainBorder.Background = solidGhostBg;

            SidebarBorder.Background = new SolidColorBrush(Color.FromArgb(15, 0, 0, 0));
            MainBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33FFFFFF"));

            ApplyAccentPolicy(mainWindowPtr, accent);
        }

        private void DisableGlassEffect()
        {
            var windowHelper = new WindowInteropHelper(this);
            IntPtr mainWindowPtr = windowHelper.Handle;
            var accent = new AccentPolicy();

            accent.AccentState = AccentState.ACCENT_DISABLED;

            this.Background = Brushes.Transparent;
            MainBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
            SidebarBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#161616"));
            MainBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E3E42"));

            ApplyAccentPolicy(mainWindowPtr, accent);
        }

        private void ApplyAccentPolicy(IntPtr hwnd, AccentPolicy accent)
        {
            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = 19;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        private void BtnMenu_Click(object sender, RoutedEventArgs e) { _isSidebarOpen = !_isSidebarOpen; double targetWidth = _isSidebarOpen ? 250 : 0; DoubleAnimation anim = new DoubleAnimation { To = targetWidth, Duration = TimeSpan.FromSeconds(0.25), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } }; SidebarBorder.BeginAnimation(Border.WidthProperty, anim); }
        private void ShowOverlayWithAnimation(Grid overlay, Border laserBorder) { overlay.Visibility = Visibility.Visible; if (laserBorder != null) { DoubleAnimation anim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5)); laserBorder.BeginAnimation(UIElement.OpacityProperty, anim); } }
        private void OnOverlayClick(object sender, MouseButtonEventArgs e) { if (e.OriginalSource == sender && sender is Grid grid) { grid.Visibility = Visibility.Collapsed; e.Handled = true; } }

        private IInputElement _elementToFocusAfterAlert = null;

        private void ShowCustomAlert(string message, string title = "Information", string colorHex = "#8A2BE2")
        {
            _elementToFocusAfterAlert = Keyboard.FocusedElement;

            LblAlertTitle.Text = title;
            LblAlertMessage.Text = message;
            LblAlertTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
            ShowOverlayWithAnimation(CustomAlertOverlay, LaserBorderAlert);

            BtnAlertOk.Focus();
        }

        private void BtnCloseAlert_Click(object sender, RoutedEventArgs e)
        {
            CustomAlertOverlay.Visibility = Visibility.Collapsed;

            if (_elementToFocusAfterAlert != null)
            {
                Keyboard.Focus(_elementToFocusAfterAlert);
                _elementToFocusAfterAlert = null; 
            }
        }

        private void BtnActionEnc_Click(object sender, RoutedEventArgs e)
        {
            string result = RadioVoid.IsChecked == true ? CryptoEngine.ToVoid(TxtInput.Text, _data.EngineConfig) : CryptoEngine.To1(TxtInput.Text, _data.EngineConfig);
            TxtResult.Document.Blocks.Clear(); TxtResult.Document.Blocks.Add(new Paragraph(new Run($">>> CRYPTAGE <<<\n\n{result}")));
        }

        private void BtnActionDec_Click(object sender, RoutedEventArgs e)
        {
            TxtResult.Document.Blocks.Clear();
            Paragraph p = new Paragraph();
            p.Inlines.Add(new Run($">>> DÉCRYPTAGE {(RadioVoid.IsChecked == true ? _data.EngineConfig.Mode2Name : _data.EngineConfig.Mode1Name).ToUpper()} <<<\n\n"));
            var segments = CryptoEngine.FromVoidSmart(TxtInput.Text, _data.Dictionary, _data.EngineConfig);
            bool isStartOfSentence = true;
            foreach (var seg in segments)
            {
                string formattedText = FormatSegment(seg.Text, ref isStartOfSentence);
                if (seg.IsAmbiguous)
                {
                    Run run = new Run(formattedText);
                    Hyperlink link = new Hyperlink(run);
                    link.Tag = seg.Possibilities;
                    link.Foreground = Brushes.Orange; link.TextDecorations = null; link.Cursor = Cursors.Hand;
                    p.Inlines.Add(link);
                }
                else p.Inlines.Add(new Run(formattedText));
            }
            TxtResult.Document.Blocks.Add(p);
        }

        private string FormatSegment(string input, ref bool isStart) { if (string.IsNullOrEmpty(input)) return input; StringBuilder sb = new StringBuilder(); foreach (char c in input) { if (char.IsLetter(c)) { if (isStart) { sb.Append(char.ToUpper(c)); isStart = false; } else sb.Append(char.ToLower(c)); } else { sb.Append(c); if (c == '.' || c == '?' || c == '!' || c == '\n') isStart = true; } } return sb.ToString(); }
        private void TxtResult_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) { TextPointer pointer = TxtResult.GetPositionFromPoint(e.GetPosition(TxtResult), true); if (pointer != null) { var element = pointer.Parent as TextElement; while (element != null && !(element is Hyperlink)) element = element.Parent as TextElement; if (element is Hyperlink link && link.Tag is List<string> possibilities) { _fullPossibilities = possibilities; ListPossibilities.ItemsSource = _fullPossibilities; ShowOverlayWithAnimation(PossibilityOverlay, LaserBorderPoss); TxtSearchPossibilities.Width = 0; TxtSearchPossibilities.Text = ""; _isSearchPossOpen = false; e.Handled = true; } } }

        private void AddWordToDictionary(string word) { string w = word.ToLower(); if (!_data.Dictionary.Contains(w)) { _data.Dictionary.Add(w); _data.UserAddedWords.Add(w); } }
        private void BtnAddDico_Click(object sender, RoutedEventArgs e) { var matches = Regex.Matches(TxtInput.Text, @"\w+"); var newCandidates = new HashSet<string>(); foreach (Match match in matches) { string w = match.Value.ToLower(); if (!_data.Dictionary.Contains(w)) newCandidates.Add(w); } if (newCandidates.Count > 0) { ListNewWordsCandidates.ItemsSource = newCandidates.OrderBy(x => x).ToList(); ListNewWordsCandidates.SelectAll(); ShowOverlayWithAnimation(AddWordsOverlay, LaserBorderAddWords); } else ShowCustomAlert("Tous les mots sont déjà dans le dictionnaire.", "Information", "#FFA500"); }
        private void BtnConfirmAddWords_Click(object sender, RoutedEventArgs e) { int count = 0; foreach (string w in ListNewWordsCandidates.SelectedItems) { AddWordToDictionary(w); count++; } SecurityManager.SaveSecureData(_data, _password); AddWordsOverlay.Visibility = Visibility.Collapsed; ShowCustomAlert($"{count} mot(s) ajouté(s) au dictionnaire.", "Opération réussie", "#FFA500"); }
        private void BtnCancelAddWords_Click(object sender, RoutedEventArgs e) => AddWordsOverlay.Visibility = Visibility.Collapsed;
        private void AnimateSearchBar(TextBox txtBox, bool open) { DoubleAnimation animation = new DoubleAnimation { From = open ? 0 : 200, To = open ? 200 : 0, Duration = new Duration(TimeSpan.FromSeconds(0.3)) }; txtBox.BeginAnimation(WidthProperty, animation); if (open) txtBox.Focus(); }
        private void BtnToggleSearchPoss_Click(object sender, RoutedEventArgs e) { _isSearchPossOpen = !_isSearchPossOpen; AnimateSearchBar(TxtSearchPossibilities, _isSearchPossOpen); }
        private void TxtSearchPossibilities_TextChanged(object sender, TextChangedEventArgs e) { string search = TxtSearchPossibilities.Text.ToLower(); if (string.IsNullOrWhiteSpace(search)) ListPossibilities.ItemsSource = _fullPossibilities; else ListPossibilities.ItemsSource = _fullPossibilities.Where(p => p.ToLower().Contains(search)).ToList(); }
        private void TxtSearchPossibilities_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter && ListPossibilities.Items.Count > 0) { ListPossibilities.SelectedIndex = 0; ListPossibilities.ScrollIntoView(ListPossibilities.SelectedItem); ListPossibilities.Focus(); } }

        private void BtnManageDict_Click(object sender, RoutedEventArgs e) { if (string.IsNullOrEmpty(_data.DictProtectionHash)) { LblDictAuthTitle.Text = "Création sécurité"; LblDictAuthMsg.Text = "Définissez un mot de passe :"; } else { LblDictAuthTitle.Text = "Accès sécurisé"; LblDictAuthMsg.Text = "Mot de passe requis :"; } TxtDictPassword.Clear(); ShowOverlayWithAnimation(DictAuthOverlay, LaserBorderAuth); TxtDictPassword.Focus(); }
        private void BtnValidateDictAuth_Click(object sender, RoutedEventArgs e) { string inputPass = TxtDictPassword.Password; if (string.IsNullOrWhiteSpace(inputPass)) return; string hashedInput = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(inputPass))); if (string.IsNullOrEmpty(_data.DictProtectionHash)) { _data.DictProtectionHash = hashedInput; SecurityManager.SaveSecureData(_data, _password); OpenDictManager(); } else { if (hashedInput == _data.DictProtectionHash) OpenDictManager(); else { ShowCustomAlert("Mot de passe incorrect.", "Accès refusé", "#FF5555"); TxtDictPassword.Clear(); } } }
        private void TxtDictPassword_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) BtnValidateDictAuth_Click(sender, e); }
        private void BtnCancelDictAuth_Click(object sender, RoutedEventArgs e) { DictAuthOverlay.Visibility = Visibility.Collapsed; TxtDictPassword.Clear(); }

        private void OpenDictManager() { DictAuthOverlay.Visibility = Visibility.Collapsed; TxtSearchDict.Width = 0; TxtSearchDict.Text = ""; _isSearchDictOpen = false; RadioDictAll.IsChecked = true; RefreshDictList(); ShowOverlayWithAnimation(DictManagerOverlay, LaserBorderDict); }
        private void BtnToggleSearchDict_Click(object sender, RoutedEventArgs e) { _isSearchDictOpen = !_isSearchDictOpen; AnimateSearchBar(TxtSearchDict, _isSearchDictOpen); }
        private void FilterDict_Click(object sender, RoutedEventArgs e) => RefreshDictList(TxtSearchDict.Text);
        private void TxtSearchDict_TextChanged(object sender, TextChangedEventArgs e) => RefreshDictList(TxtSearchDict.Text);
        private void TxtSearchDict_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter && ListDictionary.Items.Count > 0) { ListDictionary.SelectedIndex = 0; ListDictionary.ScrollIntoView(ListDictionary.SelectedItem); ListDictionary.Focus(); } }
        private void RefreshDictList(string search = "") { IEnumerable<string> source; if (RadioDictUser.IsChecked == true) source = _data.UserAddedWords; else source = _data.Dictionary; if (!string.IsNullOrWhiteSpace(search)) source = source.Where(w => w.Contains(search.ToLower())); var list = source.OrderBy(x => x).ToList(); ListDictionary.ItemsSource = list; LblDictCount.Text = $"Total : {list.Count} mots"; }
        private void BtnDeleteWord_Click(object sender, RoutedEventArgs e) { if (ListDictionary.SelectedItem is string word && MessageBox.Show($"Supprimer '{word}' ?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { _data.Dictionary.Remove(word); _data.UserAddedWords.Remove(word); SecurityManager.SaveSecureData(_data, _password); RefreshDictList(TxtSearchDict.Text); } }
        private void BtnCloseDictManager_Click(object sender, RoutedEventArgs e) => DictManagerOverlay.Visibility = Visibility.Collapsed;
        private void BtnClosePossibility_Click(object sender, RoutedEventArgs e) => PossibilityOverlay.Visibility = Visibility.Collapsed;
        private void BtnAddToDicoFromList_Click(object sender, RoutedEventArgs e) { if (ListPossibilities.SelectedItem is string w) { AddWordToDictionary(w); SecurityManager.SaveSecureData(_data, _password); ShowCustomAlert($"'{w}' a été ajouté au dictionnaire.", "Ajout réussi", "#FFA500"); BtnActionDec_Click(null, null); PossibilityOverlay.Visibility = Visibility.Collapsed; } }

        private void RefreshNotesList() { ListNotes.ItemsSource = null; ListNotes.ItemsSource = _data.Notes.Keys.OrderBy(k => k.Length).ThenBy(k => k).ToList(); }
        private void BtnNewNote_Click(object sender, RoutedEventArgs e) { int i = 1; string t = $"Note {i}"; while (_data.Notes.ContainsKey(t)) { i++; t = $"Note {i}"; } _data.Notes.Add(t, ""); RefreshNotesList(); _currentNoteKey = t; TxtNoteTitle.Text = t; TxtNoteContent.Text = ""; ShowOverlayWithAnimation(NoteEditorOverlay, LaserBorderNote); }
        private void OnNoteClicked(object sender, MouseButtonEventArgs e) { if (sender is ListBoxItem item && item.DataContext is string title && _data.Notes.ContainsKey(title)) { _currentNoteKey = title; TxtNoteTitle.Text = title; TxtNoteContent.Text = _data.Notes[title]; ShowOverlayWithAnimation(NoteEditorOverlay, LaserBorderNote); } }
        private void OnNoteRightClick(object sender, MouseButtonEventArgs e) { if (sender is ListBoxItem item) { item.IsSelected = true; item.Focus(); } }
        private void BtnSaveNote_Click(object sender, RoutedEventArgs e) { if (_currentNoteKey != null) { string c = TxtNoteContent.Text; string t = TxtNoteTitle.Text; if (t != _currentNoteKey) { _data.Notes.Remove(_currentNoteKey); if (_data.Notes.ContainsKey(t)) t += "_Copy"; _data.Notes.Add(t, c); } else _data.Notes[_currentNoteKey] = c; SecurityManager.SaveSecureData(_data, _password); RefreshNotesList(); NoteEditorOverlay.Visibility = Visibility.Collapsed; } }
        private void BtnCloseNote_Click(object sender, RoutedEventArgs e) => NoteEditorOverlay.Visibility = Visibility.Collapsed;
        private void CtxRename_Click(object sender, RoutedEventArgs e) { if (ListNotes.SelectedItem is string t) { _noteToRename = t; TxtRenameInput.Text = t; ShowOverlayWithAnimation(RenameOverlay, LaserBorderRename); } }
        private void BtnConfirmRename_Click(object sender, RoutedEventArgs e) { string n = TxtRenameInput.Text.Trim(); if (!string.IsNullOrEmpty(n) && _noteToRename != null) { if (_data.Notes.ContainsKey(n) && n != _noteToRename) { ShowCustomAlert("Ce nom existe déjà.", "Erreur", "#FF5555"); return; } string c = _data.Notes[_noteToRename]; _data.Notes.Remove(_noteToRename); _data.Notes.Add(n, c); SecurityManager.SaveSecureData(_data, _password); RefreshNotesList(); } RenameOverlay.Visibility = Visibility.Collapsed; }
        private void BtnCancelRename_Click(object sender, RoutedEventArgs e) => RenameOverlay.Visibility = Visibility.Collapsed;
        private void CtxDelete_Click(object sender, RoutedEventArgs e) { if (ListNotes.SelectedItem is string t && MessageBox.Show("Supprimer ?", "Confirmer", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { _data.Notes.Remove(t); SecurityManager.SaveSecureData(_data, _password); RefreshNotesList(); } }
        private void CtxExport_Click(object sender, RoutedEventArgs e) { if (ListNotes.SelectedItem is string t) { SaveFileDialog sfd = new SaveFileDialog { Filter = "Void Note (*.voidn)|*.voidn", FileName = $"{t}.voidn" }; if (sfd.ShowDialog() == true) { File.WriteAllText(sfd.FileName, JsonSerializer.Serialize(new { title = t, content = _data.Notes[t] })); ShowCustomAlert("Note exportée avec succès.", "Export", "#8A2BE2"); } } }

        private class VoidNoteExport { public string title { get; set; } public string content { get; set; } }

        private void BtnImportNote_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Void Note (*.voidn)|*.voidn" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(ofd.FileName);
                    var noteData = JsonSerializer.Deserialize<VoidNoteExport>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (noteData != null && !string.IsNullOrEmpty(noteData.title))
                    {
                        string newTitle = noteData.title;
                        string originalTitle = newTitle;
                        int i = 1;

                        while (_data.Notes.ContainsKey(newTitle))
                        {
                            newTitle = $"{originalTitle} ({i})";
                            i++;
                        }

                        _data.Notes.Add(newTitle, noteData.content ?? "");
                        SecurityManager.SaveSecureData(_data, _password);
                        RefreshNotesList();
                        ShowCustomAlert("Note importée avec succès !", "Import", "#8A2BE2");
                    }
                    else ShowCustomAlert("Fichier de note invalide ou corrompu.", "Erreur d'import", "#FF5555");
                }
                catch { ShowCustomAlert("Erreur lors de la lecture du fichier .voidn.", "Erreur", "#FF5555"); }
            }
        }

        private void BtnToggleSettings_Click(object sender, RoutedEventArgs e) { if (SettingsPanel.Visibility == Visibility.Collapsed) { SettingsPanel.Visibility = Visibility.Visible; NotesControls.Visibility = Visibility.Collapsed; ListNotes.Visibility = Visibility.Collapsed; BtnOpenSettings.Visibility = Visibility.Collapsed; PanelSettingsControls.Visibility = Visibility.Visible; LblNotesTitle.Visibility = Visibility.Collapsed; } else { SettingsPanel.Visibility = Visibility.Collapsed; NotesControls.Visibility = Visibility.Visible; ListNotes.Visibility = Visibility.Visible; LblNotesTitle.Visibility = Visibility.Visible; PanelSettingsControls.Visibility = Visibility.Collapsed; BtnOpenSettings.Visibility = Visibility.Visible; } }
        private void BtnExportVoid_Click(object sender, RoutedEventArgs e) { SaveFileDialog s = new SaveFileDialog { Filter = "Backup (*.void)|*.void", FileName = "backup.void" }; if (s.ShowDialog() == true && SecurityManager.DatabaseExists()) { File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.void"), s.FileName, true); ShowCustomAlert("Backup exporté.", "Succès", "#8A2BE2"); } }
        private void BtnImportVoid_Click(object sender, RoutedEventArgs e) { OpenFileDialog o = new OpenFileDialog { Filter = "Backup (*.void)|*.void" }; if (o.ShowDialog() == true) { try { File.Copy(o.FileName, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.void"), true); MessageBox.Show("L'application va redémarrer pour appliquer la sauvegarde."); System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName); Application.Current.Shutdown(); } catch { ShowCustomAlert("Erreur de restauration.", "Erreur", "#FF5555"); } } }
        private void BtnExportJson_Click(object sender, RoutedEventArgs e) { SaveFileDialog s = new SaveFileDialog { Filter = "JSON|*.json", FileName = "data.json" }; if (s.ShowDialog() == true) { File.WriteAllText(s.FileName, JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true })); ShowCustomAlert("Export JSON terminé.", "Succès", "#8A2BE2"); } }
        private void BtnImportJson_Click(object sender, RoutedEventArgs e) { OpenFileDialog o = new OpenFileDialog { Filter = "JSON|*.json" }; if (o.ShowDialog() == true) { try { var d = JsonSerializer.Deserialize<VoidData>(File.ReadAllText(o.FileName), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); if (d != null) { if (d.Dictionary != null) foreach (var w in d.Dictionary) AddWordToDictionary(w); if (d.Notes != null) foreach (var n in d.Notes) { if (!_data.Notes.ContainsKey(n.Key)) _data.Notes.Add(n.Key, n.Value); else _data.Notes.Add(n.Key + " (Import)", n.Value); } SecurityManager.SaveSecureData(_data, _password); RefreshNotesList(); ShowCustomAlert("Import JSON terminé.", "Succès", "#8A2BE2"); } } catch { ShowCustomAlert("Fichier JSON Invalide.", "Erreur", "#FF5555"); } } }

        public class RuleDisplayItem { public ShiftRule Rule { get; set; } public string DisplayText { get; set; } }
        private List<ShiftRule> _tempRules = new();
        private List<VoidCharMapping> _tempMappings = new();

        private void BtnOpenCryptoConfig_Click(object sender, RoutedEventArgs e) { TxtCryptoPassword.Clear(); ShowOverlayWithAnimation(CryptoAuthOverlay, LaserBorderCrypto); TxtCryptoPassword.Focus(); }

        private void BtnValidateCryptoAuth_Click(object sender, RoutedEventArgs e)
        {
            string inputPass = TxtCryptoPassword.Password;
            if (string.IsNullOrWhiteSpace(inputPass)) return;

            if (string.IsNullOrEmpty(_data.DictProtectionHash))
            {
                ShowCustomAlert("Veuillez définir un mot de passe dans 'Gérer dictionnaire' d'abord.", "Sécurité requise", "#FFA500");
                CryptoAuthOverlay.Visibility = Visibility.Collapsed;
                return;
            }

            string hashedInput = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(inputPass)));

            if (hashedInput == _data.DictProtectionHash)
            {
                CryptoAuthOverlay.Visibility = Visibility.Collapsed;
                OpenForgeUI();
            }
            else
            {
                ShowCustomAlert("Mot de passe incorrect.", "Accès refusé", "#FF5555");
                TxtCryptoPassword.Clear();
            }
        }

        private void TxtCryptoPassword_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) BtnValidateCryptoAuth_Click(sender, e); }
        private void BtnCancelCryptoAuth_Click(object sender, RoutedEventArgs e) { CryptoAuthOverlay.Visibility = Visibility.Collapsed; TxtCryptoPassword.Clear(); }

        private void OpenForgeUI()
        {
            TxtMode1Name.Text = _data.EngineConfig.Mode1Name;
            TxtMode2Name.Text = _data.EngineConfig.Mode2Name;
            TxtResetChars.Text = _data.EngineConfig.ResetCharacters;

            _tempRules.Clear();
            foreach (var r in _data.EngineConfig.Rules) _tempRules.Add(new ShiftRule { Target = r.Target, Shifts = new List<int>(r.Shifts) });

            _tempMappings.Clear();
            foreach (var m in _data.EngineConfig.VoidMappings) _tempMappings.Add(new VoidCharMapping { Key = m.Key, Value = m.Value });

            RefreshRuleList();
            ListVoidAlphabet.ItemsSource = null;
            ListVoidAlphabet.ItemsSource = _tempMappings;

            ShowOverlayWithAnimation(CryptoForgeOverlay, LaserBorderForge);
        }

        private void TabMode_Click(object sender, RoutedEventArgs e)
        {
            if (TabMode1.IsChecked == true) { PanelMode1.Visibility = Visibility.Visible; PanelMode2.Visibility = Visibility.Collapsed; }
            else { PanelMode1.Visibility = Visibility.Collapsed; PanelMode2.Visibility = Visibility.Visible; }
        }

        private void RefreshRuleList() { ListRules.ItemsSource = null; ListRules.ItemsSource = _tempRules.Select(r => new RuleDisplayItem { Rule = r, DisplayText = $"Cible : {r.Target}   ➔   Décalages : [{string.Join(", ", r.Shifts)}]" }).ToList(); }
        private void CmbRuleTarget_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (TxtSpecificLetter != null) { if (CmbRuleTarget.SelectedIndex == 2) TxtSpecificLetter.Visibility = Visibility.Visible; else TxtSpecificLetter.Visibility = Visibility.Collapsed; } }

        private void BtnAddRule_Click(object sender, RoutedEventArgs e)
        {
            string target = ((ComboBoxItem)CmbRuleTarget.SelectedItem).Content.ToString();
            if (target == "Lettre Spécifique")
            {
                if (string.IsNullOrWhiteSpace(TxtSpecificLetter.Text)) { ShowCustomAlert("Entrez une lettre (ex: E).", "Erreur", "#FF5555"); return; }
                target = "Lettre:" + TxtSpecificLetter.Text.ToUpper();
            }

            string shiftsRaw = TxtRuleShifts.Text;
            var shifts = new List<int>();
            var parts = shiftsRaw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts) { if (int.TryParse(p.Trim(), out int val)) shifts.Add(val); else { ShowCustomAlert("Format invalide.", "Erreur", "#FF5555"); return; } }
            if (shifts.Count == 0) { ShowCustomAlert("Entrez au moins un décalage.", "Erreur", "#FF5555"); return; }

            _tempRules.Add(new ShiftRule { Target = target, Shifts = shifts });
            RefreshRuleList(); TxtRuleShifts.Clear();
        }

        private void BtnDeleteRule_Click(object sender, RoutedEventArgs e) { if (sender is Button btn && btn.Tag is ShiftRule ruleToDelete) { _tempRules.Remove(ruleToDelete); RefreshRuleList(); } }

        private void BtnSaveForge_Click(object sender, RoutedEventArgs e)
        {
            _data.EngineConfig.Mode1Name = TxtMode1Name.Text.Trim();
            _data.EngineConfig.Mode2Name = TxtMode2Name.Text.Trim();
            _data.EngineConfig.ResetCharacters = TxtResetChars.Text;

            _data.EngineConfig.Rules.Clear();
            foreach (var r in _tempRules) _data.EngineConfig.Rules.Add(new ShiftRule { Target = r.Target, Shifts = new List<int>(r.Shifts) });

            _data.EngineConfig.VoidMappings.Clear();
            foreach (var m in _tempMappings) _data.EngineConfig.VoidMappings.Add(new VoidCharMapping { Key = m.Key, Value = m.Value });

            SecurityManager.SaveSecureData(_data, _password);
            Radio1.Content = _data.EngineConfig.Mode1Name; RadioVoid.Content = _data.EngineConfig.Mode2Name;
            CryptoForgeOverlay.Visibility = Visibility.Collapsed;
            ShowCustomAlert("Configuration cryptographique sauvegardée !", "Succès", "#8A2BE2");
        }

        private void BtnCancelForge_Click(object sender, RoutedEventArgs e) => CryptoForgeOverlay.Visibility = Visibility.Collapsed;


        private class DictExportData
        {
            public HashSet<string> Dictionary { get; set; }
            public HashSet<string> UserAddedWords { get; set; }
        }

        private void ExportEncrypted<T>(T dataObject, string defaultExt, string filter)
        {
            SaveFileDialog sfd = new SaveFileDialog { Filter = filter, DefaultExt = defaultExt };
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    string json = JsonSerializer.Serialize(dataObject);
                    byte[] salt = new byte[16];
                    RandomNumberGenerator.Fill(salt);
                    
                    using var pbkdf2 = new Rfc2898DeriveBytes(_password, salt, 100000, HashAlgorithmName.SHA256);
                    byte[] key = pbkdf2.GetBytes(32); 
                    byte[] iv = pbkdf2.GetBytes(16);

                    using Aes aes = Aes.Create();
                    aes.Key = key;
                    aes.IV = iv;

                    using MemoryStream ms = new MemoryStream();
                    ms.Write(salt, 0, salt.Length); 
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(json);
                        cs.Write(plainBytes, 0, plainBytes.Length);
                    }
                    File.WriteAllBytes(sfd.FileName, ms.ToArray());
                    ShowCustomAlert("Exportation chiffrée réussie.", "Succès", "#8A2BE2");
                }
                catch { ShowCustomAlert("Erreur lors du chiffrement ou de l'exportation.", "Erreur", "#FF5555"); }
            }
        }

        private T ImportEncrypted<T>(string filter)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = filter };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    byte[] cipherBytes = File.ReadAllBytes(ofd.FileName);
                    byte[] salt = new byte[16];
                    Array.Copy(cipherBytes, 0, salt, 0, 16);

                    using var pbkdf2 = new Rfc2898DeriveBytes(_password, salt, 100000, HashAlgorithmName.SHA256);
                    byte[] key = pbkdf2.GetBytes(32);
                    byte[] iv = pbkdf2.GetBytes(16);

                    using Aes aes = Aes.Create();
                    aes.Key = key;
                    aes.IV = iv;

                    using MemoryStream ms = new MemoryStream(cipherBytes, 16, cipherBytes.Length - 16);
                    using CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
                    using StreamReader sr = new StreamReader(cs);
                    string json = sr.ReadToEnd();

                    return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch { ShowCustomAlert("Fichier invalide ou mot de passe incorrect.", "Accès Refusé", "#FF5555"); }
            }
            return default;
        }

        private void BtnExportDict_Click(object sender, RoutedEventArgs e)
        {
            var exportData = new DictExportData { Dictionary = _data.Dictionary, UserAddedWords = _data.UserAddedWords };
            ExportEncrypted(exportData, ".voidd", "Dictionnaire Crypté (*.voidd)|*.voidd");
        }

        private void BtnImportDict_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Dictionnaire Crypté (*.voidd)|*.voidd|Fichier texte (*.txt)|*.txt"
            };

            if (ofd.ShowDialog() != true) return;

            if (ofd.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var words = File.ReadAllLines(ofd.FileName)
                                   .Select(w => w.Trim().ToLower())
                                   .Where(w => !string.IsNullOrWhiteSpace(w))
                                   .ToList();

                    _data.Dictionary.Clear();
                    foreach (var w in _data.UserAddedWords)
                        _data.Dictionary.Add(w);

                    foreach (var w in words)
                        _data.Dictionary.Add(w);

                    SecurityManager.SaveSecureData(_data, _password);
                    RefreshDictList(TxtSearchDict.Text);
                    ShowCustomAlert("Dictionnaire .txt importé avec succès.", "Import réussi", "#8A2BE2");
                }
                catch { ShowCustomAlert("Erreur lors de la lecture du fichier .txt.", "Erreur", "#FF5555"); }
                return;
            }

            var imported = ImportEncrypted<DictExportData>(ofd.FileName);
            if (imported != null)
            {
                if (imported.Dictionary != null) foreach (var w in imported.Dictionary) AddWordToDictionary(w);
                if (imported.UserAddedWords != null) foreach (var w in imported.UserAddedWords) _data.UserAddedWords.Add(w);
                SecurityManager.SaveSecureData(_data, _password);
                RefreshDictList(TxtSearchDict.Text);
                ShowCustomAlert("Dictionnaire fusionné avec succès.", "Import réussi", "#8A2BE2");
            }
        }
        private void BtnHelpForge_Click(object sender, RoutedEventArgs e)
        {
            string helpText = "• Mode 1 (Décalages) : Applique un chiffrement polyalphabétique dynamique. Vous pouvez attribuer des décalages multiples ciblant spécifiquement les voyelles, les consonnes ou une lettre exacte.\n\n" +
                              "• Mode 2 (Void) : Remplacement absolu. Chaque lettre est d'abord décalée (via le Mode 1), puis remplacée par le symbole ou la chaîne stricte que vous définissez.\n\n" +
                              "• Caractères Reset : Symboles (ex: .?!) qui, lorsqu'ils sont lus, forcent la séquence de décalage à recommencer depuis le début.\n\n" +
                              "💡 Astuce : Cliquez sur les titres 'Nom Mode 1', 'Nom Mode 2' ou 'Caractères Reset' pour comprendre leur mécanique exacte.";

            ShowCustomAlert(helpText, "Guide Cryptographique", "#FFA500");
        }

        private void HelpMode1_Click(object sender, MouseButtonEventArgs e)
        {
            string helpText = "Le Mode 1 opère selon un algorithme de substitution par blocs avec un pointeur de recherche dynamique. Voici ses spécifications :\n\n" +

                              "1. MAINTIEN DE BLOC\n" +
                              "Tant que les lettres lues consécutivement sont de même nature (ex: suite de consonnes), le moteur reste ancré sur la règle active et l'applique en boucle. Il ne cherche pas à avancer dans la liste.\n\n" +

                              "2. RECHERCHE ET SAUT DE RÈGLE (Le Pointeur)\n" +
                              "Lorsque la nature du texte change (ex: passage d'une consonne à une voyelle), le pointeur quitte sa position et descend chercher la prochaine règle compatible. S'il croise une règle de nature opposée durant sa recherche, il la saute.\n\n" +

                              "3. ROTATION CYCLIQUE\n" +
                              "Le retour à la Règle 1 n'est pas automatique. Le pointeur ne fait une boucle vers le début de la liste QUE s'il est en phase de 'Recherche' (suite à un changement de nature dans le texte) et qu'il atteint la fin des règles disponibles.\n\n" +

                              "4. DÉCALAGES MULTIPLES\n" +
                              "Pour une règle à valeurs multiples (ex: [2, 5]), l'algorithme cycle sur ces valeurs tant que le bloc de lettres continu ne change pas de nature.\n" +
                              "*Un espace ne rompt pas un bloc. 'd d' est lu comme une seule continuité de consonnes.\n\n" +

                              "5. RÉINITIALISATION FORCÉE\n" +
                              "Si configurée, la rencontre d'un caractère spécifique de réinitialisation force le pointeur global à revenir à la Règle 1, brisant le cycle avant même qu'il n'atteigne la fin de la liste.\n\n";


            ShowCustomAlert(helpText, "Spécifications Techniques : Mode 1", "#8A2BE2");
        }
       
        private void HelpMode2_Click(object sender, MouseButtonEventArgs e)
        {
            string helpText = "Le Mode 2 est une combinaison :\n\n" +
                              "1. Le texte subit d'abord les décalages mécaniques du Mode 1 en arrière-plan.\n" +
                              "2. Ensuite, chaque lettre résultante est remplacée par le symbole défini dans votre Alphabet.\n\n" +
                              "C'est un cryptage polyalphabétique combiné à une substitution visuelle stricte.";
            ShowCustomAlert(helpText, "Comprendre le Mode 2", "#8A2BE2");
        }

        private void HelpReset_Click(object sender, MouseButtonEventArgs e)
        {
            string helpText = "Les caractères de Reset (ex: .?!) sont des déclencheurs.\n\n" +
                              "Dès que le programme croise l'un de ces caractères dans votre texte, TOUTES les boucles temporelles repartent de zéro.\n\n" +
                              "Cela permet, par exemple, que chaque nouvelle phrase commence avec un cryptage neuf et impossible à analyser par fréquences.";
            ShowCustomAlert(helpText, "Caractères de Reset", "#8A2BE2");
        }

        private void BtnExportConfig_Click(object sender, RoutedEventArgs e)
        {
            ExportEncrypted(_data.EngineConfig, ".voidc", "Configuration Cryptée (*.voidc)|*.voidc");
        }

        private void BtnImportConfig_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Configuration Cryptée (*.voidc)|*.voidc" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    byte[] cipherBytes = File.ReadAllBytes(ofd.FileName);
                    byte[] salt = new byte[16];
                    Array.Copy(cipherBytes, 0, salt, 0, 16);

                    using var pbkdf2 = new Rfc2898DeriveBytes(_password, salt, 100000, HashAlgorithmName.SHA256);
                    byte[] key = pbkdf2.GetBytes(32);
                    byte[] iv = pbkdf2.GetBytes(16);

                    using Aes aes = Aes.Create();
                    aes.Key = key;
                    aes.IV = iv;

                    using MemoryStream ms = new MemoryStream(cipherBytes, 16, cipherBytes.Length - 16);
                    using CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
                    using StreamReader sr = new StreamReader(cs);
                    string json = sr.ReadToEnd();

                    dynamic imported = JsonSerializer.Deserialize(json, _data.EngineConfig.GetType(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (imported != null)
                    {
                        _data.EngineConfig = imported;
                        SecurityManager.SaveSecureData(_data, _password);
                        OpenForgeUI(); 
                        ShowCustomAlert("Configuration importée et appliquée.", "Import réussi", "#8A2BE2");
                    }
                }
                catch { ShowCustomAlert("Fichier invalide ou mot de passe incorrect.", "Accès Refusé", "#FF5555"); }
            }
        }
        private void BtnHelpPossibility_Click(object sender, RoutedEventArgs e)
        {
            string helpText = "⚠️ Mots avec accents :\n" +
                              "Si le mot que vous cherchez n'est pas dans cette liste et qu'il possédait un accent : le cryptage formate les accents pour garantir le calcul mathématique.\n\n" +
                              "🛠️ Comment l'ajouter au dictionnaire ?\n" +
                              "1. Cherchez simplement le mot SANS accent dans la liste ci-dessous (utilisez la loupe si besoin).\n" +
                              "2. Cliquez dessus pour le sélectionner.\n" +
                              "3. Cliquez sur le bouton violet 'Ajouter & Fermer' en bas pour l'ajouter au dictionnaire.";

            ShowCustomAlert(helpText, "Mots Introuvables", "#FFA500");
        }

        private Point _startPoint;
        private DragAdorner _dragAdorner;
        private AdornerLayer _adornerLayer;
        private double _mouseYOffset;
        private double _fixedX;

        private int _originalIndex = -1;
        private int _currentIndex = -1;
        private double _itemHeight = 0;

        private void ListRules_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void ListRules_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    ListBox listBox = sender as ListBox;
                    ListBoxItem listBoxItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);

                    if (listBoxItem == null) return;

                    var draggedItem = listBox.ItemContainerGenerator.ItemFromContainer(listBoxItem) as RuleDisplayItem;
                    if (draggedItem != null)
                    {
                        _originalIndex = _tempRules.IndexOf(draggedItem.Rule);
                        _currentIndex = _originalIndex;
                        _itemHeight = listBoxItem.ActualHeight; 

                        Point mousePosInItem = e.GetPosition(listBoxItem);
                        _mouseYOffset = mousePosInItem.Y;

                        Point itemPosInList = listBoxItem.TranslatePoint(new Point(0, 0), listBox);
                        _fixedX = itemPosInList.X;

                        _adornerLayer = AdornerLayer.GetAdornerLayer(listBox);
                        _dragAdorner = new DragAdorner(listBox, listBoxItem);
                        _adornerLayer.Add(_dragAdorner);
                        _dragAdorner.UpdatePosition(_fixedX, itemPosInList.Y);

                        listBoxItem.Opacity = 0.0;

                        DataObject dragData = new DataObject("RuleDisplayItemFormat", draggedItem);

                        DragDrop.DoDragDrop(listBoxItem, dragData, DragDropEffects.Move);

                        if (_dragAdorner != null)
                        {
                            _adornerLayer.Remove(_dragAdorner);
                            _dragAdorner = null;
                        }

                        for (int i = 0; i < ListRules.Items.Count; i++)
                        {
                            var container = ListRules.ItemContainerGenerator.ContainerFromIndex(i) as UIElement;
                            if (container != null)
                            {
                                container.RenderTransform = null;
                                container.Opacity = 1.0;
                            }
                        }
                    }
                }
            }
        }

        private void ListRules_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (_dragAdorner != null)
            {
                Point mousePosInList = e.GetPosition(ListRules);
                _dragAdorner.UpdatePosition(_fixedX, mousePosInList.Y - _mouseYOffset);

                int targetIndex = (int)(mousePosInList.Y / _itemHeight);
                if (targetIndex < 0) targetIndex = 0;
                if (targetIndex >= _tempRules.Count) targetIndex = _tempRules.Count - 1;

                if (targetIndex != _currentIndex)
                {
                    _currentIndex = targetIndex;

                    for (int i = 0; i < ListRules.Items.Count; i++)
                    {
                        var container = ListRules.ItemContainerGenerator.ContainerFromIndex(i) as UIElement;
                        if (container == null || i == _originalIndex) continue; 
                        double targetY = 0; 
                        if (_currentIndex > _originalIndex && i > _originalIndex && i <= _currentIndex)
                            targetY = -_itemHeight; 

                        else if (_currentIndex < _originalIndex && i >= _currentIndex && i < _originalIndex)
                            targetY = _itemHeight; 

                        TranslateTransform trans = container.RenderTransform as TranslateTransform;
                        if (trans == null)
                        {
                            trans = new TranslateTransform();
                            container.RenderTransform = trans;
                        }

                        DoubleAnimation anim = new DoubleAnimation(targetY, TimeSpan.FromMilliseconds(250))
                        {
                            EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
                        };
                        trans.BeginAnimation(TranslateTransform.YProperty, anim);
                    }
                }
            }
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void ListRules_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            e.UseDefaultCursors = false;
            Mouse.SetCursor(Cursors.SizeAll);
            e.Handled = true;
        }

        private void ListRules_Drop(object sender, DragEventArgs e)
        {
            if (_originalIndex != -1 && _currentIndex != -1 && _originalIndex != _currentIndex)
            {
                var rule = _tempRules[_originalIndex];
                _tempRules.RemoveAt(_originalIndex);
                _tempRules.Insert(_currentIndex, rule);

                RefreshRuleList(); 
            }
        }

        private void ListRules_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("RuleDisplayItemFormat") || sender == e.Source) e.Effects = DragDropEffects.None;
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do { if (current is T ancestor) return ancestor; current = VisualTreeHelper.GetParent(current); } while (current != null);
            return null;
        }


        public class DragAdorner : System.Windows.Documents.Adorner
        {
            private System.Windows.Shapes.Rectangle _child;
            private double _leftOffset;
            private double _topOffset;

            public DragAdorner(UIElement adornedElement, UIElement adornedElementToDrag)
                : base(adornedElement)
            {
                this.IsHitTestVisible = false;

                int width = (int)adornedElementToDrag.RenderSize.Width;
                int height = (int)adornedElementToDrag.RenderSize.Height;

                var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                var drawingVisual = new DrawingVisual();
                using (var context = drawingVisual.RenderOpen())
                {
                    context.DrawRectangle(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#161616")), null, new Rect(0, 0, width, height));
                    context.DrawRectangle(new VisualBrush(adornedElementToDrag), null, new Rect(0, 0, width, height));
                }
                rtb.Render(drawingVisual);

                _child = new System.Windows.Shapes.Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = new ImageBrush(rtb),
                    Opacity = 1.0,
                    IsHitTestVisible = false
                };
            }

            protected override Visual GetVisualChild(int index) => _child;
            protected override int VisualChildrenCount => 1;
            protected override Size MeasureOverride(Size constraint) { _child.Measure(constraint); return _child.DesiredSize; }
            protected override Size ArrangeOverride(Size finalSize) { _child.Arrange(new Rect(_child.DesiredSize)); return finalSize; }

            public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
            {
                GeneralTransformGroup result = new GeneralTransformGroup();
                result.Children.Add(base.GetDesiredTransform(transform));
                result.Children.Add(new TranslateTransform(_leftOffset, _topOffset));
                return result;
            }

            public void UpdatePosition(double left, double top)
            {
                _leftOffset = left;
                _topOffset = top;
                AdornerLayer layer = this.Parent as AdornerLayer;
                if (layer != null) layer.Update(this.AdornedElement);
            }
        }
    }
}