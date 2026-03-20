using System;
using System.Collections.Generic;

namespace EWMS_WPF.Models;

public partial class StockInReceipt
{
    public int StockInId { get; set; }

    public int WarehouseId { get; set; }

    public int ReceivedBy { get; set; }

    public DateTime? ReceivedDate { get; set; }

    public string? Reason { get; set; }

    public int? PurchaseOrderId { get; set; }

    public int? TransferId { get; set; }

    public decimal? TotalAmount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    public virtual User ReceivedByNavigation { get; set; } = null!;

    public virtual ICollection<StockInDetail> StockInDetails { get; set; } = new List<StockInDetail>();

    public virtual TransferRequest? Transfer { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;
}
