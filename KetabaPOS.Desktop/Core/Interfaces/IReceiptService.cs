using System.Windows.Documents;
using KetabaPOS.Desktop.Core.Models;

namespace KetabaPOS.Desktop.Core.Interfaces;

public interface IReceiptService
{
    Task<Sale?> GetSaleWithDetailsAsync(int saleId);
    Task<FixedDocument> BuildReceiptDocumentAsync(Sale sale);
    Task ExportToPdfAsync(Sale sale, string filePath);
    Task PrintReceiptAsync(Sale sale, bool showDialog = true);
}
