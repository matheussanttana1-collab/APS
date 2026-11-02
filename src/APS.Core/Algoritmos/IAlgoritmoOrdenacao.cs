using APS.Core.Modelos;

namespace APS.Core.Algoritmos;

/// <summary>
/// Contrato comum a todos os algoritmos de ordenação do sistema. Cada implementação
/// ordena o array em memória (in place) e devolve apenas o que é intrínseco ao próprio
/// algoritmo (comparações e trocas); o tempo é medido externamente por quem chama
/// <see cref="Ordenar{T}"/>, garantindo que todos sejam cronometrados da mesma forma.
/// </summary>
public interface IAlgoritmoOrdenacao
{
    string Nome { get; }

    ResultadoOrdenacao Ordenar<T>(T[] dados, IComparer<T> comparador);
}
