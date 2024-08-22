using MyShop.Entities.Models;
using MyShop.Entities.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyShop.DataAccess.Implementation
{
	public class OrderHeaderRepository : GenericRepository<OrderHeader>,IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _context;
        public OrderHeaderRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public void Update(OrderHeader orderHeader)
        {
           _context.orderHeaders.Update(orderHeader);
        }

		public void UpdateOrderStaus(int id, string? OrderStatus, string? PaymentStatus)
		{
           var orderfromdb = _context.orderHeaders.FirstOrDefault(x => x.Id == id);
            if (orderfromdb != null)
            {
                orderfromdb.OrderStatus = OrderStatus;
                orderfromdb.PaymentStatus = DateTime.Now.ToString();
                if (PaymentStatus != null)
                {
                    orderfromdb.PaymentStatus = PaymentStatus;
                }
            }
		}
	}
}
