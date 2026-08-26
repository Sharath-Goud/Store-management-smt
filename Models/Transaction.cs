using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Store_Management.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ItemId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [StringLength(10)]
        public string Type { get; set; } = "In";

        [Required]
        public int Balance { get; set; }

        public DateTime TransactionDate { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item? Item { get; set; }

        [NotMapped]
        public int Change
        {
            get
            {
                return Type == "In" ? Quantity : -Quantity;
            }
        }

        public Transaction()
        {
            TransactionDate = DateTime.Now;
            Type = "In";
        }
    }
}