using APS.Core.Comparadores;
using APS.Core.Modelos;

namespace APS.Core.Geracao;

/// <summary>
/// Gera massas de dados internas (aleatórias) nas 4 distribuições exigidas pelo
/// enunciado: aleatório, ordenado, invertido e semi-ordenado. Serve tanto para o
/// benchmark puro (arrays de <see cref="double"/>) quanto para o cenário de negócio
/// (<see cref="RegistroImagemSatelite"/>). Aceita uma semente opcional para gerar o
/// mesmo conjunto de novo (reprodutibilidade dos testes na monografia).
/// </summary>
public class GeradorDados
{
    private static readonly string[] CodigosSatelite = { "CBERS-4A", "Amazônia-1", "Sentinel-2B", "Landsat-9" };

    // Os 9 estados que compõem a Amazônia Legal.
    private static readonly string[] EstadosAmazoniaLegal = { "AC", "AM", "AP", "MA", "MT", "PA", "RO", "RR", "TO" };

    // Bounding box aproximado da Amazônia Legal.
    private const double LatitudeMinima = -9.0;
    private const double LatitudeMaxima = 2.5;
    private const double LongitudeMinima = -73.9;
    private const double LongitudeMaxima = -44.0;

    public double[] GerarValores(int tamanho, TipoDistribuicao tipo, int? semente = null)
    {
        var rng = semente.HasValue ? new Random(semente.Value) : Random.Shared;
        var valores = new double[tamanho];
        for (int i = 0; i < tamanho; i++)
            valores[i] = Math.Round(rng.NextDouble() * 1000, 2);

        AplicarDistribuicao(valores, tipo, Comparer<double>.Default, rng);
        return valores;
    }

    public RegistroImagemSatelite[] GerarRegistros(
        int tamanho, TipoDistribuicao tipo, CampoOrdenacao campo = CampoOrdenacao.IndiceAlerta, int? semente = null)
    {
        var rng = semente.HasValue ? new Random(semente.Value) : Random.Shared;
        var registros = new RegistroImagemSatelite[tamanho];
        var dataBase = new DateTime(2026, 1, 1);

        for (int i = 0; i < tamanho; i++)
        {
            registros[i] = new RegistroImagemSatelite
            {
                Id = i + 1,
                CodigoSatelite = CodigosSatelite[rng.Next(CodigosSatelite.Length)],
                DataHoraCaptura = dataBase.AddMinutes(rng.Next(0, 60 * 24 * 300)),
                Uf = EstadosAmazoniaLegal[rng.Next(EstadosAmazoniaLegal.Length)],
                Latitude = LatitudeMinima + rng.NextDouble() * (LatitudeMaxima - LatitudeMinima),
                Longitude = LongitudeMinima + rng.NextDouble() * (LongitudeMaxima - LongitudeMinima),
                AreaKm2 = Math.Round(50 + rng.NextDouble() * 450, 1),
                TamanhoArquivoMB = Math.Round(20 + rng.NextDouble() * 480, 1),
                IndiceAlerta = Math.Round(rng.NextDouble() * 100, 1)
            };
        }

        AplicarDistribuicao(registros, tipo, new ComparadorRegistro(campo), rng);
        return registros;
    }

    /// <summary>
    /// Usa Array.Sort apenas para PREPARAR o cenário de teste (fora de qualquer
    /// cronômetro) — não tem relação com os algoritmos que o sistema está avaliando.
    /// </summary>
    private static void AplicarDistribuicao<T>(T[] dados, TipoDistribuicao tipo, IComparer<T> comparador, Random rng)
    {
        switch (tipo)
        {
            case TipoDistribuicao.Aleatorio:
                break;

            case TipoDistribuicao.Ordenado:
                Array.Sort(dados, comparador);
                break;

            case TipoDistribuicao.Invertido:
                Array.Sort(dados, comparador);
                Array.Reverse(dados);
                break;

            case TipoDistribuicao.SemiOrdenado:
                Array.Sort(dados, comparador);
                EmbaralharParcialmente(dados, rng, percentual: 0.1);
                break;
        }
    }

    private static void EmbaralharParcialmente<T>(T[] dados, Random rng, double percentual)
    {
        int trocas = Math.Max(1, (int)(dados.Length * percentual));
        for (int i = 0; i < trocas; i++)
        {
            int a = rng.Next(dados.Length);
            int b = rng.Next(dados.Length);
            (dados[a], dados[b]) = (dados[b], dados[a]);
        }
    }
}
