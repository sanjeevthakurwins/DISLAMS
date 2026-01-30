using Microsoft.EntityFrameworkCore;
using MediatR;
using AutoMapper;
using DISLAMS.StudentManagement.Infrastructure.Data;
using DISLAMS.StudentManagement.Infrastructure.Repositories;
using DISLAMS.StudentManagement.Domain.Repositories;
using DISLAMS.StudentManagement.Application.Mapping;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=Ditsdev346;Database=DISLAMS_StudentManagement;Trusted_Connection=true;Encrypt=false;"));

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MappingProfile).Assembly));

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Migrate database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DISLAMS Student Management API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Global exception handling middleware
app.UseMiddleware<DISLAMS.StudentManagement.API.Middleware.ExceptionMiddleware>();

app.MapControllers();

app.Run();
