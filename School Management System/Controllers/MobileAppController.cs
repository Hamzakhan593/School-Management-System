using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace School_Management_System.Controllers;

[Authorize]
public class MobileAppController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
