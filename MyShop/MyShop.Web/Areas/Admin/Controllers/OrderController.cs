using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyShop.Entities.Models;
using MyShop.Entities.Repositories;
using MyShop.Entities.ViewModels;
using MyShop.Utilities;
using Stripe;

namespace MyShop.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.AdminRole)]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;      
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult GetData()
        {
            IEnumerable<OrderHeader> orderHeaders;
            orderHeaders = _unitOfWork.OrderHeader.GetAll(IncludeWord: "ApplicationUser");
            return Json(new { data = orderHeaders});
        }
        public IActionResult Details(int orderid)
        {
            OrderVM orderVM = new OrderVM()
            {
                OrderHeader=_unitOfWork.OrderHeader.GetFirstOrDefault(u=>u.Id==orderid,IncludeWord:"ApplicationUser"),
                OrderDetails=_unitOfWork.orderDetails.GetAll(x=>x.OrderHeaderId==orderid,IncludeWord:"Product")
            };
            return View(orderVM);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
		public IActionResult UpdateOrderDetails()
		{
            var orderfromdb = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id ==OrderVM.OrderHeader.Id);
            orderfromdb.Name = OrderVM.OrderHeader.Name;
            orderfromdb.Phone = OrderVM.OrderHeader.Phone;
            orderfromdb.Address = OrderVM.OrderHeader.Address;
            orderfromdb.City= OrderVM.OrderHeader.City;
            if (OrderVM.OrderHeader.Carrier != null)
            {
                orderfromdb.Carrier = OrderVM.OrderHeader.Carrier;
            }

			if (OrderVM.OrderHeader.TrackingNumber != null)
			{
				orderfromdb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
			}
            _unitOfWork.OrderHeader.Update(orderfromdb);
            _unitOfWork.Complete();

			TempData["Update"] = "Data Has Updated Successfully";
			return RedirectToAction("Details", "Order", new {orderid=orderfromdb.Id});
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult StartProcess()
		{
            _unitOfWork.OrderHeader.UpdateOrderStaus(OrderVM.OrderHeader.Id, SD.Processing, null);
            _unitOfWork.Complete();
			

			TempData["Update"] = "Order Status Has Updated Successfully";
			return RedirectToAction("Details", "Order", new { orderid = OrderVM.OrderHeader.Id });
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult StartShip()
		{
			var orderfromdb = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderVM.OrderHeader.Id);
            orderfromdb.TrackingNumber=OrderVM.OrderHeader.TrackingNumber;  
            orderfromdb.Carrier=OrderVM.OrderHeader.Carrier;
            orderfromdb.OrderStatus=OrderVM.OrderHeader.OrderStatus;
            orderfromdb.ShippingDate = DateTime.Now;

            _unitOfWork.OrderHeader.Update(orderfromdb);
            _unitOfWork.Complete();

			TempData["Update"] = "Order Status Has Shipped Successfully";
			return RedirectToAction("Details", "Order", new { orderid = OrderVM.OrderHeader.Id });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult CancelOrder()
		{
			var orderfromdb = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderVM.OrderHeader.Id);
            if(orderfromdb.PaymentStatus== SD.Approve)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderfromdb.PaymentIntentId
                };

                var service= new RefundService();
                Refund refund = service.Create(options);
                _unitOfWork.OrderHeader.UpdateOrderStaus(orderfromdb.Id, SD.Cancelled, SD.Refund);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateOrderStaus(orderfromdb.Id, SD.Cancelled, SD.Cancelled);
            }
            _unitOfWork.Complete();


			TempData["Update"] = "Order Has Cancelled Successfully";
			return RedirectToAction("Details", "Order", new { orderid = OrderVM.OrderHeader.Id });
		}

	}
}
