
global using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinimalAPI.Data;
using MinimalAPI.Models.Domain;
using MinimalAPI.Models.Dtos;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace MinimalAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //Start Web API Info
            var contactInfo = new OpenApiContact()
            {
                Name = "Nickpsal",
                Email = "nickpsal@gmail.com",
                Url = new Uri("https://www.datatex.gr")
            };

            var license = new OpenApiLicense()
            {
                Name = "Free"
            };

            var info = new OpenApiInfo()
            {
                Version = "V1.0",
                Title = "Minimal Api with Jwt Auth",
                Description = "Minimal Api with Jwt Auth",
                Contact = contactInfo,
                License = license
            };
            //End Web API Info

            //Start Security Settings
            var securityScheme = new OpenApiSecurityScheme()
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authernication for Minimal Api"
            };

            var securityReq = new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type  = ReferenceType.SecurityScheme,
                            Id = "Beaver"
                        }
                    },
                    new string[] { }
                }
            };
            //End Security Settings

            // Define the "Admin" authorization policy
            var adminPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireRole("Admin")
                .Build();

            // Add services to the container.
            builder.Services.AddAuthorization();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateAudience = true,
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
                    ValidateLifetime = false, // in any other app this needs to be true
                    ValidateIssuerSigningKey = true
                };
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", info);
                options.AddSecurityDefinition("Beaver", securityScheme);
                options.AddSecurityRequirement(securityReq);
            });

            //register DBContext
            builder.Services.AddDbContext<DataContext>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            //app.UseAuthentication();
            app.UseAuthorization();

            //api route with out Auth
            app.MapPost("user/register", async (DataContext context, UserRegisterRequestDTO user) =>
            {
                //check if user exists
                if (await context.Users.AnyAsync(u => u.Email == user.Email) || (await context.Users.AnyAsync(u => u.Username == user.Username)))
                {
                    return Results.BadRequest("User Already Exists");
                }
                // Hash the password using bcrypt
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);
                User NewUser = new()
                {
                    Email = user.Email,
                    Username = user.Username,
                    Role = "User",
                    Password = hashedPassword
                };
                context.Users.Add(NewUser);
                await context.SaveChangesAsync();
                return Results.Ok("User registered successfully");
            });

            app.MapPost("/user/login", async (DataContext context, UserLoginRequestDTO user) =>
            {
                var userCredentials = await context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
                if (userCredentials is null)
                {
                    return Results.BadRequest("Username dont exists");
                }else if (!BCrypt.Net.BCrypt.Verify(user.Password, userCredentials.Password))
                {
                    return Results.BadRequest("Wrong Password");
                }else
                {
                    var SecureKey = System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
                    var issuer = builder.Configuration["Jwt:Issuer"];
                    var Audience = builder.Configuration["Jwt:Audience"];
                    var SecurityKey = new SymmetricSecurityKey(SecureKey);
                    var Credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha512Signature);
                    var jwtTokenHandler = new JwtSecurityTokenHandler();
                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new[]
                        {
                            new Claim("Id", userCredentials.Id.ToString()),
                            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                            new Claim(JwtRegisteredClaimNames.Email, user.Email),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim(ClaimTypes.Role, userCredentials.Role)
                        }),
                        Expires = DateTime.Now.AddMinutes(5),
                        Audience = Audience,
                        Issuer = issuer,
                        SigningCredentials = Credentials
                    };
                    var token = jwtTokenHandler.CreateToken(tokenDescriptor);
                    return Results.Ok(jwtTokenHandler.WriteToken(token));
                }
            }).AllowAnonymous();

            //api route with Required Auth 
            app.MapGet("/book", async (DataContext context) =>
                await context.Books.ToListAsync()).RequireAuthorization();

            app.MapGet("/book/{id}", async (DataContext context, int id) =>
                await context.Books.FindAsync(id) is Book book ?
                Results.Ok(book) :
                Results.NotFound("Sorry, Book not found")).RequireAuthorization();

            app.MapPost("/book/add", async (DataContext context, Book newBook) =>
            {
                context.Books.Add(newBook);
                await context.SaveChangesAsync();
                Results.Ok(await context.Books.ToListAsync());
            }).RequireAuthorization(adminPolicy);

            app.MapPut("/book/update/{id}", async (DataContext context, int id, Book updatedBook) =>
            {
                var foundBook = await context.Books.FindAsync(id);
                if (foundBook is null)
                {
                    return Results.NotFound("Error : This book dont exists");
                }
                foundBook.Title = updatedBook.Title;
                foundBook.Author = updatedBook.Author;
                await context.SaveChangesAsync();
                return Results.Ok(await context.Books.ToListAsync());
            }).RequireAuthorization("Admin");

            app.MapDelete("/book/delete/{id}", async (DataContext context, int id) =>
            {
                var foundBook = context.Books.Find(id);
                if (foundBook is null)
                {
                    return Results.NotFound("Error : This book dont exists");
                }
                context.Books.Remove(foundBook);
                await context.SaveChangesAsync();
                return Results.Ok(await context.Books.ToListAsync());
            }).RequireAuthorization("Admin");

            app.Run();
        }
    }
}