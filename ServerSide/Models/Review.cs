using System;
using System.Collections.Generic;

namespace ServerSide.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int CustomerId { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual User Customer { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
