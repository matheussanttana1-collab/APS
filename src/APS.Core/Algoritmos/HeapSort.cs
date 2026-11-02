using APS.Core.Modelos;

namespace APS.Core.Algoritmos;

/// <summary>
/// Heap máximo clássico: constrói o heap e depois extrai repetidamente a raiz
/// (maior elemento) para o fim do array. O(n log n) em todos os cenários — serve
/// de "controle" estável para comparar com os demais algoritmos.
/// </summary>
public class HeapSort : IAlgoritmoOrdenacao
{
    public string Nome => "Heap Sort";

    public ResultadoOrdenacao Ordenar<T>(T[] dados, IComparer<T> comparador)
    {
        var resultado = new ResultadoOrdenacao();
        int n = dados.Length;

        for (int i = n / 2 - 1; i >= 0; i--)
            Heapify(dados, n, i, comparador, resultado);

        for (int i = n - 1; i > 0; i--)
        {
            (dados[0], dados[i]) = (dados[i], dados[0]);
            resultado.Trocas++;
            Heapify(dados, i, 0, comparador, resultado);
        }

        return resultado;
    }

    private static void Heapify<T>(T[] dados, int tamanho, int raiz, IComparer<T> comparador, ResultadoOrdenacao resultado)
    {
        int maior = raiz;
        int esquerda = 2 * raiz + 1;
        int direita = 2 * raiz + 2;

        if (esquerda < tamanho)
        {
            resultado.Comparacoes++;
            if (comparador.Compare(dados[esquerda], dados[maior]) > 0)
                maior = esquerda;
        }

        if (direita < tamanho)
        {
            resultado.Comparacoes++;
            if (comparador.Compare(dados[direita], dados[maior]) > 0)
                maior = direita;
        }

        if (maior != raiz)
        {
            (dados[raiz], dados[maior]) = (dados[maior], dados[raiz]);
            resultado.Trocas++;
            Heapify(dados, tamanho, maior, comparador, resultado);
        }
    }
}
