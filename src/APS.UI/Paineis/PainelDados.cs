using System.Data;
using System.Globalization;
using APS.Core.Geracao;
using APS.Core.IO;
using APS.Core.Modelos;

namespace APS.UI.Paineis;

/// <summary>
/// Painel "Dados": coleta os parâmetros do usuário (tamanho, semente, distribuição,
/// arquivo) e repassa para os serviços do APS.Core; só exibe o que volta. Gerar,
/// importar e exportar dados não têm nenhuma lógica própria aqui — tudo chama o
/// Core. O catálogo simula imagens de satélite usadas para priorizar fiscalização
/// de desmatamento/crimes ambientais na Amazônia Legal.
/// </summary>
public class PainelDados : UserControl
{
    public event Action<SessaoDados>? DadosAlterados;

    private readonly SessaoDados _sessao = new();
    private readonly GeradorDados _gerador = new();

    private readonly GrupoSegmentado _seletorFonte = new();
    private readonly GrupoSegmentado _seletorDistribuicao = new();
    private readonly GrupoSegmentado _seletorCampo = new();

    private readonly Panel[] _subPaineisFonte = new Panel[4];
    private Label _lblToast = null!;
    private DataGridView _dgvPreview = null!;
    private Label _lblPreviewSubtitulo = null!;

    public PainelDados()
    {
        SuspendLayout();
        Dock = DockStyle.Fill;
        BackColor = Tema.Fundo;

        Controls.Add(CriarCorpo());
        Controls.Add(CriarCabecalho());
        ResumeLayout(false);

        _seletorFonte.Mudou += AlternarFonte;
        _seletorCampo.Mudou += _ => AtualizarPreview();

        AlternarFonte(0);
    }

    public SessaoDados Sessao => _sessao;

    // ---------------------------------------------------------------- Cabeçalho

    private Panel CriarCabecalho()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 150, BackColor = Tema.Fundo, Padding = new Padding(32, 20, 32, 0) };

        var eyebrow = Tema.Eyebrow("Etapa 1");
        eyebrow.Location = new Point(32, 20);

        var titulo = Tema.Titulo("Dados de entrada");
        titulo.Location = new Point(32, 40);

        var descricao = Tema.Descricao(
            "Gere, digite ou importe o catálogo de imagens de satélite que será ordenado. " +
            "A preparação dos dados nunca entra no cronômetro — só o algoritmo de ordenação é medido.",
            760);
        descricao.Location = new Point(32, 82);

        var seletor = _seletorFonte.Construir(new[] { "Gerar aleatório", "Digitar", "Arquivo .txt/.csv", "Planilha .xlsx" }, larguraMaxima: 820);
        seletor.Location = new Point(32, 118);

        header.Controls.Add(eyebrow);
        header.Controls.Add(titulo);
        header.Controls.Add(descricao);
        header.Controls.Add(seletor);
        return header;
    }

    // --------------------------------------------------------------------- Corpo

    private Panel CriarCorpo()
    {
        var corpo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fundo, Padding = new Padding(32, 8, 32, 24) };

        var slotEsquerda = new Panel { Dock = DockStyle.Left, Width = 380, BackColor = Tema.Fundo, Padding = new Padding(0, 0, 20, 0) };
        slotEsquerda.Controls.Add(CriarCardConfiguracao());

        var slotDireita = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fundo };
        slotDireita.Controls.Add(CriarCardPreview());

        corpo.Controls.Add(slotDireita);
        corpo.Controls.Add(slotEsquerda);
        return corpo;
    }

    private Panel CriarCardConfiguracao()
    {
        var card = Tema.Cartao();
        card.Dock = DockStyle.Top;
        card.Height = 430;
        card.Padding = new Padding(20);

        var areaFonte = new Panel { Location = new Point(20, 20), Size = new Size(320, 236), BackColor = Color.Transparent };
        _subPaineisFonte[0] = CriarSubGerar();
        _subPaineisFonte[1] = CriarSubDigitar();
        _subPaineisFonte[2] = CriarSubArquivo();
        _subPaineisFonte[3] = CriarSubPlanilha();
        foreach (var sub in _subPaineisFonte)
        {
            sub.Location = Point.Empty;
            sub.Size = areaFonte.Size;
            areaFonte.Controls.Add(sub);
        }

        var lblCampo = Tema.Rotulo("Campo de ordenação");
        lblCampo.Location = new Point(20, 268);

        var seletorCampo = _seletorCampo.Construir(new[] { "Id", "Data", "Tamanho", "Índice" }, indiceInicial: 3, larguraMaxima: 320);
        seletorCampo.Location = new Point(20, 288);

        _lblToast = new Label
        {
            Location = new Point(20, 340), Size = new Size(320, 60),
            Font = Tema.FonteCorpo, ForeColor = Tema.Acento, BackColor = Color.Transparent
        };

        card.Controls.Add(areaFonte);
        card.Controls.Add(lblCampo);
        card.Controls.Add(seletorCampo);
        card.Controls.Add(_lblToast);
        return card;
    }

    private Panel CriarCardPreview()
    {
        var card = Tema.Cartao();
        card.Dock = DockStyle.Fill;

        var cabecalho = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.Transparent, Padding = new Padding(20, 16, 20, 0) };
        var titulo = new Label
        {
            Text = "Pré-visualização", Font = Tema.FonteSubtitulo, ForeColor = Tema.Texto,
            AutoSize = true, Location = new Point(20, 14), BackColor = Color.Transparent
        };
        _lblPreviewSubtitulo = new Label
        {
            Text = "Nenhum dado carregado ainda", Font = Tema.FonteCorpo, ForeColor = Tema.TextoSecundario,
            AutoSize = true, Location = new Point(titulo.Right + 32, 17), BackColor = Color.Transparent
        };
        cabecalho.Controls.Add(titulo);
        cabecalho.Controls.Add(_lblPreviewSubtitulo);

        var areaGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 20, 20), BackColor = Color.Transparent };
        _dgvPreview = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToOrderColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToResizeColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        Tema.EstilizarGrid(_dgvPreview);
        areaGrid.Controls.Add(_dgvPreview);

        card.Controls.Add(areaGrid);
        card.Controls.Add(cabecalho);
        return card;
    }

    // ---------------------------------------------------------- Sub-painéis: fonte

    private Panel CriarSubGerar()
    {
        var p = new Panel { BackColor = Color.Transparent };

        var lblQtd = Tema.Rotulo("Quantidade");
        lblQtd.Location = new Point(0, 0);
        var numQtd = new NumericUpDown
        {
            Location = new Point(0, 20), Size = new Size(150, 26), Minimum = 1, Maximum = 200_000, Value = 1000,
            ThousandsSeparator = true
        };
        Tema.EstilizarNumerico(numQtd);

        var lblSemente = Tema.Rotulo("Semente (opcional)");
        lblSemente.Location = new Point(166, 0);
        var numSemente = new NumericUpDown
        {
            Location = new Point(166, 20), Size = new Size(154, 26), Minimum = 0, Maximum = int.MaxValue, Value = 2026
        };
        Tema.EstilizarNumerico(numSemente);

        var lblDist = Tema.Rotulo("Distribuição");
        lblDist.Location = new Point(0, 58);
        var seletorDist = _seletorDistribuicao.Construir(new[] { "Aleatório", "Ordenado", "Invertido", "Semi-ordenado" }, larguraMaxima: 320);
        seletorDist.Location = new Point(0, 78);

        var btnGerar = Tema.BotaoPrimario("Gerar registros");
        btnGerar.Location = new Point(0, 166);
        btnGerar.Width = 320;
        btnGerar.Click += (_, _) =>
        {
            int tamanho = (int)numQtd.Value;
            int semente = (int)numSemente.Value;
            var distribuicao = (TipoDistribuicao)_seletorDistribuicao.Selecionado;
            var campo = (CampoOrdenacao)_seletorCampo.Selecionado;

            var registros = _gerador.GerarRegistros(tamanho, distribuicao, campo, semente);
            _sessao.Definir(registros, distribuicao, campo);
            MostrarToast($"{tamanho:N0} registros gerados ({NomeDistribuicao(distribuicao).ToLowerInvariant()}, semente {semente}).");
            NotificarAlteracao();
        };

        p.Controls.AddRange(new Control[] { lblQtd, numQtd, lblSemente, numSemente, lblDist, seletorDist, btnGerar });
        return p;
    }

    private Panel CriarSubDigitar()
    {
        var p = new Panel { BackColor = Color.Transparent };

        var lbl = Tema.Rotulo("Valores do Índice de Alerta (um por linha)");
        lbl.Location = new Point(0, 0);
        lbl.MaximumSize = new Size(320, 0);

        var txt = new TextBox
        {
            Location = new Point(0, 22), Size = new Size(320, 130),
            Multiline = true, ScrollBars = ScrollBars.Vertical, Font = Tema.FonteMono
        };
        Tema.EstilizarCampoTexto(txt);

        var lblContagem = new Label
        {
            Text = "0 valores reconhecidos", Font = Tema.FonteRotulo, ForeColor = Tema.TextoSecundario,
            Location = new Point(0, 158), AutoSize = true, BackColor = Color.Transparent
        };

        txt.TextChanged += (_, _) => lblContagem.Text = $"{ContarValores(txt.Text)} valores reconhecidos";

        var btnCarregar = Tema.BotaoPrimario("Carregar valores");
        btnCarregar.Location = new Point(0, 182);
        btnCarregar.Width = 320;
        btnCarregar.Click += (_, _) =>
        {
            var valores = ExtrairValores(txt.Text);
            if (valores.Count == 0)
            {
                MessageBox.Show(this, "Digite ao menos um valor numérico válido, um por linha ou separado por vírgula.",
                    "Entrada manual", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var campo = (CampoOrdenacao)_seletorCampo.Selecionado;
            var registros = _gerador.GerarRegistros(valores.Count, TipoDistribuicao.Aleatorio, campo);
            for (int i = 0; i < valores.Count; i++)
                registros[i].IndiceAlerta = valores[i];

            _sessao.Definir(registros, TipoDistribuicao.Aleatorio, campo);
            MostrarToast($"{valores.Count} valores carregados no Índice de Alerta.");
            NotificarAlteracao();
        };

        p.Controls.AddRange(new Control[] { lbl, txt, lblContagem, btnCarregar });
        return p;
    }

    private Panel CriarSubArquivo()
    {
        var p = new Panel { BackColor = Color.Transparent };

        var lblConfirmacao = new Label { Location = new Point(0, 118), Size = new Size(320, 20), Font = Tema.FonteRotulo, ForeColor = Tema.Acento, BackColor = Color.Transparent };

        var (zona, abrirDialogo) = CriarZonaArquivo(
            "Arraste um arquivo .txt/.csv",
            "Um valor por linha. Vira o Índice de Alerta.",
            "Arquivos de texto (*.txt;*.csv)|*.txt;*.csv|Todos os arquivos (*.*)|*.*",
            caminho =>
            {
                try
                {
                    var valores = ImportadorTexto.Importar(caminho);
                    var campo = (CampoOrdenacao)_seletorCampo.Selecionado;
                    var registros = _gerador.GerarRegistros(valores.Length, TipoDistribuicao.Aleatorio, campo);
                    for (int i = 0; i < valores.Length; i++)
                        registros[i].IndiceAlerta = valores[i];

                    _sessao.Definir(registros, TipoDistribuicao.Aleatorio, campo);
                    lblConfirmacao.Text = $"✓ {Path.GetFileName(caminho)} — {valores.Length} linhas";
                    MostrarToast($"{valores.Length} valores importados de {Path.GetFileName(caminho)}.");
                    NotificarAlteracao();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Não foi possível importar o arquivo:\n{ex.Message}", "Erro na importação",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        zona.Location = new Point(0, 0);
        zona.Size = new Size(320, 108);

        var btnProcurar = Tema.BotaoSecundario("Procurar arquivo...");
        btnProcurar.Location = new Point(0, 146);
        btnProcurar.Width = 320;
        btnProcurar.Click += (_, _) => abrirDialogo();

        p.Controls.AddRange(new Control[] { zona, lblConfirmacao, btnProcurar });
        return p;
    }

    private Panel CriarSubPlanilha()
    {
        var p = new Panel { BackColor = Color.Transparent };

        var lblConfirmacao = new Label { Location = new Point(0, 118), Size = new Size(320, 20), Font = Tema.FonteRotulo, ForeColor = Tema.Acento, BackColor = Color.Transparent };

        var (zona, abrirDialogo) = CriarZonaArquivo(
            "Arraste uma planilha .xlsx",
            "Use \"Gerar modelo\" abaixo para ver as colunas certas.",
            "Planilhas Excel (*.xlsx)|*.xlsx",
            caminho =>
            {
                try
                {
                    var registros = ImportadorPlanilha.ImportarRegistros(caminho);
                    var campo = (CampoOrdenacao)_seletorCampo.Selecionado;
                    _sessao.Definir(registros, TipoDistribuicao.Aleatorio, campo);
                    lblConfirmacao.Text = $"✓ {Path.GetFileName(caminho)} — {registros.Length} registros";
                    MostrarToast($"{registros.Length} registros importados de {Path.GetFileName(caminho)}.");
                    NotificarAlteracao();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Não foi possível importar a planilha:\n{ex.Message}", "Erro na importação",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        zona.Location = new Point(0, 0);
        zona.Size = new Size(320, 108);

        var btnProcurar = Tema.BotaoPrimario("Procurar planilha...");
        btnProcurar.Location = new Point(0, 146);
        btnProcurar.Width = 320;
        btnProcurar.Click += (_, _) => abrirDialogo();

        var btnModelo = Tema.BotaoSecundario("Gerar modelo de planilha");
        btnModelo.Location = new Point(0, 186);
        btnModelo.Width = 320;
        btnModelo.Click += (_, _) =>
        {
            using var dialogo = new SaveFileDialog { Filter = "Planilha Excel (*.xlsx)|*.xlsx", FileName = "modelo-catalogo-satelite.xlsx" };
            if (dialogo.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                ImportadorPlanilha.GerarModeloRegistros(dialogo.FileName);
                MostrarToast($"Modelo salvo em {Path.GetFileName(dialogo.FileName)}.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Não foi possível gerar o modelo:\n{ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        p.Controls.AddRange(new Control[] { zona, lblConfirmacao, btnProcurar, btnModelo });
        return p;
    }

    private (Panel Zona, Action AbrirDialogo) CriarZonaArquivo(string textoPrincipal, string dica, string filtro, Action<string> aoEscolherArquivo)
    {
        var zona = new Panel { BackColor = Tema.SuperficieAlta, Cursor = Cursors.Hand, AllowDrop = true };
        Tema.Arredondar(zona, 10);
        Tema.AdicionarBordaTracejada(zona, Tema.Borda);

        var lblTexto = new Label
        {
            Text = textoPrincipal, Font = Tema.FonteCorpoNegrito, ForeColor = Tema.Texto,
            AutoSize = true, MaximumSize = new Size(284, 0), Location = new Point(16, 30), BackColor = Color.Transparent
        };
        var lblDica = new Label
        {
            Text = dica, Font = Tema.FonteRotulo, ForeColor = Tema.TextoSecundario,
            AutoSize = true, MaximumSize = new Size(284, 0), Location = new Point(16, 54), BackColor = Color.Transparent
        };

        zona.Controls.Add(lblTexto);
        zona.Controls.Add(lblDica);

        void AbrirDialogo()
        {
            using var dialogo = new OpenFileDialog { Filter = filtro };
            if (dialogo.ShowDialog(this) == DialogResult.OK) aoEscolherArquivo(dialogo.FileName);
        }

        zona.Click += (_, _) => AbrirDialogo();
        lblTexto.Click += (_, _) => AbrirDialogo();
        lblDica.Click += (_, _) => AbrirDialogo();

        zona.DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy;
        };
        zona.DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } arquivos)
                aoEscolherArquivo(arquivos[0]);
        };

        return (zona, AbrirDialogo);
    }

    // -------------------------------------------------------------------- Lógica

    private void AlternarFonte(int indice)
    {
        for (int i = 0; i < _subPaineisFonte.Length; i++)
            _subPaineisFonte[i].Visible = i == indice;
    }

    private static int ContarValores(string texto) => ExtrairValores(texto).Count;

    private static List<double> ExtrairValores(string texto)
    {
        var valores = new List<double>();
        foreach (var parte in texto.Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (double.TryParse(parte.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double valor))
                valores.Add(valor);
        }
        return valores;
    }

    private static string NomeDistribuicao(TipoDistribuicao tipo) => tipo switch
    {
        TipoDistribuicao.Aleatorio => "Aleatório",
        TipoDistribuicao.Ordenado => "Ordenado",
        TipoDistribuicao.Invertido => "Invertido",
        TipoDistribuicao.SemiOrdenado => "Semi-ordenado",
        _ => tipo.ToString()
    };

    private static string NomeColunaCampo(CampoOrdenacao campo) => campo switch
    {
        CampoOrdenacao.Id => "Id",
        CampoOrdenacao.DataHoraCaptura => "Data captura",
        CampoOrdenacao.TamanhoArquivoMB => "Tamanho (MB)",
        CampoOrdenacao.IndiceAlerta => "Índice alerta",
        _ => ""
    };

    private void MostrarToast(string mensagem) => _lblToast.Text = "✓  " + mensagem;

    private void NotificarAlteracao()
    {
        AtualizarPreview();
        DadosAlterados?.Invoke(_sessao);
    }

    private void AtualizarPreview()
    {
        const int maxLinhas = 500;

        if (_sessao.Registros is not { } registros)
        {
            _dgvPreview.DataSource = null;
            return;
        }

        var tabela = new DataTable();
        tabela.Columns.Add("Id", typeof(int));
        tabela.Columns.Add("Satélite", typeof(string));
        tabela.Columns.Add("Data captura", typeof(string));
        tabela.Columns.Add("UF", typeof(string));
        tabela.Columns.Add("Tamanho (MB)", typeof(string));
        tabela.Columns.Add("Índice alerta", typeof(string));

        foreach (var r in registros.Take(maxLinhas))
        {
            tabela.Rows.Add(r.Id, r.CodigoSatelite, r.DataHoraCaptura.ToString("dd/MM/yyyy"), r.Uf,
                r.TamanhoArquivoMB.ToString("N1", CultureInfo.GetCultureInfo("pt-BR")),
                r.IndiceAlerta.ToString("N1", CultureInfo.GetCultureInfo("pt-BR")));
        }

        _dgvPreview.DataSource = tabela;

        string nomeColunaAtiva = NomeColunaCampo(_sessao.CampoAtual);
        foreach (DataGridViewColumn coluna in _dgvPreview.Columns)
            coluna.HeaderCell.Style.ForeColor = coluna.Name == nomeColunaAtiva ? Tema.Acento : Tema.TextoSecundario;

        _lblPreviewSubtitulo.Text = registros.Length > maxLinhas
            ? $"primeiros {maxLinhas} de {registros.Length:N0} · ordenado por {nomeColunaAtiva}"
            : $"{registros.Length:N0} registros · ordenado por {nomeColunaAtiva}";
    }
}
