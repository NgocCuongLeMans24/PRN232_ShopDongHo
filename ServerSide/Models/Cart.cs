using System;
using System.Collections.Generic;

namespace ServerSide.Models;

public partial class Cart
{
    public int CartId { get; set; }

    public int CustomerId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public DateTime? AddedAt { get; set; }

    public virtual User Customer { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
