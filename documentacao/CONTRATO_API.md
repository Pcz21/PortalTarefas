# Contrato REST v1

Prefixo `/api/v1/tarefas`. CriarTarefaDto, AtualizarTarefaDto, ResumoTarefaDto e DetalheTarefaDto separam entrada, resumo e detalhes da entidade EF.

| Método | Recurso | Resposta |
|---|---|---|
| GET | /?pagina=1&tamanho=10&ordenarPor=id&direcao=asc | 200, itens e totalPaginas; X-Total-Count |
| GET | /{id} | 200 ou Problem Details 404 |
| POST | / | 201, Location e detalhes; validação 400 |
| PUT | /{id} | 200; versão Base64 obrigatória; 409 em conflito |
| DELETE | /{id}?versao=base64 | 204; 400 para versão inválida; 404 ou 409 |

Página 1–100000, tamanho 1–50; ordenação permitida id, titulo e prazo; direção asc ou desc. Filtros: concluida, titulo e prioridade. A paginação e os filtros são aplicados no SQL antes da materialização. Ordenações incluem ID para desempate estável.

## Regenerar o cliente

Com o servidor em Development:

```powershell
dotnet tool restore
Invoke-WebRequest https://localhost:7157/openapi/v1.json -OutFile documentacao/openapi.json
dotnet nswag openapi2tsclient /input:documentacao/openapi.json /output:PortalTarefas.Frontend/src/api/generated.ts /classname:PortalTarefasClient /template:Fetch /operationGenerationMode:SingleClientFromOperationId /generateOptionalParameters:true /useAbortSignal:true
```

O arquivo gerado não deve ser editado manualmente. A camada api/client.ts adapta o transporte e os erros para a SPA. Scalar e OpenAPI são expostos somente em Development.
