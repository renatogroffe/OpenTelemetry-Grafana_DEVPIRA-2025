using APIContagem.Data;
using APIContagem.Models;
using APIContagem.Tracing;
using Microsoft.AspNetCore.Mvc;

namespace APIContagem.Controllers;

[ApiController]
[Route("[controller]")]
public class ContadorController : ControllerBase
{
    private readonly static Lock ContagemLock = new();

    private readonly ILogger<ContadorController> _logger;
    private readonly IConfiguration _configuration;
    private readonly Contador _contador;
    private readonly ContagemRepository _repository;

    public ContadorController(ILogger<ContadorController> logger,
        IConfiguration configuration,
        Contador contador,
        ContagemRepository repository)
    {
        _logger = logger;
        _configuration = configuration;
        _contador = contador;
        _repository = repository;
    }

    [HttpGet]
    public ResultadoContador Get()
    {
        using var activity1 = OpenTelemetryExtensions.ActivitySource
            .StartActivity("GerarValorContagem")!;

        int valorAtualContador;
        using (ContagemLock.EnterScope())
        {
            _contador.Incrementar();
            valorAtualContador = _contador.ValorAtual;
        }
        activity1.SetTag("valorAtual", valorAtualContador);
        _logger.LogInformation($"Contador - Valor atual: {valorAtualContador}");

        var resultado = new ResultadoContador()
        {
            ValorAtual = _contador.ValorAtual,
            Local = _contador.Local,
            Kernel = _contador.Kernel,
            Mensagem = _configuration["Saudacao"],
            Framework = _contador.Framework
        };
        activity1.Stop();

        using var activity2 = OpenTelemetryExtensions.ActivitySource
            .StartActivity("RegistrarRetornarValorContagem")!;

        _repository.Insert(resultado);
        _logger.LogInformation($"Registro inserido com sucesso! Valor: {valorAtualContador}");

        activity2.SetTag("valorAtual", valorAtualContador);
        activity2.SetTag("horario", $"{DateTime.UtcNow.AddHours(-3):HH:mm:ss}");

        return resultado;
    }
}