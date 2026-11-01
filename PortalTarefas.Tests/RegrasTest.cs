using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PortalTarefas.Web.Data;
using PortalTarefas.Web.Models;
namespace PortalTarefas.Tests;
public class RegrasTest
{
    private static bool Valido(object model)=>Validator.TryValidateObject(model,new ValidationContext(model),new List<ValidationResult>(),true);
    [Theory,InlineData(""),InlineData(" "),InlineData("ab")]
    public void TituloInvalidoNaoPassa(string titulo)=>Assert.False(Valido(new TarefaInputModel{Titulo=titulo}));
    [Fact]public void PrioridadeAltaExigePrazo()=>Assert.False(Valido(new TarefaInputModel{Titulo="Revisar relatório",Prioridade=Prioridade.Alta}));
    [Fact]public void PrioridadeAltaComPrazoPassa()=>Assert.True(Valido(new TarefaInputModel{Titulo="Revisar relatório",Prioridade=Prioridade.Alta,Prazo=DateOnly.FromDateTime(DateTime.Today)}));
    [Fact]public void DescricaoLongaNaoPassa()=>Assert.False(Valido(new CriarTarefaDto{Titulo="Título válido",Descricao=new string('a',501)}));
    [Fact]public void ModeloSqlMantemLimitesEConcorrencia()
    {
        using var db=new PortalTarefasDbContext(new DbContextOptionsBuilder<PortalTarefasDbContext>().UseSqlServer("Server=localhost;Database=NaoConectar;Integrated Security=true").Options);
        var entity=db.Model.FindEntityType(typeof(Tarefa))!;
        Assert.Equal(100,entity.FindProperty(nameof(Tarefa.Titulo))!.GetMaxLength());
        Assert.True(entity.FindProperty(nameof(Tarefa.Versao))!.IsConcurrencyToken);
        Assert.Contains(entity.GetIndexes(),i=>i.Properties.Select(p=>p.Name).SequenceEqual(new[]{"Concluida","Prazo"}));
    }
    [Fact,Trait("Category","Gate")]
    public void PipelineBloqueiaQuandoTesteFalha()=>Assert.NotEqual("1",Environment.GetEnvironmentVariable("PORTAL_FORCE_TEST_FAILURE"));
}
