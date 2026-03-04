using System.Windows;
using System.Windows.Input;
using Microsoft.Win32; // Pour l'ouverture du fichier bloc-note
using VoidTerminal.Services;
using VoidTerminal.Models;

namespace VoidTerminal.Views;

public partial class SetupWindow : Window
{
    public SetupWindow()
    {
        InitializeComponent();
    }

    // ✅ Permet de déplacer la fenêtre en cliquant n'importe où sur le fond
    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }

    private void BtnInit_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtPassword.Password))
        {
            MessageBox.Show("Mot de passe maître requis pour crypter la base.");
            return;
        }

        // 1. Création de la structure de données initiale
        var newData = new VoidData();

        // 2. Importation de ton dictionnaire d'origine (le bloc-note)
        MessageBox.Show("Sélectionnez votre fichier dictionnaire (.txt) pour initialiser le lexique Void.");

        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*";

        if (openFileDialog.ShowDialog() == true)
        {
            SecurityManager.ImportExternalDictionary(newData, openFileDialog.FileName);
            MessageBox.Show($"{newData.Dictionary.Count} mots importés avec succès.");
        }

        // 3. Sauvegarde sécurisée avec ton mot de passe
        bool success = SecurityManager.SaveSecureData(newData, TxtPassword.Password);

        if (success)
        {
            MessageBox.Show("Système Void initialisé et dictionnaire synchronisé.");
            new LoginWindow().Show();
            this.Close();
        }
        else
        {
            MessageBox.Show("Erreur critique lors de la création du fichier data.void.");
        }
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}