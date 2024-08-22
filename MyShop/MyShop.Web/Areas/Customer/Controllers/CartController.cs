using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyShop.Entities.Models;
using MyShop.Entities.Repositories;
using MyShop.Entities.ViewModels;
using MyShop.Utilities;
using Stripe.Checkout;
using System.Security.Claims;

namespace MyShop.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public int TotalCarts { get; set; }
        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            var claims = (ClaimsIdentity)User.Identity;
            var claim = claims.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM = new ShoppingCartVM()
            {
                CartsList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, IncludeWord: "product")
            };
            foreach (var item in ShoppingCartVM.CartsList)
            {
                ShoppingCartVM.TotalCarts += (item.Count * item.product.Price);
            }

            return View(ShoppingCartVM);
        }
        public IActionResult Plus(int cartid)
        {
            var shoppingcart = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartid);
            _unitOfWork.ShoppingCart.IncreaseCount(shoppingcart, 1);
            _unitOfWork.Complete();
            return RedirectToAction("Index");
        }

        public IActionResult Minus(int cartid)
        {
            var shoppingcart = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartid);

            if (shoppingcart.Count <= 1)
            {
                _unitOfWork.ShoppingCart.Remove(shoppingcart);
                var count = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == shoppingcart.ApplicationUserId).ToList().Count() - 1;
                HttpContext.Session.SetInt32(SD.SessionKey, count);
            }
            else
            {
                _unitOfWork.ShoppingCart.decreaseCount(shoppingcart, 1);

            }
            _unitOfWork.Complete();
            return RedirectToAction("Index");
        }

        public IActionResult Remove(int cartid)
        {
            var shoppingcart = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartid);
            _unitOfWork.ShoppingCart.Remove(shoppingcart);
            _unitOfWork.Complete();
            var count = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == shoppingcart.ApplicationUserId).ToList().Count();
            HttpContext.Session.SetInt32(SD.SessionKey, count);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Summary()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM = new ShoppingCartVM()
            {
                CartsList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, IncludeWord: "product"),
                orderHeader = new()
            };
            ShoppingCartVM.orderHeader.ApplicationUser = _unitOfWork.applicationUser.GetFirstOrDefault(x => x.Id == claim.Value);

            ShoppingCartVM.orderHeader.Name = ShoppingCartVM.orderHeader.ApplicationUser.Name;
            ShoppingCartVM.orderHeader.Address = ShoppingCartVM.orderHeader.ApplicationUser.Address;
            ShoppingCartVM.orderHeader.City = ShoppingCartVM.orderHeader.ApplicationUser.City;
            ShoppingCartVM.orderHeader.Phone = ShoppingCartVM.orderHeader.ApplicationUser.PhoneNumber;

            foreach (var item in ShoppingCartVM.CartsList)
            {
                ShoppingCartVM.orderHeader.TotalPrice += (item.Count * item.product.Price);
            }

            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Summary")]
        public IActionResult SummaryOnPost(ShoppingCartVM ShoppingCartVM)
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM.CartsList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, IncludeWord: "product");
            var u = _unitOfWork.applicationUser.GetFirstOrDefault(x => x.Id == claim.Value);

            ShoppingCartVM.orderHeader = new OrderHeader();

            ShoppingCartVM.orderHeader.OrderStatus = SD.Pending;
            ShoppingCartVM.orderHeader.PaymentStatus = SD.Pending;
            ShoppingCartVM.orderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.orderHeader.ApplicationUserId = claim.Value;

            ShoppingCartVM.orderHeader.Name = u.Name;
            ShoppingCartVM.orderHeader.Address = u.Address;
            ShoppingCartVM.orderHeader.City = u.City;
            ShoppingCartVM.orderHeader.Phone=u.PhoneNumber;


            foreach (var item in ShoppingCartVM.CartsList)
            {
                ShoppingCartVM.orderHeader.TotalPrice += (item.Count * item.product.Price);
            }

            _unitOfWork.OrderHeader.Add(ShoppingCartVM.orderHeader);
            _unitOfWork.Complete();



            foreach (var item in ShoppingCartVM.CartsList)
            {
                OrderDetails detail = new OrderDetails
                {
                    ProductId = item.ProductId,
                    OrderHeaderId = ShoppingCartVM.orderHeader.Id,
                    Price = item.product.Price,
                    Count = item.Count
                };

                _unitOfWork.orderDetails.Add(detail);
                _unitOfWork.Complete();
            }


            var Domain = "https://localhost:44345/";


            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = Domain + $"Customer/Cart/OrderConfirmation?id={ShoppingCartVM.orderHeader.Id}",
                CancelUrl = Domain + $"Customer/Cart/Index",
            };

            foreach (var item in ShoppingCartVM.CartsList)
            {
                var sessionlineoptions = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.product.Price) * 100,
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.product.Name,
                        },
                    },
                    Quantity = item.Count,
                };
                options.LineItems.Add(sessionlineoptions);
            }

            var service = new SessionService();
            Session session = service.Create(options);

            ShoppingCartVM.orderHeader.SessionId = session.Id;
            _unitOfWork.Complete();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderheader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == id);

            var service = new SessionService();

            Session session = service.Get(orderheader.SessionId);

            if (session.PaymentStatus.ToLower() == "paid")
            {
                _unitOfWork.OrderHeader.UpdateOrderStaus(id, SD.Approve, SD.Approve);
                orderheader.PaymentIntentId = session.PaymentIntentId;
                _unitOfWork.Complete();
            }

            List<ShoppingCart> shoppingcarts = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == orderheader.ApplicationUserId).ToList();

            _unitOfWork.ShoppingCart.RemoveRange(shoppingcarts);
            _unitOfWork.Complete();
            return View(id);
        }
    }
}

