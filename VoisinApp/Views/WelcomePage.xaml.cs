namespace VoisinApp.Views;

public partial class WelcomePage : ContentPage
{
    // Largeur maximale du contenu : évite l'étirement sur Windows, macOS et tablettes
    private const double MaxContentWidth = 480;
    public WelcomePage()
	{
		InitializeComponent();
	}

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width > 0)
            Container.WidthRequest = Math.Min(width, MaxContentWidth);
    }

    private async void OnJoinClicked(object? sender, EventArgs e)
    {
        // TODO : naviguer vers la page d'inscription quand elle sera créée
        await Shell.Current.GoToAsync(nameof(RegisterPage));
        await Task.CompletedTask;
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(LoginPage));
    }
}