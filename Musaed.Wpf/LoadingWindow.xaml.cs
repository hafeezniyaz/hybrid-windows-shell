using Microsoft.Web.WebView2.Core;
using Musaed.Wpf.ViewModels;
using System;
using System.Windows;

namespace Musaed.Wpf;

public partial class LoadingWindow : Window
{
    public LoadingWindow(LoadingViewModel viewModel)
    {
        InitializeComponent();
        this.DataContext = viewModel;
        this.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) this.DragMove(); };
        this.Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await WebView.EnsureCoreWebView2Async(null);
            // Subscribe to the NavigationCompleted event.
            WebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize WebView2. Please ensure the WebView2 Runtime is installed. Error: {ex.Message}", "WebView2 Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        // This event fires when the browser has successfully loaded the page.
        // It's now safe to hide the loading screen.
        // We use the Dispatcher to ensure this UI change happens on the correct thread.
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (this.DataContext is LoadingViewModel vm)
            {
                vm.IsLoading = false;
            }
        });
    }
}