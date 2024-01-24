using APICatalogo.Context;
using APICatalogo.DTOs.Mappings;
using APICatalogo.Extensions;
using APICatalogo.Filters;
using APICatalogo.Logging;
using APICatalogo.Repository;
using APICatalogo.Services;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// O m�todo AddJsonOptions com a op��o ReferenceHandler.IgnoreCycles, elimina o erro de refer�ncia c�clica na serializa��o
// de objetos os quais t�m classes que se referenciam mutuamente como uma agrega��o, usada no Entity Framework.
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

// Adicionando um servi�o
builder.Services.AddTransient<IMeuServico, MeuServico>();

// String de conex�o criada no appsettings.json
string mySqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");

// Definir o contexto da conex�o (SGBD MySql)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(mySqlConnection, ServerVersion.AutoDetect(mySqlConnection))
);

// Adicionar novo filtro de servi�o
builder.Services.AddScoped<ApiLoggingFilter>();

// Adicionar o provider de Logger
builder.Logging.AddProvider(new CustomLoggerProvider(new CustomLoggerProviderConfiguration
{
    LogLevel = LogLevel.Information
}));

// Adicionar inje��o de depend�ncia para acessar os reposit�rios
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Cria vari�vel com a configura��o do AutoMapper
var mappingConfig = new MapperConfiguration(mc =>
    {
        mc.AddProfile(new MappingProfile());
    });

// Adiciona o AutoMapper
IMapper mapper = mappingConfig.CreateMapper();
builder.Services.AddSingleton(mapper);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Adiciona o middleware de tratamento de erros
app.ConfigureExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
