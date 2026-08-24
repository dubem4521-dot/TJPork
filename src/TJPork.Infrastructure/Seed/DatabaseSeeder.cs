using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TJPork.Core.Entities;
using TJPork.Core.Enums;
using TJPork.Infrastructure.Data;
using TJPork.Infrastructure.Identity;

namespace TJPork.Infrastructure.Seed
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(TJPorkDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Ensure Database is created
            await db.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            var roles = new[] { "Admin", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Admin Users (Tinashe & Jeffery)
            var tinashe = await userManager.FindByEmailAsync("tinashe@tjfork.com");
            if (tinashe == null)
            {
                tinashe = new ApplicationUser
                {
                    UserName = "tinashe@tjfork.com",
                    Email = "tinashe@tjfork.com",
                    FullName = "Tinashe",
                    EmailConfirmed = true,
                    PhoneNumber = "+27 (0)82 234 5678",
                    DeliveryAddress = "45 Franschhoek Valley Way",
                    City = "Cape Town",
                    PostalCode = "8001",
                    AvatarUrl = "/images/about/tinashe.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-12)
                };
                var result = await userManager.CreateAsync(tinashe, "Admin2026!#Pork");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(tinashe, "Admin");
                }
            }

            var jeffery = await userManager.FindByEmailAsync("jeffery@tjfork.com");
            if (jeffery == null)
            {
                jeffery = new ApplicationUser
                {
                    UserName = "jeffery@tjfork.com",
                    Email = "jeffery@tjfork.com",
                    FullName = "Jeffery",
                    EmailConfirmed = true,
                    PhoneNumber = "+27 (0)83 345 6789",
                    DeliveryAddress = "88 Oaklands Ridge Rd",
                    City = "Johannesburg",
                    PostalCode = "2192",
                    AvatarUrl = "/images/about/jeffery.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-12)
                };
                var result = await userManager.CreateAsync(jeffery, "Admin2026!#Pork");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(jeffery, "Admin");
                }
            }

            // 3. Seed Demo Customer
            var customer = await userManager.FindByEmailAsync("customer@tjpork.com");
            if (customer == null)
            {
                customer = new ApplicationUser
                {
                    UserName = "customer@tjpork.com",
                    Email = "customer@tjpork.com",
                    FullName = "Sipho Ndlovu",
                    EmailConfirmed = true,
                    PhoneNumber = "+27 (0)84 789 0123",
                    DeliveryAddress = "742 Kloof Street, Gardens",
                    City = "Cape Town",
                    PostalCode = "8001",
                    AvatarUrl = "/images/avatars/customer1.jpg",
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                };
                var result = await userManager.CreateAsync(customer, "Customer2026!#Pork");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(customer, "Customer");

                    // Seed saved address
                    db.Addresses.Add(new Address
                    {
                        UserId = customer.Id,
                        Label = "Home",
                        RecipientName = "Sipho Ndlovu",
                        Phone = "+27 (0)84 789 0123",
                        StreetAddress = "742 Kloof Street, Gardens",
                        City = "Cape Town",
                        PostalCode = "8001",
                        IsDefault = true,
                        CreatedAt = DateTime.UtcNow.AddMonths(-6)
                    });
                }
            }

            // 4. Seed Categories
            if (!await db.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Artisanal Bacon", Slug = "artisanal-bacon", Description = "Slow-cured with hardwood smoke, pure sea salt, and organic aromatics.", IconClass = "fa-solid fa-bacon", DisplayOrder = 1 },
                    new Category { Name = "Gourmet Sausages & Boerewors", Slug = "gourmet-sausages", Description = "Hand-stuffed links and artisanal boerewors prepared with fresh coriander, herbs, and traditional spices.", IconClass = "fa-solid fa-hotdog", DisplayOrder = 2 },
                    new Category { Name = "Heritage Pork Chops", Slug = "heritage-pork-chops", Description = "Prime thick-cut chops with succulent marbling from heritage pasture breeds.", IconClass = "fa-solid fa-drumstick-bite", DisplayOrder = 3 },
                    new Category { Name = "Premium Roasts & Ribs", Slug = "premium-roasts-ribs", Description = "Braai-ready pork ribs, bone-in shoulder roasts, and crown racks for celebrations.", IconClass = "fa-solid fa-fire-burner", DisplayOrder = 4 },
                    new Category { Name = "Pork Belly & Cuts", Slug = "pork-belly-cuts", Description = "Rich, tender pork belly slabs ready for ultra-crispy crackling or braising.", IconClass = "fa-solid fa-utensils", DisplayOrder = 5 },
                    new Category { Name = "Smoked Specialties & Ham", Slug = "smoked-specialties-ham", Description = "Aged prosciutto, honey-glazed tenderloins, and artisanal cured hams.", IconClass = "fa-solid fa-award", DisplayOrder = 6 }
                };

                await db.Categories.AddRangeAsync(categories);
                await db.SaveChangesAsync();
            }

            // 5. Seed Products with South African Rand (ZAR) Pricing
            if (!await db.Products.AnyAsync())
            {
                var baconCat = await db.Categories.FirstAsync(c => c.Slug == "artisanal-bacon");
                var sausageCat = await db.Categories.FirstAsync(c => c.Slug == "gourmet-sausages");
                var chopCat = await db.Categories.FirstAsync(c => c.Slug == "heritage-pork-chops");
                var roastCat = await db.Categories.FirstAsync(c => c.Slug == "premium-roasts-ribs");
                var bellyCat = await db.Categories.FirstAsync(c => c.Slug == "pork-belly-cuts");
                var specialtyCat = await db.Categories.FirstAsync(c => c.Slug == "smoked-specialties-ham");

                var products = new List<Product>
                {
                    new Product
                    {
                        Name = "Applewood Smoked Heritage Bacon",
                        Slug = "applewood-smoked-heritage-bacon",
                        CategoryId = baconCat.Id,
                        ShortDescription = "Cured for 7 days with dark brown molasses and slowly cold-smoked over fragrant applewood.",
                        LongDescription = "Our flagship bacon is crafted from 100% pasture-raised heritage hogs from the Western Cape. Naturally dry-cured with sea salt and black peppercorns before a 14-hour cool smoke over natural applewood chips. Crisps up evenly with unmatched savory-sweet depth.",
                        Price = 115.00m,
                        DiscountPercentage = 10,
                        StockQuantity = 45,
                        ImageUrl = "/images/products/bacon-applewood.jpg",
                        IsFeatured = true,
                        IsActive = true,
                        Rating = 4.9,
                        ReviewCount = 28,
                        WeightDescription = "400g pack (Thick Cut)",
                        CuringMethod = "7-Day Dry Cure & Applewood Cold Smoke",
                        OriginFarm = "Stellenbosch Heritage Pastures"
                    },
                    new Product
                    {
                        Name = "Maple Bourbon Thick-Cut Bacon",
                        Slug = "maple-bourbon-thick-cut-bacon",
                        CategoryId = baconCat.Id,
                        ShortDescription = "Infused with oak-aged spirits and pure raw honey for a caramelized braai finish.",
                        LongDescription = "Hand-rubbed with small-batch glaze, cracked tellicherry pepper, and organic wildflower honey. Sliced extra-thick for the ultimate breakfast feast or gourmet burger topping.",
                        Price = 125.00m,
                        DiscountPercentage = 0,
                        StockQuantity = 32,
                        ImageUrl = "/images/products/bacon-maple.jpg",
                        IsFeatured = false,
                        IsActive = true,
                        Rating = 4.8,
                        ReviewCount = 19,
                        WeightDescription = "450g pack",
                        CuringMethod = "Honey Infused Hardwood Smoke",
                        OriginFarm = "Midlands Family Farm"
                    },
                    new Product
                    {
                        Name = "Artisanal Bratwurst with Roasted Garlic",
                        Slug = "artisanal-bratwurst-roasted-garlic",
                        CategoryId = sausageCat.Id,
                        ShortDescription = "Coarse-ground heritage pork with roasted garlic, marjoram, and a touch of nutmeg.",
                        LongDescription = "Crafted according to traditional butcher methods. Natural hog casings filled with juicy, coarsely ground pork shoulder, slow-roasted caramelized garlic, fresh herbs, and delicate spices. Perfect for grilling on the braai or pan-searing.",
                        Price = 95.00m,
                        DiscountPercentage = 0,
                        StockQuantity = 50,
                        ImageUrl = "/images/products/sausage-bratwurst.jpg",
                        IsFeatured = true,
                        IsActive = true,
                        Rating = 5.0,
                        ReviewCount = 34,
                        WeightDescription = "500g (4 large links)",
                        CuringMethod = "Fresh Hand-Linked Natural Casing",
                        OriginFarm = "Cedar Creek Heritage Farm"
                    },
                    new Product
                    {
                        Name = "Spicy Italian Fennel Sausage",
                        Slug = "spicy-italian-fennel-sausage",
                        CategoryId = sausageCat.Id,
                        ShortDescription = "Bursting with toasted fennel seeds, crushed peri-peri chili, and red wine.",
                        LongDescription = "A bold, savory sausage crafted with whole toasted fennel seed, cracked chili flakes, garlic cloves, and a splash of dry red wine. Incredible on the braai, pizza, in pasta ragù, or roasted with sweet bell peppers.",
                        Price = 89.99m,
                        DiscountPercentage = 0,
                        StockQuantity = 38,
                        ImageUrl = "/images/products/sausage-italian.jpg",
                        IsFeatured = false,
                        IsActive = true,
                        Rating = 4.7,
                        ReviewCount = 15,
                        WeightDescription = "500g (4 large links)",
                        CuringMethod = "Fresh Hand-Linked Natural Casing",
                        OriginFarm = "Cedar Creek Heritage Farm"
                    },
                    new Product
                    {
                        Name = "Center-Cut Bone-In Heritage Chops",
                        Slug = "center-cut-bone-in-heritage-chops",
                        CategoryId = chopCat.Id,
                        ShortDescription = "Extra-thick 1.5-inch cut chops with rich intra-muscular marbling.",
                        LongDescription = "Cut from the prime center loin of our pasture-raised Berkshire hogs. The bone-in presentation locks in natural juices during braaiing, resulting in a buttery, melt-in-your-mouth tenderness you won't find in supermarket pork.",
                        Price = 185.00m,
                        DiscountPercentage = 0,
                        StockQuantity = 22,
                        ImageUrl = "/images/products/chops-bonein.jpg",
                        IsFeatured = true,
                        IsActive = true,
                        Rating = 5.0,
                        ReviewCount = 42,
                        WeightDescription = "650g (2 thick chops)",
                        CuringMethod = "Fresh Hand-Cut Butcher Selection",
                        OriginFarm = "Midlands Heritage Estate"
                    },
                    new Product
                    {
                        Name = "Tomahawk Heritage Pork Chop",
                        Slug = "tomahawk-heritage-pork-chop",
                        CategoryId = chopCat.Id,
                        ShortDescription = "Show-stopping long-bone rib chop with magnificent marbling and fat cap.",
                        LongDescription = "The ultimate steakhouse experience at home. French-trimmed long rib bone attached to a generous prime eye of meat. Sourced exclusively from heritage breeds known for dark red flesh and nutty fat flavor.",
                        Price = 245.00m,
                        DiscountPercentage = 15,
                        StockQuantity = 14,
                        ImageUrl = "/images/products/chops-tomahawk.jpg",
                        IsFeatured = true,
                        IsActive = true,
                        Rating = 4.9,
                        ReviewCount = 21,
                        WeightDescription = "750g single Tomahawk cut",
                        CuringMethod = "Dry-Aged 14 Days Butcher Trimmed",
                        OriginFarm = "Midlands Heritage Estate"
                    },
                    new Product
                    {
                        Name = "Slow-Roast St. Louis Ribs",
                        Slug = "slow-roast-st-louis-ribs",
                        CategoryId = roastCat.Id,
                        ShortDescription = "Meaty, trimmed spare ribs with tender texture and uniform thickness for the braai.",
                        LongDescription = "Expertly squared and trimmed St. Louis cut pork ribs. Plump, juicy, and prepared for your favorite dry rub or barbecue glaze. Yields competition-quality fall-off-the-bone tenderness when smoked low and slow.",
                        Price = 210.00m,
                        DiscountPercentage = 0,
                        StockQuantity = 18,
                        ImageUrl = "/images/products/roast-ribs.jpg",
                        IsFeatured = false,
                        IsActive = true,
                        Rating = 4.8,
                        ReviewCount = 16,
                        WeightDescription = "1.2kg whole rack",
                        CuringMethod = "Butcher Hand-Trimmed Fresh Cut",
                        OriginFarm = "Stellenbosch Heritage Pastures"
                    },
                    new Product
                    {
                        Name = "Crispy Skin Heritage Pork Belly Slab",
                        Slug = "crispy-skin-heritage-pork-belly-slab",
                        CategoryId = bellyCat.Id,
                        ShortDescription = "Prime layered pork belly scored and primed for ultra-crisp crackling.",
                        LongDescription = "A master butcher favorite. Perfectly balanced alternating layers of sweet meat and silky fat, capped with pristine skin ready for high-heat blistered crackling or slow-simmered braising.",
                        Price = 175.00m,
                        DiscountPercentage = 0,
                        StockQuantity = 26,
                        ImageUrl = "/images/products/belly-slab.jpg",
                        IsFeatured = true,
                        IsActive = true,
                        Rating = 5.0,
                        ReviewCount = 37,
                        WeightDescription = "1.0kg solid slab",
                        CuringMethod = "Skin-Scored & Sea Salt Prepped",
                        OriginFarm = "Oak Valley Family Farm"
                    },
                    new Product
                    {
                        Name = "Hickory Smoked Pulled Pork Shoulder",
                        Slug = "hickory-smoked-pulled-pork-shoulder",
                        CategoryId = roastCat.Id,
                        ShortDescription = "Slow pit-smoked for 16 hours over aged hardwood. Heat and serve.",
                        LongDescription = "We do the 16-hour hardwood smoking so you can enjoy world-class artisanal barbecue in 15 minutes. Rich bark, deep smoke ring, and succulent shredded shoulder meat vacuum-sealed in its own natural juices.",
                        Price = 145.00m,
                        DiscountPercentage = 0,
                        StockQuantity = 30,
                        ImageUrl = "/images/products/roast-pulledpork.jpg",
                        IsFeatured = false,
                        IsActive = true,
                        Rating = 4.9,
                        ReviewCount = 25,
                        WeightDescription = "800g heat & serve pack",
                        CuringMethod = "16-Hour Hickory Pit Smoke",
                        OriginFarm = "Stellenbosch Heritage Pastures"
                    },
                    new Product
                    {
                        Name = "Artisanal Cured Smoked Ham",
                        Slug = "artisanal-cured-smoked-ham",
                        CategoryId = specialtyCat.Id,
                        ShortDescription = "Naturally aged with rosemary and Cape honey, cold-smoked to perfection.",
                        LongDescription = "Cured in small batches over 30 days using ancient methods. Sweetened lightly with local wildflower honey and infused with wild rosemary before gentle beechwood smoking. Incredible sliced wafer-thin for charcuterie boards.",
                        Price = 230.00m,
                        DiscountPercentage = 10,
                        StockQuantity = 15,
                        ImageUrl = "/images/products/specialty-ham.jpg",
                        IsFeatured = true,
                        IsActive = true,
                        Rating = 5.0,
                        ReviewCount = 18,
                        WeightDescription = "900g boneless half-ham",
                        CuringMethod = "30-Day Honey & Rosemary Aged Cure",
                        OriginFarm = "Highland Heritage Farm"
                    },
                    new Product
                    {
                        Name = "Smoked Honey Glazed Pork Tenderloin",
                        Slug = "smoked-honey-glazed-pork-tenderloin",
                        CategoryId = specialtyCat.Id,
                        ShortDescription = "Ultra-lean, delicate whole tenderloin basted with organic fynbos honey glaze.",
                        LongDescription = "The leanest and most delicate cut of the hog, lightly smoked and coated with a spiced honey glaze. Cooks in just 20 minutes for an elegant gourmet dinner with zero hassle.",
                        Price = 160.00m,
                        DiscountPercentage = 0,
                        StockQuantity = 20,
                        ImageUrl = "/images/products/specialty-tenderloin.jpg",
                        IsFeatured = false,
                        IsActive = true,
                        Rating = 4.8,
                        ReviewCount = 12,
                        WeightDescription = "600g whole tenderloin",
                        CuringMethod = "Honey Glaze & Mild Pecan Smoke",
                        OriginFarm = "Oak Valley Family Farm"
                    },
                    new Product
                    {
                        Name = "Farmhouse Breakfast Sage Links",
                        Slug = "farmhouse-breakfast-sage-links",
                        CategoryId = sausageCat.Id,
                        ShortDescription = "Traditional breakfast sausage seasoning with rubbed sage and spices.",
                        LongDescription = "The quintessential morning classic. Coarse heritage pork seasoned with aromatic sage, sweet marjoram, cracked pepper, and brown sugar. Perfect alongside eggs and farm roosterkoek.",
                        Price = 75.00m,
                        DiscountPercentage = 0,
                        StockQuantity = 40,
                        ImageUrl = "/images/products/sausage-sage.jpg",
                        IsFeatured = false,
                        IsActive = true,
                        Rating = 4.7,
                        ReviewCount = 14,
                        WeightDescription = "450g (8 breakfast links)",
                        CuringMethod = "Fresh Hand-Linked Natural Casing",
                        OriginFarm = "Cedar Creek Heritage Farm"
                    }
                };

                await db.Products.AddRangeAsync(products);
                await db.SaveChangesAsync();
            }

            // 6. Seed Reviews
            if (!await db.Reviews.AnyAsync())
            {
                var bacon = await db.Products.FirstAsync(p => p.Slug.Contains("applewood"));
                var chops = await db.Products.FirstAsync(p => p.Slug.Contains("bone-in"));
                var sausage = await db.Products.FirstAsync(p => p.Slug.Contains("bratwurst"));
                var belly = await db.Products.FirstAsync(p => p.Slug.Contains("belly"));

                var reviews = new List<Review>
                {
                    new Review
                    {
                        ProductId = bacon.Id,
                        CustomerName = "David van der Merwe",
                        CustomerEmail = "david.vdm@example.co.za",
                        Rating = 5,
                        Title = "The best bacon in South Africa!",
                        Comment = "Tinashe and Jeffery have completely spoiled other bacon for me. The smoky aroma when it hits the skillet is unbelievable, and it doesn't shrink into water like grocery store brands. 10/10!",
                        IsVerifiedBuyer = true,
                        IsApproved = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-14)
                    },
                    new Review
                    {
                        ProductId = chops.Id,
                        CustomerName = "Chef Anika Botha",
                        CustomerEmail = "anika.botha@bistrocapetown.co.za",
                        Rating = 5,
                        Title = "Sensational on the braai",
                        Comment = "These center-cut chops are sensational. The fat cap crisps up deliciously over the coals and the meat remains tender and juicy without brining.",
                        IsVerifiedBuyer = true,
                        IsApproved = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-10)
                    },
                    new Review
                    {
                        ProductId = sausage.Id,
                        CustomerName = "Sipho Ndlovu",
                        CustomerEmail = "customer@tjpork.com",
                        Rating = 5,
                        Title = "Authentic artisanal sausages",
                        Comment = "Ordered for our weekend family braai in Camps Bay. Everyone asked where I bought these sausages. Seasoning is spot on and the casing had that perfect snap.",
                        IsVerifiedBuyer = true,
                        IsApproved = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new Review
                    {
                        ProductId = belly.Id,
                        CustomerName = "Leanne Joubert",
                        CustomerEmail = "leanne.j@example.co.za",
                        Rating = 5,
                        Title = "Crispiest crackling ever",
                        Comment = "Followed Jeffery's roasting instructions and got glass-like crackling with melt-in-the-mouth pork belly underneath. Will definitely re-order regularly!",
                        IsVerifiedBuyer = true,
                        IsApproved = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    }
                };

                await db.Reviews.AddRangeAsync(reviews);
                await db.SaveChangesAsync();
            }

            // 7. Seed Initial Sample Orders in ZAR
            if (!await db.Orders.AnyAsync())
            {
                var prod1 = await db.Products.FirstAsync(p => p.Slug.Contains("applewood"));
                var prod2 = await db.Products.FirstAsync(p => p.Slug.Contains("bone-in"));
                var prod3 = await db.Products.FirstAsync(p => p.Slug.Contains("bratwurst"));
                var prod4 = await db.Products.FirstAsync(p => p.Slug.Contains("belly"));

                var custUser = await userManager.FindByEmailAsync("customer@tjpork.com");

                var order1 = new Order
                {
                    OrderNumber = "TJP-2026-104921",
                    UserId = custUser?.Id,
                    CustomerName = "Sipho Ndlovu",
                    CustomerEmail = "customer@tjpork.com",
                    CustomerPhone = "+27 (0)84 789 0123",
                    ShippingAddress = "742 Kloof Street, Gardens",
                    City = "Cape Town",
                    PostalCode = "8001",
                    DeliveryDate = DateTime.UtcNow.AddDays(-3),
                    DeliverySlot = DeliverySlot.Morning,
                    Subtotal = 301.98m,
                    DeliveryFee = 65.00m,
                    TaxAmount = 45.30m,
                    DiscountAmount = 0m,
                    TotalAmount = 412.28m,
                    PaymentMethod = PaymentMethod.CreditCard,
                    PaymentStatus = PaymentStatus.Paid,
                    DeliveryStatus = OrderStatus.Delivered,
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                    DeliveredAt = DateTime.UtcNow.AddDays(-3),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = prod1.Id, ProductName = prod1.Name, UnitPrice = prod1.FinalPrice, Quantity = 2, TotalPrice = prod1.FinalPrice * 2, ProductImageUrl = prod1.ImageUrl },
                        new OrderItem { ProductId = prod3.Id, ProductName = prod3.Name, UnitPrice = prod3.FinalPrice, Quantity = 1, TotalPrice = prod3.FinalPrice, ProductImageUrl = prod3.ImageUrl }
                    }
                };

                var order2 = new Order
                {
                    OrderNumber = "TJP-2026-108842",
                    UserId = custUser?.Id,
                    CustomerName = "Sipho Ndlovu",
                    CustomerEmail = "customer@tjpork.com",
                    CustomerPhone = "+27 (0)84 789 0123",
                    ShippingAddress = "742 Kloof Street, Gardens",
                    City = "Cape Town",
                    PostalCode = "8001",
                    DeliveryDate = DateTime.UtcNow.AddDays(1),
                    DeliverySlot = DeliverySlot.Afternoon,
                    Subtotal = 545.00m,
                    DeliveryFee = 0m, // Free delivery over R500
                    TaxAmount = 81.75m,
                    DiscountAmount = 50.00m,
                    TotalAmount = 576.75m,
                    PaymentMethod = PaymentMethod.CreditCard,
                    PaymentStatus = PaymentStatus.Paid,
                    DeliveryStatus = OrderStatus.Processing,
                    CreatedAt = DateTime.UtcNow.AddHours(-3),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = prod2.Id, ProductName = prod2.Name, UnitPrice = prod2.FinalPrice, Quantity = 2, TotalPrice = prod2.FinalPrice * 2, ProductImageUrl = prod2.ImageUrl },
                        new OrderItem { ProductId = prod4.Id, ProductName = prod4.Name, UnitPrice = prod4.FinalPrice, Quantity = 1, TotalPrice = prod4.FinalPrice, ProductImageUrl = prod4.ImageUrl }
                    }
                };

                var order3 = new Order
                {
                    OrderNumber = "TJP-2026-109934",
                    UserId = null,
                    CustomerName = "Claire Montgomery",
                    CustomerEmail = "claire.m@example.co.za",
                    CustomerPhone = "+27 (0)72 456 1122",
                    ShippingAddress = "1200 Florida Rd, Morningside",
                    City = "Durban",
                    PostalCode = "4001",
                    DeliveryDate = DateTime.UtcNow.AddDays(2),
                    DeliverySlot = DeliverySlot.Evening,
                    Subtotal = 525.00m,
                    DeliveryFee = 0m,
                    TaxAmount = 78.75m,
                    DiscountAmount = 0m,
                    TotalAmount = 603.75m,
                    PaymentMethod = PaymentMethod.CashOnDelivery,
                    PaymentStatus = PaymentStatus.Pending,
                    DeliveryStatus = OrderStatus.Processing,
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = prod4.Id, ProductName = prod4.Name, UnitPrice = prod4.FinalPrice, Quantity = 2, TotalPrice = prod4.FinalPrice * 2, ProductImageUrl = prod4.ImageUrl },
                        new OrderItem { ProductId = prod1.Id, ProductName = prod1.Name, UnitPrice = prod1.FinalPrice, Quantity = 1, TotalPrice = prod1.FinalPrice, ProductImageUrl = prod1.ImageUrl }
                    }
                };

                db.Orders.AddRange(order1, order2, order3);
                await db.SaveChangesAsync();
            }

            // 8. Seed Store Settings (South African Rands)
            if (!await db.StoreSettings.AnyAsync())
            {
                var settings = new List<StoreSetting>
                {
                    new StoreSetting { Key = "StoreName", Value = "T&JPork Artisanal Meats", Description = "Public store name", Group = "General" },
                    new StoreSetting { Key = "ContactEmail", Value = "orders@tjpork.com", Description = "Customer support email", Group = "General" },
                    new StoreSetting { Key = "ContactPhone", Value = "+27 (0)21 835 7675", Description = "Customer support phone", Group = "General" },
                    new StoreSetting { Key = "StandardDeliveryFee", Value = "65.00", Description = "Standard flat delivery fee in ZAR", Group = "Delivery" },
                    new StoreSetting { Key = "FreeDeliveryThreshold", Value = "500.00", Description = "Cart value for free delivery in ZAR", Group = "Delivery" },
                    new StoreSetting { Key = "TaxRatePercentage", Value = "15.0", Description = "South African VAT rate percentage", Group = "Payment" },
                    new StoreSetting { Key = "AdminNotifyEmails", Value = "tinashe@tjfork.com,jeffery@tjfork.com", Description = "Owner notification recipients", Group = "Notifications" }
                };

                await db.StoreSettings.AddRangeAsync(settings);
                await db.SaveChangesAsync();
            }
        }
    }
}
