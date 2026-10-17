namespace VoisinApp.Views.Controls;

public partial class BottomNavbar : ContentView
{
    private static readonly Color ActiveColor = Color.FromArgb("#186B58");
    private static readonly Color InactiveColor = Color.FromArgb("#8A9A93");

    // Onglet actif : "Home", "Annonces", "Messages" ou "Profil"
    public static readonly BindableProperty ActiveTabProperty =
        BindableProperty.Create(
            nameof(ActiveTab),
            typeof(string),
            typeof(BottomNavbar),
            "Home",
            propertyChanged: (bindable, _, _) => ((BottomNavbar)bindable).UpdateActiveTab());

    public string ActiveTab
    {
        get => (string)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }
    public BottomNavbar()
	{
		InitializeComponent();
        UpdateActiveTab();
    }

    private void UpdateActiveTab()
    {
        Apply(HomeIcon, HomeLabel, ActiveTab == "Home");
        Apply(AnnoncesIcon, AnnoncesLabel, ActiveTab == "Annonces");
        Apply(MessagesIcon, MessagesLabel, ActiveTab == "Messages");
        Apply(ProfilIcon, ProfilLabel, ActiveTab == "Profil");
    }

    private static void Apply(Label icon, Label label, bool isActive)
    {
        icon.Opacity = isActive ? 1 : 0.45;
        label.TextColor = isActive ? ActiveColor : InactiveColor;
    }

    private async void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Element element)
            return;

        switch (element.ClassId)
        {
            case "Home":
                if (ActiveTab != "Home")
                    await Shell.Current.GoToAsync("//HomePage");
                break;

            // TODO : brancher les autres onglets quand les pages seront créées
            case "Annonces":
                if (ActiveTab != "Annonces")
                    await Shell.Current.GoToAsync("//AdsPage");
                break;
            case "Publish":
                break;
            case "Messages":
                break;
            case "Profil":
                break;
        }
    }
}