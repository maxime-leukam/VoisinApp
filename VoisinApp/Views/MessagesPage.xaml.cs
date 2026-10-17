using VoisinApp.Models;

namespace VoisinApp.Views;

public partial class MessagesPage : ContentPage
{
    private const double MaxContentWidth = 480;

    // Données fictives en attendant le back-end
    private readonly List<Conversation> _all =
    [
        new Conversation { Initials = "GM", Name = "Grâce M.",                    Preview = "Bonjour, la perceuse est toujours dispo...", Time = "10:24", UnreadCount = 2, AvatarColorHex = "#FBE0D8" },
        new Conversation { Initials = "G·", Name = "Groupe · Jardin communautaire", Preview = "Serge : Je peux passer samedi matin",        Time = "Hier",  AvatarColorHex = "#DCEBE2" },
        new Conversation { Initials = "JE", Name = "Junior Etoundi",              Preview = "Merci encore pour le coup de main !",        Time = "Lun.",  AvatarColorHex = "#DCEAF0" },
        new Conversation { Initials = "ÉE", Name = "Équipe EntreNous",            Preview = "Votre signalement a été pris en compte.",    Time = "18 mai", AvatarColorHex = "#F7E9C6" },
    ];
    public MessagesPage()
	{
		InitializeComponent();
        ConversationsList.ItemsSource = _all;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width > 0)
            Container.WidthRequest = Math.Min(width, MaxContentWidth);
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        string search = e.NewTextValue?.Trim() ?? "";

        ConversationsList.ItemsSource = _all
            .Where(c => search.Length == 0 ||
                        c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        c.Preview.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async void OnConversationTapped(object? sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: Conversation conversation })
        {
            await Shell.Current.GoToAsync(nameof(ConversationPage),
                new Dictionary<string, object> { ["Name"] = conversation.Name });
        }
    }

    private async void OnNewMessageClicked(object? sender, EventArgs e)
    {
        // TODO : choisir un voisin pour démarrer une conversation
        await Task.CompletedTask;
    }
}