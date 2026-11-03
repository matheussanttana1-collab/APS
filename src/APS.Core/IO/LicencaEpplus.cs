using OfficeOpenXml;

namespace APS.Core.IO;

/// <summary>
/// A partir da versão 8, o EPPlus exige que a licença seja definida em código antes
/// do primeiro uso (substituiu a antiga propriedade LicenseContext). Este projeto é
/// uso não comercial (trabalho acadêmico), então usamos a licença gratuita
/// correspondente — chamado uma única vez, de forma idempotente.
/// </summary>
internal static class LicencaEpplus
{
    private static bool _definida;

    public static void Garantir()
    {
        if (_definida) return;
        ExcelPackage.License.SetNonCommercialPersonal("Projeto APS - UNIP");
        _definida = true;
    }
}
