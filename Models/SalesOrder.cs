using System;
using System.Collections.Generic;
using System.Linq;

namespace EWMS_WPF.Models;

public partial class SalesOrder
{
    public int SalesOrderId { get; set; }

    public int WarehouseId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string? CustomerPhone { get; set; }

    public string? CustomerAddress { get; set; }

    public int CreatedBy { get; set; }

    public string Status { get; set; } = null!;

    public DateTime ExpectedDeliveryDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<SalesOrderDetail> SalesOrderDetails { get; set; } = new List<SalesOrderDetail>();

    public virtual ICollection<StockOutReceipt> StockOutReceipts { get; set; } = new List<StockOutReceipt>();

    public virtual Warehouse Warehouse { get; set; } = null!;

    public string ProductsText => SalesOrderDetails != null && SalesOrderDetails.Any()
        ? string.Join(", ", SalesOrderDetails.Select(d => d.Product?.ProductName ?? "N/A"))
        : "No products";
}
