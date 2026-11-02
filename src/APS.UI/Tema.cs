using System.Drawing.Drawing2D;

namespace APS.UI;

/// <summary>
/// Paleta e helpers visuais compartilhados por toda a UI — um tema escuro com
/// identidade "Amazônia" (verde-esmeralda sobre fundo escuro). Centraliza aqui
/// em vez de repetir cores/fontes em cada painel.
/// </summary>
public static class Tema
{
    // Base
    public static readonly Color Fundo = ColorTranslator.FromHtml("#121815");
    public static readonly Color Superficie = ColorTranslator.FromHtml("#1B2520");
    public static readonly Color SuperficieAlta = ColorTranslator.FromHtml("#223029");
    public static readonly Color SuperficieSelecionada = ColorTranslator.FromHtml("#17352A");
    public static readonly Color Borda = ColorTranslator.FromHtml("#2D3A34");

    // Texto
    public static readonly Color Texto = ColorTranslator.FromHtml("#ECF3EF");
    public static readonly Color TextoSecundario = ColorTranslator.FromHtml("#94A89F");
    public static readonly Color TextoSobreAcento = ColorTranslator.FromHtml("#07140F");

    // Acento (verde — também a cor do QuickSort no gráfico)
    public static readonly Color Acento = ColorTranslator.FromHtml("#3ED58A");
    public static readonly Color AcentoHover = ColorTranslator.FromHtml("#52E29C");
    public static readonly Color AcentoPressionado = ColorTranslator.FromHtml("#2BBE78");

    public static readonly Color Alerta = ColorTranslator.FromHtml("#E0A452");
    public static readonly Color Erro = ColorTranslator.FromHtml("#E0756B");

    // Paleta categórica dos 4 algoritmos — usada nos cards, no gráfico e na tabela.
    public static readonly Color CorBubble = ColorTranslator.FromHtml("#E0A452");
    public static readonly Color CorQuick = ColorTranslator.FromHtml("#3ED58A");
    public static readonly Color CorHeap = ColorTranslator.FromHtml("#5FA8D8");
    public static readonly Color CorShell = ColorTranslator.FromHtml("#C792EA");

    // Fontes (Segoe UI simples — evita depender de variantes "Semibold" que podem
    // não existir em toda instalação do Windows e seriam silenciosamente trocadas).
    public static readonly Font FonteTitulo = new("Segoe UI", 18f, FontStyle.Bold);
    public static readonly Font FonteSubtitulo = new("Segoe UI", 11f, FontStyle.Bold);
    public static readonly Font FonteCorpo = new("Segoe UI", 9.5f);
    public static readonly Font FonteCorpoNegrito = new("Segoe UI", 9.5f, FontStyle.Bold);
    public static readonly Font FonteRotulo = new("Segoe UI", 8f, FontStyle.Bold);
    public static readonly Font FonteBotao = new("Segoe UI", 9.5f, FontStyle.Bold);
    public static readonly Font FonteMono = new("Consolas", 9f);
    public static readonly Font FonteNumeroGrande = new("Segoe UI", 28f, FontStyle.Bold);

    public static void EstilizarFormulario(Form f)
    {
        f.BackColor = Fundo;
        f.ForeColor = Texto;
        f.Font = FonteCorpo;
    }

    public static Label Eyebrow(string texto) => new()
    {
        Text = texto.ToUpperInvariant(),
        Font = FonteRotulo,
        ForeColor = Acento,
        AutoSize = true,
        BackColor = Color.Transparent
    };

    public static Label Titulo(string texto) => new()
    {
        Text = texto,
        Font = FonteTitulo,
        ForeColor = Texto,
        AutoSize = true,
        BackColor = Color.Transparent
    };

    public static Label Descricao(string texto, int largura) => new()
    {
        Text = texto,
        Font = FonteCorpo,
        ForeColor = TextoSecundario,
        AutoSize = false,
        Size = new Size(largura, 36),
        BackColor = Color.Transparent
    };

    public static Label Rotulo(string texto) => new()
    {
        Text = texto,
        Font = FonteCorpoNegrito,
        ForeColor = TextoSecundario,
        AutoSize = true,
        BackColor = Color.Transparent
    };

    public static Button BotaoPrimario(string texto)
    {
        var b = new Button
        {
            Text = texto,
            Font = FonteBotao,
            BackColor = Acento,
            ForeColor = TextoSobreAcento,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Height = 34,
            TextAlign = ContentAlignment.MiddleCenter
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = AcentoHover;
        b.FlatAppearance.MouseDownBackColor = AcentoPressionado;
        Arredondar(b, 8);
        return b;
    }

    public static Button BotaoSecundario(string texto)
    {
        var b = new Button
        {
            Text = texto,
            Font = FonteBotao,
            BackColor = SuperficieAlta,
            ForeColor = Texto,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Height = 34,
            TextAlign = ContentAlignment.MiddleCenter
        };
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.BorderColor = Borda;
        b.FlatAppearance.MouseOverBackColor = Borda;
        Arredondar(b, 8);
        return b;
    }

    public static Panel Cartao()
    {
        var p = new Panel { BackColor = Superficie };
        AdicionarBorda(p, Borda);
        Arredondar(p, 10);
        return p;
    }

    public static void EstilizarCampoTexto(Control c)
    {
        c.BackColor = SuperficieAlta;
        c.ForeColor = Texto;
        if (c is TextBox tb) tb.BorderStyle = BorderStyle.FixedSingle;
    }

    public static void EstilizarNumerico(NumericUpDown n)
    {
        n.BackColor = SuperficieAlta;
        n.ForeColor = Texto;
        n.BorderStyle = BorderStyle.FixedSingle;
    }

    public static void EstilizarComboBox(ComboBox c)
    {
        c.BackColor = SuperficieAlta;
        c.ForeColor = Texto;
        c.FlatStyle = FlatStyle.Flat;
    }

    public static void EstilizarGrid(DataGridView g)
    {
        g.BackgroundColor = Superficie;
        g.BorderStyle = BorderStyle.None;
        g.GridColor = Borda;
        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersDefaultCellStyle.BackColor = SuperficieAlta;
        g.ColumnHeadersDefaultCellStyle.ForeColor = TextoSecundario;
        g.ColumnHeadersDefaultCellStyle.Font = FonteRotulo;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = SuperficieAlta;
        g.ColumnHeadersHeight = 32;
        g.DefaultCellStyle.BackColor = Superficie;
        g.DefaultCellStyle.ForeColor = Texto;
        g.DefaultCellStyle.SelectionBackColor = SuperficieSelecionada;
        g.DefaultCellStyle.SelectionForeColor = Texto;
        g.DefaultCellStyle.Font = FonteCorpo;
        g.AlternatingRowsDefaultCellStyle.BackColor = SuperficieAlta;
        g.RowHeadersVisible = false;
        g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        g.BackgroundColor = Superficie;
    }

    /// <summary>
    /// Clipa o controle numa região de cantos arredondados. Reaplica a cada
    /// redimensionamento (necessário para controles ancorados que esticam).
    /// </summary>
    public static void Arredondar(Control c, int raio)
    {
        void Aplicar()
        {
            if (c.Width <= 1 || c.Height <= 1) return;

            int d = raio * 2;
            var r = new Rectangle(0, 0, c.Width, c.Height);
            using var caminho = new GraphicsPath();
            caminho.AddArc(r.X, r.Y, d, d, 180, 90);
            caminho.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            caminho.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            caminho.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            caminho.CloseFigure();
            c.Region = new Region(caminho);
        }

        Aplicar();
        c.Resize += (_, _) => Aplicar();
    }

    public static void AdicionarBorda(Control c, Color cor, int espessura = 1)
    {
        c.Paint += (_, e) =>
        {
            using var caneta = new Pen(cor, espessura);
            e.Graphics.DrawRectangle(caneta, 0, 0, c.Width - espessura, c.Height - espessura);
        };
    }

    public static void AdicionarBordaTracejada(Control c, Color cor, int espessura = 1)
    {
        c.Paint += (_, e) =>
        {
            using var caneta = new Pen(cor, espessura) { DashStyle = DashStyle.Dash };
            e.Graphics.DrawRectangle(caneta, 0, 0, c.Width - espessura, c.Height - espessura);
        };
    }

    public static Color CorDoAlgoritmo(string nomeAlgoritmo) => nomeAlgoritmo switch
    {
        "Bubble Sort" => CorBubble,
        "Quick Sort" => CorQuick,
        "Heap Sort" => CorHeap,
        "Shell Sort" => CorShell,
        _ => TextoSecundario
    };
}
