using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Store_Management.Models;
using Store_Management.Services;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Store_Management.Controllers
{
    public class StoreController : Controller
    {
        private readonly StoreDataService _dataService;

        public StoreController(StoreDataService dataService)
        {
            _dataService = dataService;
        }


        // ============================================================
        // ADD PAGE
        // ============================================================

        [HttpGet]
        public IActionResult Add()
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("LoggedIn")))
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }


        // ============================================================
        // STORE INDEX - BRAND SPECIFIC ITEMS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index(
    int? brandId,
    string search = null)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("LoggedIn")))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            // ============================================================
            // GET BRAND FROM URL OR SESSION
            // ============================================================

            if (!brandId.HasValue)
            {
                brandId =
                    HttpContext.Session.GetInt32(
                        "SelectedBrandId");
            }

            // ============================================================
            // NO BRAND SELECTED
            // ============================================================

            if (!brandId.HasValue)
            {
                TempData["Error"] =
                    "Please select a brand first.";

                // Change BrandSelection to your actual
                // brand-selection controller name
                return RedirectToAction(
                    "Index",
                    "BrandSelection");
            }

            // ============================================================
            // SAVE SELECTED BRAND IN SESSION
            // ============================================================

            HttpContext.Session.SetInt32(
                "SelectedBrandId",
                brandId.Value);

            // ============================================================
            // GET BRAND
            // ============================================================

            var brand =
                await _dataService.GetBrandById(
                    brandId.Value);

            if (brand == null)
            {
                TempData["Error"] =
                    "Selected brand was not found.";

                HttpContext.Session.Remove(
                    "SelectedBrandId");

                return RedirectToAction(
                    "Index",
                    "BrandSelection");
            }

            // ============================================================
            // GET ONLY SELECTED BRAND ITEMS
            // ============================================================

            var allItems =
                await _dataService.GetItemsByBrand(
                    brandId.Value);

            allItems =
                allItems?.ToList()
                ?? new List<Item>();

            // ============================================================
            // SEARCH
            // ============================================================

            search = search?.Trim();

            List<Item> filteredItems;

            if (string.IsNullOrWhiteSpace(search))
            {
                filteredItems =
                    allItems.ToList();
            }
            else
            {
                filteredItems =
                    allItems
                        .Where(i =>
                            (
                                !string.IsNullOrWhiteSpace(
                                    i.PartNumber)
                                &&
                                i.PartNumber.Trim()
                                    .Contains(
                                        search,
                                        StringComparison
                                            .OrdinalIgnoreCase)
                            )
                            ||
                            (
                                !string.IsNullOrWhiteSpace(
                                    i.ItemName)
                                &&
                                i.ItemName.Trim()
                                    .Contains(
                                        search,
                                        StringComparison
                                            .OrdinalIgnoreCase)
                            )
                        )
                        .ToList();
            }

            // ============================================================
            // TRANSACTIONS
            // ============================================================

            var allTransactions =
                new List<Transaction>();

            foreach (var item in allItems)
            {
                if (item.Transactions != null)
                {
                    allTransactions.AddRange(
                        item.Transactions);
                }
            }

            // ============================================================
            // VIEW MODEL
            // ============================================================

            var viewModel =
                new StoreViewModel
                {
                    Items = filteredItems,

                    Search =
                        search ?? string.Empty,

                    History =
                        allTransactions
                            .OrderByDescending(
                                t => t.TransactionDate)
                            .ToList()
                };

            // ============================================================
            // BRAND INFORMATION
            // ============================================================

            ViewBag.BrandId =
                brand.BrandId;

            ViewBag.BrandName =
                brand.BrandName;

            return View(viewModel);
        }


        // ============================================================
        // ADD ITEM - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> AddItem(int brandId)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("LoggedIn")))
            {
                return RedirectToAction("Login", "Account");
            }


            // Get selected brand
            var brand =
                await _dataService.GetBrandById(brandId);


            if (brand == null)
            {
                TempData["Error"] =
                    "Brand not found.";

                return RedirectToAction("Index", "Store");
            }


            // Get only items belonging to this brand
            var items =
                await _dataService.GetItemsByBrand(
                    brandId);


            // Send selected brand information to View
            ViewBag.BrandId = brand.BrandId;

            ViewBag.BrandName = brand.BrandName;


            // Send brand-specific items to View
            return View(items);
        }


        // ============================================================
        // ADD ITEM - POST
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> AddItem(
            int brandId,
            string partNumber,
            string itemName,
            int quantity,
            IFormFile photo)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("LoggedIn")))
            {
                return RedirectToAction("Login", "Account");
            }


            try
            {
                // --------------------------------------------
                // Validate Brand
                // --------------------------------------------

                var brand =
                    await _dataService.GetBrandById(
                        brandId);


                if (brand == null)
                {
                    TempData["Error"] =
                        "Invalid brand selected.";

                    return RedirectToAction("Index", "Store");
                }


                // --------------------------------------------
                // Create Item
                // --------------------------------------------

                var item = new Item
                {
                    BrandId = brand.BrandId,

                    // Keep BrandName for existing data
                    BrandName = brand.BrandName,

                    PartNumber = partNumber?.Trim(),

                    ItemName = itemName?.Trim(),

                    Quantity = quantity
                };


                // --------------------------------------------
                // Upload Image
                // --------------------------------------------

                if (photo != null &&
                    photo.Length > 0)
                {
                    var uploadsFolder =
                        Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            "uploads");


                    if (!Directory.Exists(
                        uploadsFolder))
                    {
                        Directory.CreateDirectory(
                            uploadsFolder);
                    }


                    var uniqueFileName =
                        Guid.NewGuid().ToString()
                        + "_"
                        + photo.FileName;


                    var filePath =
                        Path.Combine(
                            uploadsFolder,
                            uniqueFileName);


                    using (var stream =
                        new FileStream(
                            filePath,
                            FileMode.Create))
                    {
                        await photo.CopyToAsync(
                            stream);
                    }


                    item.ImagePath =
                        "/uploads/"
                        + uniqueFileName;
                }


                // --------------------------------------------
                // Save Item
                // --------------------------------------------

                await _dataService.AddItem(item);


                TempData["Success"] =
                    $"Item added successfully to {brand.BrandName}!";
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    $"Error adding item: {ex.Message}";
            }


            // --------------------------------------------
            // IMPORTANT:
            // Stay on the selected brand
            // --------------------------------------------

            return RedirectToAction(
                "AddItem",
                new
                {
                    brandId = brandId
                });
        }


        // ============================================================
        // UPDATE STOCK
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> Update(
            int id,
            int quantity,
            string actionType)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("LoggedIn")))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            try
            {
                // Get item first to know its brand
                var item =
                    await _dataService.GetItemById(id);

                if (item == null)
                {
                    TempData["Error"] =
                        "Item not found.";

                    return RedirectToAction("Index", "Store");
                }

                var type =
                    actionType.ToLower() == "in"
                        ? "In"
                        : "Out";

                await _dataService.UpdateItemQuantity(
                    id,
                    quantity,
                    type);

                TempData["Success"] =
                    "Stock updated successfully!";

                // Stay inside same brand
                return RedirectToAction(
                    "Index",
                    new
                    {
                        brandId = item.BrandId
                    });
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    $"Error updating stock: {ex.Message}";

                return RedirectToAction(
                    "Index");
            }
        }


        // ============================================================
        // DELETE ITEM
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("LoggedIn")))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var item =
                await _dataService.GetItemById(id);

            if (item == null)
            {
                TempData["Error"] =
                    "Item not found.";

                return RedirectToAction("Index", "Store");
            }

            var brandId =
                item.BrandId;

            var result =
                await _dataService.DeleteItem(id);

            if (result)
            {
                TempData["Success"] =
                    "Item deleted successfully!";
            }
            else
            {
                TempData["Error"] =
                    "Error deleting item!";
            }

            return RedirectToAction(
                "Index",
                new
                {
                    brandId = brandId
                });
        }


        // ============================================================
        // EDIT ITEM - GET
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("LoggedIn")))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            var item =
                await _dataService.GetItemById(id);


            if (item == null)
            {
                return NotFound();
            }


            return View(item);
        }


        // ============================================================
        // EDIT ITEM - POST
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> Edit(Item item)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("LoggedIn")))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (ModelState.IsValid)
            {
                await _dataService.UpdateItem(item);

                TempData["Success"] =
                    "Item updated successfully!";

                return RedirectToAction(
                    "Index",
                    new
                    {
                        brandId = item.BrandId
                    });
            }

            return View(item);
        }


        // ============================================================
        // TRANSACTIONS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Transactions(int id)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("LoggedIn")))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            var item =
                await _dataService.GetItemById(id);


            if (item == null)
            {
                return NotFound();
            }


            var transactions =
                await _dataService.GetItemTransactions(
                    id);


            ViewBag.Item = item;


            return View(transactions);
        }


        // ============================================================
        // EXPORT CURRENT STOCK - BRAND SPECIFIC
        // ============================================================

        public async Task<IActionResult> ExportCurrentStock(
            int brandId)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("LoggedIn")))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var brand =
                await _dataService.GetBrandById(
                    brandId);

            if (brand == null)
            {
                TempData["Error"] =
                    "Brand not found.";

                return RedirectToAction("Index", "Store");
            }

            var items =
                await _dataService.GetItemsByBrand(
                    brandId);

            var csv =
                "PartNumber,ItemName,BrandName,Quantity,CreatedAt,UpdatedAt\n";

            foreach (var item in items)
            {
                csv +=
                    $"{item.PartNumber}," +
                    $"{item.ItemName}," +
                    $"{item.BrandName}," +
                    $"{item.Quantity}," +
                    $"{item.CreatedAt}," +
                    $"{item.UpdatedAt}\n";
            }

            var bytes =
                System.Text.Encoding.UTF8.GetBytes(
                    csv);

            return File(
                bytes,
                "text/csv",
                $"StockReport_{brand.BrandName}_{DateTime.Now:yyyyMMdd}.csv");
        }


        // ============================================================
        // MONTHLY REPORT
        // ============================================================

        public async Task<IActionResult> MonthlyReport(
            int month,
            int year)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("LoggedIn")))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }


            var items =
                await _dataService.GetAllItems();


            var csv =
                "ItemName,PartNumber,BrandName,Date,Type,Quantity,Balance\n";


            foreach (var item in items)
            {
                if (item.Transactions != null)
                {
                    foreach (
                        var transaction in item.Transactions.Where(
                            t =>
                                t.TransactionDate.Month == month &&
                                t.TransactionDate.Year == year))
                    {
                        csv +=
                            $"{item.ItemName}," +
                            $"{item.PartNumber}," +
                            $"{item.BrandName}," +
                            $"{transaction.TransactionDate}," +
                            $"{transaction.Type}," +
                            $"{transaction.Quantity}," +
                            $"{transaction.Balance}\n";
                    }
                }
            }


            var bytes =
                System.Text.Encoding.UTF8.GetBytes(
                    csv);


            var monthName =
                System.Globalization.CultureInfo
                    .CurrentCulture
                    .DateTimeFormat
                    .GetMonthName(month);


            return File(
                bytes,
                "text/csv",
                $"MonthlyReport_{monthName}_{year}.csv");
        }
    }
}