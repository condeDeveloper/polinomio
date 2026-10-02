using System.Numerics;
using Conde.Polinomio;
using Xunit;

namespace Conde.Polinomio.Testes;

/// <summary>
/// Karatsuba e a transformada contra a multiplicacao INGENUA, que e a definicao.
///
/// A ingenua esta no projeto justamente para ser o juiz: ela e a propria
/// definicao de produto de polinomios, cada termo contra cada termo. Um algoritmo
/// rapido so vale se concordar com ela em todo caso.
/// </summary>
public class MultiplicacaoTest
{
    /// <summary>
    /// Karatsuba contra a ingenua em 2.000 pares sorteados, com graus dos dois
    /// lados do corte. Os graus pequenos exercitam o caso base e os grandes a
    /// recursao, e os que caem exatamente no corte sao os que pegam erro de
    /// limite.
    /// </summary>
    [Fact]
    public void KaratsubaConcordaComAIngenua()
    {
        var r = new Random(1960);
        for (var caso = 0; caso < 2000; caso++)
        {
            var grauA = r.Next(0, 90);
            var grauB = r.Next(0, 90);
            var a = Sortear(grauA, r);
            var b = Sortear(grauB, r);
            Assert.Equal(a * b, Karatsuba.Multiplicar(a, b));
        }
    }

    /// <summary>
    /// Os graus exatamente no corte, um por um. O limite entre a recursao e o caso
    /// base e onde um erro de "menor" contra "menor ou igual" se esconde, e ele
    /// nao da excecao: da um coeficiente errado no meio.
    /// </summary>
    [Fact]
    public void OCorteDaRecursaoEstaCerto()
    {
        var r = new Random(7);
        for (var grau = Karatsuba.Corte - 4; grau <= Karatsuba.Corte + 4; grau++)
        {
            for (var k = 0; k < 20; k++)
            {
                var a = Sortear(grau, r);
                var b = Sortear(grau, r);
                Assert.Equal(a * b, Karatsuba.Multiplicar(a, b));
            }
        }
    }

    /// <summary>
    /// O grau do produto e a soma dos graus, e o coeficiente lider e o produto
    /// dos lideres. Sao duas identidades que valem sempre e que pegam erro de
    /// tamanho de vetor sem depender de sorteio.
    /// </summary>
    [Fact]
    public void OGrauEOLiderDoProdutoSaoPrevisiveis()
    {
        var r = new Random(11);
        for (var caso = 0; caso < 500; caso++)
        {
            var a = Sortear(r.Next(1, 60), r);
            var b = Sortear(r.Next(1, 60), r);
            var p = Karatsuba.Multiplicar(a, b);
            Assert.Equal(a.Grau + b.Grau, p.Grau);
            Assert.Equal(a.Lider * b.Lider, p.Lider);
        }
    }

    /// <summary>
    /// A transformada teorica dos numeros contra a ingenua modular, em 1.000
    /// pares. Aqui NAO ha tolerancia: a transformada num corpo finito e exata, e
    /// qualquer diferenca e defeito.
    /// </summary>
    [Fact]
    public void ATransformadaConcordaComAIngenuaModular()
    {
        var r = new Random(1965);
        for (var caso = 0; caso < 1000; caso++)
        {
            var a = SortearModular(r.Next(1, 70), r);
            var b = SortearModular(r.Next(1, 70), r);
            Assert.Equal(Ntt.MultiplicarIngenuo(a, b), Ntt.Multiplicar(a, b));
        }
    }

    /// <summary>
    /// A transformada e a inversa dela sao inversas de verdade: ida e volta
    /// devolve o vetor original, valor por valor. E a propriedade que todo o resto
    /// se apoia, e conferi-la isola a transformada da convolucao.
    /// </summary>
    [Fact]
    public void AIdaEVoltaDaTransformadaEhAIdentidade()
    {
        var r = new Random(23);
        foreach (var n in new[] { 1, 2, 4, 8, 16, 256, 1024 })
        {
            for (var caso = 0; caso < 20; caso++)
            {
                var original = new long[n];
                for (var i = 0; i < n; i++) original[i] = r.NextInt64(0, Modular.Primo);
                var a = (long[])original.Clone();
                Ntt.Transformar(a, false);
                Ntt.Transformar(a, true);
                Assert.Equal(original, a);
            }
        }
    }

    /// <summary>
    /// Tamanho que nao e potencia de dois nao passa calado. A permutacao por
    /// inversao de bits e o laco de borboletas supoem isso, e sem a conferencia o
    /// resultado sairia errado em silencio.
    /// </summary>
    [Fact]
    public void TamanhoQueNaoEhPotenciaDeDoisNaoPassa()
    {
        Assert.Throws<ArgumentException>(() => Ntt.Transformar(new long[3], false));
        Assert.Throws<ArgumentException>(() => Modular.RaizDaUnidade(3));
    }

    /// <summary>
    /// A raiz da unidade e exata: elevada a n da 1, e nenhuma potencia menor da.
    /// E a unica propriedade que a transformada usa, e e por isso que ela funciona
    /// tanto no complexo quanto num corpo finito.
    /// </summary>
    [Fact]
    public void ARaizDaUnidadeTemOrdemExata()
    {
        for (var k = 1; k <= 20; k++)
        {
            var n = 1 << k;
            var w = Modular.RaizDaUnidade(n);
            Assert.Equal(1, Modular.Potencia(w, n));
            // Nenhuma potencia propria menor pode dar 1; basta conferir n/2,
            // porque qualquer ordem menor divide n e portanto divide n/2.
            Assert.NotEqual(1, Modular.Potencia(w, n / 2));
        }
    }

    /// <summary>Multiplicar por zero da zero, e os casos de borda nao estouram.</summary>
    [Fact]
    public void OsCasosDeBordaNaoEstouram()
    {
        var p = Inteiro.De(1, 2, 3);
        Assert.Equal(Inteiro.Zero, Karatsuba.Multiplicar(p, Inteiro.Zero));
        Assert.Equal(Inteiro.Zero, Karatsuba.Multiplicar(Inteiro.Zero, p));
        Assert.Equal(p, Karatsuba.Multiplicar(p, Inteiro.Um));
        Assert.Empty(Ntt.Multiplicar([], [1, 2]));
        Assert.Equal(-1, Inteiro.Zero.Grau);
    }

    internal static Inteiro Sortear(int grau, Random r)
    {
        var c = new BigInteger[grau + 1];
        for (var k = 0; k <= grau; k++) c[k] = r.Next(-50, 51);
        c[grau] = r.Next(1, 51);   // garante o grau
        return Inteiro.De(c);
    }

    private static long[] SortearModular(int tamanho, Random r)
    {
        var v = new long[tamanho];
        for (var k = 0; k < tamanho; k++) v[k] = r.NextInt64(0, Modular.Primo);
        return v;
    }
}
