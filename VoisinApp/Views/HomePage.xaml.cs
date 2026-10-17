namespace VoisinApp.Views;

public partial class HomePage : ContentPage
{
	private const double MaxContentWidth = 480;
	public HomePage()
	{
		InitializeComponent();
	}

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width > 0)
            Container.WidthRequest = Math.Min(width, MaxContentWidth);
    }

    // TODO : remplacer par les vraies données de l'utilisateur (back-end)
    // GreetingLabel.Text = $"Bonjour {prenom}";
    // ZoneLabel.Text = $"{quartier} ›";

    private async void OnNotificationsTapped(object? sender, TappedEventArgs e)
    {
        // TODO : page des notifications
        await Task.CompletedTask;
    }

    private async void OnAskHelpClicked(object? sender, EventArgs e)
    {
        // TODO : await Shell.Current.GoToAsync(nameof(HelpRequestPage));
        await Shell.Current.GoToAsync(nameof(HelpRequestPage));
    }

    private async void OnAskHelpTapped(object? sender, TappedEventArgs e)
    {
        // TODO : await Shell.Current.GoToAsync(nameof(HelpRequestPage));
        await Shell.Current.GoToAsync(nameof(HelpRequestPage));
    }

    private async void OnPublishAdTapped(object? sender, TappedEventArgs e)
    {
        // TODO : await Shell.Current.GoToAsync(nameof(NewAdPage));
        await Shell.Current.GoToAsync(nameof(NewAdPage));
        await Task.CompletedTask;
    }

    private async void OnEventsTapped(object? sender, TappedEventArgs e)
    {
        // TODO : await Shell.Current.GoToAsync(nameof(EventsPage));
        await Shell.Current.GoToAsync(nameof(EventsPage));
        await Task.CompletedTask;
    }

    private async void OnReportTapped(object? sender, TappedEventArgs e)
    {
        // TODO : await Shell.Current.GoToAsync(nameof(ReportPage));
        await Shell.Current.GoToAsync(nameof(ReportPage));
        await Task.CompletedTask;
    }

    private async void OnSeeAllTapped(object? sender, TappedEventArgs e)
    {
        // TODO : liste complète des publications du quartier
        await Task.CompletedTask;
    }
}