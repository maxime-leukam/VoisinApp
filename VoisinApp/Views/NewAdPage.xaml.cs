namespace VoisinApp.Views;

public partial class NewAdPage : ContentPage
{
    private const double MaxContentWidth = 480;

    private static readonly Color Green = Color.FromArgb("#186B58");
    private static readonly Color Dark = Color.FromArgb("#17382F");

    private string _selectedType = "À vendre";
    public NewAdPage()
	{
		InitializeComponent();
        SelectType(_selectedType);
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

    private async void OnAddPhotosTapped(object? sender, TappedEventArgs e)
    {
        // TODO : sélectionner jusqu'à 5 photos (MediaPicker.Default.PickPhotosAsync)
        await Task.CompletedTask;
    }

    private void OnTypeTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border segment)
            SelectType(segment.ClassId);
    }

    private void SelectType(string type)
    {
        _selectedType = type;

        foreach (var segment in TypeGrid.Children.OfType<Border>())
        {
            bool isSelected = segment.ClassId == type;

            segment.BackgroundColor = isSelected ? Colors.White : Colors.Transparent;

            if (segment.Content is Label label)
            {
                label.TextColor = isSelected ? Green : Dark;
                label.FontAttributes = isSelected ? FontAttributes.Bold : FontAttributes.None;
            }
        }

        // Pas de prix pour un objet à donner
        PriceSection.IsVisible = type != "À donner";
    }

    private async void OnPublishClicked(object? sender, EventArgs e)
    {
        // TODO : envoyer l'annonce au back-end avec :
        // _selectedType, TitleEntry.Text, DescriptionEditor.Text, PriceEntry.Text
        await Task.CompletedTask;
    }
}