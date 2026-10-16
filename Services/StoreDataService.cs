using Microsoft.EntityFrameworkCore;
using Store_Management.Data;
using Store_Management.Models;

namespace Store_Management.Services
{
    public class StoreDataService
    {
        private readonly ApplicationDbContext _context;

        public StoreDataService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> AuthenticateUser(string employeeId, string password)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.EmployeeId == employeeId &&
                    u.Password == password);
        }

        public async Task<bool> EmployeeIdExists(string employeeId)
        {
            return await _context.Users
                .AnyAsync(u => u.EmployeeId == employeeId);
        }

        public async Task<User> RegisterUser(
            string employeeId,
            string name,
            string password,
            string role)
        {
            var user = new User
            {
                EmployeeId = employeeId.Trim(),
                Username = name.Trim(),
                Password = password,
                Role = role
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<bool> ResetPassword(
    string employeeId,
    string newPassword)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.EmployeeId == employeeId);

            if (user == null)
            {
                return false;
            }

            user.Password = newPassword;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Item>> GetAllItems()
        {
            return await _context.Items
                .Include(i => i.Transactions)
                .Include(i => i.Brand)
                .OrderBy(i => i.Id)
                .ToListAsync();
        }

        public async Task<Item> GetItemById(int id)
        {
            return await _context.Items
                .Include(i => i.Transactions)
                .Include(i => i.Brand)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<List<Item>> GetItemsByBrand(int brandId)
        {
            return await _context.Items
                .Include(i => i.Transactions)
                .Include(i => i.Brand)
                .Where(i => i.BrandId == brandId)
                .OrderBy(i => i.Id)
                .ToListAsync();
        }


        public async Task<Item> AddItem(Item item)
        {
            item.CreatedAt = DateTime.Now;
            item.UpdatedAt = DateTime.Now;

            // Validate and calculate the derived values.
            if (item.LifeTime.HasValue)
            {
                item.LifeTimeDays =
                    (item.LifeTime.Value * 3000m) / 2400m;
            }

            if (item.LeadTime.HasValue)
            {
                item.LeadTimeDays =
                    (item.LeadTime.Value * 2400m) / 2400m;
            }

            // Add the item, including both calculated fields.
            _context.Items.Add(item);

            // Save the item to the database.
            await _context.SaveChangesAsync();

            // Record the initial stock transaction.
            if (item.Quantity > 0)
            {
                var transaction = new Transaction
                {
                    ItemId = item.Id,
                    Quantity = item.Quantity,
                    Type = "In",
                    Balance = item.Quantity,
                    TransactionDate = DateTime.Now
                };

                _context.Transactions.Add(transaction);

                await _context.SaveChangesAsync();
            }

            return item;
        }


        public async Task<Item> UpdateItem(Item item)
        {
            var existingItem = await _context.Items.FindAsync(item.Id);

            if (existingItem == null)
            {
                throw new Exception("Item not found");
            }

            existingItem.PartNumber = item.PartNumber;
            existingItem.ItemName = item.ItemName;
            existingItem.BrandId = item.BrandId;
            existingItem.BrandName = item.BrandName;
            existingItem.ImagePath = item.ImagePath;
            existingItem.ModelNumber = item.ModelNumber;
            existingItem.ProjectCode = item.ProjectCode;
            existingItem.Status = item.Status;
            existingItem.Remarks = item.Remarks;

            existingItem.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return existingItem;
        }



        public async Task<Item> UpdateItemQuantity(
            int itemId,
            int change,
            string type)
        {
            if (change <= 0)
            {
                throw new Exception("Quantity must be greater than zero.");
            }

            var item = await _context.Items.FindAsync(itemId);

            if (item == null)
            {
                throw new Exception("Item not found.");
            }

            if (string.Equals(type, "In", StringComparison.OrdinalIgnoreCase))
            {
                item.Quantity += change;
            }
            else if (string.Equals(type, "Out", StringComparison.OrdinalIgnoreCase))
            {
                if (item.Quantity < change)
                {
                    throw new Exception(
                        $"Insufficient stock. Available quantity: {item.Quantity}");
                }

                // Reduce available quantity.
                item.Quantity -= change;

                // Increase cumulative StockOut quantity.
                item.StockOut += change;
            }
            else
            {
                throw new Exception("Invalid transaction type.");
            }

            item.UpdatedAt = DateTime.Now;


            // Recalculate LifeTimeDays using the current quantity.
            if (item.LifeTime.HasValue)
            {
                item.LifeTimeDays =
                    ((decimal)item.Quantity * item.LifeTime.Value) / 2400m;
            }


            // Record the stock transaction.
            var transaction = new Transaction
            {
                ItemId = itemId,
                Quantity = change,
                Type = string.Equals(
                    type, "Out", StringComparison.OrdinalIgnoreCase) ? "Out" : "In",
                Balance = item.Quantity,
                TransactionDate = DateTime.Now
            };

            _context.Transactions.Add(transaction);

            // Save Quantity, StockOut, UpdatedAt and the transaction together.
            await _context.SaveChangesAsync();

            return item;
        }


        public async Task<bool> DeleteItem(int id)
        {
            var item =
                await _context.Items.FindAsync(id);

            if (item == null)
            {
                return false;
            }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Transaction>> GetItemTransactions(
            int itemId)
        {
            return await _context.Transactions
                .Where(t => t.ItemId == itemId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        public async Task<List<Brand>> GetBrands()
        {
            return await _context.BrandMaster
                .Where(b => b.IsActive)
                .OrderBy(b => b.BrandName)
                .ToListAsync();
        }

        public async Task<Brand> AddBrand(string brandName)
        {
            var exists = await _context.BrandMaster
                .AnyAsync(x =>
                    x.BrandName.ToLower() ==
                    brandName.ToLower()
                    && x.IsActive);

            if (exists)
            {
                throw new Exception("Brand already exists");
            }

            var brand = new Brand
            {
                BrandName = brandName.Trim(),
                IsActive = true,
                CreatedDate = DateTime.Now
            };
            _context.BrandMaster.Add(brand);
            await _context.SaveChangesAsync();
            return brand;
        }

        public async Task<Brand> GetBrandById(int id)
        {
            return await _context.BrandMaster
                .FirstOrDefaultAsync(b =>
                    b.BrandId == id &&
                    b.IsActive);
        }

        public async Task UpdateBrand(
            int id,
            string brandName)
        {
            var brand =
                await _context.BrandMaster.FindAsync(id);

            if (brand == null)
            {
                throw new Exception("Brand not found");
            }
            brand.BrandName = brandName.Trim();
            await _context.SaveChangesAsync();
        }


        public async Task DeleteBrand(int id)
        {
            var brand =
                await _context.BrandMaster.FindAsync(id);

            if (brand == null)
            {
                throw new Exception("Brand not found");
            }
            brand.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }
}