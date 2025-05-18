using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;
using Scalar.AspNetCore;
using Seagull.API.Services;
using Seagull.Core.Entities.Identity;
using Seagull.Infrastructure.Data;
using Seagull.Infrastructure.Hooks;
using Seagull.Infrastructure.Services;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

#region CORS

builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionPolicy",
        policy => policy
            .WithOrigins(
                //"https://trusted-web-client.com", Типа хостед веб клиент (админка мб)
                "http://localhost:5173" // Это Vite Dev Server
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

#endregion

#region DATA

builder.Services.AddDbContext<MainContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

#endregion

#region IDENTITY

builder.Services.AddScoped<TokenService>();

builder.Services.AddIdentity<User, Role>(o =>
{
    o.User.RequireUniqueEmail = true;

    o.Password.RequireDigit = false;
    o.Password.RequireLowercase = false;
    o.Password.RequireUppercase = false;
    o.Password.RequireNonAlphanumeric = false;
})
    .AddEntityFrameworkStores<MainContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
    options.DefaultChallengeScheme =
    options.DefaultForbidScheme =
    options.DefaultScheme =
    options.DefaultSignInScheme =
    options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
    };
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.RequireRole("Admin"))
    .AddPolicy("Moderator", policy => policy.RequireRole("Moderator"));

#endregion

#region API

builder.Services.AddControllers();

#endregion

#region MINIO

builder.Services.AddSingleton<IMinioClient>(new MinioClient()
    .WithEndpoint("auth-minio:9000")
    .WithCredentials("minioadmin", "minioadmin")
    .WithSSL(false)
    .Build());

#endregion

#region SWAGGER CONFIG

//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Aether API", Version = "v1" });
//    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
//        Name = "Authorization",
//        In = ParameterLocation.Header,
//        Type = SecuritySchemeType.ApiKey,
//        Scheme = "Bearer"
//    });
//    c.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type=ReferenceType.SecurityScheme,
//                    Id="Bearer"
//                }
//            },
//            Array.Empty<string>()
//        }
//    });

//    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
//    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
//});

#endregion

#region SCALAR CONFIG

builder.Services.AddSwaggerGen(opt =>
{
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    opt.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

#endregion

#region Custom services

builder.Services.AddTransient<S3Service>();
builder.Services.AddTransient<S3Hook>();
builder.Services.AddTransient<InviteGeneratorService>();

#endregion

var app = builder.Build();

#region Migrations

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetService<MainContext>();

    try
    {
        ctx.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database");
    }
}

#endregion

#region SEED

// Добавление ролей если их еще нет
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

    foreach (var role in new[] { "Moderator", "Admin" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new Role(role));
    }
}

// Добавление первого администратора если его нет
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    // Убедимся, что роль Admin существует
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new Role("Admin"));

    // Проверяем, есть ли уже администратор
    var adminUserName = config["RootCredentials:UserName"] ?? "aexra";
    var adminEmail = config["RootCredentials:Email"] ?? "defdXs@yandex.ru";
    var adminPassword = config["RootCredentials:Password"] ?? "bimbimbim";
    var adminTag = config["RootCredentials:Tag"] ?? "aexra";
    var adminDisplayName = config["RootCredentials:DisplayName"] ?? "aexra";

    var adminUser = await userManager.FindByNameAsync(adminEmail);

    if (adminUser == null)
    {
        // Создаем нового администратора
        var newAdmin = new User
        {
            UserName = adminUserName,
            Email = adminEmail,
            EmailConfirmed = true,
            Tag = adminTag,
            DisplayName = adminDisplayName
        };

        var createResult = await userManager.CreateAsync(newAdmin, adminPassword);

        if (createResult.Succeeded)
        {
            // Назначаем роль Admin
            await userManager.AddToRoleAsync(newAdmin, "Admin");
            Console.WriteLine("Администратор успешно создан");
        }
        else
        {
            Console.WriteLine("Ошибка при создании администратора:");
            foreach (var error in createResult.Errors)
            {
                Console.WriteLine(error.Description);
            }
        }
    }
    else
    {
        // Проверяем, есть ли у пользователя роль Admin
        var isAdmin = await userManager.IsInRoleAsync(adminUser, "Admin");

        if (!isAdmin)
        {
            // Если роли нет - добавляем
            await userManager.AddToRoleAsync(adminUser, "Admin");
            Console.WriteLine("Роль Admin добавлена существующему администратору");
        }
    }
}

#endregion

app.UseCors("ProductionPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

#region SWAGGER

//app.UseSwagger();
//app.UseSwaggerUI();

#endregion

#region SCALAR

app.UseSwagger(opt =>
{
    opt.RouteTemplate = "openapi/{documentName}.json";
});
app.MapScalarApiReference(opt =>
{
    opt.Title = "Scalar Example";
    opt.Theme = ScalarTheme.Default;
    opt.DefaultHttpClient = new(ScalarTarget.Http, ScalarClient.Http11);
});

#endregion

app.MapGet("/test", () => "This is base Seagull API!");

app.Run();