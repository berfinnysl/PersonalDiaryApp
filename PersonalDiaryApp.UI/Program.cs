using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1) MVC servisini kaydet (Controller + Views)
builder.Services.AddControllersWithViews();

// 2) API çağrıları için HttpClient
builder.Services.AddHttpClient("DiaryApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:44353/");
});

// 3) Session servisi (MVC içinde oturum bilgisi tutmak için)
//    Cookie ayarlarını isteğe göre değiştirebilirsin
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 4) Razor view'larda HttpContext erişimi için
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Hata sayfası & HSTS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// HTTPS, statik dosyalar, routing
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 5) Session middleware'ı mutlaka UseRouting’den sonra, UseAuthorization’dan önce
app.UseSession();

// 6) Eğer MVC içinde [Authorize] kullanıyorsan:
//app.UseAuthentication();  

app.UseAuthorization();

// 7) Varsayılan rota
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
