using NodaTime;

namespace APIContagem.Data;

public class HistoricoContagem
{
    public int? Id { get; set; }
    public Instant DataProcessamento { get; set; }
    public int ValorAtual { get; set; }
    public string? Producer { get; set; }
    public string? Kernel { get; set; }
    public string? Framework { get; set; }
    public string? Mensagem { get; set; }
}