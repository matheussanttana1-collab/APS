using APS.Core.Modelos;

namespace APS.UI;

/// <summary>
/// Pacote de resultados que o painel de Execução entrega ao painel de Resultados:
/// as métricas (sempre presentes) e, só numa execução única sobre o dataset atual,
/// a listagem textual antes/depois por algoritmo.
/// </summary>
public class ResultadoParaExibicao
{
    public required List<ResultadoExecucao> Execucoes { get; init; }
    public string? DadosAntes { get; init; }
    public Dictionary<string, string>? DadosDepoisPorAlgoritmo { get; init; }
}
