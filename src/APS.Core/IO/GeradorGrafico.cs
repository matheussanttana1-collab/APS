using System.Windows.Forms.DataVisualization.Charting;
using APS.Core.Modelos;

namespace APS.Core.IO;

/// <summary>
/// Renderiza o gráfico comparativo inteiramente em memória, usando um
/// <see cref="Chart"/> instanciado fora de qualquer formulário — não depende do que
/// está (ou não) sendo exibido na tela, então o relatório exportado fica consistente
/// mesmo que a janela de resultados nunca tenha sido aberta.
/// </summary>
public static class GeradorGrafico
{
    public static byte[]? GerarImagemTempoPorTamanho(IReadOnlyList<ResultadoExecucao> resultados)
    {
        if (resultados.Count == 0) return null;

        using var chart = new Chart { Width = 900, Height = 500 };

        var area = new ChartArea("AreaPrincipal");
        area.AxisX.Title = "Tamanho da entrada";
        area.AxisY.Title = "Tempo médio (ms)";
        chart.ChartAreas.Add(area);
        chart.Legends.Add(new Legend("Legenda"));

        foreach (var grupoAlgoritmo in resultados.GroupBy(r => r.NomeAlgoritmo))
        {
            var serie = new Series(grupoAlgoritmo.Key)
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                IsVisibleInLegend = true
            };

            var pontosPorTamanho = grupoAlgoritmo
                .GroupBy(r => r.Tamanho)
                .Select(g => (Tamanho: g.Key, TempoMedio: g.Average(r => r.TempoMs)))
                .OrderBy(p => p.Tamanho);

            foreach (var ponto in pontosPorTamanho)
                serie.Points.AddXY(ponto.Tamanho, ponto.TempoMedio);

            chart.Series.Add(serie);
        }

        using var fluxo = new MemoryStream();
        chart.SaveImage(fluxo, ChartImageFormat.Png);
        return fluxo.ToArray();
    }
}
