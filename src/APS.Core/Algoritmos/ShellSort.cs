using APS.Core.Modelos;

namespace APS.Core.Algoritmos;

/// <summary>
/// Sequência de gaps clássica (n/2, n/4, ..., 1): insertion sort com comparações
/// a distâncias decrescentes, reduzindo o número de trocas longas antes do passe final.
/// </summary>
public class ShellSort : IAlgoritmoOrdenacao
{
    public string Nome => "Shell Sort";

    public ResultadoOrdenacao Ordenar<T>(T[] dados, IComparer<T> comparador)
    {
        var resultado = new ResultadoOrdenacao();
        int n = dados.Length;

        for (int gap = n / 2; gap > 0; gap /= 2)
        {
            for (int i = gap; i < n; i++)
            {
                T temp = dados[i];
                int j = i;

                while (j >= gap)
                {
                    resultado.Comparacoes++;
                    if (comparador.Compare(dados[j - gap], temp) <= 0) break;

                    dados[j] = dados[j - gap];
                    resultado.Trocas++;
                    j -= gap;
                }

                dados[j] = temp;
            }
        }

        return resultado;
    }
}
