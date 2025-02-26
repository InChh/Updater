using System.Threading.Tasks;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Updater.Helpers;

public static class MessageBoxHelper
{
    public static async Task ShowError(string message)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
            MessageBoxManager.GetMessageBoxStandard("错误", message, ButtonEnum.Ok, Icon.Error).ShowAsync());
    }

    public static async Task ShowInfo(string message)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
            MessageBoxManager.GetMessageBoxStandard("提示", message, ButtonEnum.Ok, Icon.Info).ShowAsync());
    }
}