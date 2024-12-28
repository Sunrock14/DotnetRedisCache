using System.Text.Json;
using CacheRedis.Api;
using CacheRedis.Api.CacheService;  
using Microsoft.AspNetCore.Mvc;  

var builder = WebApplication.CreateBuilder(args);  

// Gerekli servis konfigürasyonlarý  
builder.Services.AddOpenApi();  
builder.Services.AddStackExchangeRedisCache(options =>  
{  
    options.Configuration = builder.Configuration.GetConnectionString("RedisConn");  
    options.InstanceName = "myCacheRedisMainKey:";  
});  
builder.Services.AddScoped<ICacheService, CacheService>();  

var app = builder.Build();  
app.MapOpenApi();  
app.UseHttpsRedirection();
app.UseMiddleware<CachePerformanceMiddleware>();


// 1. Temel Veri Getirme ve Caching  
app.MapGet("/products", async (ICacheService cacheService) =>  
{  
    var result = await cacheService.GetOrSetAsync(  
        cacheService.GenerateCacheKey("products", "list"),  
        async () => await GetProductsFromDatabase(),  
        slidingExpiration: TimeSpan.FromMinutes(10),  // Ýstek atýldýkça silinme süresi artacak
        absoluteExpiration: TimeSpan.FromHours(1)  // Kalýcý olarak silinir
    );  

    return Results.Ok(result);  
});  

// 2. Parametreli Veri Getirme  
app.MapGet("/product/{id}", async (  
    ICacheService cacheService,   
    int id) =>  
{  
    var result = await cacheService.GetOrSetAsync(  
        cacheService.GenerateCacheKey("product", id.ToString()),  
        async () => await GetProductByIdFromDatabase(id),  
        slidingExpiration: TimeSpan.FromMinutes(5)  
    );  

    return result != null   
        ? Results.Ok(result)   
        : Results.NotFound();  
});  

// 3. Veri Ekleme ve Cache Güncelleme  
app.MapPost("/product", async (  
    ICacheService cacheService,   
    [FromBody] Product product) =>  
{  
    // Yeni ürünü veritabanýna kaydet  
    var savedProduct = await SaveProductToDatabase(product);  

    // Ürün listesi cache'ini temizle  
    await cacheService.RemoveAsync(  
        cacheService.GenerateCacheKey("products", "list")  
    );  

    // Yeni ürünü cache'e ekle  
    await cacheService.SetAsync(  
        cacheService.GenerateCacheKey("product", savedProduct.Id.ToString()),   
        savedProduct,  
        slidingExpiration: TimeSpan.FromHours(1)  
    );  

    return Results.Created($"/product/{savedProduct.Id}", savedProduct);  
});  

// 4. Kullanýcý Bazlý Dinamik Caching  
app.MapGet("/user/{username}", async (  
    ICacheService cacheService,   
    string username) =>  
{  
    var result = await cacheService.GetOrSetAsync(  
        cacheService.GenerateCacheKey("user", username, "profile"),  
        async () => await GetUserByUsernameFromDatabase(username),  
        slidingExpiration: TimeSpan.FromMinutes(30)  
    );  

    return result != null   
        ? Results.Ok(result)   
        : Results.NotFound();  
});  

// 5. Toplu Cache Silme Senaryosu  
app.MapDelete("/clear-product-cache", async (ICacheService cacheService) =>  
{  
    // Tüm ürün ile ilgili cache anahtarlarýný temizle  
    await cacheService.RemoveAsync(  
        cacheService.GenerateCacheKey("products", "list")  
    );  

    return Results.Ok("Ürün önbelleði temizlendi");  
});  

// 6. Karmaþýk Nesne Caching  
app.MapGet("/complex-data", async (ICacheService cacheService) =>  
{  
    var result = await cacheService.GetOrSetAsync(  
        cacheService.GenerateCacheKey("complex", "data"),  
        async () => await GetComplexDataFromDatabase(),  
        absoluteExpiration: TimeSpan.FromHours(2)  
    );  

    return Results.Ok(result);  
});  


/*
 * 
 * Methodlar Database verisini simüle ederek gerçeðe yakýn sonuçlar elde etmemizi saðlar
 * Eklediðim middleware ile istekler arasý geçen süre kontrol edilebilir.
 * Cache iþleminin ne kadar fark ettiðini görmek adýna her endpointi iki kere çaðýr
 * Ýlk istek Databaseten geldiði için daha uzun sürecektir.
 * Ýkinci istek ise cache'ten gelecektir 
 * 
 * Kendi aldýðým sonuçlar
 * Database - Cache
 * 318ms - 1ms
 * 62ms - 1 ms
 * 2128ms - 1ms
 * 
 * Buradaki Database verisini gecikme eklediðimizi unutmayalým.
 * 
 */



// Simüle Edilmiþ Veritabaný Metodlarý  
async Task<List<Product>> GetProductsFromDatabase()  
{  
    await Task.Delay(100);
    return new List<Product>  
    {  
        new Product(1, "Laptop", 1000),  
        new Product(2, "Smartphone", 500),  
        new Product(3, "Tablet", 300)  
    };  
}  

async Task<Product?> GetProductByIdFromDatabase(int id)  
{  
    await Task.Delay(500); 
    return new List<Product>  
    {  
        new Product(1, "Laptop", 1000),  
        new Product(2, "Smartphone", 500),  
        new Product(3, "Tablet", 300)  
    }.FirstOrDefault(p => p.Id == id);  
}  

async Task<Product> SaveProductToDatabase(Product product)  
{  
    await Task.Delay(1000);  
    return product with { Id = new Random().Next(100, 1000) };  
}  

async Task<User?> GetUserByUsernameFromDatabase(string username)  
{  
    await Task.Delay(500); 
    return new User(1, username, $"{username}@example.com");  
}  

async Task<object> GetComplexDataFromDatabase()  
{  
    await Task.Delay(2000); 
    return new   
    {  
        Products = await GetProductsFromDatabase(),  
        Users = new List<User>   
        {   
            new User(1, "john_doe", "john@example.com"),  
            new User(2, "jane_smith", "jane@example.com")  
        },  
        Timestamp = DateTime.UtcNow  
    };  
}  

app.Run();

// Örnek model sýnýflarý  
public record Product(int Id, string Name, decimal Price);
public record User(int Id, string Username, string Email);