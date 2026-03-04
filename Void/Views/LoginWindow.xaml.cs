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

        // BONUS : Place directement le curseur dans la case au démarrage de l'appli !
        TxtPassword.Focus();
    }

    // Permet de déplacer la fenêtre sans barre de titre
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
            // On appelle notre belle alerte rouge au lieu du vilain MessageBox
            ShowCustomAlert("Mot de passe incorrect.", "Accès refusé", "#FF5555");
            TxtPassword.Clear();
        }
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    // Appuie sur "Entrée" pour valider
    private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BtnLogin_Click(sender, e);
        }
    }

    // --- GESTIONNAIRE D'ALERTES SUR MESURE ---
    private IInputElement _elementToFocusAfterAlert = null;

    private void ShowCustomAlert(string message, string title, string colorHex)
    {
        _elementToFocusAfterAlert = Keyboard.FocusedElement;

        LblAlertTitle.Text = title;
        LblAlertMessage.Text = message;
        LblAlertTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));

        CustomAlertOverlay.Visibility = Visibility.Visible;
        BtnAlertOk.Focus(); // Donne le focus au bouton OK pour la touche Entrée
    }

    private void BtnCloseAlert_Click(object sender, RoutedEventArgs e)
    {
        CustomAlertOverlay.Visibility = Visibility.Collapsed;

        // On remet le curseur dans la case du mot de passe
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

    // Ferme l'alerte si on clique dans le vide (fond noir)
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