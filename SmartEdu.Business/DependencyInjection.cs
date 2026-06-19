using Microsoft.Extensions.DependencyInjection;
using SmartEdu.Business.Interfaces;
using SmartEdu.Business.Services;
using SmartEdu.DataAccess.Repositories;

namespace SmartEdu.Business
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<ISubjectService, SubjectService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped(
            typeof(IRepository<>),
            typeof(Repository<>));
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }
    }
}
