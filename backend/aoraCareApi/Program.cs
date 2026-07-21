using aoraCareApi.Application.Services;
using aoraCareApi.Application.Services.Interfaces;
using aoraCareApi.Application.Validators;
using aoraCareApi.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
);

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

app.MapControllers();

app.Run();
