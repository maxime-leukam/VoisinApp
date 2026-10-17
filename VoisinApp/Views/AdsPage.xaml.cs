using VoisinApp.Models;

namespace VoisinApp.Views;

public partial class AdsPage : ContentPage
{
    private const double MaxContentWidth = 480;

    private static readonly Color Dark = Color.FromArgb("#17382F");
    private static readonly Color Border = Color.FromArgb("#E6E4DC");

    private string _selectedType = "Tout";

    // Données fictives en attendant le back-end
    private readonly List<Ad> _allAds =
    [
        new Ad { Title = "Fauteuil en velours", Type = "À vendre", Price = "35 000 FCFA",    Info = "À 350 m · Hier",      Image = "ad_armchair.jpg" },
        new Ad { Title = "Plantes aromatiques", Type = "À donner", Price = "Gratuit",        Info = "À 600 m · il y a 2 h", Image = "ad_plants.jpg" },
        new Ad { Title = "Vélo de ville",       Type = "À vendre", Price = "75 000 FCFA",    Info = "À 800 m · Lundi",      Image = "ad_bike.jpg" },
        new Ad { Title = "Soutien scolaire",    Type = "Services", Price = "3 000 FCFA / h", Info = "À 1 km · Mardi",       Image = "ad_tutoring.jpg" },
    ];

    public AdsPage()
    {
        InitializeComponent();
        UpdateChips();
        ApplyFilters();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width > 0)
            Container.WidthRequest = Math.Min(width, MaxContentWidth);
    }

    private void OnChipTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border chip)
            return;

        _selectedType = chip.ClassId;
        UpdateChips();
        ApplyFilters();
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void UpdateChips()
    {
        foreach (var chip in ChipsLayout.Children.OfType<Border>())
        {
            bool isSelected = chip.ClassId == _selectedType;

            chip.BackgroundColor = isSelected ? Dark : Colors.White;
            chip.Stroke = new SolidColorBrush(isSelected ? Dark : Border);

            if (chip.Content is Label label)
            {
                label.TextColor = isSelected ? Colors.White : Dark;
                label.FontAttributes = isSelected ? FontAttributes.Bold : FontAttributes.None;
            }
        }
    }

    private void ApplyFilters()
    {
        string search = SearchEntry.Text?.Trim() ?? "";

        AdsCollection.ItemsSource = _allAds
            .Where(a => _selectedType == "Tout" || a.Type == _selectedType)
            .Where(a => search.Length == 0 ||
                        a.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async void OnNewAdClicked(object? sender, EventArgs e)
    {
        // TODO : await Shell.Current.GoToAsync(nameof(NewAdPage));
        await Shell.Current.GoToAsync(nameof(NewAdPage));
        await Task.CompletedTask;
    }

    private async void OnAdTapped(object? sender, TappedEventArgs e)
    {
        // TODO : page de détail d'une annonce
        await Task.CompletedTask;
    }
}