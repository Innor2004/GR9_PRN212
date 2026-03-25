using System;
using System.Collections.Generic;

namespace EWMS_WPF.Models;

public partial class UserWarehouse
{
    public int UserWarehouseId { get; set; }

    public int UserId { get; set; }

    public int WarehouseId { get; set; }

    public DateTime? AssignedDate { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
