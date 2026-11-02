using APS.Core.Algoritmos;
using APS.Core.Benchmark;
using APS.Core.Comparadores;
using APS.Core.Modelos;

namespace APS.UI.Paineis;

/// <summary>
/// Painel "Execução": escolhe algoritmos e parâmetros, dispara o trabalho em uma
/// Task em segundo plano e repassa o progresso para a barra. Toda a iteração pela
/// matriz de testes, o cronômetro e o cálculo de métricas ficam no APS.Core — este
/// painel só chama <see cref="SuiteBenchmark"/>/<see cref="ExecutorBenchmark"/>.
/// </summary>
public class PainelExecucao : UserControl
{
    public event Action<ResultadoParaExibicao>? ResultadosProntos;
    public event Action? SolicitouVerResultados;

    private static readonly (IAlgoritmoOrdenacao Algoritmo, string Complexidade, string Descricao, Color Cor)[] InfoAlgoritmos =
    {
        (new BubbleSort(), "O(n²)", "Troca vizinhos repetidamente; referência de pior caso.", Tema.CorBubble),
        (new QuickSort(), "O(n log n) médio", "Particiona com pivô aleatório; rápido na prática.", Tema.CorQuick),
        (new HeapSort(), "O(n log n)", "Heap máximo; tempo estável em qualquer cenário.", Tema.CorHeap),
        (new ShellSort(), "≈ O(n^1,3)", "Inserção com saltos (gaps) decrescentes.", Tema.CorShell),
    };

    private static readonly int[] TamanhosDisponiveis = { 1_000, 5_000, 10_000, 50_000 };

    private readonly List<(IAlgoritmoOrdenacao Algoritmo, Func<bool> Selecionado)> _cardsAlgoritmo = new();
    private readonly List<(int Tamanho, Func<bool> Selecionado)> _chipsTamanho = new();
    private readonly List<(TipoDistribuicao Tipo, Func<bool> Selecionado)> _chipsDistribuicao = new();
    private readonly GrupoSegmentado _seletorRepeticoes = new();

    private SessaoDados? _sessaoAtual;
    private CancellationTokenSource? _cts;

    private Label _lblDatasetAtual = null!;
    private Button _btnExecutarUnico = null!;
    private Label _lblAviso = null!;
    private Label _lblTotalRuns = null!;
    private Label _lblFormula = null!;
    private Button _btnExecutarSuite = null!;
    private Button _btnCancelar = null!;
    private Button _btnVerResultados = null!;
    private ProgressBar _progressBar = null!;
    private Label _lblStatusAtual = null!;
    private Label _lblDoneTotal = null!;
    private FlowLayoutPanel _painelLog = null!;

    public PainelExecucao()
    {
        SuspendLayout();
        Dock = DockStyle.Fill;
        BackColor = Tema.Fundo;

        Controls.Add(CriarCorpo());
        Controls.Add(CriarCabecalho());
        ResumeLayout(false);

        AtualizarSessao(null);
        AtualizarMatriz();
        AdicionarLog("pronto para executar", Tema.TextoSecundario);
    }

    public void AtualizarSessao(SessaoDados? sessao)
    {
        _sessaoAtual = sessao;
        _lblDatasetAtual.Text = (sessao is { TemDados: true })
            ? $"{sessao.Tamanho:N0} imagens catalogadas carregadas na aba Dados ({sessao.TipoDistribuicaoAtual})."
            : "Nenhum catálogo carregado — gere ou importe dados na aba Dados primeiro.";
        _btnExecutarUnico.Enabled = sessao is { TemDados: true };
    }

    // ---------------------------------------------------------------- Cabeçalho

    private Panel CriarCabecalho()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 118, BackColor = Tema.Fundo, Padding = new Padding(32, 20, 32, 0) };

        var eyebrow = Tema.Eyebrow("Etapa 2");
        eyebrow.Location = new Point(32, 20);

        var titulo = Tema.Titulo("Execução do benchmark");
        titulo.Location = new Point(32, 40);

        var descricao = Tema.Descricao(
            "Cada combinação gera um vetor novo, faz uma cópia para cada algoritmo e cronometra apenas a chamada Ordenar() — a geração dos dados nunca entra no tempo medido.",
            820);
        descricao.Location = new Point(32, 82);

        header.Controls.Add(eyebrow);
        header.Controls.Add(titulo);
        header.Controls.Add(descricao);
        return header;
    }

    // --------------------------------------------------------------------- Corpo

    private Panel CriarCorpo()
    {
        var corpo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fundo, Padding = new Padding(32, 8, 32, 24) };

        var slotDireita = new Panel { Dock = DockStyle.Right, Width = 380, BackColor = Tema.Fundo, Padding = new Padding(20, 0, 0, 0) };
        slotDireita.Controls.Add(CriarCardMatriz());

        var slotEsquerda = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fundo, AutoScroll = true };
        slotEsquerda.Controls.Add(CriarColunaEsquerda());

        corpo.Controls.Add(slotEsquerda);
        corpo.Controls.Add(slotDireita);
        return corpo;
    }

    private TableLayoutPanel CriarColunaEsquerda()
    {
        var tabela = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, BackColor = Tema.Fundo, AutoSize = false };
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 246));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        tabela.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var secUnico = CriarSecaoExecucaoUnica();
        secUnico.Dock = DockStyle.Fill;
        var secAlgoritmos = CriarSecaoAlgoritmos();
        secAlgoritmos.Dock = DockStyle.Fill;
        var secTamanhos = CriarSecaoTamanhos();
        secTamanhos.Dock = DockStyle.Fill;
        var secDistRep = CriarSecaoDistribuicaoRepeticoes();
        secDistRep.Dock = DockStyle.Fill;

        tabela.Controls.Add(secUnico, 0, 0);
        tabela.Controls.Add(secAlgoritmos, 0, 1);
        tabela.Controls.Add(secTamanhos, 0, 2);
        tabela.Controls.Add(secDistRep, 0, 3);
        return tabela;
    }

    private Panel CriarSecaoExecucaoUnica()
    {
        var card = Tema.Cartao();
        card.Padding = new Padding(18);

        var eyebrow = Tema.Eyebrow("Execução única");
        eyebrow.Location = new Point(18, 14);

        _lblDatasetAtual = new Label
        {
            Location = new Point(18, 34), Size = new Size(560, 36),
            Font = Tema.FonteCorpo, ForeColor = Tema.TextoSecundario, BackColor = Color.Transparent
        };

        _btnExecutarUnico = Tema.BotaoSecundario("Executar no dataset atual");
        _btnExecutarUnico.Location = new Point(18, 70);
        _btnExecutarUnico.Width = 230;
        _btnExecutarUnico.Click += async (_, _) => await ExecutarUnicoAsync();

        card.Controls.Add(eyebrow);
        card.Controls.Add(_lblDatasetAtual);
        card.Controls.Add(_btnExecutarUnico);
        return card;
    }

    private Panel CriarSecaoAlgoritmos()
    {
        var p = new Panel { BackColor = Color.Transparent };
        var lbl = Tema.Rotulo("Algoritmos (clique para marcar/desmarcar)");
        lbl.Location = new Point(0, 0);
        p.Controls.Add(lbl);

        int largura = 154, altura = 92, espaco = 10;
        for (int i = 0; i < InfoAlgoritmos.Length; i++)
        {
            int coluna = i % 2, linha = i / 2;
            var card = CriarCardAlgoritmo(InfoAlgoritmos[i]);
            card.Location = new Point(coluna * (largura + espaco), 26 + linha * (altura + espaco));
            card.Size = new Size(largura, altura);
            p.Controls.Add(card);
        }
        return p;
    }

    private Panel CriarCardAlgoritmo((IAlgoritmoOrdenacao Algoritmo, string Complexidade, string Descricao, Color Cor) info)
    {
        var card = new Panel { BackColor = Tema.SuperficieSelecionada, Cursor = Cursors.Hand };
        Tema.Arredondar(card, 10);
        Tema.AdicionarBorda(card, Tema.Borda);

        var linha = new Panel { Location = new Point(14, 14), Size = new Size(22, 4), BackColor = info.Cor };
        var lblNome = new Label
        {
            Text = info.Algoritmo.Nome, Font = Tema.FonteCorpoNegrito, ForeColor = Tema.Texto,
            AutoSize = true, Location = new Point(14, 24), BackColor = Color.Transparent
        };
        var lblCheck = new Label
        {
            Text = "✓", Font = Tema.FonteCorpoNegrito, ForeColor = Tema.Acento,
            AutoSize = true, Location = new Point(130, 12), BackColor = Color.Transparent
        };
        var lblDescricao = new Label
        {
            Text = info.Descricao, Font = Tema.FonteRotulo, ForeColor = Tema.TextoSecundario,
            Location = new Point(14, 46), Size = new Size(128, 28), BackColor = Color.Transparent
        };
        var lblComplexidade = new Label
        {
            Text = info.Complexidade, Font = Tema.FonteRotulo, ForeColor = info.Cor,
            AutoSize = true, Location = new Point(14, 72), BackColor = Color.Transparent
        };

        card.Controls.AddRange(new Control[] { linha, lblNome, lblCheck, lblDescricao, lblComplexidade });

        // Estado de seleção fica numa variável própria, não em lblCheck.Visible:
        // o getter de Control.Visible também depende da visibilidade de todos os
        // ancestrais, então usá-lo como flag de negócio lê "false" sempre que este
        // painel não é a aba ativa no momento da leitura.
        bool selecionado = true;
        void AtualizarVisual()
        {
            card.BackColor = selecionado ? Tema.SuperficieSelecionada : Tema.SuperficieAlta;
            lblCheck.Visible = selecionado;
        }
        AtualizarVisual();

        void AlternarEMarcar(Control c)
        {
            c.Click += (_, _) => { selecionado = !selecionado; AtualizarVisual(); AtualizarMatriz(); };
            c.Cursor = Cursors.Hand;
            foreach (Control filho in c.Controls) AlternarEMarcar(filho);
        }
        AlternarEMarcar(card);

        _cardsAlgoritmo.Add((info.Algoritmo, () => selecionado));
        return card;
    }

    private Panel CriarSecaoTamanhos()
    {
        var p = new Panel { BackColor = Color.Transparent };
        var lbl = Tema.Rotulo("Tamanhos do vetor");
        lbl.Location = new Point(0, 0);

        var linhaChips = new FlowLayoutPanel { Location = new Point(0, 24), Size = new Size(700, 36), BackColor = Color.Transparent, WrapContents = false };
        foreach (var tamanho in TamanhosDisponiveis)
        {
            var (chip, selecionado) = CriarChipMultiplo(tamanho.ToString("N0"), selecionadoInicial: tamanho != 50_000);
            chip.Click += (_, _) => AtualizarMatriz();
            _chipsTamanho.Add((tamanho, selecionado));
            linhaChips.Controls.Add(chip);
        }

        _lblAviso = new Label
        {
            Location = new Point(0, 64), Size = new Size(700, 20),
            Font = Tema.FonteRotulo, ForeColor = Tema.Alerta, BackColor = Color.Transparent, Visible = false
        };

        p.Controls.Add(lbl);
        p.Controls.Add(linhaChips);
        p.Controls.Add(_lblAviso);
        return p;
    }

    private Panel CriarSecaoDistribuicaoRepeticoes()
    {
        var p = new Panel { BackColor = Color.Transparent };

        var lblDist = Tema.Rotulo("Distribuições");
        lblDist.Location = new Point(0, 0);
        var linhaChips = new FlowLayoutPanel { Location = new Point(0, 24), Size = new Size(430, 36), BackColor = Color.Transparent, WrapContents = false };
        foreach (var tipo in Enum.GetValues<TipoDistribuicao>())
        {
            var (chip, selecionado) = CriarChipMultiplo(NomeDistribuicao(tipo));
            _chipsDistribuicao.Add((tipo, selecionado));
            linhaChips.Controls.Add(chip);
        }

        var lblRep = Tema.Rotulo("Repetições");
        lblRep.Location = new Point(450, 0);
        var seletorRep = _seletorRepeticoes.Construir(new[] { "1×", "3×", "5×", "10×" }, indiceInicial: 1);
        seletorRep.Location = new Point(450, 24);

        p.Controls.Add(lblDist);
        p.Controls.Add(linhaChips);
        p.Controls.Add(lblRep);
        p.Controls.Add(seletorRep);
        return p;
    }

    private (Button Chip, Func<bool> Selecionado) CriarChipMultiplo(string texto, bool selecionadoInicial = true)
    {
        var chip = new Button
        {
            Text = texto, Font = Tema.FonteCorpoNegrito, Height = 30, AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(12, 0, 12, 0),
            Margin = new Padding(0, 0, 8, 0), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        chip.FlatAppearance.BorderSize = 1;

        bool selecionado = selecionadoInicial;
        void Atualizar()
        {
            chip.BackColor = selecionado ? Tema.SuperficieSelecionada : Color.Transparent;
            chip.ForeColor = selecionado ? Tema.Acento : Tema.TextoSecundario;
            chip.FlatAppearance.BorderColor = selecionado ? Tema.Acento : Tema.Borda;
        }
        Atualizar();
        chip.Click += (_, _) => { selecionado = !selecionado; Atualizar(); };
        Tema.Arredondar(chip, 15);

        return (chip, () => selecionado);
    }

    // --------------------------------------------------------- Card da matriz (direita)

    private Panel CriarCardMatriz()
    {
        var card = Tema.Cartao();
        card.Dock = DockStyle.Fill;

        var topo = new Panel { Dock = DockStyle.Top, Height = 330, BackColor = Color.Transparent, Padding = new Padding(20, 18, 20, 0) };

        var eyebrow = Tema.Eyebrow("Matriz de execução");
        eyebrow.Location = new Point(20, 18);

        _lblTotalRuns = new Label
        {
            Text = "0", Font = Tema.FonteNumeroGrande, ForeColor = Tema.Texto,
            AutoSize = true, Location = new Point(20, 38), BackColor = Color.Transparent
        };
        var lblUnidade = new Label
        {
            Text = "ordenações", Font = Tema.FonteCorpo, ForeColor = Tema.TextoSecundario,
            AutoSize = true, Location = new Point(20, 80), BackColor = Color.Transparent
        };

        _lblFormula = new Label
        {
            Location = new Point(20, 104), Size = new Size(320, 36),
            Font = Tema.FonteRotulo, ForeColor = Tema.TextoSecundario, BackColor = Color.Transparent
        };

        _btnExecutarSuite = Tema.BotaoPrimario("Executar benchmark");
        _btnExecutarSuite.Location = new Point(20, 148);
        _btnExecutarSuite.Width = 320;
        _btnExecutarSuite.Click += async (_, _) => await ExecutarSuiteAsync();

        _btnCancelar = Tema.BotaoSecundario("Cancelar");
        _btnCancelar.Location = new Point(20, 148);
        _btnCancelar.Width = 320;
        _btnCancelar.Visible = false;
        _btnCancelar.Click += (_, _) => _cts?.Cancel();

        _progressBar = new ProgressBar { Location = new Point(20, 192), Size = new Size(320, 6), Minimum = 0, Maximum = 100 };

        _lblStatusAtual = new Label
        {
            Text = "Aguardando", Location = new Point(20, 206), Size = new Size(220, 18),
            Font = Tema.FonteRotulo, ForeColor = Tema.TextoSecundario, AutoEllipsis = true, BackColor = Color.Transparent
        };
        _lblDoneTotal = new Label
        {
            Text = "", Location = new Point(240, 206), Size = new Size(100, 18), TextAlign = ContentAlignment.MiddleRight,
            Font = Tema.FonteRotulo, ForeColor = Tema.TextoSecundario, BackColor = Color.Transparent
        };

        _btnVerResultados = Tema.BotaoSecundario("Ver resultados →");
        _btnVerResultados.Location = new Point(20, 232);
        _btnVerResultados.Width = 320;
        _btnVerResultados.Visible = false;
        _btnVerResultados.Click += (_, _) => SolicitouVerResultados?.Invoke();

        topo.Controls.AddRange(new Control[]
        {
            eyebrow, _lblTotalRuns, lblUnidade, _lblFormula, _btnExecutarSuite, _btnCancelar,
            _progressBar, _lblStatusAtual, _lblDoneTotal, _btnVerResultados
        });

        var areaLog = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 8, 20, 18), BackColor = Color.Transparent };
        _painelLog = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoScroll = true, BackColor = Tema.Fundo, Padding = new Padding(10)
        };
        areaLog.Controls.Add(_painelLog);

        card.Controls.Add(areaLog);
        card.Controls.Add(topo);
        return card;
    }

    private void AdicionarLog(string texto, Color cor)
    {
        var lbl = new Label
        {
            Text = "›  " + texto, Font = Tema.FonteMono, ForeColor = cor,
            AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 3)
        };
        _painelLog.Controls.Add(lbl);

        while (_painelLog.Controls.Count > 80)
            _painelLog.Controls.RemoveAt(0);

        if (_painelLog.Controls.Count > 0)
            _painelLog.ScrollControlIntoView(_painelLog.Controls[^1]);
    }

    private void LimparLog() => _painelLog.Controls.Clear();

    // -------------------------------------------------------------------- Lógica

    private int ObterRepeticoesSelecionadas() => _seletorRepeticoes.Selecionado switch
    {
        0 => 1,
        1 => 3,
        2 => 5,
        _ => 10
    };

    private void AtualizarMatriz()
    {
        int algoritmos = ObterAlgoritmosSelecionados().Count;
        int tamanhos = _chipsTamanho.Count(c => c.Selecionado());
        int distribuicoes = _chipsDistribuicao.Count(c => c.Selecionado());
        int repeticoes = ObterRepeticoesSelecionadas();

        int total = algoritmos * tamanhos * distribuicoes * repeticoes;
        _lblTotalRuns.Text = total.ToString("N0");
        _lblFormula.Text = $"{algoritmos} algoritmos × {tamanhos} tamanhos × {distribuicoes} distribuições × {repeticoes} repetições";
        _btnExecutarSuite.Enabled = total > 0;

        bool bubbleEm50k = ObterAlgoritmosSelecionados().Any(a => a is BubbleSort) && _chipsTamanho.Any(c => c.Tamanho == 50_000 && c.Selecionado());
        _lblAviso.Visible = bubbleEm50k;
        _lblAviso.Text = bubbleEm50k ? "⚠ Bubble Sort com 50.000 itens pode levar vários segundos por repetição." : "";
    }

    private async Task ExecutarUnicoAsync()
    {
        if (_sessaoAtual is not { TemDados: true } sessao)
        {
            MessageBox.Show(this, "Carregue ou gere um dataset na aba Dados antes de executar.", "Nenhum dado",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var algoritmos = ObterAlgoritmosSelecionados();
        if (algoritmos.Count == 0)
        {
            MessageBox.Show(this, "Marque ao menos um algoritmo.", "Nenhum algoritmo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnExecutarUnico.Enabled = false;
        _lblStatusAtual.Text = "Executando no dataset atual...";

        try
        {
            var resultado = await Task.Run(() => ExecutarUnicoInterno(sessao, algoritmos));
            ResultadosProntos?.Invoke(resultado);
            _lblStatusAtual.Text = $"Execução única concluída ({resultado.Execucoes.Count} algoritmo(s)).";
        }
        finally
        {
            _btnExecutarUnico.Enabled = true;
        }
    }

    private static ResultadoParaExibicao ExecutarUnicoInterno(SessaoDados sessao, List<IAlgoritmoOrdenacao> algoritmos)
    {
        var executor = new ExecutorBenchmark();
        var execucoes = new List<ResultadoExecucao>();
        var depoisPorAlgoritmo = new Dictionary<string, string>();

        var registros = sessao.Registros!;
        string antes = FormatarListagem(registros);
        var comparador = new ComparadorRegistro(sessao.CampoAtual);

        foreach (var algoritmo in algoritmos)
        {
            var (resultado, ordenados) = executor.Executar(algoritmo, registros, comparador, sessao.TipoDistribuicaoAtual);
            execucoes.Add(resultado);
            depoisPorAlgoritmo[algoritmo.Nome] = FormatarListagem(ordenados);
        }

        return new ResultadoParaExibicao { Execucoes = execucoes, DadosAntes = antes, DadosDepoisPorAlgoritmo = depoisPorAlgoritmo };
    }

    private static string FormatarListagem<T>(IReadOnlyList<T> dados)
    {
        const int maxItens = 30;
        string texto = string.Join(" | ", dados.Take(maxItens).Select(d => d!.ToString()));
        return dados.Count > maxItens ? $"{texto}  ... (+{dados.Count - maxItens} itens)" : texto;
    }

    private async Task ExecutarSuiteAsync()
    {
        var algoritmos = ObterAlgoritmosSelecionados();
        var tamanhos = _chipsTamanho.Where(c => c.Selecionado()).Select(c => c.Tamanho).ToList();
        var distribuicoes = _chipsDistribuicao.Where(c => c.Selecionado()).Select(c => c.Tipo).ToList();
        int repeticoes = ObterRepeticoesSelecionadas();

        if (algoritmos.Count == 0 || tamanhos.Count == 0 || distribuicoes.Count == 0)
        {
            MessageBox.Show(this, "Marque ao menos um algoritmo, um tamanho e uma distribuição.", "Configuração incompleta",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (algoritmos.Any(a => a is BubbleSort) && tamanhos.Max() >= 50_000)
        {
            var resposta = MessageBox.Show(this,
                $"Bubble Sort é O(n²) e pode demorar muito em {tamanhos.Max():N0} elementos. Deseja continuar mesmo assim?",
                "Aviso de desempenho", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (resposta != DialogResult.Yes) return;
        }

        var configuracao = new ConfiguracaoSuite
        {
            Algoritmos = algoritmos, Tamanhos = tamanhos, Distribuicoes = distribuicoes, Repeticoes = repeticoes
        };

        DefinirExecutando(true);
        LimparLog();
        AdicionarLog($"SuiteBenchmark: {configuracao.TotalExecucoes} ordenações", Tema.TextoSecundario);
        _cts = new CancellationTokenSource();

        var progresso = new Progress<ProgressoBenchmark>(p =>
        {
            _progressBar.Value = Math.Clamp((int)p.Percentual, 0, 100);
            _lblStatusAtual.Text = p.DescricaoAtual;
            _lblDoneTotal.Text = $"{p.Concluidos}/{p.Total}";
        });

        try
        {
            var suite = new SuiteBenchmark();
            var resultados = await Task.Run(() => suite.Executar(configuracao, progresso, _cts.Token), _cts.Token);

            foreach (var grupo in resultados.GroupBy(r => (r.NomeAlgoritmo, r.Tamanho, r.TipoDistribuicao)))
            {
                double media = grupo.Average(r => r.TempoMs);
                AdicionarLog($"✓ {grupo.Key.NomeAlgoritmo,-10} n={grupo.Key.Tamanho,-7:N0} {grupo.Key.TipoDistribuicao,-13} {media,8:N3} ms", Tema.Texto);
            }
            AdicionarLog("concluído", Tema.Acento);

            ResultadosProntos?.Invoke(new ResultadoParaExibicao { Execucoes = resultados });
            _lblStatusAtual.Text = "Concluído";
            _btnVerResultados.Visible = true;
        }
        catch (OperationCanceledException)
        {
            AdicionarLog("cancelado pelo usuário", Tema.Alerta);
            _lblStatusAtual.Text = "Cancelado";
        }
        finally
        {
            DefinirExecutando(false);
            _cts = null;
        }
    }

    private List<IAlgoritmoOrdenacao> ObterAlgoritmosSelecionados() =>
        _cardsAlgoritmo.Where(c => c.Selecionado()).Select(c => c.Algoritmo).ToList();

    private static string NomeDistribuicao(TipoDistribuicao tipo) => tipo switch
    {
        TipoDistribuicao.Aleatorio => "Aleatório",
        TipoDistribuicao.Ordenado => "Ordenado",
        TipoDistribuicao.Invertido => "Invertido",
        TipoDistribuicao.SemiOrdenado => "Semi-ordenado",
        _ => tipo.ToString()
    };

    private void DefinirExecutando(bool executando)
    {
        _btnExecutarSuite.Visible = !executando;
        _btnCancelar.Visible = executando;
        if (executando) _btnVerResultados.Visible = false;
        if (!executando) _progressBar.Value = 0;
    }
}
