export type ErroApi={status:number;mensagem:string;campos:Record<string,string[]>;tipo:'rede'|'http'};
export function normalizarErro(error:any):ErroApi {
 const status=typeof error?.status==='number'?error.status:0;
 let problem=error?.result || (error?.title || error?.errors ? error : undefined);
 if(!problem && typeof error?.response==='string'){try{problem=JSON.parse(error.response)}catch{}}
 return {status,tipo:status?'http':'rede',mensagem:status?(problem?.title||`A operação retornou HTTP ${status}.`):'Não foi possível conectar ao servidor.',campos:problem?.errors||{}};
}
