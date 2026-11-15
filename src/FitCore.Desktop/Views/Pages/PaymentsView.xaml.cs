using System.Windows;
using System.Windows.Controls;
using FitCore.Desktop.ViewModels.Pages;

namespace FitCore.Desktop.Views.Pages;

public partial class PaymentsView : UserControl
{
    public PaymentsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PaymentsViewModel vm)
            vm.ShowReceiptRequested += OnShowReceipt;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PaymentsViewModel vm)
            vm.ShowReceiptRequested -= OnShowReceipt;
    }

    private async void OnShowReceipt(object? sender, int paymentId)
    {
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(sp);
        var receiptVm = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<FitCore.Desktop.ViewModels.Members.ReceiptViewModel>(scope.ServiceProvider);
        var window = new FitCore.Desktop.Views.Members.ReceiptWindow { DataContext = receiptVm, Owner = Window.GetWindow(this) };
        await receiptVm.InitializeForPaymentAsync(paymentId);
        window.ShowDialog();
    }

    private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid && grid.SelectedItem is PaymentRow row)
        {
            if (DataContext is PaymentsViewModel vm)
                vm.ShowReceiptCommand.Execute(row);
        }
    }
}
