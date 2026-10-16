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

            ViewBag.BrandId =
                brand.BrandId;

            ViewBag.BrandName =
                brand.BrandName;

            return View(viewModel);
        }



        [HttpGet]
        public async Task<IActionResult> AddItem(
            int brandId,
            string operation = "In")
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("LoggedIn")))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get selected brand
            var brand = await _dataService.GetBrandById(brandId);

            if (brand == null)
            {
                TempData["Error"] = "Brand not found.";
                return RedirectToAction("Index", "Store");
            }

            // Get items belonging to this brand
            var items = await _dataService.GetItemsByBrand(brandId);

            // Send brand information to the view
            ViewBag.BrandId = brand.BrandId;
            ViewBag.BrandName = brand.BrandName;

            // Send selected operation to the view
            ViewBag.Operation = operation == "Out" ? "Out" : "In";

            return View(items);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(
    int brandId,
    string partNumber,
    string itemName,
    int quantity,
    string? modelNumber,
    string? projectCode,
    int? lifeTime,
    int? leadTime,
    string? status,
    IFormFile? photo)
        {
            // 1. Check login session
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("LoggedIn")))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // 2. Validate quantity
                if (quantity <= 0)
                {
                    TempData["Error"] = "Quantity must be greater than zero.";

                    return RedirectToAction(
                        "AddItem",
                        new { brandId, operation = "In" });
                }

                // 3. Validate required fields
                if (string.IsNullOrWhiteSpace(partNumber) ||
                    string.IsNullOrWhiteSpace(itemName) ||
                    string.IsNullOrWhiteSpace(modelNumber) ||
                    string.IsNullOrWhiteSpace(projectCode) ||
                    !lifeTime.HasValue ||
                    lifeTime.Value <= 0 ||
                    !leadTime.HasValue ||
                    leadTime.Value <= 0 ||
                    string.IsNullOrWhiteSpace(status))
                {
                    TempData["Error"] =
                        "Please fill in all required fields. " +
                        "Life Time and Lead Time must be greater than zero.";

                    return RedirectToAction(
                        "AddItem",
                        new { brandId, operation = "In" });
                }

                // 4. Validate status
                status = status.Trim();

                if (status != "Good" && status != "NG")
                {
                    TempData["Error"] = "Please select a valid status.";

                    return RedirectToAction(
                        "AddItem",
                        new { brandId, operation = "In" });
                }

                // 5. Get selected brand
                var brand = await _dataService.GetBrandById(brandId);

                if (brand == null)
                {
                    TempData["Error"] = "Invalid brand selected.";

                    return RedirectToAction("Index", "Store");
                }

                // LifeTimeDays = LifeTime * 3000 / 2400
                decimal lifeTimeDays =
                    ((decimal)quantity * lifeTime.Value) / 2400m;

                // LeadTimeDays = (LeadTime * 2400) / 2400
                decimal leadTimeDays =
                    (leadTime.Value * 2400m) / 2400m;

                // 7. Create item with original and calculated values
                var item = new Item
                {
                    BrandId = brand.BrandId,
                    BrandName = brand.BrandName,

                    PartNumber = partNumber.Trim(),
                    ItemName = itemName.Trim(),
                    Quantity = quantity,

                    ModelNumber = modelNumber.Trim(),
                    ProjectCode = projectCode.Trim(),

                    // Original input values
                    LifeTime = lifeTime.Value,
                    LeadTime = leadTime.Value,

                    // Calculated values
                    LifeTimeDays = lifeTimeDays,
                    LeadTimeDays = leadTimeDays,

                    Status = status
                };

                // 8. Upload image if provided
                if (photo != null && photo.Length > 0)
                {
                    var uploadsFolder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads");

                    Directory.CreateDirectory(uploadsFolder);

                    var extension = Path.GetExtension(photo.FileName);
                    var uniqueFileName = Guid.NewGuid().ToString() + extension;

                    var filePath = Path.Combine(
                        uploadsFolder,
                        uniqueFileName);

                    using (var stream = new FileStream(
                        filePath,
                        FileMode.Create))
                    {
                        await photo.CopyToAsync(stream);
                    }

                    item.ImagePath = "/uploads/" + uniqueFileName;
                }

                // 9. Save item to database
                await _dataService.AddItem(item);

                TempData["Success"] =
                    $"Item added successfully to {brand.BrandName}!";
            }
            catch (Exception ex)
            {
                // Log the actual exception
                Console.Error.WriteLine(ex);

                TempData["Error"] =
                    "Unable to save the item. Please check the application logs.";
            }

            // 10. Return to Stock In page
            return RedirectToAction(
                "AddItem",
                new { brandId, operation = "In" });
        }


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

        [HttpGet]
        public async Task<IActionResult> ReportsHistory(int? brandId)
        {
            // 1. Check login
            if (HttpContext.Session.GetString("LoggedIn") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            // 2. Get selected brand from session if not passed in URL
            if (!brandId.HasValue)
            {
                brandId = HttpContext.Session.GetInt32("SelectedBrandId");
            }

            // 3. If no brand is selected, go to brand selection
            if (!brandId.HasValue)
            {
                TempData["Error"] = "Please select a brand first.";
                return RedirectToAction("Index", "BrandSelection");
            }

            // 4. Get the selected brand
            var brand = await _dataService.GetBrandById(brandId.Value);

            if (brand == null)
            {
                HttpContext.Session.Remove("SelectedBrandId");
                TempData["Error"] = "Selected brand was not found.";
                return RedirectToAction("Index", "BrandSelection");
            }

            // 5. Save the selected brand in session
            HttpContext.Session.SetInt32("SelectedBrandId", brand.BrandId);

            // 6. Get items only for the selected brand
            var items = await _dataService.GetItemsByBrand(brand.BrandId)
                         ?? new List<Item>();

            // 7. Get transaction history for those items
            var history = items
                .Where(item => item.Transactions != null)
                .SelectMany(item => item.Transactions)
                .OrderByDescending(transaction => transaction.TransactionDate)
                .ToList();

            // 8. Prepare the view model
            var viewModel = new StoreViewModel
            {
                Items = items.ToList(),
                Search = string.Empty,
                History = history
            };

            // 9. Pass brand information to the view
            ViewBag.BrandId = brand.BrandId;
            ViewBag.BrandName = brand.BrandName;

            // 10. Open Views/Store/ReportsHistory.cshtml
            return View("ReportsHistory", viewModel);
        }


        [HttpGet]
        public async Task<IActionResult> StockInOut(int? brandId)
        {
            if (HttpContext.Session.GetString("LoggedIn") != "true")
                return RedirectToAction("Login", "Account");

            if (!brandId.HasValue)
                brandId = HttpContext.Session.GetInt32("SelectedBrandId");

            if (!brandId.HasValue)
            {
                TempData["Error"] = "Please select a brand first.";
                return RedirectToAction("Index", "BrandSelection");
            }

            var brand = await _dataService.GetBrandById(brandId.Value);

            if (brand == null)
            {
                TempData["Error"] = "Selected brand was not found.";
                return RedirectToAction("Index", "BrandSelection");
            }

            HttpContext.Session.SetInt32("SelectedBrandId", brand.BrandId);
            ViewBag.BrandId = brand.BrandId;
            ViewBag.BrandName = brand.BrandName;

            return View("StockInOut");
        }


        // STOCK OUT PAGE
        [HttpGet]
        public async Task<IActionResult> StockOut(int? brandId)
        {
            if (HttpContext.Session.GetString("LoggedIn") != "true")
                return RedirectToAction("Login", "Account");

            if (!brandId.HasValue)
                brandId = HttpContext.Session.GetInt32("SelectedBrandId");

            if (!brandId.HasValue)
            {
                TempData["Error"] = "Please select a brand first.";
                return RedirectToAction("Index", "BrandSelection");
            }

            var brand = await _dataService.GetBrandById(brandId.Value);

            if (brand == null)
            {
                TempData["Error"] = "Selected brand was not found.";
                return RedirectToAction("Index", "BrandSelection");
            }

            HttpContext.Session.SetInt32("SelectedBrandId", brand.BrandId);

            var items = await _dataService.GetItemsByBrand(brand.BrandId)
                        ?? new List<Item>();

            ViewBag.BrandId = brand.BrandId;
            ViewBag.BrandName = brand.BrandName;

            return View("StockOut", items.ToList());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StockOutItem(
            int id,
            int brandId,
            int quantity,
            string? remarks)
        {
            if (HttpContext.Session.GetString("LoggedIn") != "true")
                return RedirectToAction("Login", "Account");

            if (quantity <= 0)
            {
                TempData["Error"] = "Quantity must be greater than zero.";
                return RedirectToAction("StockOut", new { brandId });
            }

            var item = await _dataService.GetItemById(id);

            if (item == null || item.BrandId != brandId)
            {
                TempData["Error"] = "Item not found for the selected brand.";
                return RedirectToAction("StockOut", new { brandId });
            }

            if (item.Quantity < quantity)
            {
                TempData["Error"] =
                    $"Insufficient stock. Available quantity: {item.Quantity}.";

                return RedirectToAction("StockOut", new { brandId });
            }

            try
            {
                // Reduce stock and record the Out transaction.
                await _dataService.UpdateItemQuantity(id, quantity, "Out");

                // Reload the item to get the updated quantity.
                item = await _dataService.GetItemById(id);

                if (item != null)
                {
                    item.Remarks = string.IsNullOrWhiteSpace(remarks)
                        ? null
                        : remarks.Trim();

                    await _dataService.UpdateItem(item);
                }

                TempData["Success"] =
                    $"Stock Out successful. Removed quantity: {quantity}.";
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                TempData["Error"] =
                    "Stock Out failed. Please check the application logs.";
            }

            return RedirectToAction("StockOut", new { brandId });
        }

    }
}