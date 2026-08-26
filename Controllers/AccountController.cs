using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Store_Management.Models;
using Store_Management.Services;
using System.Threading.Tasks;

namespace Store_Management.Controllers
{
    public class AccountController : Controller
    {
        private readonly StoreDataService _dataService;

        public AccountController(StoreDataService dataService)
        {
            _dataService = dataService;
        }


        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("LoggedIn")))
            {
                return RedirectToAction("BrandSelection", "Account");
            }

            return View(new LoginViewModel());
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _dataService.AuthenticateUser(
                    model.Username,
                    model.Password
                );

                if (user != null)
                {
                    HttpContext.Session.SetString("LoggedIn", "true");
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("Role", user.Role);

                    return RedirectToAction("BrandSelection", "Account");
                }

                ViewBag.Error = "Invalid username or password.";
            }

            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> BrandSelection()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("LoggedIn")))
            {
                return RedirectToAction("Login");
            }

            var brands = await _dataService.GetBrands();

            return View(brands);
        }

        [HttpPost]
        public async Task<IActionResult> AddBrand(string brandName)
        {

            if (!string.IsNullOrWhiteSpace(brandName))
            {
                await _dataService.AddBrand(brandName);
            }

            return RedirectToAction("BrandSelection");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBrand(int id, string brandName)
        {

            await _dataService.UpdateBrand(id, brandName);

            return RedirectToAction("BrandSelection");
        }

        public async Task<IActionResult> DeleteBrand(int id)
        {

            await _dataService.DeleteBrand(id);

            return RedirectToAction("BrandSelection");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }
    }
}