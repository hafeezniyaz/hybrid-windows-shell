using CommunityToolkit.Mvvm.ComponentModel;

namespace Musaed.Wpf.ViewModels
{

/// <summary>
/// This is the ViewModel for our MainWindow.
/// It uses the MVVM Toolkit to simplify property change notifications.
/// The 'partial' keyword is required for the source generators to work.
/// </summary>
public partial class LoadingViewModel : ObservableObject
{
    // This attribute is a source generator. It automatically creates a public property
    // named "IsLoading" that will notify the UI whenever its value changes.
    // We initialize it to true because the app starts in a loading state.
    [ObservableProperty]
    private bool _isLoading = true;

    // Same here: this creates a public "StatusMessage" property.
    [ObservableProperty]
    private string _statusMessage = "Initializing...";

        [ObservableProperty]
        private string _webViewSource;
    }

}
