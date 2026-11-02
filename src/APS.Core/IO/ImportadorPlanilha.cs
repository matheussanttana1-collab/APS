using APS.Core.Modelos;
using ClosedXML.Excel;

namespace APS.Core.IO;

/// <summary>
/// Lê/gera o catálogo de imagens de satélite em planilha (.xlsx) via ClosedXML.
/// </summary>
public static class ImportadorPlanilha
{
    public static RegistroImagemSatelite[] ImportarRegistros(string caminhoArquivo)
    {
        using var pasta = new XLWorkbook(caminhoArquivo);
        var planilha = pasta.Worksheets.First();
        var registros = new List<RegistroImagemSatelite>();

        foreach (var linha in planilha.RowsUsed().Skip(1))
        {
            registros.Add(new RegistroImagemSatelite
            {
                Id = linha.Cell(1).GetValue<int>(),
                CodigoSatelite = linha.Cell(2).GetString(),
                DataHoraCaptura = linha.Cell(3).GetDateTime(),
                Uf = linha.Cell(4).GetString(),
                Latitude = linha.Cell(5).GetValue<double>(),
                Longitude = linha.Cell(6).GetValue<double>(),
                AreaKm2 = linha.Cell(7).GetValue<double>(),
                TamanhoArquivoMB = linha.Cell(8).GetValue<double>(),
                IndiceAlerta = linha.Cell(9).GetValue<double>()
            });
        }

        if (registros.Count == 0)
            throw new InvalidDataException("Nenhum registro válido foi encontrado na planilha.");

        return registros.ToArray();
    }

    public static void GerarModeloRegistros(string caminhoArquivo)
    {
        using var pasta = new XLWorkbook();
        var planilha = pasta.Worksheets.Add("Catalogo");
        string[] cabecalhos =
        {
            "Id", "CodigoSatelite", "DataHoraCaptura", "Uf", "Latitude",
            "Longitude", "AreaKm2", "TamanhoArquivoMB", "IndiceAlerta"
        };

        for (int i = 0; i < cabecalhos.Length; i++)
            planilha.Cell(1, i + 1).Value = cabecalhos[i];

        planilha.Cell(2, 1).Value = 1;
        planilha.Cell(2, 2).Value = "CBERS-4A";
        planilha.Cell(2, 3).Value = DateTime.Now;
        planilha.Cell(2, 4).Value = "PA";
        planilha.Cell(2, 5).Value = -3.1;
        planilha.Cell(2, 6).Value = -60.0;
        planilha.Cell(2, 7).Value = 250.5;
        planilha.Cell(2, 8).Value = 180.2;
        planilha.Cell(2, 9).Value = 72.5;

        planilha.Columns().AdjustToContents();
        pasta.SaveAs(caminhoArquivo);
    }
}
