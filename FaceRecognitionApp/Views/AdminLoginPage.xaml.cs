using FaceRecognitionApp.ViewModels;

namespace FaceRecognitionApp.Views;

public partial class AdminLoginPage : ContentPage
{
    public AdminLoginPage(AdminLoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
