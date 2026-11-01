import{test}from'node:test';import assert from'node:assert/strict';import{normalizarErro}from'../src/api/errors.ts';
test('falha de rede tem mensagem própria',()=>assert.equal(normalizarErro(new TypeError('fetch')).tipo,'rede'));
test('Problem Details preserva status e campos',()=>{const e=normalizarErro({status:400,result:{title:'Inválido',errors:{Titulo:['Obrigatório']}}});assert.equal(e.status,400);assert.deepEqual(e.campos.Titulo,['Obrigatório'])});
test('404 não é confundido com falha de rede',()=>assert.equal(normalizarErro({status:404}).tipo,'http'));
