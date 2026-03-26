using FaceRecognitionApp.ViewModels;

namespace FaceRecognitionApp.Views;

public partial class UsersListPage : ContentPage
{
    private readonly UsersListViewModel _viewModel;

    public UsersListPage(UsersListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;  
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadUsersCommand.ExecuteAsync(null);

    }
}
