namespace VoisinApp.Views;

public partial class ReportPage : ContentPage
{
    private const double MaxContentWidth = 480;

    private static readonly Color Green = Color.FromArgb("#186B58");
    private static readonly Color Dark = Color.FromArgb("#17382F");
    private static readonly Color LineColor = Color.FromArgb("#E6E4DC");
    private static readonly Color SelectedBackground = Color.FromArgb("#EFF6F1");

    private string _selectedCategory = "Voirie";

    public ReportPage()
    {
        InitializeComponent();
        SelectCategory(_selectedCategory);
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

    private void OnCategoryTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border category)
            SelectCategory(category.ClassId);
    }

    private void SelectCategory(string name)
    {
        _selectedCategory = name;

        foreach (var category in CategoriesGrid.Children.OfType<Border>())
        {
            bool isSelected = category.ClassId == name;

            category.Stroke = new SolidColorBrush(isSelected ? Green : LineColor);
            category.StrokeThickness = isSelected ? 2 : 1;
            category.BackgroundColor = isSelected ? SelectedBackground : Colors.White;

            if (category.Content is HorizontalStackLayout row)
            {
                foreach (var label in row.Children.OfType<Label>())
                    label.TextColor = isSelected ? Green : Dark;
            }
        }
    }

    private async void OnMapTapped(object? sender, TappedEventArgs e)
    {
        // TODO : ouvrir une vraie carte pour placer un repère
        await Task.CompletedTask;
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        // TODO : envoyer le signalement au back-end avec :
        // _selectedCategory, LocationEntry.Text, DescriptionEditor.Text
        await Task.CompletedTask;
    }
}
