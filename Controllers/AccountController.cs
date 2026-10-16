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
                    model.EmployeeId,
                    model.Password
                );

                if (user != null)
                {
                    HttpContext.Session.SetString("LoggedIn", "true");
                    HttpContext.Session.SetString("EmployeeId", user.EmployeeId);
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("Role", user.Role);

                    return RedirectToAction("BrandSelection", "Account");
                }

                ViewBag.Error = "Invalid Employee ID or password.";
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

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check whether Employee ID already exists
            var employeeExists =
                await _dataService.EmployeeIdExists(model.EmployeeId);

            if (employeeExists)
            {
                ViewBag.Error =
                    "This Employee ID is already registered.";

                return View(model);
            }

            try
            {
                await _dataService.RegisterUser(
                    model.EmployeeId,
                    model.Name,
                    model.Password,
                    model.Role
                );

                TempData["Success"] =
                    "Account created successfully. Please login.";

                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                ViewBag.Error =
                    "Unable to create account. Please try again.";

                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }


        [HttpPost]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var employeeExists =
                await _dataService.EmployeeIdExists(
                    model.EmployeeId.Trim()
                );

            if (!employeeExists)
            {
                ViewBag.Error =
                    "Employee ID not found.";

                return View(model);
            }

            try
            {
                var passwordUpdated =
                    await _dataService.ResetPassword(
                        model.EmployeeId.Trim(),
                        model.NewPassword
                    );

                if (!passwordUpdated)
                {
                    ViewBag.Error =
                        "Unable to reset password. Please try again.";

                    return View(model);
                }

                TempData["Success"] =
                    "Password reset successfully. Please login with your new password.";

                return RedirectToAction("Login");
            }
            catch (Exception)
            {
                ViewBag.Error =
                    "Unable to reset password. Please try again.";

                return View(model);
            }
        }
    }
}