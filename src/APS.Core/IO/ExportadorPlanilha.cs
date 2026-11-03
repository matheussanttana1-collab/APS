using System.Drawing;
using APS.Core.Modelos;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;

namespace APS.Core.IO;

/// <summary>
/// Monta o relatório de performance em .xlsx: resultados brutos, um resumo por
/// cenário (tabela + gráfico de barras nativo do Excel) e um gráfico de linhas
/// nativo comparando os 4 algoritmos em todos os tamanhos testados. Única porta de
/// entrada do front para gerar relatórios — nenhuma lógica de montagem de planilha
/// vive na UI.
/// </summary>
public static class ExportadorPlanilha
{
    // Mesma paleta usada nos cards de algoritmo e no gráfico em tela (Tema.cs),
    // para que o relatório exportado "combine" com o que o usuário já viu no app.
    private static readonly Color CorBubble = ColorTranslator.FromHtml("#E0A452");
    private static readonly Color CorQuick = ColorTranslator.FromHtml("#3ED58A");
    private static readonly Color CorHeap = ColorTranslator.FromHtml("#5FA8D8");
    private static readonly Color CorShell = ColorTranslator.FromHtml("#C792EA");
    private static readonly Color CorCabecalhoTexto = ColorTranslator.FromHtml("#1F6B4A");
    private static readonly Color CorCabecalhoFundo = ColorTranslator.FromHtml("#E7F2EC");

    public static void ExportarResultados(IReadOnlyList<ResultadoExecucao> resultados, string caminhoArquivo)
    {
        LicencaEpplus.Garantir();

        using var pasta = new ExcelPackage();

        EscreverResultadosBrutos(pasta, resultados);
        EscreverResumo(pasta, resultados);
        EscreverGraficoGeral(pasta, resultados);

        pasta.SaveAs(new FileInfo(caminhoArquivo));
    }

    private static Color CorDoAlgoritmo(string nomeAlgoritmo) => nomeAlgoritmo switch
    {
        "Bubble Sort" => CorBubble,
        "Quick Sort" => CorQuick,
        "Heap Sort" => CorHeap,
        "Shell Sort" => CorShell,
        _ => Color.Gray
    };

    private static void EstilizarCabecalho(ExcelRange intervalo)
    {
        intervalo.Style.Font.Bold = true;
        intervalo.Style.Font.Color.SetColor(CorCabecalhoTexto);
        intervalo.Style.Fill.PatternType = ExcelFillStyle.Solid;
        intervalo.Style.Fill.BackgroundColor.SetColor(CorCabecalhoFundo);
        intervalo.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    }

    // ----------------------------------------------------------- Resultados brutos

    private static void EscreverResultadosBrutos(ExcelPackage pasta, IReadOnlyList<ResultadoExecucao> resultados)
    {
        var planilha = pasta.Workbook.Worksheets.Add("Resultados brutos");
        string[] cabecalhos = { "Algoritmo", "Tamanho", "Distribuição", "Repetição", "Tempo (ms)", "Comparações", "Trocas" };

        for (int i = 0; i < cabecalhos.Length; i++)
            planilha.Cells[1, i + 1].Value = cabecalhos[i];
        EstilizarCabecalho(planilha.Cells[1, 1, 1, cabecalhos.Length]);

        int linha = 2;
        foreach (var r in resultados)
        {
            planilha.Cells[linha, 1].Value = r.NomeAlgoritmo;
            planilha.Cells[linha, 2].Value = r.Tamanho;
            planilha.Cells[linha, 3].Value = r.TipoDistribuicao.ToString();
            planilha.Cells[linha, 4].Value = r.Repeticao;
            planilha.Cells[linha, 5].Value = Math.Round(r.TempoMs, 4);
            planilha.Cells[linha, 6].Value = r.Comparacoes;
            planilha.Cells[linha, 7].Value = r.Trocas;
            linha++;
        }

        if (linha > 2)
            planilha.Cells[1, 1, linha - 1, cabecalhos.Length].AutoFilter = true;

        planilha.View.FreezePanes(2, 1);
        planilha.Cells.AutoFitColumns();
    }

    // -------------------------------------------------------------------- Resumo

    private static void EscreverResumo(ExcelPackage pasta, IReadOnlyList<ResultadoExecucao> resultados)
    {
        var planilha = pasta.Workbook.Worksheets.Add("Resumo");

        planilha.Cells[1, 1].Value = "Resumo por cenário — tempo de cada repetição e média por algoritmo";
        planilha.Cells[1, 1].Style.Font.Bold = true;
        planilha.Cells[1, 1].Style.Font.Size = 14;
        planilha.Cells[1, 1].Style.Font.Color.SetColor(CorCabecalhoTexto);

        if (resultados.Count == 0) return;

        var cenarios = resultados
            .Select(r => (r.Tamanho, r.TipoDistribuicao))
            .Distinct()
            .OrderBy(c => c.Tamanho).ThenBy(c => c.TipoDistribuicao)
            .ToList();

        int linhaAtual = 3;
        int indiceGrafico = 0;

        foreach (var cenario in cenarios)
        {
            var doCenario = resultados
                .Where(r => r.Tamanho == cenario.Tamanho && r.TipoDistribuicao == cenario.TipoDistribuicao)
                .ToList();
            int repeticoes = doCenario.Max(r => r.Repeticao);

            int linhaTitulo = linhaAtual;
            planilha.Cells[linhaTitulo, 1].Value = $"Tamanho: {cenario.Tamanho:N0}   ·   Distribuição: {cenario.TipoDistribuicao}";
            planilha.Cells[linhaTitulo, 1].Style.Font.Bold = true;
            planilha.Cells[linhaTitulo, 1].Style.Font.Color.SetColor(CorCabecalhoTexto);
            linhaAtual++;

            int linhaCabecalho = linhaAtual;
            planilha.Cells[linhaCabecalho, 1].Value = "Algoritmo";
            for (int rep = 1; rep <= repeticoes; rep++)
                planilha.Cells[linhaCabecalho, 1 + rep].Value = $"Execução {rep}";
            int colunaMedia = repeticoes + 2;
            planilha.Cells[linhaCabecalho, colunaMedia].Value = "Média (ms)";
            EstilizarCabecalho(planilha.Cells[linhaCabecalho, 1, linhaCabecalho, colunaMedia]);
            linhaAtual++;

            var porAlgoritmo = doCenario
                .GroupBy(r => r.NomeAlgoritmo)
                .Select(g => new { Nome = g.Key, Execucoes = g.OrderBy(r => r.Repeticao).ToList(), Media = g.Average(r => r.TempoMs) })
                .OrderBy(a => a.Media)
                .ToList();

            int linhaPrimeiraLinha = linhaAtual;
            foreach (var algoritmo in porAlgoritmo)
            {
                planilha.Cells[linhaAtual, 1].Value = algoritmo.Nome;
                foreach (var execucao in algoritmo.Execucoes)
                    planilha.Cells[linhaAtual, 1 + execucao.Repeticao].Value = Math.Round(execucao.TempoMs, 4);

                planilha.Cells[linhaAtual, colunaMedia].Value = Math.Round(algoritmo.Media, 4);
                planilha.Cells[linhaAtual, colunaMedia].Style.Font.Bold = true;
                linhaAtual++;
            }
            int linhaUltimaLinha = linhaAtual - 1;

            InserirGraficoDoCenario(
                planilha, $"grafico_resumo_{indiceGrafico++}",
                $"Tempo médio — n={cenario.Tamanho:N0}, {cenario.TipoDistribuicao}",
                linhaTitulo, linhaPrimeiraLinha, linhaUltimaLinha, colunaMedia);

            linhaAtual += 15; // espaço reservado para o gráfico não sobrepor o próximo bloco
        }

        planilha.Column(1).Width = 26;
        for (int c = 2; c <= 12; c++)
            planilha.Column(c).Width = 13;
    }

    private static void InserirGraficoDoCenario(
        ExcelWorksheet planilha, string nome, string titulo,
        int linhaTitulo, int linhaPrimeiraLinha, int linhaUltimaLinha, int colunaMedia)
    {
        var grafico = (ExcelBarChart)planilha.Drawings.AddChart(nome, eChartType.ColumnClustered);
        grafico.Title.Text = titulo;

        var serie = (ExcelBarChartSerie)grafico.Series.Add(
            planilha.Cells[linhaPrimeiraLinha, colunaMedia, linhaUltimaLinha, colunaMedia],
            planilha.Cells[linhaPrimeiraLinha, 1, linhaUltimaLinha, 1]);
        serie.Header = "Tempo médio (ms)";

        // Bubble Sort é O(n²) e os outros três O(n log n): numa escala comum, a
        // barra do Bubble esmaga as demais lá embaixo perto de zero — igual ao
        // problema já corrigido no gráfico em tela. Escala log resolve aqui também.
        grafico.YAxis.LogBase = 10;
        grafico.YAxis.Title.Text = "Tempo médio (ms) — escala log";
        serie.DataLabel.ShowValue = true;
        grafico.Legend.Remove();
        grafico.SetPosition(linhaTitulo - 1, 0, colunaMedia + 1, 10);
        grafico.SetSize(480, 260);
    }

    // ---------------------------------------------------------------- Gráfico geral

    private static void EscreverGraficoGeral(ExcelPackage pasta, IReadOnlyList<ResultadoExecucao> resultados)
    {
        var planilha = pasta.Workbook.Worksheets.Add("Gráfico");
        if (resultados.Count == 0) return;

        var tamanhos = resultados.Select(r => r.Tamanho).Distinct().OrderBy(t => t).ToList();
        var algoritmos = resultados.Select(r => r.NomeAlgoritmo).Distinct().ToList();

        planilha.Cells[1, 1].Value = "Tamanho";
        for (int i = 0; i < algoritmos.Count; i++)
            planilha.Cells[1, i + 2].Value = algoritmos[i];
        EstilizarCabecalho(planilha.Cells[1, 1, 1, algoritmos.Count + 1]);

        for (int linha = 0; linha < tamanhos.Count; linha++)
        {
            planilha.Cells[linha + 2, 1].Value = tamanhos[linha];
            for (int coluna = 0; coluna < algoritmos.Count; coluna++)
            {
                var tempos = resultados
                    .Where(r => r.Tamanho == tamanhos[linha] && r.NomeAlgoritmo == algoritmos[coluna])
                    .Select(r => r.TempoMs)
                    .ToList();

                if (tempos.Count > 0)
                    planilha.Cells[linha + 2, coluna + 2].Value = Math.Round(tempos.Average(), 4);
            }
        }

        planilha.Cells.AutoFitColumns();

        var grafico = planilha.Drawings.AddChart("grafico_geral", eChartType.LineMarkers);
        grafico.Title.Text = "Tempo médio × tamanho da entrada, por algoritmo";

        int ultimaLinhaDados = tamanhos.Count + 1;
        for (int coluna = 0; coluna < algoritmos.Count; coluna++)
        {
            var serie = (ExcelLineChartSerie)grafico.Series.Add(
                planilha.Cells[2, coluna + 2, ultimaLinhaDados, coluna + 2],
                planilha.Cells[2, 1, ultimaLinhaDados, 1]);
            serie.Header = algoritmos[coluna];
            serie.Border.Fill.Color = CorDoAlgoritmo(algoritmos[coluna]);
        }

        // Mesmo motivo da aba Resumo: escala log deixa os 4 algoritmos visíveis,
        // cada um na sua própria grandeza, em vez do Bubble Sort "achatar" os outros.
        grafico.YAxis.LogBase = 10;
        grafico.YAxis.Title.Text = "Tempo médio (ms) — escala log";
        grafico.XAxis.Title.Text = "Tamanho da entrada";
        grafico.SetPosition(0, 0, algoritmos.Count + 3, 0);
        grafico.SetSize(820, 440);
    }
}
