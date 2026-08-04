// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using webShopping.Models;

namespace webShopping.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            public string Name { get; set; }

            [Required]
            public string LastName { get; set; }

            public string Addres { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
            public string PostaKodu { get; set; }
            public string TelphonNo { get; set; }
            public string Role { get; set; }
            public IEnumerable<SelectListItem> RoleList { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            Input = new InputModel();
            PrepareRoleList();
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            PrepareRoleList();

            if (!ModelState.IsValid)
                return Page();

            try
            {
                var user = new ApplicationUser
                {
                    UserName = Input.Email.Trim(),
                    Email = Input.Email.Trim(),
                    EmailConfirmed = true,
                    Addres = Input.Addres,
                    City = Input.City,
                    Country = Input.Country,
                    Name = Input.Name,
                    LastName = Input.LastName,
                    PhoneNumber = Input.TelphonNo,
                    PostaKodu = Input.PostaKodu,
                    Role = Input.Role
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    return Page();
                }

                _logger.LogInformation("User created a new account with password.");

                await EnsureRolesExistAsync();

                var roleToAssign = Diger.Role_User;
                if (User.IsInRole(Diger.Role_Admin) && !string.IsNullOrWhiteSpace(user.Role))
                    roleToAssign = user.Role;

                await _userManager.AddToRoleAsync(user, roleToAssign);

                if (_userManager.Options.SignIn.RequireConfirmedAccount)
                {
                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                }

                if (User.IsInRole(Diger.Role_Admin) && !string.IsNullOrWhiteSpace(user.Role))
                    return RedirectToAction("Index", "User");

                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {Email}", Input?.Email);
                ModelState.AddModelError(string.Empty,
                    "Could not create account. Please check your details and try again.");
                return Page();
            }
        }

        private void PrepareRoleList()
        {
            Input ??= new InputModel();
            Input.RoleList = _roleManager.Roles
                .Where(i => i.Name != Diger.Role_Birey)
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Name });
        }

        private async Task EnsureRolesExistAsync()
        {
            foreach (var role in new[] { Diger.Role_Admin, Diger.Role_User, Diger.Role_Birey })
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
                throw new NotSupportedException("The default UI requires a user store with email support.");
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
