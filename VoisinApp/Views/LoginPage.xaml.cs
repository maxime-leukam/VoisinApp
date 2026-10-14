namespace VoisinApp.Views;

public partial class LoginPage : ContentPage
{
    private const double MaxContentWidth = 480;
    public LoginPage()
	{
		InitializeComponent();
	}

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width > 0)
            Container.WidthRequest = Math.Min(width, MaxContentWidth);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        // TODO : appeler le back-end pour authentifier l'utilisateur
        // EmailEntry.Text et PasswordEntry.Text contiennent les valeurs saisies
        await Task.CompletedTask;
        await Shell.Current.GoToAsync("//HomePage");
    }

    private async void OnForgotPasswordTapped(object? sender, TappedEventArgs e)
    {
        // TODO : page "Mot de passe oublié"
        await Task.CompletedTask;
    }

    private async void OnCreateAccountTapped(object? sender, TappedEventArgs e)
    {
        // TODO : page d'inscription
        await Task.CompletedTask;
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }
}