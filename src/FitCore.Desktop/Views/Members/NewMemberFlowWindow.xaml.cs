using System.Windows;

namespace FitCore.Desktop.Views.Members;

public partial class NewMemberFlowWindow : Window
{
    public NewMemberFlowWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var work = SystemParameters.WorkArea;
        if (Height > work.Height - 40) Height = work.Height - 40;
        if (Width > work.Width - 40) Width = work.Width - 40;
        Left = work.Left + (work.Width - Width) / 2;
        Top = work.Top + (work.Height - Height) / 2;
    }
}
