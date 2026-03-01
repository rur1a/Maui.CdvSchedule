using Maui.CdvSchedule.App.Presentation.ViewModels;

namespace Maui.CdvSchedule.App.Presentation.Pages;

public partial class ProfilePage : ContentPage
{
    private ProfileViewModel ViewModel => (ProfileViewModel)BindingContext;

    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ViewModel.RefreshFromState();
    }

    private async void OnSignInClicked(object? sender, EventArgs e)
    {
        await ViewModel.SignInAsync();
    }

    private async void OnSignOutClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlert(
            ViewModel.SignOutConfirmTitle,
            ViewModel.SignOutConfirmQuestion,
            ViewModel.SignOutButtonText,
            ViewModel.CancelText);

        if (confirmed)
        {
            await ViewModel.SignOutAsync();
        }
    }
}
