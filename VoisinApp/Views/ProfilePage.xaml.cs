namespace VoisinApp.Views;

public partial class ProfilePage : ContentPage
{
    private const double MaxContentWidth = 480;

    public ProfilePage()
    {
        InitializeComponent();

        // TODO : remplacer par les vraies données de l'utilisateur (back-end)
        // NameLabel.Text = utilisateur.NomComplet;
        // InitialsLabel.Text = utilisateur.Initiales;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width > 0)
            Container.WidthRequest = Math.Min(width, MaxContentWidth);
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        // TODO : page de modification du profil
        await Task.CompletedTask;
    }

    private async void OnNotificationsTapped(object? sender, TappedEventArgs e)
    {
        // TODO : réglages des notifications
        await Task.CompletedTask;
    }

    private async void OnZoneTapped(object? sender, TappedEventArgs e)
    {
        // TODO : choix de la zone et du rayon
        await Task.CompletedTask;
    }

    private async void OnPrivacyTapped(object? sender, TappedEventArgs e)
    {
        // TODO : confidentialité et sécurité
        await Task.CompletedTask;
    }

    private async void OnLogoutTapped(object? sender, TappedEventArgs e)
    {
        bool confirm = await DisplayAlert("Se déconnecter",
                                          "Voulez-vous vraiment vous déconnecter ?",
                                          "Oui", "Annuler");
        if (!confirm)
            return;

        // TODO : supprimer la session côté back-end / stockage local
        await Shell.Current.GoToAsync("//WelcomePage");
    }
}
