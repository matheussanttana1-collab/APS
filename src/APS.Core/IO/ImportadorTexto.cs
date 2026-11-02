using System.Globalization;

namespace APS.Core.IO;

/// <summary>
/// Lê dados externos de um arquivo texto simples: um valor por linha, ou valores
/// separados por vírgula/ponto-e-vírgula/tabulação.
/// </summary>
public static class ImportadorTexto
{
    private static readonly char[] Separadores = { ',', ';', '\t' };

    public static double[] Importar(string caminhoArquivo)
    {
        var valores = new List<double>();

        foreach (var linha in File.ReadLines(caminhoArquivo))
        {
            if (string.IsNullOrWhiteSpace(linha)) continue;

            foreach (var parte in linha.Split(Separadores, StringSplitOptions.RemoveEmptyEntries))
            {
                if (double.TryParse(parte.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double valor))
                    valores.Add(valor);
            }
        }

        if (valores.Count == 0)
            throw new InvalidDataException("Nenhum valor numérico válido foi encontrado no arquivo.");

        return valores.ToArray();
    }
}
