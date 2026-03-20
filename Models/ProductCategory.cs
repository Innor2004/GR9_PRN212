using System;
using System.Collections.Generic;

namespace EWMS_WPF.Models;

public partial class ProductCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public int? SupplierId { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual Supplier? Supplier { get; set; }
}
