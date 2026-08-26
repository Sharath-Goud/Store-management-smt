namespace Store_Management.Models
{
    public class Brand
    {
        public int BrandId { get; set; }
        public string BrandName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
