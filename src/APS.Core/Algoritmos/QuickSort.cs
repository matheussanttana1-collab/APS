using APS.Core.Modelos;

namespace APS.Core.Algoritmos;

/// <summary>
/// Partição de Lomuto com pivô aleatório. O pivô aleatório (em vez de fixo no
/// primeiro/último elemento) evita a degradação para O(n²) — e o estouro de pilha
/// de recursão em arrays grandes — quando a entrada já está ordenada ou invertida,
/// cenários que este sistema testa explicitamente.
/// </summary>
public class QuickSort : IAlgoritmoOrdenacao
{
    public string Nome => "Quick Sort";

    public ResultadoOrdenacao Ordenar<T>(T[] dados, IComparer<T> comparador)
    {
        var resultado = new ResultadoOrdenacao();
        if (dados.Length > 1)
            OrdenarRecursivo(dados, 0, dados.Length - 1, comparador, resultado);
        return resultado;
    }

    private static void OrdenarRecursivo<T>(T[] dados, int inicio, int fim, IComparer<T> comparador, ResultadoOrdenacao resultado)
    {
        if (inicio >= fim) return;

        int indicePivo = Particionar(dados, inicio, fim, comparador, resultado);
        OrdenarRecursivo(dados, inicio, indicePivo - 1, comparador, resultado);
        OrdenarRecursivo(dados, indicePivo + 1, fim, comparador, resultado);
    }

    private static int Particionar<T>(T[] dados, int inicio, int fim, IComparer<T> comparador, ResultadoOrdenacao resultado)
    {
        int indiceAleatorio = Random.Shared.Next(inicio, fim + 1);
        if (indiceAleatorio != fim)
        {
            (dados[indiceAleatorio], dados[fim]) = (dados[fim], dados[indiceAleatorio]);
            resultado.Trocas++;
        }

        T pivo = dados[fim];
        int i = inicio - 1;

        for (int j = inicio; j < fim; j++)
        {
            resultado.Comparacoes++;
            if (comparador.Compare(dados[j], pivo) <= 0)
            {
                i++;
                if (i != j)
                {
                    (dados[i], dados[j]) = (dados[j], dados[i]);
                    resultado.Trocas++;
                }
            }
        }

        if (i + 1 != fim)
        {
            (dados[i + 1], dados[fim]) = (dados[fim], dados[i + 1]);
            resultado.Trocas++;
        }

        return i + 1;
    }
}
