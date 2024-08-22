using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyShop.DataAccess;
using MyShop.DataAccess.Implementation;
using MyShop.Entities.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using MyShop.Utilities;
using Stripe;
using MyShop.Entities.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("MyShop.Web")));

builder.Services.Configure<StripeData>(builder.Configuration.GetSection("Stripe"));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(
     option => option.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(4)
     ).AddDefaultTokenProviders().AddDefaultUI()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();

// Set your Stripe API key here
StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<string>();
StripeConfiguration.ApiKey = "sk_test_51PptSmP1h3KSXZNdgPHNeWi1zDAIxaYjtl3wtXCXM3nwV6beI2ZBHxjQdoHHCxrnr4P5RcBl5BmwuYUowMN8dSnV00DoWDTs1X"; // Replace with your actual Secret Key

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseSession();
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Admin}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "CustomerLayout",
    pattern: "{area=CustomerLayout}/{controller=Home}/{action=Index}/{id?}");

app.Run();
