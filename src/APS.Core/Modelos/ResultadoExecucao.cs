namespace APS.Core.Modelos;

/// <summary>
/// Resultado completo de uma execução de benchmark: identifica o cenário testado
/// (algoritmo, tamanho, distribuição, repetição) e o tempo medido externamente,
/// somado às métricas intrínsecas devolvidas pelo próprio algoritmo.
/// </summary>
public class ResultadoExecucao
{
    public string NomeAlgoritmo { get; set; } = string.Empty;
    public int Tamanho { get; set; }
    public TipoDistribuicao TipoDistribuicao { get; set; }
    public int Repeticao { get; set; }
    public double TempoMs { get; set; }
    public long Comparacoes { get; set; }
    public long Trocas { get; set; }
}
