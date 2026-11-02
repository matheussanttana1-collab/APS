using APS.Core.Algoritmos;
using APS.Core.Modelos;

namespace APS.Core.Benchmark;

/// <summary>
/// Parâmetros de uma suíte completa: matriz de algoritmos × tamanhos × distribuições,
/// repetida <see cref="Repeticoes"/> vezes para cada combinação.
/// </summary>
public class ConfiguracaoSuite
{
    public required IReadOnlyList<IAlgoritmoOrdenacao> Algoritmos { get; init; }
    public required IReadOnlyList<int> Tamanhos { get; init; }
    public required IReadOnlyList<TipoDistribuicao> Distribuicoes { get; init; }
    public int Repeticoes { get; init; } = 5;

    public int TotalExecucoes => Algoritmos.Count * Tamanhos.Count * Distribuicoes.Count * Repeticoes;
}
