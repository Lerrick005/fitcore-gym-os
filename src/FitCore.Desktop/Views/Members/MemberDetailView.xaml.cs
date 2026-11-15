using System;
using System.Windows;
using System.Windows.Controls;
using FitCore.Desktop.ViewModels.Members;
using FitCore.Application.Attendance;
using FitCore.Application.Loyalty;
using Microsoft.Extensions.DependencyInjection;

namespace FitCore.Desktop.Views.Members;

public partial class MemberDetailView : UserControl
{
    public MemberDetailView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MemberDetailViewModel vm)
        {
            vm.SellPlanRequested += OnSellPlanRequested;
            vm.RecordPaymentRequested += OnRecordPaymentRequested;
            vm.CheckInRequested += OnCheckInRequested;
            vm.RedeemRewardRequested += OnRedeemReward;
            vm.RenewRequested += OnRenewRequested;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MemberDetailViewModel vm)
        {
            vm.SellPlanRequested -= OnSellPlanRequested;
            vm.RecordPaymentRequested -= OnRecordPaymentRequested;
            vm.CheckInRequested -= OnCheckInRequested;
            vm.RedeemRewardRequested -= OnRedeemReward;
            vm.RenewRequested -= OnRenewRequested;
        }
    }

    private async void OnSellPlanRequested(object? sender, EventArgs e)
    {
        if (sender is not MemberDetailViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var sellVm = scope.ServiceProvider.GetRequiredService<SellPlanViewModel>();
        var window = new SellPlanWindow { DataContext = sellVm, Owner = Window.GetWindow(this) };

        sellVm.CloseRequested += async (_, ok) =>
        {
            if (ok) await vm.LoadAsync();
            window.Close();
        };

        await sellVm.InitializeAsync(vm.Id);
        window.ShowDialog();
    }

    private async void OnRecordPaymentRequested(object? sender, EventArgs e)
    {
        if (sender is not MemberDetailViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var paymentVm = scope.ServiceProvider.GetRequiredService<RecordPaymentViewModel>();
        var window = new RecordPaymentWindow { DataContext = paymentVm, Owner = Window.GetWindow(this) };

        paymentVm.CloseRequested += async (_, ok) =>
        {
            if (ok)
            {
                await vm.LoadAsync();
                // Open the newly-generated receipt
                try
                {
                    var receipts = scope.ServiceProvider.GetRequiredService<FitCore.Application.Receipts.IReceiptService>();
                    var latestPayment = await scope.ServiceProvider.GetRequiredService<FitCore.Application.Payments.IPaymentService>().ListForMemberAsync(vm.Id);
                    if (latestPayment.IsSuccess && latestPayment.Value!.Count > 0)
                    {
                        var paymentId = latestPayment.Value[0].Id;
                        var receiptVm = scope.ServiceProvider.GetRequiredService<ReceiptViewModel>();
                        var receiptWindow = new ReceiptWindow { DataContext = receiptVm, Owner = Window.GetWindow(this) };
                        await receiptVm.InitializeForPaymentAsync(paymentId);
                        receiptWindow.ShowDialog();
                    }
                }
                catch { /* non-fatal */ }
            }
            window.Close();
        };

        int? membershipId = vm.HasActiveMembership ? vm.ActiveMembershipId : (int?)null;
        await paymentVm.InitializeAsync(vm.Id, membershipId);
        window.ShowDialog();
    }

    private void OnRenewRequested(object? sender, EventArgs e) => OnSellPlanRequested(sender, e);

    private async void OnCheckInRequested(object? sender, EventArgs e)
    {
        if (sender is not MemberDetailViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var attendance = scope.ServiceProvider.GetRequiredService<IAttendanceService>();

        var result = await attendance.CheckInAsync(new CheckInRequest(vm.Id, null));
        if (!result.IsSuccess)
        {
            MessageBox.Show(result.ErrorMessage ?? "Check-in failed.", "Check-in", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var checkIn = result.Value!;
        MessageBox.Show(checkIn.Message, $"Check-in: {checkIn.ResultCode}", MessageBoxButton.OK,
            checkIn.IsValid ? MessageBoxImage.Information : MessageBoxImage.Warning);
        await vm.LoadAsync();
    }

    private async void OnRedeemReward(object? sender, int rewardId)
    {
        if (sender is not MemberDetailViewModel vm) return;
        var sp = ((App)System.Windows.Application.Current).Services;
        if (sp is null) return;

        var confirm = MessageBox.Show("Redeem this reward now?", "Confirm redemption", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ILoyaltyRewardService>();
        var result = await svc.RedeemAsync(vm.Id, rewardId);

        if (result.IsSuccess)
        {
            MessageBox.Show("Reward redeemed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            await vm.LoadAsync();
        }
        else
        {
            MessageBox.Show(result.ErrorMessage ?? "Redemption failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}



