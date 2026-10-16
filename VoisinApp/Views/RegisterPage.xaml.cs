namespace VoisinApp.Views;

public partial class RegisterPage : ContentPage
{
	private const double MaxContentWidth = 480;
	public RegisterPage()
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

	// Le bouton "Continuer" n'est actif que si la case est cochée
	private void OnCertifyChanged(object? sender, CheckedChangedEventArgs e)
	{
		ContinueButton.IsEnabled = e.Value;
		ContinueButton.Opacity = e.Value ? 1 : 0.5;
	}

	// Permet de cocher en touchant le texte
	private void OnCertifyLabelTapped(object? sender, TappedEventArgs e)
	{
		CertifyCheck.IsChecked = !CertifyCheck.IsChecked;
	}

	private async void OnContinueClicked(object? sender, EventArgs e)
	{
		await Task.CompletedTask;
	}
}