using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using VoidTerminal.Services;

namespace VoidTerminal.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        TxtPassword.Focus();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        var (success, data) = SecurityManager.LoadSecureData(TxtPassword.Password);

        if (success && data != null)
        {
            new MainWindow(data, TxtPassword.Password).Show();
            this.Close();
        }
        else
        {
            ShowCustomAlert("Mot de passe incorrect.", "Accès refusé", "#FF5555");
            TxtPassword.Clear();
        }
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BtnLogin_Click(sender, e);
        }
    }

    private IInputElement _elementToFocusAfterAlert = null;

    private void ShowCustomAlert(string message, string title, string colorHex)
    {
        _elementToFocusAfterAlert = Keyboard.FocusedElement;

        LblAlertTitle.Text = title;
        LblAlertMessage.Text = message;
        LblAlertTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));

        CustomAlertOverlay.Visibility = Visibility.Visible;
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
        else
        {
            TxtPassword.Focus();
        }
    }

    private void OnOverlayClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == sender && sender is System.Windows.Controls.Grid grid)
        {
            grid.Visibility = Visibility.Collapsed;
            TxtPassword.Focus();
            e.Handled = true;
        }
    }
}