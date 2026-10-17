using Microsoft.Maui.Controls.Shapes;

namespace VoisinApp.Views;

// Reçoit le nom du contact depuis la liste des messages
[QueryProperty(nameof(ContactName), "Name")]
public partial class ConversationPage : ContentPage
{
    private const double MaxContentWidth = 480;

    private static readonly Color Green = Color.FromArgb("#186B58");
    private static readonly Color Dark = Color.FromArgb("#17382F");
    private static readonly Color Muted = Color.FromArgb("#5B6F66");

    private string _contactName = "Grâce M.";

    public string ContactName
    {
        get => _contactName;
        set
        {
            _contactName = value;
            NameLabel.Text = value;
        }
    }

    public ConversationPage()
    {
        InitializeComponent();
        LoadSampleMessages();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width > 0)
            Container.WidthRequest = Math.Min(width, MaxContentWidth);
    }

    // Messages fictifs en attendant le back-end
    private void LoadSampleMessages()
    {
        MessagesStack.Add(CreateBubble("Bonjour Mireille, j'ai vu votre demande pour une perceuse.", "", false));
        MessagesStack.Add(CreateBubble("La mienne est disponible si vous voulez !", "10:18", false));
        MessagesStack.Add(CreateBubble("Ce serait parfait, merci beaucoup ! Je peux passer vers 18 h ?", "10:21 · Lu", true));
        MessagesStack.Add(CreateBubble("Oui, pas de souci. Je suis près du Carrefour Maetur.", "10:24", false));
    }

    private static Border CreateBubble(string text, string time, bool isMine)
    {
        var content = new VerticalStackLayout { Spacing = 4 };

        content.Add(new Label
        {
            Text = text,
            FontSize = 15,
            LineHeight = 1.2,
            TextColor = isMine ? Colors.White : Dark
        });

        if (!string.IsNullOrEmpty(time))
        {
            content.Add(new Label
            {
                Text = time,
                FontSize = 10,
                HorizontalOptions = LayoutOptions.End,
                TextColor = isMine ? Color.FromArgb("#CFE3DA") : Muted
            });
        }

        return new Border
        {
            BackgroundColor = isMine ? Green : Colors.White,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Padding = new Thickness(16, 12),
            MaximumWidthRequest = 300,
            HorizontalOptions = isMine ? LayoutOptions.End : LayoutOptions.Start,
            Content = content
        };
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        string text = MessageEntry.Text?.Trim() ?? "";
        if (text.Length == 0)
            return;

        MessagesStack.Add(CreateBubble(text, DateTime.Now.ToString("HH:mm"), true));
        MessageEntry.Text = string.Empty;

        // TODO : envoyer le message au back-end

        await MessagesScroll.ScrollToAsync(SafetyNotice, ScrollToPosition.End, true);
    }

    private async void OnAttachClicked(object? sender, EventArgs e)
    {
        // TODO : joindre une photo
        await Task.CompletedTask;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
