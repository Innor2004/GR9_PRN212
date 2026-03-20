using System;
using System.Collections.Generic;

namespace EWMS_WPF.Models;

public partial class Location
{
    public int LocationId { get; set; }

    public int WarehouseId { get; set; }

    public string LocationCode { get; set; } = null!;

    public string? LocationName { get; set; }

    public string? Rack { get; set; }

    public int Capacity { get; set; }

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<StockInDetail> StockInDetails { get; set; } = new List<StockInDetail>();

    public virtual ICollection<StockOutDetail> StockOutDetails { get; set; } = new List<StockOutDetail>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
