using APS.Core.Geracao;
using APS.Core.Modelos;

namespace APS.Core.Benchmark;

/// <summary>
/// Varre a matriz completa (algoritmos × tamanhos × distribuições × repetições) para
/// alimentar a tela de resultados e o relatório exportado. Gera uma massa de dados
/// nova (via <see cref="GeradorDados"/>) para cada combinação de tamanho/distribuição,
/// e todos os algoritmos selecionados ordenam cópias dela — portanto comparam
/// exatamente os mesmos dados em cada rodada.
/// </summary>
public class SuiteBenchmark
{
    private readonly ExecutorBenchmark _executor = new();
    private readonly GeradorDados _gerador = new();

    public List<ResultadoExecucao> Executar(
        ConfiguracaoSuite configuracao,
        IProgress<ProgressoBenchmark>? progresso = null,
        CancellationToken cancellationToken = default)
    {
        var resultados = new List<ResultadoExecucao>(configuracao.TotalExecucoes);
        int concluidos = 0;

        foreach (var tamanho in configuracao.Tamanhos)
        {
            foreach (var distribuicao in configuracao.Distribuicoes)
            {
                double[] dadosBase = _gerador.GerarValores(tamanho, distribuicao);

                foreach (var algoritmo in configuracao.Algoritmos)
                {
                    for (int repeticao = 1; repeticao <= configuracao.Repeticoes; repeticao++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var (resultado, _) = _executor.Executar(
                            algoritmo, dadosBase, Comparer<double>.Default, distribuicao, repeticao);

                        resultados.Add(resultado);
                        concluidos++;

                        progresso?.Report(new ProgressoBenchmark
                        {
                            Concluidos = concluidos,
                            Total = configuracao.TotalExecucoes,
                            DescricaoAtual = $"{algoritmo.Nome} · {tamanho} elementos · {distribuicao} · " +
                                              $"repetição {repeticao}/{configuracao.Repeticoes}"
                        });
                    }
                }
            }
        }

        return resultados;
    }
}
