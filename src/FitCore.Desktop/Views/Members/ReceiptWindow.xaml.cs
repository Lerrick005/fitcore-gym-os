using System;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps.Packaging;
using System.Windows.Xps;

namespace FitCore.Desktop.Views.Members;

public partial class ReceiptWindow : Window
{
    public ReceiptWindow()
    {
        InitializeComponent();
        if (DataContext is FitCore.Desktop.ViewModels.Members.ReceiptViewModel vm)
            vm.PrintRequested += (_, __) => PrintReceipt();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void PrintReceipt()
    {
        var choice = MessageBox.Show(
            "Click Yes to print to a printer.\nClick No to save as PDF to your Desktop.",
            "Print receipt",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (choice == MessageBoxResult.Cancel) return;
        if (choice == MessageBoxResult.Yes) PrintToPrinter();
        else if (choice == MessageBoxResult.No) SaveAsPdf();
    }

    private void PrintToPrinter()
    {
        try
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true) return;

            var pageWidth = printDialog.PrintableAreaWidth;
            var pageHeight = printDialog.PrintableAreaHeight;

            var content = ReceiptContent;
            var parent = content.Parent as Panel;
            parent?.Children.Remove(content);

            content.Measure(new Size(pageWidth, pageHeight));
            content.Arrange(new Rect(new Size(pageWidth, pageHeight)));
            content.UpdateLayout();

            // Render to a fixed size layout
            var fixedSize = new Size(80 * 3.7795, double.NaN); // 80mm ~ 302 DIPs wide
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                var brush = new VisualBrush(content) { Stretch = Stretch.None };
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, 302, content.ActualHeight));
                dc.DrawRectangle(brush, null, new Rect(0, 0, content.ActualWidth, content.ActualHeight));
            }

            printDialog.PrintVisual(visual, "FitCore Receipt");

            parent?.Children.Insert(0, content);
            MessageBox.Show("Receipt sent to printer.", "Print", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Print failed: {ex.Message}\n\nTry 'Save as PDF' instead.", "Print",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SaveAsPdf()
    {
        try
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"receipt-{DateTime.Now:yyyyMMdd-HHmmss}.pdf",
                DefaultExt = ".pdf",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (saveDialog.ShowDialog() != true) return;

            var content = ReceiptContent;
            var parent = content.Parent as Panel;
            parent?.Children.Remove(content);

            var pageWidth = 595.0;  // A4 @ 72 DPI = 595pt
            var pageHeight = 842.0;
            content.Measure(new Size(pageWidth, pageHeight));
            content.Arrange(new Rect(new Size(pageWidth, pageHeight)));
            content.UpdateLayout();

            using (var xpsDoc = new XpsDocument(saveDialog.FileName.Replace(".pdf", ".xps"), FileAccess.ReadWrite))
            {
                var xpsWriter = XpsDocument.CreateXpsDocumentWriter(xpsDoc);
                xpsWriter.Write(content);
            }

            parent?.Children.Insert(0, content);

            MessageBox.Show(
                $"Saved XPS: {saveDialog.FileName.Replace(".pdf", ".xps")}\n\n" +
                "To convert to PDF:\n" +
                "1. Open the XPS file\n" +
                "2. Print → Microsoft Print to PDF",
                "Save receipt",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save failed: {ex.Message}", "Save", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
