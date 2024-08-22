using Microsoft.AspNetCore.Mvc;
using MyShop.Entities.Models;
using MyShop.DataAccess;
using MyShop.Entities.Repositories;
using Microsoft.EntityFrameworkCore;
using MyShop.Entities.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;


namespace MyShop.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult GetData()
        {
            var Products = _unitOfWork.Product.GetAll(IncludeWord:"Category");
            return Json(new {data=Products});
        }


        [HttpGet]
        public IActionResult Create()
        {
            ProductVM productVM = new ProductVM()
            {
                product = new Product(),
                CategoryList = _unitOfWork.Category.GetAll().Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                })
            };
            return View(productVM);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProductVM ProductVM,IFormFile File)
        {
            if (ModelState.IsValid)
            {
                string RootPath = _webHostEnvironment.WebRootPath;
                if (File != null)
                {
                    string Filename=Guid.NewGuid().ToString();
                    var Upload = Path.Combine(RootPath, @"Images\Products");
                    var extention = Path.GetExtension(File.FileName);

                    using(var filestream = new FileStream(Path.Combine(Upload, Filename + extention), FileMode.Create))
                    {
                        File.CopyTo(filestream);
                    }
                    ProductVM.product.Img= @"\Images\Products\" + Filename + extention;
                }
                _unitOfWork.Product.Add(ProductVM.product);
                _unitOfWork.Complete();
                TempData["Create"] = "Data Has Created Successfully";
                return RedirectToAction("Index");
            }
            return View(ProductVM.product);

        }

        [HttpGet]
        public IActionResult Edit(int? Id)
        {
            if (Id == null || Id == 0)
            {
                NotFound();
            }
            ProductVM productVM = new ProductVM()
            {
                product = _unitOfWork.Product.GetFirstOrDefault(x => x.Id == Id),
                CategoryList = _unitOfWork.Category.GetAll().Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                })
            };
            return View(productVM);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProductVM ProductVM,IFormFile? File)
        {
            if (ModelState.IsValid)
            {
                string RootPath = _webHostEnvironment.WebRootPath;
                if (File != null)
                {
                    string Filename = Guid.NewGuid().ToString();
                    var Upload = Path.Combine(RootPath, @"Images\Products");
                    var extention = Path.GetExtension(File.FileName);

                    if (ProductVM.product.Img != null)
                    {
                        var oldImg = Path.Combine(RootPath, ProductVM.product.Img.TrimStart('\\'));
                        if(System.IO.File.Exists(oldImg)) {
                            System.IO.File.Delete(oldImg);
                        }
                    }

                    using (var filestream = new FileStream(Path.Combine(Upload, Filename + extention), FileMode.Create))
                    {
                        File.CopyTo(filestream);
                    }
                    ProductVM.product.Img = @"\Images\Products\" + Filename + extention;
                }


                _unitOfWork.Product.Update(ProductVM.product);
                _unitOfWork.Complete();
                TempData["Update"] = "Data Has Updated Successfully";
                return RedirectToAction("Index");
            }
            return View(ProductVM.product);

        }

        /*[HttpGet]
        public IActionResult Delete(int? Id)
        {
            if (Id == null || Id == 0)
            {
                NotFound();
            }
            var Product = _unitOfWork.Product.GetFirstOrDefault(x => x.Id == Id);
            return View(Product);
        }*/
        [HttpDelete]
        public IActionResult Delete(int? Id)
        {
            var Product = _unitOfWork.Product.GetFirstOrDefault(x => x.Id == Id);
            if (Product == null)
            {
                return Json(new {success= false,message="Error While Deleting"});
            }

            _unitOfWork.Product.Remove(Product);
            var oldImg = Path.Combine(_webHostEnvironment.WebRootPath, Product.Img.TrimStart('\\'));
            if (System.IO.File.Exists(oldImg))
            {
                System.IO.File.Delete(oldImg);
            }
            _unitOfWork.Complete();

            /*TempData["Delete"] = "Data Has Deleted Successfully";*/
            return Json(new { success = true, message = "File Has Been Deleted" });
        }

    }
}