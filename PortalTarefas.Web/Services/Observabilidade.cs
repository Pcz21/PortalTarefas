using System.Diagnostics;
using System.Diagnostics.Metrics;
namespace PortalTarefas.Web.Services;
public static class Observabilidade
{
    public const string Nome="PortalTarefas";
    public static readonly ActivitySource Traces=new(Nome);
    private static readonly Meter Meter=new(Nome);
    private static readonly Counter<long> Criadas=Meter.CreateCounter<long>("portal.tarefas.criadas",description:"Tarefas criadas neste processo");
    public static readonly Histogram<double> Duracao=Meter.CreateHistogram<double>("portal.operacao.duracao","ms");
    private static long total;
    public static long Total=>Interlocked.Read(ref total);
    public static void RegistrarCriacao(){Interlocked.Increment(ref total);Criadas.Add(1,new KeyValuePair<string,object?>("operacao","criar"));}
}
