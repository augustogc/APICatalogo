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

// O método AddJsonOptions com a opção ReferenceHandler.IgnoreCycles, elimina o erro de referência cíclica na serialização
// de objetos os quais têm classes que se referenciam mutuamente como uma agregação, usada no Entity Framework.
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

// Adicionando um serviço
builder.Services.AddTransient<IMeuServico, MeuServico>();

// String de conexão criada no appsettings.json
string mySqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");

// Definir o contexto da conexão (SGBD MySql)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(mySqlConnection, ServerVersion.AutoDetect(mySqlConnection))
);

// Adicionar novo filtro de serviço
builder.Services.AddScoped<ApiLoggingFilter>();

// Adicionar o provider de Logger
builder.Logging.AddProvider(new CustomLoggerProvider(new CustomLoggerProviderConfiguration
{
    LogLevel = LogLevel.Information
}));

// Adicionar injeção de dependência para acessar os repositórios
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Cria variável com a configuração do AutoMapper
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
