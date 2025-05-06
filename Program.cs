    using MedicalAPI_1.Repositories;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.OpenApi.Models;
    using System.Text;
    using UserAccountAPI.Common;
    using UserAccountAPI.Data;
    using UserAccountAPI.DTOs;
    using UserAccountAPI.Mappings;
    using UserAccountAPI.Models;
    using UserAccountAPI.Repositories;
    using UserAccountAPI.Repositories.Interfaces;
    using UserAccountAPI.Services;
    using UserAccountAPI.Services.Interfaces;

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddCors(options =>
    {
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        // ????? ?????? ???? ???? ?????? ?? ???????
        policy.WithOrigins("http://localhost:7248") // ?? ?????? ??? URL ??? ??????
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    });


    builder.Services.AddControllers();

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"])),
            ClockSkew = TimeSpan.Zero
        };
    });


    // Configure Authorization Policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy => policy.RequireRole(UserRoles.Admin));
        options.AddPolicy("RequireDoctorRole", policy => policy.RequireRole(UserRoles.Doctor));
        options.AddPolicy("RequirePatientRole", policy => policy.RequireRole(UserRoles.Patient));
        options.AddPolicy("RequireAdminOrDoctorRole", policy =>
            policy.RequireRole(UserRoles.Admin, UserRoles.Doctor));
    });

    // Configure AutoMapper
    builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAppointmentService, AppointmentService>();


    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IPatientRepository, PatientRepository>();
    builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
    builder.Services.AddScoped<IDeptRepository, DeptRepository>();
    builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();


    builder.Services.AddAutoMapper(config =>
    {
        config.CreateMap<ApplicationUser, UserDTO>();
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "User Account API", Version = "v1" });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
             BearerFormat = "JWT",

        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });


    });



    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowSpecificOrigin");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    async Task SeedFirstAdmin(IServiceProvider serviceProvider)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // Create Admin role if it doesn't exist
            if (!await roleManager.RoleExistsAsync(UserRoles.Admin))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = UserRoles.Admin });
                Console.WriteLine("Admin role created.");
            }
            else
            {
                Console.WriteLine("Admin role already exists.");
            }

            string adminEmail = configuration["AdminUser:Email"];
            string adminPassword = configuration["AdminUser:Password"];

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                Console.WriteLine("Admin credentials missing in configuration. Using defaults.");
                adminEmail = "admin@example.com";
                adminPassword = "Admin123!@#";
            }

            Console.WriteLine($"Admin Email: {adminEmail}");
            Console.WriteLine($"Admin Password is set: {!string.IsNullOrEmpty(adminPassword)}");

            // Check if admin exists
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true // First admin has confirmed email for immediate access
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
                    Console.WriteLine($"Admin user '{adminEmail}' created successfully.");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Failed to create admin user: {errors}");
                }
            }
            else
            {
                Console.WriteLine($"Admin user '{adminEmail}' already exists.");

                // Ensure the existing user has admin role
                if (!await userManager.IsInRoleAsync(adminUser, UserRoles.Admin))
                {
                    await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
                    Console.WriteLine("Added Admin role to existing user.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SeedFirstAdmin: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<ApplicationDbContext>();
            var config = services.GetRequiredService<IConfiguration>();

            // Check if admin credentials exist in config
            var adminEmail = config["AdminUser:Email"];
            var adminPassword = config["AdminUser:Password"];

            Console.WriteLine($"Admin Email from config: {adminEmail ?? "[null]"}");
            Console.WriteLine($"Admin Password exists: {!string.IsNullOrEmpty(adminPassword)}");

            // Apply pending migrations
            if (context.Database.GetPendingMigrations().Any())
            {
                Console.WriteLine("Applying pending migrations...");
                context.Database.Migrate();
            }
            else
            {
                Console.WriteLine("Database is up to date.");
            }

            // Seed first admin
            await SeedFirstAdmin(services);
            Console.WriteLine("Admin seeding process completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during startup: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    app.Run();