using Microsoft.EntityFrameworkCore;
using SmartEdu.Business;
using SmartEdu.Business.Interfaces;
using SmartEdu.DataAccess;
using SmartEdu.RazorWeb.Data;
using SmartEdu.RazorWeb.Hubs;
using SmartEdu.RazorWeb.Services;
using SmartEdu.Shared.Settings;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SmartEdu.RazorWeb
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Cấu hình DbContext  tầng DataAccess
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            // 2. Đăng ký các dịch vụ Business Layer (Nơi chứa ChatService, etc.)
            // Bạn tạo một class "DependencyInjection.cs" trong tầng Business để quản lý


            
            builder.Services.AddBusinessServices();

            // 3. Add services to the container
            builder.Services.AddRazorPages()
                .AddMvcOptions(options => options.Filters.Add<SmartEdu.RazorWeb.Filters.SessionAuthorizeFilter>());
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                });
            builder.Services.AddSignalR();
            builder.Services.AddScoped<
    IChatNotificationService,
    ChatNotificationService>();
            builder.Services.AddScoped<
    IDocumentNotificationService,
    DocumentNotificationService>();
            builder.Services.AddScoped<
    ISubjectNotificationService,
    SubjectNotificationService>();
            // Đăng ký các phần cấu hình
            builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("Gemini"));
            builder.Services.Configure<HuggingFaceSettings>(builder.Configuration.GetSection("HuggingFace"));
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout =
                    TimeSpan.FromHours(8);
            });
            builder.Services.AddHttpClient();

            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                await DataSeeder.SeedAdminAsync(context);
            }
            // 4. Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();
            app.MapRazorPages();
            app.MapHub<SubjectHub>("/subjectHub");
            app.MapHub<ChatHub>("/chatHub");
            app.MapHub<DocumentHub>("/documentHub");
            app.Run();
        }
    }
}