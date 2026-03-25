using System;
using System.Collections.Generic;

namespace EWMS_WPF.Models;

public partial class TransferRequest
{
    public int TransferId { get; set; }

    public int FromWarehouseId { get; set; }

    public int ToWarehouseId { get; set; }

    public string TransferType { get; set; } = null!;

    public int RequestedBy { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? RequestedDate { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public string? Status { get; set; }

    public string? Reason { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual Warehouse FromWarehouse { get; set; } = null!;

    public virtual User RequestedByNavigation { get; set; } = null!;

    public virtual ICollection<StockInReceipt> StockInReceipts { get; set; } = new List<StockInReceipt>();

    public virtual ICollection<StockOutReceipt> StockOutReceipts { get; set; } = new List<StockOutReceipt>();

    public virtual Warehouse ToWarehouse { get; set; } = null!;

    public virtual ICollection<TransferDetail> TransferDetails { get; set; } = new List<TransferDetail>();
}
