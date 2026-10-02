using System.Numerics;

namespace Conde.Polinomio;

/// <summary>
/// Karatsuba, 1960: multiplicar em O(n^1,585) em vez de O(n^2).
///
/// A ideia e a mesma do algoritmo para inteiros, e o truque cabe numa linha.
/// Partindo cada polinomio em duas metades, a = a1*x^m + a0 e b = b1*x^m + b0, o
/// produto pede quatro multiplicacoes de metade do tamanho:
///
///   a*b = a1*b1*x^2m + (a1*b0 + a0*b1)*x^m + a0*b0
///
/// Karatsuba notou que o termo do meio sai de graca:
///
///   a1*b0 + a0*b1 = (a1 + a0)*(b1 + b0) - a1*b1 - a0*b0
///
/// Sao TRES multiplicacoes em vez de quatro, e o expoente cai de log2(4) = 2
/// para log2(3) = 1,585. A recursao T(n) = 3*T(n/2) + O(n) resolve nisso.
///
/// Isso derrubou uma conjectura de Kolmogorov, que tinha afirmado em 1956 que
/// multiplicar exigia n^2 operacoes. Karatsuba era aluno dele e levou uma semana.
///
/// O corte para a multiplicacao ingenua nao e preciosismo: abaixo dele as tres
/// chamadas recursivas e as somas custam mais que a dupla varredura.
/// </summary>
public static class Karatsuba
{
    /// <summary>
    /// Abaixo deste grau a ingenua ganha. MEDIDO, e a medicao me corrigiu: eu
    /// tinha escrito 32 de cabeca, e nos graus 32 e 64 a ingenua ainda ganha, no
    /// 64 por mais de duas vezes. O karatsuba so passa a valer a pena a partir de
    /// 128, porque abaixo disso as tres chamadas recursivas, as quatro somas de
    /// vetor e as alocacoes custam mais que a dupla varredura.
    /// </summary>
    public const int Corte = 128;

    public static Inteiro Multiplicar(Inteiro a, Inteiro b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.EhZero || b.EhZero) return Inteiro.Zero;
        var c = Multiplicar(Vetor(a), Vetor(b));
        return Inteiro.De(c);
    }

    private static BigInteger[] Vetor(Inteiro p)
    {
        var v = new BigInteger[p.Grau + 1];
        for (var k = 0; k <= p.Grau; k++) v[k] = p[k];
        return v;
    }

    private static BigInteger[] Multiplicar(BigInteger[] a, BigInteger[] b)
    {
        if (a.Length == 0 || b.Length == 0) return [];
        if (a.Length <= Corte || b.Length <= Corte) return Ingenua(a, b);

        var m = Math.Max(a.Length, b.Length) / 2;
        var (a0, a1) = Partir(a, m);
        var (b0, b1) = Partir(b, m);

        var z0 = Multiplicar(a0, b0);
        var z2 = Multiplicar(a1, b1);
        // O termo do meio pela identidade de Karatsuba: uma multiplicacao, nao duas.
        var z1 = Subtrair(Subtrair(Multiplicar(Somar(a0, a1), Somar(b0, b1)), z0), z2);

        var saida = new BigInteger[a.Length + b.Length - 1];
        Acumular(saida, z0, 0);
        Acumular(saida, z1, m);
        Acumular(saida, z2, 2 * m);
        return saida;
    }

    private static BigInteger[] Ingenua(BigInteger[] a, BigInteger[] b)
    {
        var c = new BigInteger[a.Length + b.Length - 1];
        for (var i = 0; i < a.Length; i++)
        {
            if (a[i].IsZero) continue;
            for (var j = 0; j < b.Length; j++) c[i + j] += a[i] * b[j];
        }
        return c;
    }

    private static (BigInteger[] Baixo, BigInteger[] Alto) Partir(BigInteger[] v, int m)
    {
        if (v.Length <= m) return (v, []);
        var baixo = new BigInteger[m];
        var alto = new BigInteger[v.Length - m];
        Array.Copy(v, 0, baixo, 0, m);
        Array.Copy(v, m, alto, 0, alto.Length);
        return (baixo, alto);
    }

    private static BigInteger[] Somar(BigInteger[] a, BigInteger[] b)
    {
        var n = Math.Max(a.Length, b.Length);
        var c = new BigInteger[n];
        for (var k = 0; k < n; k++)
            c[k] = (k < a.Length ? a[k] : BigInteger.Zero) + (k < b.Length ? b[k] : BigInteger.Zero);
        return c;
    }

    private static BigInteger[] Subtrair(BigInteger[] a, BigInteger[] b)
    {
        var n = Math.Max(a.Length, b.Length);
        var c = new BigInteger[n];
        for (var k = 0; k < n; k++)
            c[k] = (k < a.Length ? a[k] : BigInteger.Zero) - (k < b.Length ? b[k] : BigInteger.Zero);
        return c;
    }

    private static void Acumular(BigInteger[] saida, BigInteger[] parcela, int deslocamento)
    {
        for (var k = 0; k < parcela.Length; k++)
        {
            var pos = k + deslocamento;
            if (pos < saida.Length) saida[pos] += parcela[k];
            else if (!parcela[k].IsZero) throw new InvalidOperationException("coeficiente fora do tamanho previsto");
        }
    }
}
