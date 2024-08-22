using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyShop.DataAccess;
using MyShop.Utilities;
using System.Security.Claims;

namespace MyShop.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.AdminRole)]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public UsersController(ApplicationDbContext context)
        {
            _context = context; 
        }
        public IActionResult Index()
        {
            // to get all users without me (admin)
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim=claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            string userId = claim.Value;

            return View(_context.applicationUsers.Where(x=>x.Id!=userId).ToList()); 
        }
        public IActionResult LockUnlock(string? Id)
        {
            var user=_context.applicationUsers.FirstOrDefault(x=>x.Id==Id);
            if (user == null)
            {
                return NotFound();
            }
            if(user.LockoutEnd==null || user.LockoutEnd < DateTime.Now)
            {
                user.LockoutEnd = DateTime.Now.AddYears(1);
            }
            else
            {
                user.LockoutEnd = DateTime.Now;
            }
            _context.SaveChanges();
            return RedirectToAction("Index", "Users", new {area="Admin"});
        }
    }
}
