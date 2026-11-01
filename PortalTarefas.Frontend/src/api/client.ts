import {PortalTarefasClient,CriarTarefaDto} from './generated';
import {normalizarErro} from './errors';
const base=import.meta.env.VITE_API_URL||'';
let csrf:string|undefined;
export async function obterCsrf(force=false){if(!csrf||force){const r=await fetch(base+'/auth/csrf',{credentials:'same-origin'});csrf=(await r.json()).token}return csrf!}
export const http={fetch:async(url:RequestInfo,options:RequestInit={})=>{
 const headers=new Headers(options.headers);
 if(!['GET','HEAD','OPTIONS'].includes((options.method||'GET').toUpperCase()))headers.set('X-CSRF-TOKEN',await obterCsrf());
 return fetch(url,{...options,headers,credentials:'same-origin'});
}};
export const cliente=new PortalTarefasClient(base,http);
async function executar<T>(fn:()=>Promise<T>):Promise<T>{try{return await fn()}catch(e:any){if(e?.name==='AbortError')throw e;throw normalizarErro(e)}}
export const api={
 listar:(q:URLSearchParams,signal?:AbortSignal)=>executar(()=>cliente.listarTarefas(Number(q.get('pagina')||1),Number(q.get('tamanho')||5),q.get('ordenarPor')||'id',q.get('direcao')||'asc',q.has('concluida')?q.get('concluida')==='true':undefined,q.get('titulo')||undefined,undefined,signal)),
 obter:(id:number,signal?:AbortSignal)=>executar(()=>cliente.obterTarefa(id,signal)),
 criar:(dados:{titulo:string;descricao:string;prioridade:number;prazo:string})=>executar(()=>cliente.criarTarefa(new CriarTarefaDto({...dados,prazo:dados.prazo?new Date(dados.prazo+'T00:00:00'):undefined})))
};
