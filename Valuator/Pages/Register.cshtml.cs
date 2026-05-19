using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Services;

namespace Valuator.Pages;

public class RegisterModel : PageModel
{
    private readonly UserStore _userStore;

    public RegisterModel(UserStore userStore)
    {
        _userStore = userStore;
    }

    [BindProperty]
    public string Login { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? Error { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!_userStore.TryCreateUser(Login, Password, out string error))
        {
            Error = error;
            return Page();
        }

        return RedirectToPage("/Login");
    }
}
