namespace APS.Core.Modelos;

/// <summary>
/// Representa uma imagem de satélite catalogada para fiscalização ambiental.
/// </summary>
public class RegistroImagemSatelite
{
    public int Id { get; set; }
    public string CodigoSatelite { get; set; } = string.Empty;
    public DateTime DataHoraCaptura { get; set; }
    public string Uf { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double AreaKm2 { get; set; }
    public double TamanhoArquivoMB { get; set; }
    public double IndiceAlerta { get; set; }

    public override string ToString() =>
        $"#{Id} | {CodigoSatelite} | {DataHoraCaptura:dd/MM/yyyy HH:mm} | {Uf} | " +
        $"Lat {Latitude:F4}, Lon {Longitude:F4} | {AreaKm2:F1} km² | " +
        $"{TamanhoArquivoMB:F1} MB | Alerta {IndiceAlerta:F1}";
}
