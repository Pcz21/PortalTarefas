using System.ComponentModel.DataAnnotations;

namespace PortalTarefas.Web.Models;

// Reutiliza as validações de título e descrição da Unidade 1.
public class TarefaInputModel : TarefaEntrada, IValidatableObject
{
    public string Versao { get; set; } = "";
    [EnumDataType(typeof(Prioridade), ErrorMessage = "Selecione uma prioridade válida.")]
    public Prioridade Prioridade { get; set; } = Prioridade.Normal;

    [DataType(DataType.Date)]
    [Display(Name = "Prazo")]
    public DateOnly? Prazo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Prioridade == Prioridade.Alta && Prazo is null)
            yield return new ValidationResult("Tarefas de prioridade alta precisam de um prazo.", [nameof(Prazo)]);
    }

    public static TarefaInputModel DeTarefa(Tarefa tarefa) => new()
    {
        Titulo = tarefa.Titulo, Descricao = tarefa.Descricao, Concluida = tarefa.Concluida,
        Prioridade = tarefa.Prioridade, Prazo = tarefa.Prazo, Versao = Convert.ToBase64String(tarefa.Versao)
    };
}
