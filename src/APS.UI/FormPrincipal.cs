using APS.UI.Paineis;

namespace APS.UI;

/// <summary>
/// Shell da aplicação: barra lateral com as 3 etapas (Dados → Execução →
/// Resultados), card com o conjunto atual, e a área de conteúdo que troca de
/// painel. Não contém nenhuma lógica de negócio — só orquestra quem escuta quem.
/// </summary>
public class FormPrincipal : Form
{
    private readonly PainelDados _painelDados;
    private readonly PainelExecucao _painelExecucao;
    private readonly PainelResultados _painelResultados;

    private readonly List<ItemEtapa> _itens = new();
    private readonly Label _lblConjuntoNumero;
    private readonly Label _lblConjuntoUnidade;
    private readonly Label _lblConjuntoOrigem;
    private readonly Label _lblConjuntoDistribuicao;
    private readonly Label _lblStatusBarra;

    private int _etapaAtual;

    public FormPrincipal()
    {
        Text = "APS · Geoprocessamento de Imagens de Satélite — Fiscalização Ambiental da Amazônia";
        ClientSize = new Size(1320, 860);
        MinimumSize = new Size(1100, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Tema.EstilizarFormulario(this);
        SuspendLayout();

        _painelDados = new PainelDados();
        _painelExecucao = new PainelExecucao();
        _painelResultados = new PainelResultados();

        var barraStatus = CriarBarraStatus(out _lblStatusBarra);

        var conteudo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fundo, Padding = new Padding(0) };
        conteudo.Controls.Add(_painelResultados);
        conteudo.Controls.Add(_painelExecucao);
        conteudo.Controls.Add(_painelDados);

        var barraLateral = CriarBarraLateral(out _lblConjuntoNumero, out _lblConjuntoUnidade, out _lblConjuntoOrigem, out _lblConjuntoDistribuicao);

        // Corpo = barra lateral + conteúdo, ocupando tudo acima da barra de status.
        // Dois níveis de Dock simples (um par por nível) evitam ambiguidade na ordem
        // de encaixe que surgiria com Left+Fill+Bottom todos no mesmo container.
        var corpo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fundo };
        corpo.Controls.Add(conteudo);
        corpo.Controls.Add(barraLateral);

        Controls.Add(corpo);
        Controls.Add(barraStatus);
        ResumeLayout(false);

        _painelDados.DadosAlterados += sessao =>
        {
            _painelExecucao.AtualizarSessao(sessao);
            AtualizarCardConjunto(sessao);
            _itens[0].AtualizarSubtitulo(sessao.TemDados
                ? $"{sessao.Tamanho:N0} imagens · {FormatarDistribuicao(sessao.TipoDistribuicaoAtual)}"
                : "Nenhum dado carregado ainda");
        };

        _painelExecucao.ResultadosProntos += resultado =>
        {
            _painelResultados.AtualizarResultados(resultado);
            _itens[1].AtualizarSubtitulo($"{resultado.Execucoes.Count} ordenações realizadas");
            _itens[2].AtualizarSubtitulo($"{resultado.Execucoes.Count} medições disponíveis");
        };
        _painelExecucao.SolicitouVerResultados += () => IrParaEtapa(2);

        IrParaEtapa(0);
    }

    private static string FormatarDistribuicao(Core.Modelos.TipoDistribuicao tipo) => tipo switch
    {
        Core.Modelos.TipoDistribuicao.Aleatorio => "aleatório",
        Core.Modelos.TipoDistribuicao.Ordenado => "ordenado",
        Core.Modelos.TipoDistribuicao.Invertido => "invertido",
        Core.Modelos.TipoDistribuicao.SemiOrdenado => "semi-ordenado",
        _ => tipo.ToString()
    };

    private void IrParaEtapa(int indice)
    {
        _etapaAtual = indice;
        _painelDados.Visible = indice == 0;
        _painelExecucao.Visible = indice == 1;
        _painelResultados.Visible = indice == 2;

        for (int i = 0; i < _itens.Count; i++)
            _itens[i].DefinirSelecionado(i == indice);

        _lblStatusBarra.Text = indice switch
        {
            0 => "Pronto para gerar ou importar dados.",
            1 => "Configure os algoritmos e execute o benchmark.",
            _ => "Resultados prontos para análise e exportação."
        };
    }

    private Panel CriarBarraLateral(out Label lblNumero, out Label lblUnidade, out Label lblOrigem, out Label lblDistribuicao)
    {
        var barra = new Panel { Dock = DockStyle.Left, Width = 272, BackColor = Tema.Superficie, Padding = new Padding(20, 24, 20, 16) };

        var eyebrow = Tema.Eyebrow("Fiscalização ambiental");
        eyebrow.Location = new Point(20, 24);

        var titulo = new Label
        {
            Text = "Geoprocessamento de imagens de satélite · Amazônia Legal",
            Font = Tema.FonteSubtitulo,
            ForeColor = Tema.Texto,
            Location = new Point(20, 44),
            AutoSize = true,
            MaximumSize = new Size(232, 0),
            BackColor = Color.Transparent
        };

        var etapas = new (string Titulo, string Subtitulo)[]
        {
            ("Dados", "Nenhum dado carregado ainda"),
            ("Execução", "Configure e execute os algoritmos"),
            ("Resultados", "Aguardando execução")
        };

        int y = titulo.Bottom + 20;
        for (int i = 0; i < etapas.Length; i++)
        {
            int indiceCapturado = i;
            var item = CriarItemEtapa(i + 1, etapas[i].Titulo, etapas[i].Subtitulo, () => IrParaEtapa(indiceCapturado));
            item.Painel.Location = new Point(12, y);
            barra.Controls.Add(item.Painel);
            _itens.Add(item);
            y += item.Painel.Height + 4;
        }

        var cardConjunto = Tema.Cartao();
        cardConjunto.Size = new Size(232, 128);
        cardConjunto.Location = new Point(20, y + 20);

        var eyebrowConjunto = Tema.Eyebrow("Conjunto atual");
        eyebrowConjunto.Location = new Point(14, 12);

        lblNumero = new Label
        {
            Text = "0", Font = Tema.FonteNumeroGrande, ForeColor = Tema.Texto,
            AutoSize = true, Location = new Point(14, 30), BackColor = Color.Transparent
        };
        lblUnidade = new Label
        {
            Text = "registros", Font = Tema.FonteCorpo, ForeColor = Tema.TextoSecundario,
            AutoSize = true, Location = new Point(14, 68), BackColor = Color.Transparent
        };
        lblOrigem = CriarTag("—");
        lblDistribuicao = CriarTag("—");
        var linhaTags = new FlowLayoutPanel
        {
            Location = new Point(14, 86), MaximumSize = new Size(204, 0), AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, BackColor = Color.Transparent
        };
        linhaTags.Controls.Add(lblOrigem);
        linhaTags.Controls.Add(lblDistribuicao);

        cardConjunto.Controls.AddRange(new Control[] { eyebrowConjunto, lblNumero, lblUnidade, linhaTags });

        var rodape = new Label
        {
            Text = "UNIP · APS 2026/2 · Estrutura de Dados",
            Font = Tema.FonteRotulo,
            ForeColor = Tema.TextoSecundario,
            Dock = DockStyle.Bottom,
            Height = 36,
            Padding = new Padding(20, 10, 0, 0),
            BackColor = Color.Transparent
        };

        barra.Controls.Add(rodape);
        barra.Controls.Add(eyebrow);
        barra.Controls.Add(titulo);
        barra.Controls.Add(cardConjunto);

        return barra;
    }

    private static Label CriarTag(string texto)
    {
        var lbl = new Label
        {
            Text = texto,
            Font = Tema.FonteRotulo,
            ForeColor = Tema.Acento,
            BackColor = Tema.SuperficieSelecionada,
            AutoSize = true,
            Padding = new Padding(7, 3, 7, 3)
        };
        Tema.Arredondar(lbl, 4);
        return lbl;
    }

    private void AtualizarCardConjunto(SessaoDados sessao)
    {
        _lblConjuntoNumero.Text = sessao.Tamanho.ToString("N0");
        _lblConjuntoUnidade.Text = "imagens catalogadas";
        _lblConjuntoOrigem.Text = FormatarDistribuicao(sessao.TipoDistribuicaoAtual);
        _lblConjuntoDistribuicao.Text = "por " + FormatarCampo(sessao.CampoAtual);
    }

    private static string FormatarCampo(Core.Modelos.CampoOrdenacao campo) => campo switch
    {
        Core.Modelos.CampoOrdenacao.Id => "Id",
        Core.Modelos.CampoOrdenacao.DataHoraCaptura => "Data",
        Core.Modelos.CampoOrdenacao.TamanhoArquivoMB => "Tamanho",
        Core.Modelos.CampoOrdenacao.IndiceAlerta => "Índice",
        _ => campo.ToString()
    };

    private ItemEtapa CriarItemEtapa(int numero, string titulo, string subtitulo, Action aoClicar)
    {
        var painel = new Panel { Size = new Size(232, 72), Cursor = Cursors.Hand, BackColor = Color.Transparent };
        Tema.Arredondar(painel, 8);

        var circulo = new Label
        {
            Text = numero.ToString(),
            Size = new Size(26, 26),
            Location = new Point(10, 10),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Tema.FonteCorpoNegrito,
            BackColor = Tema.SuperficieAlta,
            ForeColor = Tema.TextoSecundario
        };
        Tema.Arredondar(circulo, 13);

        var lblTitulo = new Label
        {
            Text = titulo, Font = Tema.FonteSubtitulo, ForeColor = Tema.Texto,
            AutoSize = true, Location = new Point(46, 8), BackColor = Color.Transparent
        };
        var lblSubtitulo = new Label
        {
            Text = subtitulo, Font = Tema.FonteCorpo, ForeColor = Tema.TextoSecundario,
            AutoSize = true, MaximumSize = new Size(176, 0), Location = new Point(46, 28), BackColor = Color.Transparent
        };

        painel.Controls.AddRange(new Control[] { circulo, lblTitulo, lblSubtitulo });

        void ClickEmTudo(Control c)
        {
            c.Click += (_, _) => aoClicar();
            c.Cursor = Cursors.Hand;
            foreach (Control filho in c.Controls) ClickEmTudo(filho);
        }
        ClickEmTudo(painel);

        return new ItemEtapa { Painel = painel, Circulo = circulo, Titulo = lblTitulo, Subtitulo = lblSubtitulo };
    }

    private Panel CriarBarraStatus(out Label lblStatus)
    {
        var barra = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Tema.Superficie };

        lblStatus = new Label
        {
            Text = "Pronto.", Font = Tema.FonteRotulo, ForeColor = Tema.TextoSecundario,
            Dock = DockStyle.Left, AutoSize = true, Padding = new Padding(14, 7, 0, 0),
            BackColor = Color.Transparent
        };

        var lblInfo = new Label
        {
            Text = "Cronômetro: Stopwatch · somente Ordenar()      ·      .NET 10 · Windows Forms · ClosedXML",
            Font = Tema.FonteRotulo, ForeColor = Tema.TextoSecundario,
            Dock = DockStyle.Right, AutoSize = true, Padding = new Padding(0, 7, 14, 0),
            BackColor = Color.Transparent
        };

        // Dois docks simples (Left + Right) dentro da mesma barra: sem Fill no meio,
        // a ordem de adição não importa para nenhum dos dois.
        barra.Controls.Add(lblStatus);
        barra.Controls.Add(lblInfo);
        return barra;
    }

    private sealed class ItemEtapa
    {
        public required Panel Painel { get; init; }
        public required Label Circulo { get; init; }
        public required Label Titulo { get; init; }
        public required Label Subtitulo { get; init; }

        public void DefinirSelecionado(bool selecionado)
        {
            Painel.BackColor = selecionado ? Tema.SuperficieSelecionada : Color.Transparent;
            Circulo.BackColor = selecionado ? Tema.Acento : Tema.SuperficieAlta;
            Circulo.ForeColor = selecionado ? Tema.TextoSobreAcento : Tema.TextoSecundario;
            Titulo.ForeColor = selecionado ? Tema.Texto : Tema.TextoSecundario;
        }

        public void AtualizarSubtitulo(string texto) => Subtitulo.Text = texto;
    }
}
