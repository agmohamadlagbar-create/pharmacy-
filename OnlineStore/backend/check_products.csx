using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Infrastructure.Persistence;
using OnlineStore.Domain.Entities;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseSqlServer(config.GetConnectionString("DefaultConnection"));

using var db = new ApplicationDbContext(optionsBuilder.Options);

var products = db.Products.ToList();
Console.WriteLine($"Total products: {products.Count}");
foreach(var p in products)
{
    Console.WriteLine($"ID: {p.Id}, Name: {p.Name}, Status: {p.Status}, Stock: {p.StockQuantity}");
}
