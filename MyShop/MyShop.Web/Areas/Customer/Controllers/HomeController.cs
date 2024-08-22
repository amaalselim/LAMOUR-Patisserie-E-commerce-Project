using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyShop.Entities.Models;
using MyShop.Entities.Repositories;
using MyShop.Utilities;
using System.Security.Claims;
using X.PagedList.Extensions;

namespace MyShop.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public HomeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            
        }
        public IActionResult Index(int? page)
        {
            var PageNumber = page ?? 1;
            var PageSize = 8;
            var products = _unitOfWork.Product.GetAll().ToPagedList(PageNumber,PageSize);

            return View(products);
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }
            else
            {
                ShoppingCart obj = new ShoppingCart()
                {
                    ProductId = id,
                    product = _unitOfWork.Product.GetFirstOrDefault(x => x.Id == id, IncludeWord: "Category"),
                    Count = 1
                };
                return View(obj);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCart shc)
        {
            var claims = (ClaimsIdentity)User.Identity;
            var claim = claims.FindFirst(ClaimTypes.NameIdentifier);
            shc.ApplicationUserId = claim.Value;
            shc.Id = 0;

            ShoppingCart obj = _unitOfWork.ShoppingCart.GetFirstOrDefault(
                u => u.ApplicationUserId == claim.Value && u.ProductId == shc.ProductId);

            if (obj == null)
            {
                _unitOfWork.ShoppingCart.Add(shc);
                _unitOfWork.Complete();
                HttpContext.Session.SetInt32(SD.SessionKey, _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == claim.Value).ToList().Count());
                
            }
            else
            {
                _unitOfWork.ShoppingCart.IncreaseCount(obj, shc.Count);
                _unitOfWork.Complete();
            }
            
            return RedirectToAction("Index");
        }
    }
}
