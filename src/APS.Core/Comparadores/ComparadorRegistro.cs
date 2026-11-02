using APS.Core.Modelos;

namespace APS.Core.Comparadores;

/// <summary>
/// Compara registros do catálogo por um campo configurável, permitindo que os
/// mesmos algoritmos genéricos ordenem a "coleção de negócio" por qualquer critério.
/// </summary>
public class ComparadorRegistro : IComparer<RegistroImagemSatelite>
{
    private readonly CampoOrdenacao _campo;
    private readonly bool _decrescente;

    public ComparadorRegistro(CampoOrdenacao campo, bool decrescente = false)
    {
        _campo = campo;
        _decrescente = decrescente;
    }

    public int Compare(RegistroImagemSatelite? x, RegistroImagemSatelite? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return _decrescente ? 1 : -1;
        if (y is null) return _decrescente ? -1 : 1;

        int resultado = _campo switch
        {
            CampoOrdenacao.Id => x.Id.CompareTo(y.Id),
            CampoOrdenacao.DataHoraCaptura => x.DataHoraCaptura.CompareTo(y.DataHoraCaptura),
            CampoOrdenacao.TamanhoArquivoMB => x.TamanhoArquivoMB.CompareTo(y.TamanhoArquivoMB),
            CampoOrdenacao.IndiceAlerta => x.IndiceAlerta.CompareTo(y.IndiceAlerta),
            _ => throw new ArgumentOutOfRangeException(nameof(_campo), _campo, null)
        };

        return _decrescente ? -resultado : resultado;
    }
}
