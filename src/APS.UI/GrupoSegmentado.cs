namespace APS.UI;

/// <summary>
/// Widget de UI puramente visual: um grupo de botões estilo "chip" que funciona
/// como rádio (só um selecionado por vez). Reaproveitado pelos seletores de fonte
/// de dados, distribuição e campo de ordenação.
/// </summary>
public sealed class GrupoSegmentado
{
    private readonly List<Button> _botoes = new();

    public int Selecionado { get; private set; }
    public event Action<int>? Mudou;

    /// <param name="larguraMaxima">
    /// Largura além da qual os chips quebram para a linha de baixo. Sem isso, um
    /// FlowLayoutPanel com AutoSize cresce para caber tudo numa linha só e o
    /// WrapContents nunca entra em ação — é o que limita a quebra de linha.
    /// </param>
    public FlowLayoutPanel Construir(IReadOnlyList<string> opcoes, int indiceInicial = 0, int larguraMaxima = 0)
    {
        Selecionado = indiceInicial;
        var painel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent
        };
        if (larguraMaxima > 0)
            painel.MaximumSize = new Size(larguraMaxima, 0);

        for (int i = 0; i < opcoes.Count; i++)
        {
            int indice = i;
            var botao = new Button
            {
                Text = opcoes[i],
                Font = Tema.FonteCorpoNegrito,
                Height = 30,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 6, 6),
                Padding = new Padding(12, 0, 12, 0),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            botao.FlatAppearance.BorderSize = 1;
            botao.Click += (_, _) => Selecionar(indice);
            Tema.Arredondar(botao, 15);

            _botoes.Add(botao);
            painel.Controls.Add(botao);
        }

        AtualizarEstilos();
        return painel;
    }

    public void Selecionar(int indice)
    {
        if (indice < 0 || indice >= _botoes.Count || indice == Selecionado)
        {
            Selecionado = indice;
            AtualizarEstilos();
            return;
        }

        Selecionado = indice;
        AtualizarEstilos();
        Mudou?.Invoke(indice);
    }

    private void AtualizarEstilos()
    {
        for (int i = 0; i < _botoes.Count; i++)
        {
            bool selecionado = i == Selecionado;
            _botoes[i].BackColor = selecionado ? Tema.SuperficieSelecionada : Color.Transparent;
            _botoes[i].ForeColor = selecionado ? Tema.Acento : Tema.TextoSecundario;
            _botoes[i].FlatAppearance.BorderColor = selecionado ? Tema.Acento : Tema.Borda;
        }
    }
}
