using System.Numerics;
using Conde.Polinomio;
using Xunit;

namespace Conde.Polinomio.Testes;

/// <summary>
/// Raizes racionais, fatores repetidos e divisao, tudo em aritmetica exata.
/// </summary>
public class RaizesTest
{
    /// <summary>
    /// O juiz e a construcao: monta-se o polinomio a PARTIR das raizes que se
    /// quer, multiplicando os fatores, e depois exige-se que o teorema da raiz
    /// racional devolva exatamente aquelas.
    ///
    /// Isso e mais forte que procurar raizes num polinomio sorteado, porque a
    /// resposta e conhecida antes de calcular.
    /// </summary>
    [Fact]
    public void AsRaizesConstruidasSaoTodasAchadas()
    {
        var r = new Random(1824);
        for (var caso = 0; caso < 500; caso++)
        {
            var quantas = 1 + r.Next(4);
            var esperadas = new SortedSet<Fracao>();
            var p = Inteiro.Um;
            for (var k = 0; k < quantas; k++)
            {
                var cima = r.Next(-8, 9);
                var baixo = r.Next(1, 5);
                var raiz = new Fracao(cima, baixo);
                esperadas.Add(raiz);
                // (baixo*x - cima), que tem raiz cima/baixo e coeficientes inteiros
                p *= Inteiro.De((long)-raiz.Cima, (long)raiz.Baixo);
            }
            var achadas = Raizes.Racionais(p);
            Assert.Equal(esperadas.ToList(), achadas);
        }
    }

    /// <summary>
    /// Um polinomio sem raiz racional nenhuma devolve lista vazia. x^2 - 2 e o
    /// exemplo classico: a raiz existe e e irracional, e o teorema so acha as
    /// racionais.
    /// </summary>
    [Fact]
    public void SemRaizRacionalADevolveVazio()
    {
        Assert.Empty(Raizes.Racionais(Inteiro.De(-2, 0, 1)));      // x^2 - 2
        Assert.Empty(Raizes.Racionais(Inteiro.De(1, 0, 1)));       // x^2 + 1
        Assert.Empty(Raizes.Racionais(Inteiro.De(-2, 0, 0, 1)));   // x^3 - 2
    }

    /// <summary>
    /// O zero so e raiz quando o termo constante e zero, e precisa de tratamento
    /// separado: com constante zero, TODO numerador dividiria e o teorema perde o
    /// sentido. Esquecer isso devolve lixo ou nada.
    /// </summary>
    [Fact]
    public void OZeroEhTratadoSeparado()
    {
        // x^3 - x = x(x-1)(x+1): raizes 0, 1 e -1
        var p = Inteiro.De(0, -1, 0, 1);
        Assert.Equal([new Fracao(-1), Fracao.Zero, Fracao.Um], Raizes.Racionais(p));
        Assert.Equal(1, Raizes.Multiplicidade(p, Fracao.Zero));

        // x^3 = x*x*x: o zero com multiplicidade 3
        var q = Inteiro.De(0, 0, 0, 1);
        Assert.Equal([Fracao.Zero], Raizes.Racionais(q));
        Assert.Equal(3, Raizes.Multiplicidade(q, Fracao.Zero));
    }

    /// <summary>
    /// A multiplicidade construida e a achada batem. Em ponto flutuante uma raiz
    /// dupla vira duas raizes proximas ou um par complexo, dependendo do erro, e
    /// nao ha tolerancia que decida. Em exato, a pergunta tem resposta.
    /// </summary>
    [Fact]
    public void AMultiplicidadeConstruidaEhAAchada()
    {
        var r = new Random(1832);
        for (var caso = 0; caso < 300; caso++)
        {
            var raiz = r.Next(-5, 6);
            var vezes = 1 + r.Next(4);
            var p = Inteiro.Um;
            for (var k = 0; k < vezes; k++) p *= Inteiro.De(-raiz, 1);
            // Um fator extra sem essa raiz, para nao ficar trivial.
            var outra = raiz + 1 + r.Next(3);
            p *= Inteiro.De(-outra, 1);

            Assert.Equal(vezes, Raizes.Multiplicidade(p, new Fracao(raiz)));
            Assert.Equal(vezes > 1, Raizes.TemFatorRepetido(p));
        }
    }

    /// <summary>
    /// Tirar os fatores repetidos deixa cada raiz uma vez so, e o grau da parte
    /// livre e o numero de raizes distintas.
    ///
    /// O argumento e de uma linha: se (x-a)^k divide p com k maior que 1, entao
    /// (x-a)^(k-1) divide a derivada, porque derivar baixa o expoente em um. Logo
    /// o MDC entre p e p' junta exatamente os fatores repetidos.
    /// </summary>
    [Fact]
    public void TirarORepetidoDeixaCadaRaizUmaVez()
    {
        var r = new Random(1850);
        for (var caso = 0; caso < 300; caso++)
        {
            var distintas = 1 + r.Next(4);
            var raizes = new SortedSet<int>();
            while (raizes.Count < distintas) raizes.Add(r.Next(-6, 7));

            var p = Inteiro.Um;
            foreach (var a in raizes)
            {
                var vezes = 1 + r.Next(3);
                for (var k = 0; k < vezes; k++) p *= Inteiro.De(-a, 1);
            }

            var livre = Raizes.SemFatorRepetido(p);
            Assert.Equal(distintas, livre.Grau);
            foreach (var a in raizes)
                Assert.True(livre.Avaliar(new Fracao(a)).EhZero, $"{a} deixou de ser raiz da parte livre");
        }
    }

    /// <summary>
    /// A divisao com resto fecha: a = q*b + r com grau(r) menor que grau(b), em
    /// 1.000 pares. E a identidade que define a divisao, e ela nao depende de
    /// implementacao nenhuma.
    /// </summary>
    [Fact]
    public void ADivisaoFechaAIdentidade()
    {
        var r = new Random(1869);
        for (var caso = 0; caso < 1000; caso++)
        {
            var a = Racional.De(MultiplicacaoTest.Sortear(r.Next(0, 12), r));
            var b = Racional.De(MultiplicacaoTest.Sortear(r.Next(0, 8), r));
            var (q, resto) = Racional.DividirComResto(a, b);
            Assert.Equal(a, q * b + resto);
            Assert.True(resto.EhZero || resto.Grau < b.Grau,
                $"o resto saiu com grau {resto.Grau} e o divisor tem {b.Grau}");
        }
    }

    /// <summary>
    /// O MDC divide os dois e e o maior que divide: conferido contra a
    /// construcao, montando dois polinomios com um fator comum conhecido.
    /// </summary>
    [Fact]
    public void OMdcEhODivisorComumConstruido()
    {
        var r = new Random(1879);
        for (var caso = 0; caso < 300; caso++)
        {
            var comum = MultiplicacaoTest.Sortear(1 + r.Next(3), r);
            var a = Racional.De(comum * MultiplicacaoTest.Sortear(1 + r.Next(3), r));
            var b = Racional.De(comum * MultiplicacaoTest.Sortear(1 + r.Next(3), r));

            var mdc = Racional.Mdc(a, b);
            Assert.Equal(Racional.Zero, Racional.DividirComResto(a, mdc).Resto);
            Assert.Equal(Racional.Zero, Racional.DividirComResto(b, mdc).Resto);
            // E o fator comum construido divide o MDC.
            Assert.True(mdc.Grau >= comum.Grau,
                $"o MDC saiu com grau {mdc.Grau} e o fator comum tem {comum.Grau}");
            Assert.Equal(Fracao.Um, mdc[mdc.Grau]);   // monico
        }
    }

    /// <summary>
    /// A divisao inteira so fecha quando da exato, e devolver nulo e melhor que
    /// arredondar: arredondar daria um quociente plausivel e errado.
    /// </summary>
    [Fact]
    public void ADivisaoInteiraRecusaQuandoNaoFecha()
    {
        // (x^2 + 1) dividido por (2x) nao fecha em inteiro
        Assert.Null(Inteiro.DividirComResto(Inteiro.De(1, 0, 1), Inteiro.De(0, 2)));
        // mas (2x^2) por (2x) fecha
        var ok = Inteiro.DividirComResto(Inteiro.De(0, 0, 2), Inteiro.De(0, 2));
        Assert.NotNull(ok);
        Assert.Equal(Inteiro.De(0, 1), ok!.Value.Quociente);
        Assert.Equal(Inteiro.Zero, ok.Value.Resto);
        Assert.Throws<DivideByZeroException>(() => Inteiro.DividirComResto(Inteiro.Um, Inteiro.Zero));
    }

    /// <summary>
    /// Horner avalia igual a forma ingenua com potencias, em 2.000 casos. O ganho
    /// do Horner e de contagem, nao de valor: ele nao pode dar outra resposta.
    /// </summary>
    [Fact]
    public void HornerConcordaComAFormaIngenua()
    {
        var r = new Random(1819);
        for (var caso = 0; caso < 2000; caso++)
        {
            var p = MultiplicacaoTest.Sortear(r.Next(0, 15), r);
            var x = new BigInteger(r.Next(-20, 21));

            var ingenua = BigInteger.Zero;
            for (var k = 0; k <= p.Grau; k++) ingenua += p[k] * BigInteger.Pow(x, k);
            Assert.Equal(ingenua, p.Avaliar(x));
        }
    }
}
