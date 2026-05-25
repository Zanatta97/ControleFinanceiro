using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Interfaces;
using ControleFinanceiroAPI.Logging;
using ControleFinanceiroAPI.Middleware;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace ControleFinanceiroAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddIdentity<Usuario, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.AddScoped<IUnityOfWork, UnityOfWork>();

            // Registra o LogSettingsManager como Singleton: uma única instância compartilhada
            // entre o RequestLoggingMiddleware (que lê o estado) e o LogsController
            // (que altera o estado via endpoints). É essa instância única que faz
            // o toggle funcionar em tempo real sem reiniciar a API.
            builder.Services.AddSingleton<LogSettingsManager>();

            // Registra o ApiLogService como Singleton: uma única instância é criada e
            // reutilizada durante toda a vida da aplicação.
            // Singleton é a escolha correta aqui porque o serviço gerencia seu próprio
            // DbContext via IServiceScopeFactory (não depende de nenhum serviço Scoped diretamente).
            builder.Services.AddSingleton<IApiLogService, ApiLogService>();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();

            // A ordem dos middlewares importa muito: o primeiro registrado é o mais externo,
            // ou seja, ele envolve todos os outros — é o primeiro a receber o request
            // e o último a processar a resposta.
            //
            // Pipeline resultante:
            //   Request → [RequestLoggingMiddleware] → [ErrorHandlingMiddleware] → Controllers → Response
            //
            // RequestLoggingMiddleware fica por fora para capturar o request/response completo,
            // incluindo a resposta de erro formatada pelo ErrorHandlingMiddleware.
            //
            // ErrorHandlingMiddleware fica por dentro para interceptar exceções dos controllers
            // e devolver um JSON padronizado (ApiResponseDTO) antes de subir para o middleware de log.
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseMiddleware<ErrorHandlingMiddleware>();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
