using System.Collections.Generic;

namespace Store_Management.Models
{
    public class StoreViewModel
    {
        public List<Item> Items { get; set; } = new List<Item>();
        public string Search { get; set; } = string.Empty;
        public string FilterBy { get; set; } = string.Empty;
        public List<Transaction> History { get; set; } = new List<Transaction>();

        public StoreViewModel()
        {
            Items = new List<Item>();
            History = new List<Transaction>();
        }

        public List<Transaction> GetItemHistory(int itemId)
        {
            var item = Items?.Find(i => i.Id == itemId);
            return item?.Transactions != null
                ? new List<Transaction>(item.Transactions)
                : new List<Transaction>();
        }

        public List<Item> SearchItems(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return Items ?? new List<Item>();

            return Items?.FindAll(i =>
                i.ItemName.Contains(searchTerm, System.StringComparison.OrdinalIgnoreCase) ||
                i.PartNumber.Contains(searchTerm, System.StringComparison.OrdinalIgnoreCase)
            ) ?? new List<Item>();
        }

        public int TotalItems => Items?.Count ?? 0;

        public int TotalQuantity
        {
            get
            {
                if (Items == null) return 0;
                int total = 0;
                foreach (var item in Items)
                {
                    total += item.Quantity;
                }
                return total;
            }
        }
    }
}