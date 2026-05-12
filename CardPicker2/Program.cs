using System.Security.Cryptography;

using CardPicker2.Services;

using Serilog;

namespace CardPicker2;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext();
        });

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddHsts(options =>
        {
            options.ExcludedHosts.Clear();
        });
        builder.Services.Configure<CardLibraryOptions>(options =>
        {
            options.LibraryFilePath = Path.Combine(builder.Environment.ContentRootPath, "data", "cards.json");
        });
        builder.Services.AddSingleton<DuplicateCardDetector>();
        builder.Services.AddSingleton<IMealCardRandomizer, MealCardRandomizer>();
        builder.Services.AddScoped<ICardLibraryService, CardLibraryService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
            app.Use(async (context, next) =>
            {
                var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
                context.Items["CspNonce"] = nonce;
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.ContentSecurityPolicy =
                        $"default-src 'self'; script-src 'self' 'nonce-{nonce}'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
                    return Task.CompletedTask;
                });

                await next();
            });
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorPages()
           .WithStaticAssets();

        app.Run();
    }
}