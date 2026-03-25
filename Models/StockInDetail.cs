using System;
using System.Collections.Generic;

namespace EWMS_WPF.Models;

public partial class StockInDetail
{
    public int StockInDetailId { get; set; }

    public int StockInId { get; set; }

    public int ProductId { get; set; }

    public int LocationId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? TotalPrice { get; set; }

    public virtual Location Location { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual StockInReceipt StockIn { get; set; } = null!;
}
