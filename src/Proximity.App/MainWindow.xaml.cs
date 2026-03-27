using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Proximity.UI.ViewModels;

namespace Proximity.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;

    public MainWindow(ILogger<MainWindow> logger, MainViewModel viewModel)
    {
        _logger = logger;
        DataContext = viewModel;
        InitializeComponent();

        // Auto-scroll chat messages
        if (viewModel.ChatMessages is INotifyCollectionChanged chatCollection)
        {
            chatCollection.CollectionChanged += (_, _) =>
            {
                if (ChatListBox.Items.Count > 0)
                {
                    ChatListBox.ScrollIntoView(ChatListBox.Items[^1]);
                }
            };
        }

        _logger.LogInformation("MainWindow initialized with MainViewModel");
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _logger.LogInformation("MainWindow closed");
    }
}