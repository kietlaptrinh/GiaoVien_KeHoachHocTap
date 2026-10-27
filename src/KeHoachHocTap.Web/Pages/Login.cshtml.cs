using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeHoachHocTap.Web.Pages;

public class LoginModel : PageModel
{
    public void OnGet()
    {
    }

    public IActionResult OnGetLogout()
    {
        return RedirectToPage("/Index");
    }
}
