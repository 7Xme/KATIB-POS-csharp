using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using KetabaPOS.Desktop.Core.Interfaces;
using KetabaPOS.Desktop.Core.Models;
using KetabaPOS.Desktop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WpfMedia = System.Windows.Media;
using WpfControls = System.Windows.Controls;
using WpfSize = System.Windows.Size;
using WpfHA = System.Windows.HorizontalAlignment;

namespace KetabaPOS.Desktop.Infrastructure.Services;

public class ReceiptService : IReceiptService
{
    private readonly AppDbContext _context;
    private Dictionary<string, string> _settings = new();

    private static readonly WpfMedia.Color PrimaryBlue = WpfMedia.Color.FromRgb(37, 99, 235);
    private static readonly WpfMedia.Color LightGray = WpfMedia.Color.FromRgb(248, 250, 252);
    private static readonly WpfMedia.Color BorderGray = WpfMedia.Color.FromRgb(226, 232, 240);
    private static readonly WpfMedia.Color DarkText = WpfMedia.Color.FromRgb(30, 41, 59);
    private static readonly WpfMedia.Color MutedText = WpfMedia.Color.FromRgb(100, 116, 139);
    private static readonly WpfMedia.Color SuccessGreen = WpfMedia.Color.FromRgb(16, 185, 129);

    public ReceiptService(AppDbContext context) { _context = context; }

    public async Task<Sale?> GetSaleWithDetailsAsync(int saleId)
    {
        return await _context.Sales
            .Include(s => s.User)
            .Include(s => s.Customer)
            .Include(s => s.SaleItems).ThenInclude(si => si.Product)
            .FirstOrDefaultAsync(s => s.Id == saleId);
    }

    private async Task LoadSettingsAsync()
    {
        _settings = await _context.Settings.ToDictionaryAsync(s => s.Key, s => s.Value);
    }

    private string Setting(string key, string fallback = "") =>
        _settings.GetValueOrDefault(key, fallback);

    public async Task<FixedDocument> BuildReceiptDocumentAsync(Sale sale)
    {
        await LoadSettingsAsync();

        var doc = new FixedDocument();
        var pageContent = new PageContent();
        var fixedPage = new FixedPage { Width = 794, Height = 1123 };
        pageContent.Child = fixedPage;
        doc.Pages.Add(pageContent);

        var canvas = new WpfControls.Canvas();
        fixedPage.Children.Add(canvas);

        double y = 40;

        // --- Header: Logo + Company Info ---
        var headerBorder = new Border
        {
            Background = new WpfMedia.SolidColorBrush(PrimaryBlue),
            Padding = new Thickness(40, 24, 40, 24),
            Child = CreateHeaderContent(sale)
        };
        headerBorder.Measure(new WpfSize(794, double.PositiveInfinity));
        headerBorder.Arrange(new Rect(0, y, 794, headerBorder.DesiredSize.Height));
        canvas.Children.Add(headerBorder);
        y += headerBorder.DesiredSize.Height + 20;

        // --- Receipt Title ---
        var titleBlock = new TextBlock
        {
            Text = "RECEIPT / INVOICE",
            FontSize = 26, FontWeight = FontWeights.Bold,
            Foreground = new WpfMedia.SolidColorBrush(DarkText),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4)
        };
        titleBlock.Measure(new WpfSize(794, double.PositiveInfinity));
        titleBlock.Arrange(new Rect(0, y, 794, titleBlock.DesiredSize.Height));
        canvas.Children.Add(titleBlock);
        y += titleBlock.DesiredSize.Height + 4;

        y = DrawDivider(canvas, y);

        // --- Invoice Info ---
        y = DrawInfoSection(canvas, y, sale);

        y = DrawDivider(canvas, y);

        // --- Items Table Header ---
        y = DrawTableHeader(canvas, y, "Item", "Qty", "Price", "Disc.", "Total");

        // --- Items ---
        foreach (var item in sale.SaleItems)
        {
            y = DrawTableRow(canvas, y,
                item.Product?.Name ?? "Item",
                item.Quantity.ToString(),
                item.UnitPrice.ToString("N2"),
                item.DiscountAmount > 0 ? item.DiscountAmount.ToString("N2") : "-",
                item.TotalPrice.ToString("N2"));
        }

        y = DrawDivider(canvas, y);

        // --- Totals ---
        y = DrawTotalLine(canvas, y, "Subtotal:", sale.Subtotal, false);
        y = DrawTotalLine(canvas, y, "Tax:", sale.TaxAmount, false);
        if (sale.DiscountAmount > 0)
            y = DrawTotalLine(canvas, y, "Discount:", -sale.DiscountAmount, false);
        y = DrawDivider(canvas, y);
        y = DrawTotalLine(canvas, y, "TOTAL:", sale.TotalAmount, true);
        y = DrawTotalLine(canvas, y, "Paid:", sale.PaidAmount, false);
        y = DrawTotalLine(canvas, y, "Change:", sale.ChangeAmount, false);

        y += 16;

        // --- Footer ---
        y = DrawDivider(canvas, y);
        var footer = Setting("receipt_footer", "Thank you for your purchase!");
        var footerBlock = new TextBlock
        {
            Text = footer,
            FontSize = 14,
            Foreground = new WpfMedia.SolidColorBrush(MutedText),
            TextAlignment = TextAlignment.Center,
            FontStyle = FontStyles.Italic,
            Margin = new Thickness(40, 12, 40, 0)
        };
        footerBlock.Measure(new WpfSize(714, double.PositiveInfinity));
        footerBlock.Arrange(new Rect(0, y, 794, footerBlock.DesiredSize.Height));
        canvas.Children.Add(footerBlock);
        y += footerBlock.DesiredSize.Height + 40;

        // Bottom branding
        var brandBlock = new TextBlock
        {
            Text = $"Generated by Ketaba POS | {DateTime.Now:yyyy-MM-dd HH:mm}",
            FontSize = 10,
            Foreground = new WpfMedia.SolidColorBrush(MutedText),
            TextAlignment = TextAlignment.Center
        };
        brandBlock.Measure(new WpfSize(794, double.PositiveInfinity));
        brandBlock.Arrange(new Rect(0, y, 794, brandBlock.DesiredSize.Height));
        canvas.Children.Add(brandBlock);

        return doc;
    }

    public async Task ExportToPdfAsync(Sale sale, string filePath)
    {
        await LoadSettingsAsync();
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Element(c => ComposePdfHeader(c, sale));
                page.Content().Element(c => ComposePdfContent(c, sale));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generated by Ketaba POS | ").FontSize(9).FontColor(Colors.Grey.Medium);
                    t.Span($"{DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(filePath);
    }

    public async Task PrintReceiptAsync(Sale sale, bool showDialog = true)
    {
        var doc = await BuildReceiptDocumentAsync(sale);
        var dlg = new PrintDialog();
        if (showDialog)
        {
            if (dlg.ShowDialog() != true) return;
        }
        dlg.PrintDocument(doc.DocumentPaginator, $"Receipt-{sale.InvoiceNumber}");
    }

    // --- WPF content helpers ---

    private WpfControls.Grid CreateHeaderContent(Sale sale)
    {
        var grid = new WpfControls.Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var logoPath = Setting("company_logo");
        if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
        {
            try
            {
                var img = new WpfControls.Image
                {
                    Source = new BitmapImage(new Uri(logoPath)),
                    Width = 64, Height = 64,
                    Stretch = WpfMedia.Stretch.Uniform
                };
                Grid.SetColumn(img, 0);
                grid.Children.Add(img);
            }
            catch { }
        }

        var stack = new StackPanel { Margin = new Thickness(16, 0, 0, 0) };
        Grid.SetColumn(stack, 1);
        stack.Children.Add(new TextBlock
        {
            Text = Setting("company_name", "KETABA POS"),
            FontSize = 22, FontWeight = FontWeights.Bold,
            Foreground = WpfMedia.Brushes.White
        });
        var addr = Setting("company_address");
        if (!string.IsNullOrEmpty(addr))
            stack.Children.Add(new TextBlock
            { Text = addr, FontSize = 12, Foreground = new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(191, 219, 254)) });
        var phone = Setting("company_phone");
        if (!string.IsNullOrEmpty(phone))
            stack.Children.Add(new TextBlock
            { Text = phone, FontSize = 12, Foreground = new WpfMedia.SolidColorBrush(WpfMedia.Color.FromRgb(191, 219, 254)) });
        grid.Children.Add(stack);
        return grid;
    }

    private double DrawInfoSection(WpfControls.Canvas canvas, double y, Sale sale)
    {
        var grid = new WpfControls.Grid { Margin = new Thickness(40, 0, 40, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());

        AddInfoRow(grid, 0, 0, "Invoice #:", sale.InvoiceNumber);
        AddInfoRow(grid, 0, 1, "Date:", sale.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
        AddInfoRow(grid, 0, 2, "Cashier:", sale.User?.DisplayName ?? "N/A");
        AddInfoRow(grid, 1, 0, "Payment:", sale.PaymentMethod.ToString());
        AddInfoRow(grid, 1, 1, "Customer:", sale.Customer?.Name ?? "Walk-in");
        AddInfoRow(grid, 1, 2, "Status:", sale.Status.ToString());

        grid.Measure(new WpfSize(714, double.PositiveInfinity));
        grid.Arrange(new Rect(0, y, 794, grid.DesiredSize.Height));
        canvas.Children.Add(grid);
        return y + grid.DesiredSize.Height + 12;
    }

    private void AddInfoRow(WpfControls.Grid grid, int col, int row, string label, string value)
    {
        var lbl = new TextBlock
        {
            Text = label, FontSize = 11, FontWeight = FontWeights.SemiBold,
            Foreground = new WpfMedia.SolidColorBrush(MutedText)
        };
        Grid.SetColumn(lbl, col * 2);
        Grid.SetRow(lbl, row);
        grid.Children.Add(lbl);

        var val = new TextBlock
        {
            Text = value, FontSize = 11,
            Foreground = new WpfMedia.SolidColorBrush(DarkText),
            Margin = new Thickness(8, 0, 0, 4)
        };
        Grid.SetColumn(val, col * 2 + 1);
        Grid.SetRow(val, row);
        grid.Children.Add(val);
    }

    private double DrawDivider(WpfControls.Canvas canvas, double y)
    {
        var line = new System.Windows.Shapes.Rectangle
        {
            Height = 1,
            Fill = new WpfMedia.SolidColorBrush(BorderGray),
            Width = 714,
            Margin = new Thickness(40, 0, 40, 0)
        };
        Canvas.SetLeft(line, 0);
        Canvas.SetTop(line, y);
        canvas.Children.Add(line);
        return y + 8;
    }

    private double DrawTableHeader(WpfControls.Canvas canvas, double y, params string[] cols)
    {
        var headerGrid = new WpfControls.Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 1; i < cols.Length; i++)
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        for (int i = 0; i < cols.Length; i++)
        {
            var tb = new TextBlock
            {
                Text = cols[i],
                FontSize = 11, FontWeight = FontWeights.Bold,
                Foreground = WpfMedia.Brushes.White,
                TextAlignment = i == 0 ? TextAlignment.Left : TextAlignment.Right,
                Padding = new Thickness(i > 0 ? 8 : 0, 6, 0, 6)
            };
            Grid.SetColumn(tb, i);
            headerGrid.Children.Add(tb);
        }

        var border = new Border
        {
            Background = new WpfMedia.SolidColorBrush(PrimaryBlue),
            Padding = new Thickness(40, 0, 40, 0),
            Child = headerGrid
        };

        border.Measure(new WpfSize(794, double.PositiveInfinity));
        border.Arrange(new Rect(0, y, 794, border.DesiredSize.Height));
        canvas.Children.Add(border);
        return y + border.DesiredSize.Height;
    }

    private double DrawTableRow(WpfControls.Canvas canvas, double y, params string[] cols)
    {
        var grid = new WpfControls.Grid { Margin = new Thickness(40, 0, 40, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 1; i < cols.Length; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

        for (int i = 0; i < cols.Length; i++)
        {
            var tb = new TextBlock
            {
                Text = cols[i],
                FontSize = 11,
                Foreground = new WpfMedia.SolidColorBrush(DarkText),
                TextAlignment = i == 0 ? TextAlignment.Left : TextAlignment.Right,
                Margin = new Thickness(i > 0 ? 8 : 0, 4, 0, 4)
            };
            Grid.SetColumn(tb, i);
            grid.Children.Add(tb);
        }

        grid.Measure(new WpfSize(714, double.PositiveInfinity));
        grid.Arrange(new Rect(0, y, 794, grid.DesiredSize.Height));
        canvas.Children.Add(grid);

        var line = new System.Windows.Shapes.Rectangle
        {
            Height = 0.5,
            Fill = new WpfMedia.SolidColorBrush(BorderGray),
            Width = 714,
            Margin = new Thickness(40, 0, 40, 0)
        };
        Canvas.SetLeft(line, 0);
        Canvas.SetTop(line, y + grid.DesiredSize.Height);
        canvas.Children.Add(line);

        return y + grid.DesiredSize.Height + 4;
    }

    private double DrawTotalLine(WpfControls.Canvas canvas, double y, string label, decimal value, bool bold)
    {
        var grid = new WpfControls.Grid { Margin = new Thickness(40, 2, 40, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

        var lbl = new TextBlock
        {
            Text = label,
            FontSize = bold ? 16 : 12,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Medium,
            Foreground = new WpfMedia.SolidColorBrush(bold ? PrimaryBlue : DarkText),
            HorizontalAlignment = WpfHA.Right
        };
        Grid.SetColumn(lbl, 0);
        grid.Children.Add(lbl);

        var val = new TextBlock
        {
            Text = value.ToString("N2"),
            FontSize = bold ? 16 : 12,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Medium,
            Foreground = new WpfMedia.SolidColorBrush(bold ? PrimaryBlue : DarkText),
            TextAlignment = TextAlignment.Right,
            Margin = new Thickness(8, 0, 0, 0)
        };
        Grid.SetColumn(val, 1);
        grid.Children.Add(val);

        grid.Measure(new WpfSize(714, double.PositiveInfinity));
        grid.Arrange(new Rect(0, y, 794, grid.DesiredSize.Height));
        canvas.Children.Add(grid);
        return y + grid.DesiredSize.Height;
    }

    // --- PDF helpers ---

    private void ComposePdfHeader(IContainer container, Sale sale)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                var logoPath = Setting("company_logo");
                if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                    row.ConstantItem(80).Image(logoPath).FitArea();

                row.RelativeItem().PaddingLeft(16).Column(c =>
                {
                    c.Item().Text(Setting("company_name", "KETABA POS"))
                        .FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                    var addr = Setting("company_address");
                    if (!string.IsNullOrEmpty(addr))
                        c.Item().Text(addr).FontSize(10).FontColor(Colors.Grey.Medium);
                    var phone = Setting("company_phone");
                    if (!string.IsNullOrEmpty(phone))
                        c.Item().Text(phone).FontSize(10).FontColor(Colors.Grey.Medium);
                });
            });

            col.Item().PaddingTop(12).LineHorizontal(1).LineColor(Colors.Blue.Medium);
            col.Item().PaddingTop(8).Text("RECEIPT / INVOICE").FontSize(22).Bold().AlignCenter();
            col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposePdfContent(IContainer container, Sale sale)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Invoice #: {sale.InvoiceNumber}").SemiBold();
                    c.Item().Text($"Date: {sale.CreatedAt:yyyy-MM-dd HH:mm}");
                    c.Item().Text($"Cashier: {sale.User?.DisplayName ?? "N/A"}");
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Payment: {sale.PaymentMethod}").SemiBold();
                    c.Item().Text($"Customer: {sale.Customer?.Name ?? "Walk-in"}");
                    c.Item().Text($"Status: {sale.Status}");
                });
            });

            col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(4);
                    c.ConstantColumn(50);
                    c.ConstantColumn(80);
                    c.ConstantColumn(70);
                    c.ConstantColumn(80);
                });

                table.Header(h =>
                {
                    h.Cell().Background(Colors.Blue.Lighten4)
                        .Padding(6).Text("Item").Bold().FontSize(10);
                    h.Cell().Background(Colors.Blue.Lighten4)
                        .Padding(6).Text("Qty").Bold().FontSize(10).AlignCenter();
                    h.Cell().Background(Colors.Blue.Lighten4)
                        .Padding(6).Text("Price").Bold().FontSize(10).AlignRight();
                    h.Cell().Background(Colors.Blue.Lighten4)
                        .Padding(6).Text("Disc.").Bold().FontSize(10).AlignRight();
                    h.Cell().Background(Colors.Blue.Lighten4)
                        .Padding(6).Text("Total").Bold().FontSize(10).AlignRight();
                });

                var index = 0;
                foreach (var item in sale.SaleItems)
                {
                    var bg = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                    table.Cell().Background(bg).Padding(6).Text(item.Product?.Name ?? "Item").FontSize(10);
                    table.Cell().Background(bg).Padding(6).Text(item.Quantity.ToString()).FontSize(10).AlignCenter();
                    table.Cell().Background(bg).Padding(6).Text(item.UnitPrice.ToString("N2")).FontSize(10).AlignRight();
                    table.Cell().Background(bg).Padding(6)
                        .Text(item.DiscountAmount > 0 ? item.DiscountAmount.ToString("N2") : "-")
                        .FontSize(10).AlignRight();
                    table.Cell().Background(bg).Padding(6).Text(item.TotalPrice.ToString("N2"))
                        .FontSize(10).AlignRight().Bold();
                    index++;
                }
            });

            col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            col.Item().AlignRight().Width(250).Column(c =>
            {
                c.Item().Row(r => { r.RelativeItem().Text("Subtotal:").SemiBold(); r.ConstantItem(80).Text(sale.Subtotal.ToString("N2")).AlignRight(); });
                c.Item().Row(r => { r.RelativeItem().Text("Tax:").SemiBold(); r.ConstantItem(80).Text(sale.TaxAmount.ToString("N2")).AlignRight(); });
                if (sale.DiscountAmount > 0)
                    c.Item().Row(r => { r.RelativeItem().Text("Discount:").SemiBold(); r.ConstantItem(80).Text($"(-{sale.DiscountAmount:N2})").AlignRight(); });
                c.Item().PaddingVertical(2).LineHorizontal(1);
                c.Item().Row(r => { r.RelativeItem().Text("TOTAL:").Bold().FontSize(14); r.ConstantItem(80).Text(sale.TotalAmount.ToString("N2")).AlignRight().Bold().FontSize(14); });
                c.Item().Row(r => { r.RelativeItem().Text("Paid:"); r.ConstantItem(80).Text(sale.PaidAmount.ToString("N2")).AlignRight(); });
                c.Item().Row(r => { r.RelativeItem().Text("Change:"); r.ConstantItem(80).Text(sale.ChangeAmount.ToString("N2")).AlignRight(); });
            });

            var footer = Setting("receipt_footer", "Thank you for your purchase!");
            col.Item().PaddingTop(16).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            col.Item().PaddingTop(8).Text(footer).FontSize(12).Italic().AlignCenter().FontColor(Colors.Grey.Medium);
        });
    }
}
