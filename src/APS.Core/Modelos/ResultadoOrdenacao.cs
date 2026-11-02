namespace APS.Core.Modelos;

/// <summary>
/// Métricas intrínsecas de uma execução de ordenação (contadas pelo próprio algoritmo).
/// O tempo não entra aqui — é medido externamente pelo executor do benchmark.
/// </summary>
public class ResultadoOrdenacao
{
    public long Comparacoes { get; set; }
    public long Trocas { get; set; }
}
