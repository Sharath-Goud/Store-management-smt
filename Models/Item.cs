using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Store_Management.Models
{
    public class Item
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string PartNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ItemName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ModelNumber { get; set; }

        [StringLength(100)]
        public string? ProjectCode { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        public int StockOut { get; set; } = 0;

        public int NgStatus { get; set; } = 0;

        [StringLength(500)]
        public string? Remarks { get; set; }

        [Required]
        public int BrandId { get; set; }

        [ForeignKey("BrandId")]
        public virtual Brand? Brand { get; set; }

        [StringLength(100)]
        public string BrandName { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [StringLength(500)]
        public string? ImagePath { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public virtual ICollection<Transaction>? Transactions { get; set; }

        public int? LifeTime { get; set; }

        public int? LeadTime { get; set; }
        public decimal? LifeTimeDays { get; set; }
        public decimal? LeadTimeDays { get; set; }

        public Item()
        {
            Transactions = new List<Transaction>();

            CreatedAt = DateTime.Now;

            UpdatedAt = DateTime.Now;

            Quantity = 0;
        }
    }
}