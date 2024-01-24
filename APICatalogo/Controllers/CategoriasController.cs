﻿using APICatalogo.Context;
using APICatalogo.DTOs;
using APICatalogo.Models;
using APICatalogo.Pagination;
using APICatalogo.Repository;
using APICatalogo.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace APICatalogo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriasController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;

        public CategoriasController(IUnitOfWork contexto, IConfiguration config, ILogger<CategoriasController> logger, IMapper mapper)
        {
            _uow = contexto;
            _configuration = config;
            _logger = logger;
            _mapper = mapper;
        }

        // Pegar dados de configuração do arquivo appsettings.json (IConfiguration)
        [HttpGet("autor")]
        public string GetAutor()
        {
            var autor = _configuration["autor"];
            var conexao = _configuration["ConnectionStrings:DefaultConnection"];

            return $"Autor : {autor} Conexão : {conexao}";
        }

        // [FromServices] faz a injeção de dependência diretamente na action.
        [HttpGet("saudacao/{nome}")]
        public ActionResult<string> GetSaudacao([FromServices] IMeuServico meuServico, string nome)
        {
            return meuServico.Saudacao(nome);
        }


        #region -> Anotações
        /* Este método usa duas classes que se auto referenciam (agregação). Na serialização do json ocorre um erro
         de referência cíclica. Para eliminar este problema é necessário adicionar a opção "IgnoreCycles" no método
         builder.Services.AddControllers() da classe Program. */
        #endregion

        [HttpGet("produtos")]
        public async Task<ActionResult<IEnumerable<CategoriaDTO>>> GetCategoriasProdutos()
        {
            _logger.LogInformation("======================= GET api/categorias/produtos/controle =======================");

            var categorias = await _uow.CategoriaRepository.GetCategoriasProdutos();
            var categoriasDto = _mapper.Map<List<CategoriaDTO>>(categorias);

            #region -> Anotações
            /* O método Where é usado pra restringir a consulta.Pois é uma boa prática não retornar todos os dados diretamente
            na mesma consulta, pois onera o banco.

            O método .AsNoTracking() no EF não deixa ratrear o objeto para fazer cache. Pois é consulta somente leitura.
            Isso melhora a performance. 
            OBS: Só deve ser usado quando temos certeza que o objeto não precisar ser atualizado posteriormente(update). */
            #endregion
            return categoriasDto;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoriaDTO>>> Get([FromQuery] CategoriasParameters categoriasParameters)
        {
            _logger.LogInformation("======================= GET api/categorias/controle =======================");

            //var categorias = _uow.CategoriaRepository.Get().ToList();
            var categorias = await _uow.CategoriaRepository.GetCategorias(categoriasParameters);

            if (categorias is null)
            {
                return NotFound("Categorias não encontradas...");
            }

            var metadata = new
            {
                categorias.TotalCount,
                categorias.PageSize,
                categorias.CurrentPage,
                categorias.TotalPages,
                categorias.HasNext,
                categorias.HasPrevious
            };

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(metadata));

            var categoriasDto = _mapper.Map<List<CategoriaDTO>>(categorias);

            return categoriasDto;
        }

        /* 
         O id:int serve pra restringir o tipo de parâmetro a ser passado na url.
         O Name = "ObterCategoria" serve para criar uma rota, a qual pode ser utilizada pelos métodos deste controller. 
        */
        [HttpGet("{id:int}", Name = "ObterCategoria")] 
        public async Task<ActionResult<Categoria>> Get(int id)
        {
            _logger.LogInformation($"======================= GET api/categorias/id = {id} =======================");

            try
            {
                var categoria = await _uow.CategoriaRepository.GetById(p => p.CategoriaId == id);

                if (categoria == null)
                {
                    _logger.LogInformation($"======================= GET api/categorias/id = {id} NOT FOUND =======================");

                    return NotFound($"Categoria com id = {id} não encontrada...");
                }

                var categoriaDto = _mapper.Map<CategoriaDTO>(categoria);

                return Ok(categoriaDto);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                                  "Ocorreu um problema ao tratar a sua solicitação.");
            }
        }


        /*
         Retorna criando uma rota definida no método Get(int id).
         É necessário definir essa rota no Get(int id) para funcionar. 
        */
        [HttpPost]
        public async Task<ActionResult> Post(CategoriaDTO categoriaDto)
        {
            var categoria = _mapper.Map<Categoria>(categoriaDto);

            _uow.CategoriaRepository.Add(categoria);
            await _uow.Commit();

            var categoriaDtoRetornada = _mapper.Map<CategoriaDTO>(categoria);

            // Retorna criando uma rota definida no método Get(int id) [Name = "ObterCategoria"].                   
            return new CreatedAtRouteResult("ObterCategoria", new { id = categoria.CategoriaId }, categoriaDtoRetornada);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, CategoriaDTO categoriaDto)
        {
            if (id != categoriaDto.CategoriaId)
            {
                return BadRequest();
            }

            var categoria = _mapper.Map<Categoria>(categoriaDto);

            _uow.CategoriaRepository.Update(categoria);
            await _uow.Commit();

            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<CategoriaDTO>> Delete(int id)
        {
            var categoria = await _uow.CategoriaRepository.GetById(p => p.CategoriaId == id);

            if (categoria == null)
            {
                return NotFound($"Categoria com id = {id} não encontrada...");
            }

            _uow.CategoriaRepository.Delete(categoria);
            await _uow.Commit();

            var categoriaDto = _mapper.Map<CategoriaDTO>(categoria);

            return categoriaDto;
        }
    }
}
