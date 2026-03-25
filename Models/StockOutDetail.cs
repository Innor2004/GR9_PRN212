using System;
using System.Collections.Generic;

namespace EWMS_WPF.Models;

public partial class StockOutDetail
{
    public int StockOutDetailId { get; set; }

    public int StockOutId { get; set; }

    public int ProductId { get; set; }

    public int LocationId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? TotalPrice { get; set; }

    public virtual Location Location { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual StockOutReceipt StockOut { get; set; } = null!;
}
