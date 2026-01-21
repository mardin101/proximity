using System.Windows;
using Microsoft.Extensions.Logging;

namespace Proximity.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;

    public MainWindow(ILogger<MainWindow> logger)
    {
        _logger = logger;
        InitializeComponent();
        
        _logger.LogInformation("MainWindow initialized");
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _logger.LogInformation("MainWindow closed");
    }
}