using System;
using System.Collections.Generic;

namespace EWMS_WPF.Models;

public partial class TransferDetail
{
    public int TransferDetailId { get; set; }

    public int TransferId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual TransferRequest Transfer { get; set; } = null!;
}
