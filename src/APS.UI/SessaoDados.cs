using APS.Core.Modelos;

namespace APS.UI;

/// <summary>
/// Estado do catálogo atualmente carregado na tela (gerado, digitado ou
/// importado), compartilhado entre os painéis. É só um "porta-objetos" para a UI
/// orquestrar — nenhuma lógica de geração/ordenação/relatório mora aqui.
/// </summary>
public class SessaoDados
{
    public RegistroImagemSatelite[]? Registros { get; private set; }
    public TipoDistribuicao TipoDistribuicaoAtual { get; private set; } = TipoDistribuicao.Aleatorio;
    public CampoOrdenacao CampoAtual { get; set; } = CampoOrdenacao.IndiceAlerta;

    public bool TemDados => Registros is not null;
    public int Tamanho => Registros?.Length ?? 0;

    public void Definir(RegistroImagemSatelite[] registros, TipoDistribuicao tipo, CampoOrdenacao campo)
    {
        Registros = registros;
        TipoDistribuicaoAtual = tipo;
        CampoAtual = campo;
    }
}
