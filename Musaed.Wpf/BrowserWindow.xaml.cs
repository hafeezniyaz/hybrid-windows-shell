using System;
using System.Threading.Tasks;
using System.Windows;
namespace Musaed.Wpf;

public partial class BrowserWindow : Window
{
    // A flag to ensure we only initialize once.
    private bool _isWebViewInitialized = false;

    public BrowserWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the WebView2 control if needed, then navigates to the specified URL.
    /// </summary>
    public async Task NavigateAsync(string url)
    {
        // 1. Ensure the browser is initialized. This will only run the first time.
        if (!_isWebViewInitialized)
        {
            try
            {
                await WebView.EnsureCoreWebView2Async(null);
                _isWebViewInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize WebView2. Error: {ex.Message}", "WebView2 Error");
                return;
            }
        }

        // 2. Now that we are certain it's initialized, we can safely navigate.
        if (WebView != null && WebView.CoreWebView2 != null)
        {
            WebView.CoreWebView2.Navigate(url);
        }
    }
}