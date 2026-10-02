using System.Numerics;
using Conde.Polinomio;
using Xunit;

namespace Conde.Polinomio.Testes;

/// <summary>
/// Interpolacao: Lagrange e Newton constroem o MESMO polinomio por caminhos que
/// nao se parecem, e e isso que faz um servir de juiz do outro.
/// </summary>
public class InterpolacaoTest
{
    /// <summary>
    /// O teorema de unicidade, conferido em 800 conjuntos de pontos: por n+1
    /// pontos com abscissas distintas passa EXATAMENTE um polinomio de grau no
    /// maximo n.
    ///
    /// Lagrange soma polinomios base; Newton monta uma tabela triangular de
    /// diferencas divididas. Nao ha nada em comum entre os dois caminhos, entao
    /// concordarem coeficiente a coeficiente, em fracao exata, nao e coincidencia.
    /// </summary>
    [Fact]
    public void LagrangeENewtonDaoOMesmoPolinomio()
    {
        var r = new Random(1795);
        for (var caso = 0; caso < 800; caso++)
        {
            var n = 1 + r.Next(8);
            var (x, y) = SortearPontos(n, r);
            Assert.Equal(Interpolacao.Lagrange(x, y), Interpolacao.Newton(x, y));
        }
    }

    /// <summary>
    /// O polinomio passa PELOS pontos, nao perto deles: em fracao exata isso e
    /// uma igualdade. Com ponto flutuante seria uma tolerancia, e a diferenca
    /// entre as duas coisas e o motivo de este projeto usar fracao.
    /// </summary>
    [Fact]
    public void OPolinomioPassaExatamentePelosPontos()
    {
        var r = new Random(1687);
        for (var caso = 0; caso < 800; caso++)
        {
            var n = 1 + r.Next(9);
            var (x, y) = SortearPontos(n, r);
            var p = Interpolacao.Newton(x, y);
            for (var i = 0; i < x.Count; i++)
                Assert.Equal(new Fracao(y[i]), p.Avaliar(x[i]));
            Assert.True(p.Grau <= n, $"o grau saiu {p.Grau} com {n + 1} pontos");
        }
    }

    /// <summary>
    /// Interpolar um polinomio nos proprios pontos devolve ELE, qualquer que seja
    /// o conjunto de abscissas. E a unicidade vista de outro angulo, e e um teste
    /// forte porque a resposta e conhecida de antemao.
    /// </summary>
    [Fact]
    public void InterpolarUmPolinomioDevolveEle()
    {
        var r = new Random(1770);
        for (var caso = 0; caso < 500; caso++)
        {
            var grau = r.Next(0, 7);
            var original = MultiplicacaoTest.Sortear(grau, r);
            var x = new List<BigInteger>();
            var y = new List<BigInteger>();
            var usados = new HashSet<int>();
            while (x.Count <= grau)
            {
                var v = r.Next(-20, 21);
                if (!usados.Add(v)) continue;
                x.Add(v);
                y.Add(original.Avaliar(v));
            }
            Assert.Equal(Racional.De(original), Interpolacao.Newton(x, y));
        }
    }

    /// <summary>
    /// Abscissa repetida nao passa calada: sem distincao nao ha unicidade, e com
    /// duas ordenadas diferentes no mesmo x nao existe polinomio nenhum.
    /// </summary>
    [Fact]
    public void AbscissaRepetidaNaoPassa()
    {
        BigInteger[] x = [1, 2, 2];
        BigInteger[] y = [10, 20, 30];
        Assert.Throws<ArgumentException>(() => Interpolacao.Newton(x, y));
        Assert.Throws<ArgumentException>(() => Interpolacao.Lagrange(x, y));
    }

    /// <summary>
    /// A diferenca dividida de ordem n de um polinomio de grau n e EXATAMENTE o
    /// coeficiente lider dele, e a de ordem n+1 e zero.
    ///
    /// E o analogo discreto da derivada: a n-esima derivada de um polinomio de
    /// grau n e constante, e a seguinte e zero. Essa identidade pega erro de
    /// deslocamento na tabela triangular, que e o defeito classico ali.
    /// </summary>
    [Fact]
    public void ADiferencaDivididaDeOrdemNDaOCoeficienteLider()
    {
        var r = new Random(1710);
        for (var caso = 0; caso < 400; caso++)
        {
            var grau = r.Next(1, 7);
            var p = MultiplicacaoTest.Sortear(grau, r);
            var x = new List<BigInteger>();
            var y = new List<BigInteger>();
            var usados = new HashSet<int>();
            while (x.Count <= grau + 1)
            {
                var v = r.Next(-15, 16);
                if (!usados.Add(v)) continue;
                x.Add(v);
                y.Add(p.Avaliar(v));
            }
            var dif = Interpolacao.DiferencasDivididas(x, y);
            Assert.Equal(new Fracao(p.Lider), dif[grau]);
            Assert.Equal(Fracao.Zero, dif[grau + 1]);
        }
    }

    /// <summary>
    /// O caso mais simples que ainda tem conteudo: dois pontos dao a reta que
    /// passa por eles, e o coeficiente angular e a diferenca dividida de ordem 1.
    /// </summary>
    [Fact]
    public void DoisPontosDaoAReta()
    {
        BigInteger[] x = [2, 5];
        BigInteger[] y = [7, 16];
        var p = Interpolacao.Newton(x, y);
        Assert.Equal(1, p.Grau);
        Assert.Equal(new Fracao(3), p[1]);     // (16-7)/(5-2) = 3
        Assert.Equal(new Fracao(1), p[0]);     // 7 - 3*2 = 1
    }

    private static (List<BigInteger> X, List<BigInteger> Y) SortearPontos(int n, Random r)
    {
        var x = new List<BigInteger>();
        var y = new List<BigInteger>();
        var usados = new HashSet<int>();
        while (x.Count <= n)
        {
            var v = r.Next(-25, 26);
            if (!usados.Add(v)) continue;
            x.Add(v);
            y.Add(r.Next(-100, 101));
        }
        return (x, y);
    }
}
