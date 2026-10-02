# polinomio

Polinômios do zero em C# e .NET 8: Karatsuba, transformada teórica dos números,
interpolação de Lagrange e Newton, raízes racionais e fatores repetidos. Tudo em
aritmética **exata**, com inteiro grande e fração.

```
$ dotnet medidor.dll transformada

       n   ingenua ms   transformada ms   razao
     256         1,26              0,21    6,1x
   1.024        19,40              0,84   23,2x
   4.096       352,49              3,61   97,7x
  16.384   (nao rodou)             18,30       -
```

## O juiz é a definição

A multiplicação ingênua — cada termo contra cada termo — está no projeto
justamente para ser julgada contra ela. Ela **é** a definição de produto de
polinômios, e um algoritmo rápido só vale se concordar com ela em todo caso.

Karatsuba é conferido em 2.000 pares sorteados com graus dos dois lados do corte,
mais os graus **exatamente no corte**, um por um: o limite entre a recursão e o
caso base é onde um erro de "menor" contra "menor ou igual" se esconde, e ele não
dá exceção — dá um coeficiente errado no meio.

E na transformada **não há tolerância nenhuma** na comparação. Num corpo finito a
transformada é exata: a raiz da unidade é exata, a ida e volta devolve o vetor
original valor por valor, e qualquer diferença é defeito, não arredondamento. É
essa a diferença para a FFT em `double`, onde a partir de certo tamanho o
arredondamento final erra e não há como saber sem um juiz.

## Karatsuba, e a conjectura que ele derrubou

O truque cabe numa linha. Partindo cada polinômio ao meio, o produto pede quatro
multiplicações de metade do tamanho. Karatsuba notou que o termo do meio sai de
graça:

```
a1·b0 + a0·b1 = (a1 + a0)·(b1 + b0) − a1·b1 − a0·b0
```

São **três** multiplicações em vez de quatro, e o expoente cai de log₂4 = 2 para
log₂3 = 1,585.

Isso derrubou uma conjectura de Kolmogorov, que tinha afirmado em 1956 que
multiplicar exigia n² operações. Karatsuba era aluno dele e levou uma semana.

```
$ dotnet medidor.dll karatsuba

    grau   ingenua ms   karatsuba ms   razao   crescimento ao dobrar
     256         1,27           0,88     1,4x   ingenua 3,0x, kara 2,8x
     512         5,05           2,84     1,8x   ingenua 4,0x, kara 3,2x
   1.024        20,72           7,41     2,8x   ingenua 4,1x, kara 2,6x
   2.048        66,39          23,25     2,9x   ingenua 3,2x, kara 3,1x
```

A última coluna é o que importa: dobrar o grau multiplica a ingênua por ~4 (que é
2²) e o Karatsuba por ~3 (que é 2^1,585). A razão entre os dois cresce com o
tamanho, e é isso que separa um expoente de outro.

## O corte que eu tinha chutado

Eu escrevi o corte da recursão em **32** de cabeça. A medição desmentiu:

```
$ dotnet medidor.dll corte

    grau   ingenua ms   karatsuba ms   quem ganha
      32       0,0169         0,0226      ingenua
      64       0,0792         0,0767      ingenua
     128       0,3218         0,2612    karatsuba
     256       1,4486         0,8706    karatsuba
```

No grau 64 a ingênua ainda ganha. O Karatsuba só passa a valer a pena a partir de
**128**, porque abaixo disso as três chamadas recursivas, as quatro somas de vetor
e as alocações custam mais que a dupla varredura. O corte está em 128 agora.

## A transformada num corpo finito não tem erro

É literalmente o mesmo algoritmo de Cooley-Tukey, com `e^(2πi/n)` trocado pela
raiz da unidade de ordem n módulo p. A transformada usa **uma** propriedade da
raiz: elevada a n dá 1, e nenhuma potência menor dá. O corpo finito tem isso, e de
brinde não tem arredondamento.

O primo não é arbitrário: 998.244.353 = 119·2²³ + 1. Um primo só serve se `p−1`
tiver uma potência de dois grande como fator, porque a transformada precisa de uma
raiz primitiva de ordem 2^k — e é por isso que o mesmo punhado de primos aparece
em todo código de competição do mundo.

O teste confere a ordem exata da raiz para todos os k de 1 a 20, e a ida e volta da
transformada em tamanhos de 1 a 1.024.

## A interpolação: dois caminhos, um polinômio

Por n+1 pontos com abscissas **distintas** passa exatamente **um** polinômio de
grau no máximo n. Não "pelo menos um", não "vários": um.

Lagrange soma n+1 polinômios base, cada um valendo 1 no seu ponto e 0 nos outros.
Newton monta uma tabela triangular de diferenças divididas. Não há nada em comum
entre os dois caminhos — então concordarem **coeficiente a coeficiente, em fração
exata**, em 800 conjuntos de pontos, não é coincidência: é o teorema de unicidade
aparecendo.

E o polinômio passa **pelos** pontos, não perto deles. Em fração exata isso é uma
igualdade; com ponto flutuante seria uma tolerância, e a diferença entre as duas
coisas é o motivo de este projeto usar fração.

O teste que mais pega defeito: a diferença dividida de ordem n de um polinômio de
grau n é **exatamente** o coeficiente líder dele, e a de ordem n+1 é zero. É o
análogo discreto da derivada, e pega erro de deslocamento na tabela triangular,
que é o defeito clássico ali.

## Raiz dupla é uma pergunta de sim ou não

Em ponto flutuante uma raiz dupla vira duas raízes próximas ou um par complexo,
dependendo do erro, e não há tolerância que decida isso direito. Em aritmética
exata a pergunta tem resposta.

O argumento é de uma linha: se `(x−a)^k` divide p com k maior que 1, então
`(x−a)^(k−1)` divide a derivada, porque derivar baixa o expoente em um. Logo o MDC
entre p e p′ junta **exatamente** os fatores repetidos, e dividir por ele deixa
cada raiz uma vez só.

O teorema da raiz racional transforma uma busca infinita numa finita: se p/q em
forma reduzida é raiz, então p divide o termo constante e q divide o líder. O
juiz aqui é a **construção**: monta-se o polinômio a partir das raízes que se
quer, multiplicando os fatores, e exige-se que a busca devolva exatamente aquelas.
É mais forte que procurar raízes num polinômio sorteado, porque a resposta é
conhecida antes de calcular.

E o caso do termo constante zero precisa de tratamento separado: ali todo
numerador dividiria e o teorema perde o sentido. Esquecer isso devolve lixo ou
nada.

## Por que `BigInteger` e não `long`

```
$ dotnet medidor.dll crescimento

    grau   digitos na entrada   digitos no produto   cabe em long
       1                   40                   80            NAO
   1.000                   40                   80            NAO
```

Com coeficientes de 40 dígitos, o produto tem 80. Com `long` isso estouraria em
silêncio — e um juiz que estoura em silêncio é pior que nenhum.

## As peças

| arquivo | o que faz |
|---|---|
| `Inteiro` | coeficientes `BigInteger`, Horner, divisão com resto |
| `Fracao` | fração exata, para o que sai do mundo dos inteiros |
| `Racional` | polinômio sobre um corpo; divisão sempre fecha, e daí sai o MDC |
| `Karatsuba` | três multiplicações em vez de quatro |
| `Modular` | aritmética módulo primo, e por que o primo é aquele |
| `Ntt` | a FFT num corpo finito, sem erro nenhum |
| `Interpolacao` | Lagrange e Newton, um servindo de juiz do outro |
| `Raizes` | raízes racionais, multiplicidade e parte livre de quadrado |

São 23 testes.

## Como rodar

```
dotnet test testes/Polinomio.Testes/Polinomio.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- karatsuba
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- corte
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- transformada
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- crescimento
```

## Licença

MIT.
