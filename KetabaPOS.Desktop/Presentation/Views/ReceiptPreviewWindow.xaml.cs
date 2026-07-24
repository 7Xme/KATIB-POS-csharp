using System.Windows;
using System.Windows.Documents;
using KetabaPOS.Desktop.Core.Models;
using KetabaPOS.Desktop.Core.Interfaces;
using Microsoft.Win32;

namespace KetabaPOS.Desktop.Presentation.Views;

public partial class ReceiptPreviewWindow : Window
{
    private readonly IReceiptService _receiptService;
    private readonly Sale _sale;
    private readonly FixedDocument _document;

    public ReceiptPreviewWindow(IReceiptService receiptService, Sale sale, FixedDocument document)
    {
        InitializeComponent();
        _receiptService = receiptService;
        _sale = sale;
        _document = document;
        TitleText.Text = $"Receipt / Invoice — {sale.InvoiceNumber}";
        DocViewer.Document = document;
    }

    private async void SavePdf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Receipt as PDF",
            Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
            FileName = $"Receipt_{_sale.InvoiceNumber}_{DateTime.Now:yyyyMMdd}.pdf"
        };

        if (dialog.ShowDialog(this) == true)
        {
            try
            {
                StatusText.Text = "Saving PDF...";
                await _receiptService.ExportToPdfAsync(_sale, dialog.FileName);
                StatusText.Text = $"PDF saved: {dialog.FileName}";
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(this, $"Failed to save PDF:\n{ex.Message}", "PDF Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "PDF save failed.";
            }
        }
    }

    private async void Print_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "Preparing print...";
            await _receiptService.PrintReceiptAsync(_sale, showDialog: true);
            StatusText.Text = "Print sent.";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(this, $"Print failed:\n{ex.Message}", "Print Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Print failed.";
        }
    }
}
