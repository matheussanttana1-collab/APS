namespace APS.Core.Modelos;

/// <summary>
/// Progresso de uma suíte de benchmark em andamento, reportado via IProgress&lt;T&gt;
/// para a UI atualizar a barra de progresso sem a suíte conhecer nada de interface.
/// </summary>
public class ProgressoBenchmark
{
    public int Concluidos { get; set; }
    public int Total { get; set; }
    public string DescricaoAtual { get; set; } = string.Empty;
    public double Percentual => Total == 0 ? 0 : (double)Concluidos / Total * 100;
}
