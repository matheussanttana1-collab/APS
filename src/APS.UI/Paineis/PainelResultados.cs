using System.Data;
using System.Globalization;
using System.Windows.Forms.DataVisualization.Charting;
using APS.Core.IO;
using APS.Core.Modelos;

namespace APS.UI.Paineis;

/// <summary>
/// Painel "Resultados": grid + gráfico + ranking + listagem antes/depois +
/// exportar. O botão de exportar só chama <see cref="ExportadorPlanilha"/> (Core)
/// e informa onde o arquivo foi salvo — a montagem do .xlsx em si não tem nenhum
/// código aqui.
/// </summary>
public class PainelResultados : UserControl
{
    private List<ResultadoExecucao> _execucoes = new();
    private Dictionary<string, string>? _dadosDepoisPorAlgoritmo;
    private TipoDistribuicao? _distribuicaoAtiva;

    private Panel _slotFiltro = null!;
    private Button _btnExportar = null!;

    private DataGridView _dgvResultados = null!;
    private Chart _chart = null!;
    private FlowLayoutPanel _painelRanking = null!;
    private Label _lblRankingSubtitulo = null!;

    private TextBox _txtAntes = null!;
    private TextBox _txtDepois = null!;
    private ComboBox _cboAlgoritmoDepois = null!;
    private Label _lblResumoAntesDepois = null!;

    public PainelResultados()
    {
        SuspendLayout();
        Dock = DockStyle.Fill;
        BackColor = Tema.Fundo;

        Controls.Add(CriarCorpo());
        Controls.Add(CriarCabecalho());
        ResumeLayout(false);

        AtualizarTudo();
    }

    public void AtualizarResultados(ResultadoParaExibicao resultado)
    {
        _execucoes = resultado.Execucoes;
        _dadosDepoisPorAlgoritmo = resultado.DadosDepoisPorAlgoritmo;
        _distribuicaoAtiva = _execucoes.Select(r => r.TipoDistribuicao).Distinct().OrderBy(d => d).FirstOrDefault();

        if (resultado.DadosAntes is not null)
            _txtAntes.Text = resultado.DadosAntes;

        AtualizarTudo();
    }

    // ---------------------------------------------------------------- Cabeçalho

    private Panel CriarCabecalho()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Tema.Fundo, Padding = new Padding(32, 20, 32, 0) };

        var eyebrow = Tema.Eyebrow("Etapa 3");
        eyebrow.Location = new Point(32, 20);

        var titulo = Tema.Titulo("Resultados");
        titulo.Location = new Point(32, 40);

        var toolbar = new Panel { Dock = DockStyle.Right, Width = 500, BackColor = Color.Transparent };
        _btnExportar = Tema.BotaoPrimario("Exportar relatório (planilha)");
        _btnExportar.Location = new Point(toolbar.Width - 230, 44);
        _btnExportar.Width = 230;
        _btnExportar.Click += (_, _) => ExportarRelatorio();

        _slotFiltro = new Panel { Location = new Point(0, 44), Size = new Size(250, 34), BackColor = Color.Transparent };

        toolbar.Controls.Add(_btnExportar);
        toolbar.Controls.Add(_slotFiltro);

        header.Controls.Add(toolbar);
        header.Controls.Add(eyebrow);
        header.Controls.Add(titulo);
        return header;
    }

    // --------------------------------------------------------------------- Corpo

    private Panel CriarCorpo()
    {
        var corpo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fundo, AutoScroll = true, Padding = new Padding(32, 16, 32, 24) };

        var tabela = new TableLayoutPanel { Dock = DockStyle.Top, Height = 672, ColumnCount = 1, BackColor = Tema.Fundo };
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 336));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 336));

        var linha1 = CriarLinhaGraficoRanking();
        linha1.Dock = DockStyle.Fill;
        var linha2 = CriarLinhaTabelaAntesDepois();
        linha2.Dock = DockStyle.Fill;

        tabela.Controls.Add(linha1, 0, 0);
        tabela.Controls.Add(linha2, 0, 1);

        corpo.Controls.Add(tabela);
        return corpo;
    }

    private Panel CriarLinhaGraficoRanking()
    {
        var linha = new Panel { BackColor = Tema.Fundo, Padding = new Padding(0, 0, 0, 16) };

        var slotRanking = new Panel { Dock = DockStyle.Right, Width = 300, BackColor = Tema.Fundo, Padding = new Padding(16, 0, 0, 0) };
        slotRanking.Controls.Add(CriarCardRanking());

        var slotChart = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fundo };
        slotChart.Controls.Add(CriarCardGrafico());

        linha.Controls.Add(slotChart);
        linha.Controls.Add(slotRanking);
        return linha;
    }

    private Panel CriarLinhaTabelaAntesDepois()
    {
        var linha = new Panel { BackColor = Tema.Fundo };

        var slotAntesDepois = new Panel { Dock = DockStyle.Right, Width = 320, BackColor = Tema.Fundo, Padding = new Padding(16, 0, 0, 0) };
        slotAntesDepois.Controls.Add(CriarCardAntesDepois());

        var slotTabela = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fundo };
        slotTabela.Controls.Add(CriarCardTabela());

        linha.Controls.Add(slotTabela);
        linha.Controls.Add(slotAntesDepois);
        return linha;
    }

    // ------------------------------------------------------------------ Gráfico

    private Panel CriarCardGrafico()
    {
        var card = Tema.Cartao();
        card.Dock = DockStyle.Fill;

        var cabecalho = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(18, 14, 18, 0), BackColor = Color.Transparent };
        var titulo = new Label
        {
            Text = "Tempo médio × tamanho do vetor", Font = Tema.FonteSubtitulo, ForeColor = Tema.Texto,
            AutoSize = true, Location = new Point(18, 14), BackColor = Color.Transparent
        };
        cabecalho.Controls.Add(titulo);

        var areaChart = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 10, 10), BackColor = Color.Transparent };
        // Chart lança ArgumentException se for redimensionado para 0px durante os
        // layouts intermediários da construção (antes do form ter seu tamanho
        // final) — MinimumSize evita que isso aconteça.
        _chart = new Chart { Dock = DockStyle.Fill, BackColor = Tema.Superficie, MinimumSize = new Size(100, 100) };
        var area = new ChartArea("Area")
        {
            BackColor = Tema.Superficie,
            AxisX = { LineColor = Tema.Borda, MajorGrid = { LineColor = Tema.Borda }, LabelStyle = { ForeColor = Tema.TextoSecundario }, TitleForeColor = Tema.TextoSecundario },
            AxisY = { LineColor = Tema.Borda, MajorGrid = { LineColor = Tema.Borda }, LabelStyle = { ForeColor = Tema.TextoSecundario }, TitleForeColor = Tema.TextoSecundario }
        };
        area.AxisX.Title = "Tamanho da entrada";
        area.AxisY.Title = "Tempo médio (ms)";
        _chart.ChartAreas.Add(area);
        _chart.Legends.Add(new Legend("Legenda") { BackColor = Color.Transparent, ForeColor = Tema.TextoSecundario, Docking = Docking.Top, Alignment = StringAlignment.Far });
        areaChart.Controls.Add(_chart);

        card.Controls.Add(areaChart);
        card.Controls.Add(cabecalho);
        return card;
    }

    private void AtualizarGrafico()
    {
        _chart.Series.Clear();
        var linha = _execucoes.Where(r => r.TipoDistribuicao == _distribuicaoAtiva).ToList();
        if (linha.Count == 0) return;

        foreach (var grupoAlgoritmo in linha.GroupBy(r => r.NomeAlgoritmo))
        {
            var cor = Tema.CorDoAlgoritmo(grupoAlgoritmo.Key);
            var serie = new Series(grupoAlgoritmo.Key)
            {
                ChartType = SeriesChartType.Line, BorderWidth = 3, Color = cor, IsVisibleInLegend = true,
                MarkerStyle = MarkerStyle.Circle, MarkerSize = 7, MarkerColor = Tema.Superficie, MarkerBorderColor = cor, MarkerBorderWidth = 2
            };

            var pontos = grupoAlgoritmo
                .GroupBy(r => r.Tamanho)
                .Select(g => (Tamanho: g.Key, TempoMedio: g.Average(r => r.TempoMs)))
                .OrderBy(p => p.Tamanho);

            foreach (var ponto in pontos)
                serie.Points.AddXY(ponto.Tamanho, ponto.TempoMedio);

            _chart.Series.Add(serie);
        }
    }

    // ------------------------------------------------------------------ Ranking

    private Panel CriarCardRanking()
    {
        var card = Tema.Cartao();
        card.Dock = DockStyle.Fill;
        card.Padding = new Padding(18);

        var titulo = new Label
        {
            Text = "Ranking", Font = Tema.FonteSubtitulo, ForeColor = Tema.Texto,
            AutoSize = true, Location = new Point(18, 14), BackColor = Color.Transparent
        };
        _lblRankingSubtitulo = new Label
        {
            Location = new Point(18, 38), Size = new Size(240, 18),
            Font = Tema.FonteRotulo, ForeColor = Tema.TextoSecundario, BackColor = Color.Transparent
        };

        _painelRanking = new FlowLayoutPanel
        {
            Location = new Point(18, 62), Size = new Size(240, 230),
            FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent, AutoScroll = true
        };

        card.Controls.Add(titulo);
        card.Controls.Add(_lblRankingSubtitulo);
        card.Controls.Add(_painelRanking);
        return card;
    }

    private void AtualizarRanking()
    {
        _painelRanking.Controls.Clear();

        var linha = _execucoes.Where(r => r.TipoDistribuicao == _distribuicaoAtiva).ToList();
        if (linha.Count == 0)
        {
            _lblRankingSubtitulo.Text = "Sem medições";
            return;
        }

        int maiorTamanho = linha.Max(r => r.Tamanho);
        _lblRankingSubtitulo.Text = $"em n = {maiorTamanho:N0}";

        var noMaior = linha.Where(r => r.Tamanho == maiorTamanho)
            .GroupBy(r => r.NomeAlgoritmo)
            .Select(g => (Nome: g.Key, Media: g.Average(r => r.TempoMs)))
            .OrderBy(x => x.Media)
            .ToList();

        double maisRapido = noMaior.Count > 0 ? noMaior[0].Media : 1;
        double maisLento = noMaior.Count > 0 ? noMaior[^1].Media : 1;

        for (int i = 0; i < noMaior.Count; i++)
        {
            var (nome, media) = noMaior[i];
            var cor = Tema.CorDoAlgoritmo(nome);

            var linhaPainel = new Panel { Size = new Size(238, 50), BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 6) };

            var lblPos = new Label { Text = (i + 1).ToString(), Font = Tema.FonteRotulo, ForeColor = Tema.TextoSecundario, Location = new Point(0, 0), AutoSize = true, BackColor = Color.Transparent };
            var lblNome = new Label { Text = nome, Font = Tema.FonteCorpoNegrito, ForeColor = Tema.Texto, Location = new Point(18, -2), AutoSize = true, BackColor = Color.Transparent };
            var lblTempo = new Label { Text = $"{FormatarMs(media)} ms", Font = Tema.FonteCorpo, ForeColor = Tema.TextoSecundario, Location = new Point(170, 0), AutoSize = true, BackColor = Color.Transparent };

            var trilho = new Panel { Location = new Point(18, 24), Size = new Size(190, 6), BackColor = Tema.SuperficieAlta };
            double proporcao = maisLento > maisRapido ? 0.08 + 0.92 * (Math.Log10(media) - (Math.Log10(maisRapido) - 0.3)) / (Math.Log10(maisLento) - (Math.Log10(maisRapido) - 0.3)) : 1.0;
            proporcao = Math.Clamp(proporcao, 0.05, 1.0);
            var barra = new Panel { Location = new Point(0, 0), Size = new Size((int)(190 * proporcao), 6), BackColor = cor };
            trilho.Controls.Add(barra);

            string rel = i == 0 ? "mais rápido" : $"{(media / maisRapido):N1}×";
            var lblRel = new Label { Text = rel, Font = Tema.FonteRotulo, ForeColor = i == 0 ? Tema.Acento : Tema.TextoSecundario, Location = new Point(18, 34), AutoSize = true, BackColor = Color.Transparent };

            linhaPainel.Controls.AddRange(new Control[] { lblPos, lblNome, lblTempo, trilho, lblRel });
            _painelRanking.Controls.Add(linhaPainel);
        }
    }

    // -------------------------------------------------------------------- Tabela

    private Panel CriarCardTabela()
    {
        var card = Tema.Cartao();
        card.Dock = DockStyle.Fill;

        var cabecalho = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(18, 14, 18, 0), BackColor = Color.Transparent };
        var titulo = new Label
        {
            Text = "Tabela de medições", Font = Tema.FonteSubtitulo, ForeColor = Tema.Texto,
            AutoSize = true, Location = new Point(18, 14), BackColor = Color.Transparent
        };
        cabecalho.Controls.Add(titulo);

        var areaGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 0, 18, 18), BackColor = Color.Transparent };
        _dgvResultados = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        Tema.EstilizarGrid(_dgvResultados);
        areaGrid.Controls.Add(_dgvResultados);

        card.Controls.Add(areaGrid);
        card.Controls.Add(cabecalho);
        return card;
    }

    private void AtualizarGrid()
    {
        var tabela = new DataTable();
        tabela.Columns.Add("Algoritmo", typeof(string));
        tabela.Columns.Add("n", typeof(string));
        tabela.Columns.Add("Distribuição", typeof(string));
        tabela.Columns.Add("Tempo médio (ms)", typeof(string));
        tabela.Columns.Add("Comparações", typeof(string));
        tabela.Columns.Add("Trocas", typeof(string));
        tabela.Columns.Add("Relativo", typeof(string));

        var linha = _execucoes.Where(r => r.TipoDistribuicao == _distribuicaoAtiva).ToList();
        var resumo = linha
            .GroupBy(r => (r.NomeAlgoritmo, r.Tamanho))
            .Select(g => new
            {
                g.Key.NomeAlgoritmo,
                g.Key.Tamanho,
                Media = g.Average(r => r.TempoMs),
                Comparacoes = (long)g.Average(r => r.Comparacoes),
                Trocas = (long)g.Average(r => r.Trocas)
            })
            .OrderBy(r => r.Tamanho).ThenBy(r => r.Media)
            .ToList();

        foreach (var r in resumo)
        {
            double melhorNoTamanho = resumo.Where(x => x.Tamanho == r.Tamanho).Min(x => x.Media);
            double relativo = r.Media / melhorNoTamanho;
            string textoRelativo = relativo < 1.005 ? "1×" : $"{relativo:N1}×";

            tabela.Rows.Add(r.NomeAlgoritmo, r.Tamanho.ToString("N0"), _distribuicaoAtiva?.ToString() ?? "",
                FormatarMs(r.Media), r.Comparacoes.ToString("N0"), r.Trocas.ToString("N0"), textoRelativo);
        }

        _dgvResultados.DataSource = tabela;
    }

    // ------------------------------------------------------------- Antes/depois

    private Panel CriarCardAntesDepois()
    {
        var card = Tema.Cartao();
        card.Dock = DockStyle.Fill;
        card.Padding = new Padding(18);

        var titulo = new Label
        {
            Text = "Antes e depois", Font = Tema.FonteSubtitulo, ForeColor = Tema.Texto,
            AutoSize = true, Location = new Point(18, 14), BackColor = Color.Transparent
        };
        _lblResumoAntesDepois = new Label
        {
            Location = new Point(18, 38), Size = new Size(280, 32),
            Font = Tema.FonteRotulo, ForeColor = Tema.TextoSecundario, BackColor = Color.Transparent
        };

        var lblAlgoritmo = Tema.Rotulo("Algoritmo");
        lblAlgoritmo.Location = new Point(18, 76);
        _cboAlgoritmoDepois = new ComboBox { Location = new Point(18, 96), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
        Tema.EstilizarComboBox(_cboAlgoritmoDepois);
        _cboAlgoritmoDepois.SelectedIndexChanged += (_, _) =>
        {
            if (_cboAlgoritmoDepois.SelectedItem is string nome && _dadosDepoisPorAlgoritmo is not null)
                _txtDepois.Text = _dadosDepoisPorAlgoritmo.GetValueOrDefault(nome, "");
        };

        var lblAntes = Tema.Rotulo("Antes");
        lblAntes.Location = new Point(18, 130);
        _txtAntes = new TextBox
        {
            Location = new Point(18, 150), Size = new Size(280, 54),
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = Tema.FonteMono
        };
        Tema.EstilizarCampoTexto(_txtAntes);

        var lblDepois = Tema.Rotulo("Depois");
        lblDepois.Location = new Point(18, 210);
        _txtDepois = new TextBox
        {
            Location = new Point(18, 230), Size = new Size(280, 54),
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = Tema.FonteMono
        };
        Tema.EstilizarCampoTexto(_txtDepois);

        card.Controls.AddRange(new Control[]
        {
            titulo, _lblResumoAntesDepois, lblAlgoritmo, _cboAlgoritmoDepois, lblAntes, _txtAntes, lblDepois, _txtDepois
        });
        return card;
    }

    private void AtualizarAntesDepois()
    {
        bool temDados = _dadosDepoisPorAlgoritmo is { Count: > 0 };

        _cboAlgoritmoDepois.Items.Clear();
        _txtDepois.Text = string.Empty;
        _lblResumoAntesDepois.Text = temDados
            ? "Resultado de uma execução única sobre o dataset atual (aba Execução)."
            : "Disponível após uma execução única sobre o dataset atual (aba Execução).";

        if (!temDados) return;

        foreach (var nome in _dadosDepoisPorAlgoritmo!.Keys)
            _cboAlgoritmoDepois.Items.Add(nome);

        _cboAlgoritmoDepois.SelectedIndex = 0;
    }

    // --------------------------------------------------------------- Filtro/export

    private void AtualizarFiltroDistribuicao()
    {
        _slotFiltro.Controls.Clear();
        var distribuicoesPresentes = _execucoes.Select(r => r.TipoDistribuicao).Distinct().OrderBy(d => d).ToList();
        if (distribuicoesPresentes.Count == 0) return;

        if (_distribuicaoAtiva is null || !distribuicoesPresentes.Contains(_distribuicaoAtiva.Value))
            _distribuicaoAtiva = distribuicoesPresentes[0];

        var grupo = new GrupoSegmentado();
        var opcoes = distribuicoesPresentes.Select(d => d.ToString()).ToArray();
        int indiceInicial = distribuicoesPresentes.IndexOf(_distribuicaoAtiva.Value);
        var painel = grupo.Construir(opcoes, indiceInicial);
        grupo.Mudou += indice =>
        {
            _distribuicaoAtiva = distribuicoesPresentes[indice];
            AtualizarGrafico();
            AtualizarRanking();
            AtualizarGrid();
        };

        _slotFiltro.Controls.Add(painel);
    }

    private void ExportarRelatorio()
    {
        if (_execucoes.Count == 0)
        {
            MessageBox.Show(this, "Não há resultados para exportar. Execute algum algoritmo primeiro.", "Nada para exportar",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialogo = new SaveFileDialog { Filter = "Planilha Excel (*.xlsx)|*.xlsx", FileName = "relatorio-performance.xlsx" };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            ExportadorPlanilha.ExportarResultados(_execucoes, dialogo.FileName);
            MessageBox.Show(this, $"Relatório exportado para:\n{dialogo.FileName}", "Exportação concluída",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível exportar o relatório:\n{ex.Message}", "Erro",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string FormatarMs(double v) => v switch
    {
        >= 100 => v.ToString("N1", CultureInfo.GetCultureInfo("pt-BR")),
        >= 1 => v.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")),
        _ => v.ToString("N3", CultureInfo.GetCultureInfo("pt-BR"))
    };

    private void AtualizarTudo()
    {
        AtualizarFiltroDistribuicao();
        AtualizarGrafico();
        AtualizarRanking();
        AtualizarGrid();
        AtualizarAntesDepois();
    }
}
