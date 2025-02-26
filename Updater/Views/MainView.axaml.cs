using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Updater.ViewModels;

namespace Updater.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        DataContext = App.Services.GetRequiredService<MainViewModel>();
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        var dc = (DataContext as MainViewModel)!;
        dc.UpdateCommand.Execute(null);
    }
}