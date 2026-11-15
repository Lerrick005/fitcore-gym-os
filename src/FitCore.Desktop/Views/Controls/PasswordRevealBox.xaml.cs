using System.Windows;
using System.Windows.Controls;

namespace FitCore.Desktop.Views.Controls;

public partial class PasswordRevealBox : UserControl
{
    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.Register(nameof(Password), typeof(string), typeof(PasswordRevealBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordChanged));

    public static readonly DependencyProperty IsRevealedProperty =
        DependencyProperty.Register(nameof(IsRevealed), typeof(bool), typeof(PasswordRevealBox),
            new PropertyMetadata(false, OnIsRevealedChanged));

    public string Password
    {
        get => (string)GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    public bool IsRevealed
    {
        get => (bool)GetValue(IsRevealedProperty);
        set => SetValue(IsRevealedProperty, value);
    }

    public PasswordRevealBox()
    {
        InitializeComponent();
    }

    private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (PasswordRevealBox)d;
        var newPassword = e.NewValue as string ?? string.Empty;

        if (self.IsRevealed)
        {
            if (self.RevealedInput.Text != newPassword)
                self.RevealedInput.Text = newPassword;
        }
        else
        {
            if (self.MaskedInput.Password != newPassword)
                self.MaskedInput.Password = newPassword;
        }
    }

    private static void OnIsRevealedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var self = (PasswordRevealBox)d;
        var revealed = (bool)e.NewValue;

        self.UpdateGlyph();

        if (revealed)
        {
            self.RevealedInput.Text = self.Password;
            self.RevealedInput.Focus();
            self.RevealedInput.CaretIndex = self.RevealedInput.Text.Length;
        }
        else
        {
            self.MaskedInput.Password = self.Password;
            self.MaskedInput.Focus();
        }
    }

    private void MaskedInput_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (IsRevealed) return;
        SetCurrentValue(PasswordProperty, MaskedInput.Password);
    }

    private void RevealedInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsRevealed) return;
        SetCurrentValue(PasswordProperty, RevealedInput.Text);
    }

    private void ToggleReveal_Click(object sender, RoutedEventArgs e)
    {
        IsRevealed = !IsRevealed;
    }

    private void UpdateGlyph()
    {
        var rootGrid = (Grid)Content;
        var button = (Button)rootGrid.Children[1];
        if (button?.Template is null) return;
        var glyph = (TextBlock)button.Template.FindName("Glyph", button);
        if (glyph != null)
            glyph.Text = IsRevealed ? "\uED1A" : "\uE7B3";
    }
}
