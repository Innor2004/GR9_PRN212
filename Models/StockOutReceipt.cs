using System;
using System.Collections.Generic;

namespace EWMS_WPF.Models;

public partial class StockOutReceipt
{
    public int StockOutId { get; set; }

    public int WarehouseId { get; set; }

    public int IssuedBy { get; set; }

    public DateTime? IssuedDate { get; set; }

    public string? Reason { get; set; }

    public int? SalesOrderId { get; set; }

    public int? TransferId { get; set; }

    public decimal? TotalAmount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User IssuedByNavigation { get; set; } = null!;

    public virtual SalesOrder? SalesOrder { get; set; }

    public virtual ICollection<StockOutDetail> StockOutDetails { get; set; } = new List<StockOutDetail>();

    public virtual TransferRequest? Transfer { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;
}
