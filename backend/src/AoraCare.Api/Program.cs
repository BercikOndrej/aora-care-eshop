using AoraCare.Api.Middlewares;
using AoraCare.Application.Services;
using AoraCare.Application.Services.Interfaces;
using AoraCare.Application.Validators;
using AoraCare.Domain.Repositories;
using AoraCare.Infrastructure.Data;
using AoraCare.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// * Services

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
);

// Middlewares registartion
builder.Services.AddTransient<GlobalExceptionHandlingMiddleware>();

// Repositories
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Application services
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Validators
builder.Services.AddValidatorsFromAssemblyContaining<CategoryAddDtoValidator>();

// .NET 8+ trims the "Async" suffix from action names by default, which breaks
// nameof(XAsync) references used in CreatedAtAction/RedirectToAction. Keep full
// method names as action names so nameof stays accurate.
builder.Services.AddControllers(options => options.SuppressAsyncSuffixInActionNames = false);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseGlobalExceptionMiddleware();

app.MapControllers();

app.Run();
