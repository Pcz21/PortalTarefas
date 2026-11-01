using System.ComponentModel.DataAnnotations;
namespace PortalTarefas.Web.Models;
public class CriarTarefaDto : TarefaEntrada, IValidatableObject
{
    [EnumDataType(typeof(Prioridade), ErrorMessage = "Prioridade inválida.")]
    public Prioridade Prioridade { get; set; } = Prioridade.Normal;
    public DateOnly? Prazo { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Prioridade == Prioridade.Alta && Prazo is null)
            yield return new ValidationResult("Prioridade alta exige prazo.", [nameof(Prazo)]);
    }
}
public class AtualizarTarefaDto : CriarTarefaDto
{
    [Required(ErrorMessage = "Informe a versão original.")]
    public string Versao { get; set; } = "";
}
public record ResumoTarefaDto(int Id, string Titulo, bool Concluida, Prioridade Prioridade, DateOnly? Prazo);
public record DetalheTarefaDto(int Id, string Titulo, string? Descricao, bool Concluida, Prioridade Prioridade, DateOnly? Prazo, string Versao)
{
    public static DetalheTarefaDto De(Tarefa t) => new(t.Id,t.Titulo,t.Descricao,t.Concluida,t.Prioridade,t.Prazo,Convert.ToBase64String(t.Versao));
}
public record PaginaTarefas(ResumoTarefaDto[] Itens, int Pagina, int Tamanho, int Total, int TotalPaginas);
