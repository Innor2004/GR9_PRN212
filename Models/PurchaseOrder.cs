using System;
using System.Collections.Generic;
using System.Linq;

namespace EWMS_WPF.Models;

public partial class PurchaseOrder
{
    public int PurchaseOrderId { get; set; }

    public int SupplierId { get; set; }

    public int WarehouseId { get; set; }

    public int CreatedBy { get; set; }

    public string Status { get; set; } = null!;

    public DateTime ExpectedReceivingDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();

    public virtual ICollection<StockInReceipt> StockInReceipts { get; set; } = new List<StockInReceipt>();

    public virtual Supplier Supplier { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;

    public string ProductsText => PurchaseOrderDetails != null && PurchaseOrderDetails.Any()
        ? string.Join(", ", PurchaseOrderDetails.Select(d => d.Product?.ProductName ?? "N/A"))
        : "No products";
}
