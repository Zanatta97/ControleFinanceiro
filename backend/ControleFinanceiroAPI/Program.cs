using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Logging;
using ControleFinanceiroAPI.Middleware;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Repositories;
using ControleFinanceiroAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

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

            builder.Services.AddIdentity<Usuario, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false; //Tira a exigência de pelo menos um dígito
                options.Password.RequireLowercase = false; //Tira a exigência de pelo menos uma letra minúscula
                options.Password.RequireUppercase = false; //Tira a exigência de pelo menos uma letra maiúscula
                options.Password.RequireNonAlphanumeric = false; //Tira a exigência de pelo menos um caractere especial
                options.Password.RequiredLength = 6; //Define o comprimento mínimo da senha para 6 caracteres
            })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.Zero, // Elimina o tempo de tolerância para expiração do token
                        ValidIssuer = builder.Configuration["JWT:ValidIssuer"], //Uma forma de buscar
                        ValidAudience = builder.Configuration.GetSection("Jwt:ValidAudience").Get<string>(), //Outra forma de buscar
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
                    };
                });

            builder.Services.AddScoped<IUnityOfWork, UnityOfWork>(); //Já instancia todos os repositórios dentro do UnityOfWork
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IUsuarioService, UsuarioService>();
            builder.Services.AddScoped<IAmbienteService, AmbienteService>();
            builder.Services.AddScoped<ICategoriaService, CategoriaService>();
            builder.Services.AddScoped<IContaService, ContaService>();
            builder.Services.AddScoped<IOrcamentoService, OrcamentoService>();
            builder.Services.AddScoped<ITransacaoService, TransacaoService>();
            builder.Services.AddScoped<IRelatorioService, RelatorioService>();

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

            app.UseAuthentication();
            app.UseAuthorization();
            

            app.MapControllers();

            app.Run();
        }
    }
}
