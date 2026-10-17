using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace VoisinApp.Views;

public partial class EventsPage : ContentPage
{
    private const double MaxContentWidth = 480;

    private static readonly Color Green = Color.FromArgb("#186B58");
    private static readonly Color Dark = Color.FromArgb("#17382F");
    private static readonly Color Muted = Color.FromArgb("#5B6F66");

    private int _selectedDay = 1;
    public EventsPage()
	{
		InitializeComponent();
        BuildDays();
	}

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width > 0)
            Container.WidthRequest = Math.Min(width, MaxContentWidth);
    }

    // Bandeau de 5 jours : la veille, aujourd'hui et les 3 jours suivants
    private void BuildDays()
    {
        var fr = new CultureInfo("fr-FR");
        DaysGrid.Children.Clear();

        for (int i = 0; i < 5; i++)
        {
            DateTime date = DateTime.Today.AddDays(i - 1);

            string name = fr.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek)
                .TrimEnd('.')
                .ToUpperInvariant();
            if (name.Length > 3)
                name = name[..3];

            var dayLabel = new Label
            {
                Text = name,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center
            };

            var numberLabel = new Label
            {
                Text = date.Day.ToString(),
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center
            };

            var chip = new Border
            {
                ClassId = i.ToString(),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = new Thickness(0, 10),
                Content = new VerticalStackLayout
                {
                    Spacing = 2,
                    Children = { dayLabel, numberLabel }
                }
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += OnDayTapped;
            chip.GestureRecognizers.Add(tap);

            DaysGrid.Add(chip, i, 0);
        }

        UpdateDays();
    }

    private void OnDayTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border chip && int.TryParse(chip.ClassId, out int index))
        {
            _selectedDay = index;
            UpdateDays();
            // TODO : charger les événements du jour sélectionné (back-end)
        }
    }

    private void UpdateDays()
    {
        foreach (var chip in DaysGrid.Children.OfType<Border>())
        {
            bool isSelected = chip.ClassId == _selectedDay.ToString();

            chip.BackgroundColor = isSelected ? Green : Colors.Transparent;

            if (chip.Content is VerticalStackLayout stack)
            {
                foreach (var label in stack.Children.OfType<Label>())
                    label.TextColor = isSelected ? Colors.White : (label.FontSize > 12 ? Dark : Muted);
            }
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//HomePage");
    }

    private async void OnNewEventClicked(object? sender, EventArgs e)
    {
        // TODO : page de création d'événement
        await Task.CompletedTask;
    }

    private async void OnFeaturedEventTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(EventDetailPage));
    }

    private async void OnUpcomingEventTapped(object? sender, TappedEventArgs e)
    {
        // TODO : ouvrir le détail de l'événement choisi
        await Shell.Current.GoToAsync(nameof(EventDetailPage));
    }
}