namespace VoisinApp.Views;

public partial class HelpRequestPage : ContentPage
{
    private const double MaxContentWidth = 480;

    private static readonly Color Green = Color.FromArgb("#186B58");
    private static readonly Color Dark = Color.FromArgb("#17382F");
    private static readonly Color Border = Color.FromArgb("#E6E4DC");
    private static readonly Color SelectedBackground = Color.FromArgb("#EFF6F1");

    private string _selectedCategory = "Bricolage";

    public HelpRequestPage()
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
        if (sender is Border border)
            SelectCategory(border.ClassId);
    }

    // Met en évidence la catégorie choisie
    private void SelectCategory(string category)
    {
        _selectedCategory = category;

        foreach (var border in CategoriesGrid.Children.OfType<Border>())
        {
            bool isSelected = border.ClassId == category;

            border.Stroke = new SolidColorBrush(isSelected ? Green : Border);
            border.StrokeThickness = isSelected ? 2 : 1;
            border.BackgroundColor = isSelected ? SelectedBackground : Colors.White;

            if (border.Content is Label label)
                label.TextColor = isSelected ? Green : Dark;
        }
    }

    private async void OnPublishClicked(object? sender, EventArgs e)
    {
        // TODO : envoyer la demande au back-end avec :
        // _selectedCategory, TitleEntry.Text, DescriptionEditor.Text, UrgentSwitch.IsToggled
        await Task.CompletedTask;
    }
}