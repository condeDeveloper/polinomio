using System.Numerics;

namespace Conde.Polinomio;

/// <summary>
/// Raizes racionais e fatores repetidos, tudo em aritmetica exata.
///
/// Achar raiz de polinomio em ponto flutuante e um campo minado conhecido: uma
/// raiz dupla vira duas raizes proximas ou um par complexo, dependendo do erro, e
/// nao ha tolerancia que decida isso direito. Em aritmetica exata a pergunta
/// "esta raiz e dupla?" tem resposta de sim ou nao.
/// </summary>
public static class Raizes
{
    /// <summary>
    /// O teorema da raiz racional: se p/q em forma reduzida e raiz de um
    /// polinomio de coeficientes inteiros, entao p divide o termo CONSTANTE e q
    /// divide o coeficiente LIDER.
    ///
    /// Isso transforma uma busca infinita numa busca finita, e e um dos poucos
    /// resultados que dao TODAS as raizes de um tipo sem aproximacao nenhuma.
    ///
    /// O caso do termo constante zero precisa de cuidado: ali o zero e raiz e todo
    /// p dividiria, entao o fator x e tirado antes.
    /// </summary>
    public static List<Fracao> Racionais(Inteiro p)
    {
        ArgumentNullException.ThrowIfNull(p);
        if (p.EhZero) throw new ArgumentException("todo numero e raiz do polinomio zero", nameof(p));

        var saida = new List<Fracao>();
        // Tira os fatores x: o zero e raiz e o termo constante precisa ser nao nulo
        // para o teorema valer.
        var k = 0;
        while (p[k].IsZero) k++;
        if (k > 0)
        {
            saida.Add(Fracao.Zero);
            var c = new BigInteger[p.Grau - k + 1];
            for (var i = 0; i <= p.Grau - k; i++) c[i] = p[i + k];
            p = Inteiro.De(c);
        }
        if (p.Grau == 0) return saida;

        foreach (var numerador in Divisores(BigInteger.Abs(p[0])))
            foreach (var denominador in Divisores(BigInteger.Abs(p.Lider)))
            {
                foreach (var sinal in new[] { 1, -1 })
                {
                    var candidato = new Fracao(numerador * sinal, denominador);
                    if (saida.Contains(candidato)) continue;
                    if (Racional.De(p).Avaliar(candidato).EhZero) saida.Add(candidato);
                }
            }
        saida.Sort();
        return saida;
    }

    /// <summary>Todos os divisores positivos, por tentativa ate a raiz.</summary>
    public static List<BigInteger> Divisores(BigInteger n)
    {
        if (n.Sign <= 0) throw new ArgumentOutOfRangeException(nameof(n), "divisores de um positivo");
        var saida = new List<BigInteger>();
        for (BigInteger d = 1; d * d <= n; d++)
        {
            if (!(n % d).IsZero) continue;
            saida.Add(d);
            if (d * d != n) saida.Add(n / d);
        }
        saida.Sort();
        return saida;
    }

    /// <summary>
    /// A parte sem fator repetido: p dividido pelo maximo divisor comum entre p e
    /// a derivada dele.
    ///
    /// O argumento e bonito e tem uma linha: se (x-a)^k divide p com k maior que
    /// 1, entao (x-a)^(k-1) divide a derivada, porque derivar baixa o expoente em
    /// um. Logo o MDC entre p e p' junta exatamente os fatores repetidos, e
    /// dividir por ele deixa cada raiz uma vez so.
    ///
    /// Em ponto flutuante isso nao funciona, porque o MDC exige comparar com zero.
    /// </summary>
    public static Racional SemFatorRepetido(Inteiro p)
    {
        ArgumentNullException.ThrowIfNull(p);
        if (p.EhZero) throw new ArgumentException("o polinomio zero nao tem parte livre de quadrado", nameof(p));
        var r = Racional.De(p);
        var d = r.Derivada();
        if (d.EhZero) return r.Monico();
        var mdc = Racional.Mdc(r, d);
        var (q, resto) = Racional.DividirComResto(r, mdc);
        if (!resto.EhZero) throw new InvalidOperationException("o MDC nao dividiu exato; isso e defeito");
        return q.Monico();
    }

    /// <summary>
    /// Verdadeiro quando o polinomio tem algum fator repetido. E uma pergunta de
    /// sim ou nao, decidida sem tolerancia: o MDC com a derivada tem grau maior
    /// que zero.
    /// </summary>
    public static bool TemFatorRepetido(Inteiro p)
    {
        ArgumentNullException.ThrowIfNull(p);
        if (p.EhZero) return true;
        var r = Racional.De(p);
        var d = r.Derivada();
        return !d.EhZero && Racional.Mdc(r, d).Grau > 0;
    }

    /// <summary>
    /// A multiplicidade de uma raiz: quantas vezes (x - a) divide o polinomio.
    /// Zero quando a nem e raiz.
    /// </summary>
    public static int Multiplicidade(Inteiro p, Fracao a)
    {
        ArgumentNullException.ThrowIfNull(p);
        if (p.EhZero) throw new ArgumentException("multiplicidade no polinomio zero nao faz sentido", nameof(p));
        var r = Racional.De(p);
        var fator = Racional.De(-a, Fracao.Um);   // (x - a)
        var n = 0;
        while (true)
        {
            var (q, resto) = Racional.DividirComResto(r, fator);
            if (!resto.EhZero) return n;
            r = q;
            n++;
        }
    }
}
