using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Api.Extensions
{
    public static class SwaggerSetup
    {
        public static void AddSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                options.IncludeXmlComments(xmlPath);

                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "OpImage Processing Image API",
                    Description = "Interface de programação de aplicativos (API)" + 
                    "projetada para facilitar a análise de imagens médicas.",                    
                });
            });

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Swagger documentation: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("{applicationUrl}/swagger/index.html\n");
            Console.WriteLine("");
        }
    }
}