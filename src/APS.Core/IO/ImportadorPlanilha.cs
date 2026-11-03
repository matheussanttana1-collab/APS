using APS.Core.Modelos;
using OfficeOpenXml;

namespace APS.Core.IO;

/// <summary>
/// Lê/gera o catálogo de imagens de satélite em planilha (.xlsx) via EPPlus.
/// </summary>
public static class ImportadorPlanilha
{
    private static readonly string[] Cabecalhos =
    {
        "Id", "CodigoSatelite", "DataHoraCaptura", "Uf", "Latitude",
        "Longitude", "AreaKm2", "TamanhoArquivoMB", "IndiceAlerta"
    };

    public static RegistroImagemSatelite[] ImportarRegistros(string caminhoArquivo)
    {
        LicencaEpplus.Garantir();

        using var pasta = new ExcelPackage(new FileInfo(caminhoArquivo));
        var planilha = pasta.Workbook.Worksheets[0];
        var registros = new List<RegistroImagemSatelite>();

        int ultimaLinha = planilha.Dimension?.End.Row ?? 1;
        for (int linha = 2; linha <= ultimaLinha; linha++)
        {
            if (planilha.Cells[linha, 1].Value is null) continue;

            registros.Add(new RegistroImagemSatelite
            {
                Id = Convert.ToInt32(planilha.Cells[linha, 1].Value),
                CodigoSatelite = planilha.Cells[linha, 2].Text,
                DataHoraCaptura = planilha.Cells[linha, 3].GetValue<DateTime>(),
                Uf = planilha.Cells[linha, 4].Text,
                Latitude = planilha.Cells[linha, 5].GetValue<double>(),
                Longitude = planilha.Cells[linha, 6].GetValue<double>(),
                AreaKm2 = planilha.Cells[linha, 7].GetValue<double>(),
                TamanhoArquivoMB = planilha.Cells[linha, 8].GetValue<double>(),
                IndiceAlerta = planilha.Cells[linha, 9].GetValue<double>()
            });
        }

        if (registros.Count == 0)
            throw new InvalidDataException("Nenhum registro válido foi encontrado na planilha.");

        return registros.ToArray();
    }

    public static void GerarModeloRegistros(string caminhoArquivo)
    {
        LicencaEpplus.Garantir();

        using var pasta = new ExcelPackage();
        var planilha = pasta.Workbook.Worksheets.Add("Catalogo");

        for (int i = 0; i < Cabecalhos.Length; i++)
        {
            planilha.Cells[1, i + 1].Value = Cabecalhos[i];
            planilha.Cells[1, i + 1].Style.Font.Bold = true;
        }

        planilha.Cells[2, 1].Value = 1;
        planilha.Cells[2, 2].Value = "CBERS-4A";
        planilha.Cells[2, 3].Value = DateTime.Now;
        planilha.Cells[2, 3].Style.Numberformat.Format = "dd/mm/yyyy hh:mm";
        planilha.Cells[2, 4].Value = "PA";
        planilha.Cells[2, 5].Value = -3.1;
        planilha.Cells[2, 6].Value = -60.0;
        planilha.Cells[2, 7].Value = 250.5;
        planilha.Cells[2, 8].Value = 180.2;
        planilha.Cells[2, 9].Value = 72.5;

        planilha.Cells.AutoFitColumns();
        pasta.SaveAs(new FileInfo(caminhoArquivo));
    }
}
