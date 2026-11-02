using APS.Core.Modelos;

namespace APS.Core.Algoritmos;

/// <summary>
/// Implementação clássica com parada antecipada: se um passe inteiro não faz
/// nenhuma troca, o array já está ordenado e o algoritmo encerra. Mantém o pior
/// caso O(n²), mas chega a O(n) no melhor caso (array já ordenado).
/// </summary>
public class BubbleSort : IAlgoritmoOrdenacao
{
    public string Nome => "Bubble Sort";

    public ResultadoOrdenacao Ordenar<T>(T[] dados, IComparer<T> comparador)
    {
        var resultado = new ResultadoOrdenacao();
        int n = dados.Length;

        for (int i = 0; i < n - 1; i++)
        {
            bool houveTroca = false;

            for (int j = 0; j < n - i - 1; j++)
            {
                resultado.Comparacoes++;
                if (comparador.Compare(dados[j], dados[j + 1]) > 0)
                {
                    (dados[j], dados[j + 1]) = (dados[j + 1], dados[j]);
                    resultado.Trocas++;
                    houveTroca = true;
                }
            }

            if (!houveTroca) break;
        }

        return resultado;
    }
}
