using System.Text;
using System.Threading.RateLimiting;

using BE_ECOMMERCE.Constants;
using BE_ECOMMERCE.Data;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


string? jwtKey = builder.Configuration["Jwt:Key"];
string? jwtIssuer = builder.Configuration["Jwt:Issuer"];
string? jwtAudience = builder.Configuration["Jwt:Audience"];

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Đăng ký ApplicationDbContext sử dụng SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();


// Nạp cấu hình Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = jwtIssuer,
    ValidAudience = jwtAudience,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey ?? throw new InvalidOperationException("Jwt:Key is missing!")))
});

builder.Services.AddAuthorization(options =>
{
    // Duyệt trực tiếp qua mảng (Không dùng Reflection)
    foreach (var permission in AppPermissions.All)
    {
        options.AddPolicy(permission, policy =>
            policy.RequireClaim("Permission", permission));
    }
});


builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Trello API", Version = "v1" });

    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Vui lòng dán Token vào đây",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});


// 1. Thêm dịch vụ Rate Limiter
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("Giới_Hạn_Spam_Click", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(10); // Khung thời gian 10 giây
        opt.PermitLimit = 3; // Chỉ cho phép tối đa 3 request
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0; // Vượt quá là chặn luôn, không xếp hàng
    });

    // Đổi status code trả về thành 429 (Too Many Requests) thay vì 503
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});



builder.Services.AddCors(options => options.AddPolicy("AllowReactApp", policy =>
        // SOI KỸ CHỖ NÀY:
        _ = policy.WithOrigins("http://localhost:" + builder.Configuration["PORT:FE"]) // <--- CHUẨN
                                                                                       // policy.WithOrigins("http://localhost:5173/") <--- SAI (Có dấu gạch chéo ở cuối là vứt đi ngay)
              .AllowAnyHeader()
              .AllowAnyMethod()));


WebApplication app = builder.Build();

// [THÊM ĐOẠN NÀY] - Khởi chạy cỗ máy đồng bộ tự động
await DbSeeder.AutoSyncPermissions(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI(); // Bật giao diện web của Swagger
    _ = app.UseRateLimiter();
}

// app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Ép app chạy ở cổng 5000
app.Urls.Add("http://localhost:" + builder.Configuration["PORT:BE"]);
app.Run();