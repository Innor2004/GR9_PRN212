using System;
using System.Collections.Generic;

namespace EWMS_WPF.Models;

public partial class ProductCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    
}
