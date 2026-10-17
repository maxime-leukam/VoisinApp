namespace VoisinApp.Views;

public partial class EventDetailPage : ContentPage
{
    private const double MaxContentWidth = 480;

    private bool _isGoing;
    public EventDetailPage()
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

    // Bascule visuelle "Je participe" / "Vous participez" (sans back-end)
    private void OnJoinClicked(object? sender, EventArgs e)
    {
        _isGoing = !_isGoing;

        if (_isGoing)
        {
            JoinButton.Text = "✓  Vous participez";
            JoinButton.BackgroundColor = Color.FromArgb("#17382F");
            ParticipantsLabel.Text = "43 voisins participent";
        }
        else
        {
            JoinButton.Text = "✓  Je participe";
            JoinButton.BackgroundColor = Color.FromArgb("#186B58");
            ParticipantsLabel.Text = "42 voisins participent";
        }

        // TODO : enregistrer la participation côté back-end
    }
}