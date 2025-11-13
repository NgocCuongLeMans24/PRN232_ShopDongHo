namespace ClientSide.DataDtos
{
    public class ReviewDto
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string CustomerName { get; set; } = string.Empty;
    }

    public class ProductReviewsDto
    {
        public List<ReviewDto> Reviews { get; set; } = new();
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<RatingDistributionDto> RatingDistribution { get; set; } = new();
    }

    public class RatingDistributionDto
    {
        public int Rating { get; set; }
        public int Count { get; set; }
    }
}

