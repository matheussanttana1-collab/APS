using System.Diagnostics;
using APS.Core.Algoritmos;
using APS.Core.Modelos;

namespace APS.Core.Benchmark;

/// <summary>
/// Roda um único algoritmo sobre uma CÓPIA do dataset recebido, cronometrando
/// somente a chamada de ordenação (a cópia do array fica fora do Stopwatch, assim
/// como qualquer geração/leitura de dados que tenha acontecido antes). Usado tanto
/// pela suíte de benchmark (puro desempenho) quanto por uma execução única sobre o
/// dataset atualmente carregado na tela (que também precisa do array já ordenado
/// para a listagem antes/depois).
/// </summary>
public class ExecutorBenchmark
{
    public (ResultadoExecucao Resultado, T[] DadosOrdenados) Executar<T>(
        IAlgoritmoOrdenacao algoritmo,
        T[] dadosOriginais,
        IComparer<T> comparador,
        TipoDistribuicao tipoDistribuicao,
        int repeticao = 1)
    {
        T[] copia = (T[])dadosOriginais.Clone();

        var cronometro = Stopwatch.StartNew();
        ResultadoOrdenacao resultadoOrdenacao = algoritmo.Ordenar(copia, comparador);
        cronometro.Stop();

        var resultado = new ResultadoExecucao
        {
            NomeAlgoritmo = algoritmo.Nome,
            Tamanho = dadosOriginais.Length,
            TipoDistribuicao = tipoDistribuicao,
            Repeticao = repeticao,
            TempoMs = cronometro.Elapsed.TotalMilliseconds,
            Comparacoes = resultadoOrdenacao.Comparacoes,
            Trocas = resultadoOrdenacao.Trocas
        };

        return (resultado, copia);
    }
}
