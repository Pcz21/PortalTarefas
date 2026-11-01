using System.ComponentModel.DataAnnotations;

namespace PortalTarefas.Web.Models;

public enum Prioridade { Baixa = 1, Normal = 2, Alta = 3 }

public class Tarefa
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string? Descricao { get; set; }
    public bool Concluida { get; set; }
    public Prioridade Prioridade { get; set; } = Prioridade.Normal;
    public DateOnly? Prazo { get; set; }
    public byte[] Versao { get; set; } = [];
    public string? ProprietarioId { get; set; }
}
public class EventoTarefa
{
    public long Id { get; set; }
    public int TarefaId { get; set; }
    public string Operacao { get; set; } = "";
    public DateTime InstanteUtc { get; set; }
}

public class TarefaEntrada
{
    private string titulo = "";

    [Required(ErrorMessage = "Informe o título.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O título deve ter entre 3 e 100 caracteres.")]
    [Display(Name = "Título")]
    public string Titulo { get => titulo; set => titulo = value?.Trim() ?? ""; }

    [StringLength(500, ErrorMessage = "A descrição deve ter até 500 caracteres.")]
    [Display(Name = "Descrição")]
    public string? Descricao { get; set; }

    [Display(Name = "Concluída")]
    public bool Concluida { get; set; }

    public Dictionary<string, string[]> Validar()
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(this, new ValidationContext(this), resultados, true);
        return resultados.SelectMany(r => r.MemberNames.Select(m => (Campo: m, Erro: r.ErrorMessage!)))
            .GroupBy(r => r.Campo).ToDictionary(g => g.Key, g => g.Select(r => r.Erro).ToArray());
    }
}
