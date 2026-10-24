using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services ----------
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Defaut")));

var serviceJeton = new ServiceJeton(builder.Configuration);
builder.Services.AddSingleton(serviceJeton);
builder.Services.AddSingleton<IServiceEmail, ServiceEmailConsole>();

builder.Services.AddScoped<ServiceNotifications>();
builder.Services.AddScoped<ServiceConversations>();
builder.Services.AddScoped<ServiceProfil>();
builder.Services.AddScoped<ServiceFichiers>();

// ---------- Authentification par jeton JWT ----------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // on garde les noms de claims tels quels ("uid")
        options.TokenValidationParameters = serviceJeton.ParametresValidation();
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async contexte =>
            {
                var db = contexte.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                string? uid = contexte.Principal?.FindFirst("uid")?.Value;

                if (!long.TryParse(uid, out long id))
                {
                    contexte.Fail("Jeton invalide.");
                    return;
                }

                var etat = await db.Utilisateurs
                    .Where(u => u.Id == id)
                    .Select(u => new { u.Actif, u.DerniereActiviteLe })
                    .FirstOrDefaultAsync();

                if (etat is null || !etat.Actif)
                {
                    contexte.Fail("Compte introuvable ou désactivé.");
                    return;
                }

                // Met à jour la dernière activité (au plus une fois par minute)
                DateTime limite = DateTime.UtcNow.AddMinutes(-1);
                if (etat.DerniereActiviteLe is null || etat.DerniereActiviteLe < limite)
                {
                    await db.Utilisateurs
                        .Where(u => u.Id == id)
                        .ExecuteUpdateAsync(s => s.SetProperty(u => u.DerniereActiviteLe, DateTime.UtcNow));
                }
            }
        };
    });

builder.Services.AddAuthorization();

// ---------- Limitation des tentatives (connexion, inscription...) ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", contexte => RateLimitPartition.GetFixedWindowLimiter(
        contexte.Connection.RemoteIpAddress?.ToString() ?? "inconnu",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

var app = builder.Build();

// ---------- Création de la base SQLite au premier lancement ----------
using (var portee = app.Services.CreateScope())
{
    var db = portee.ServiceProvider.GetRequiredService<AppDbContext>();
    DbInitialiseur.Initialiser(db);
}

// ---------- Pipeline ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(erreur => erreur.Run(async contexte =>
    {
        contexte.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await contexte.Response.WriteAsJsonAsync(new ErreurDto("Une erreur est survenue. Réessayez plus tard."));
    }));
}

app.UseStaticFiles();   // sert les images de wwwroot/uploads
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "VoisinApp API en ligne").AllowAnonymous();
app.MapControllers();

app.Run();
