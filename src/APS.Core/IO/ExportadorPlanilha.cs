using APS.Core.Modelos;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;

namespace APS.Core.IO;

/// <summary>
/// Monta o relatório de performance em .xlsx: resultados brutos, resumo
/// (tempo médio por algoritmo/tamanho/distribuição) e o gráfico comparativo.
/// Única porta de entrada do front para gerar relatórios — nenhuma lógica de
/// montagem de planilha vive na UI.
/// </summary>
public static class ExportadorPlanilha
{
    public static void ExportarResultados(IReadOnlyList<ResultadoExecucao> resultados, string caminhoArquivo)
    {
        using var pasta = new XLWorkbook();

        EscreverResultadosBrutos(pasta, resultados);
        EscreverResumo(pasta, resultados);
        InserirGrafico(pasta, resultados);

        pasta.SaveAs(caminhoArquivo);
    }

    private static void EscreverResultadosBrutos(XLWorkbook pasta, IReadOnlyList<ResultadoExecucao> resultados)
    {
        var planilha = pasta.Worksheets.Add("Resultados brutos");
        string[] cabecalhos = { "Algoritmo", "Tamanho", "Distribuição", "Repetição", "Tempo (ms)", "Comparações", "Trocas" };

        for (int i = 0; i < cabecalhos.Length; i++)
            planilha.Cell(1, i + 1).Value = cabecalhos[i];

        int linha = 2;
        foreach (var r in resultados)
        {
            planilha.Cell(linha, 1).Value = r.NomeAlgoritmo;
            planilha.Cell(linha, 2).Value = r.Tamanho;
            planilha.Cell(linha, 3).Value = r.TipoDistribuicao.ToString();
            planilha.Cell(linha, 4).Value = r.Repeticao;
            planilha.Cell(linha, 5).Value = r.TempoMs;
            planilha.Cell(linha, 6).Value = r.Comparacoes;
            planilha.Cell(linha, 7).Value = r.Trocas;
            linha++;
        }

        planilha.Columns().AdjustToContents();
    }

    private static void EscreverResumo(XLWorkbook pasta, IReadOnlyList<ResultadoExecucao> resultados)
    {
        var planilha = pasta.Worksheets.Add("Resumo");
        string[] cabecalhos = { "Algoritmo", "Tamanho", "Distribuição", "Tempo médio (ms)" };

        for (int i = 0; i < cabecalhos.Length; i++)
            planilha.Cell(1, i + 1).Value = cabecalhos[i];

        int linha = 2;
        foreach (var r in CalcularResumo(resultados))
        {
            planilha.Cell(linha, 1).Value = r.NomeAlgoritmo;
            planilha.Cell(linha, 2).Value = r.Tamanho;
            planilha.Cell(linha, 3).Value = r.Distribuicao;
            planilha.Cell(linha, 4).Value = Math.Round(r.TempoMedioMs, 4);
            linha++;
        }

        planilha.Columns().AdjustToContents();
    }

    private static void InserirGrafico(XLWorkbook pasta, IReadOnlyList<ResultadoExecucao> resultados)
    {
        byte[]? imagemPng = GeradorGrafico.GerarImagemTempoPorTamanho(resultados);
        if (imagemPng is null) return;

        var planilha = pasta.Worksheets.Add("Gráfico");
        using var fluxo = new MemoryStream(imagemPng);
        planilha.AddPicture(fluxo, XLPictureFormat.Png)
            .MoveTo(planilha.Cell(2, 2))
            .WithSize(760, 420);
    }

    private static List<(string NomeAlgoritmo, int Tamanho, string Distribuicao, double TempoMedioMs)> CalcularResumo(
        IReadOnlyList<ResultadoExecucao> resultados)
    {
        return resultados
            .GroupBy(r => (r.NomeAlgoritmo, r.Tamanho, Distribuicao: r.TipoDistribuicao.ToString()))
            .Select(g => (g.Key.NomeAlgoritmo, g.Key.Tamanho, g.Key.Distribuicao, TempoMedioMs: g.Average(r => r.TempoMs)))
            .OrderBy(r => r.Tamanho).ThenBy(r => r.NomeAlgoritmo)
            .ToList();
    }
}
