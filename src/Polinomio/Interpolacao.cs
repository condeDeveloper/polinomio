using System.Numerics;

namespace Conde.Polinomio;

/// <summary>
/// Interpolacao: achar o polinomio que passa pelos pontos dados.
///
/// O teorema e de unicidade e e o que torna o assunto tratavel: por n+1 pontos
/// com abscissas DISTINTAS passa exatamente UM polinomio de grau no maximo n. Nao
/// "pelo menos um", nao "varios": um. Lagrange e Newton constroem o mesmo
/// polinomio por caminhos que nao se parecem, e e isso que faz um servir de juiz
/// do outro.
/// </summary>
public static class Interpolacao
{
    /// <summary>
    /// Lagrange: a soma dos n+1 polinomios base, cada um valendo 1 no seu ponto e
    /// 0 em todos os outros.
    ///
    /// A forma e a mais clara de todas e a pior de usar: acrescentar um ponto
    /// obriga a refazer tudo, e avaliar custa O(n^2). Ela esta aqui pela clareza
    /// e para servir de juiz da forma de Newton.
    ///
    /// Em fracao exata, para que o polinomio saia certo mesmo quando os
    /// coeficientes intermediarios nao sao inteiros.
    /// </summary>
    public static Racional Lagrange(IReadOnlyList<BigInteger> x, IReadOnlyList<BigInteger> y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        if (x.Count != y.Count) throw new ArgumentException("quantidades diferentes de abscissa e ordenada");
        ConferirDistintas(x);

        var soma = Racional.Zero;
        for (var i = 0; i < x.Count; i++)
        {
            // base_i(t) = produto de (t - x_j) / (x_i - x_j) para j diferente de i
            var baseI = Racional.Um;
            for (var j = 0; j < x.Count; j++)
            {
                if (j == i) continue;
                var denominador = new Fracao(x[i] - x[j]);
                baseI *= Racional.De(new Fracao(-x[j]) / denominador, Fracao.Um / denominador);
            }
            soma += baseI * new Fracao(y[i]);
        }
        return soma;
    }

    /// <summary>
    /// Newton, por diferencas divididas.
    ///
    /// A vantagem sobre Lagrange e pratica: os coeficientes sao calculados UMA vez
    /// e acrescentar um ponto novo custa O(n) em vez de refazer tudo. A tabela de
    /// diferencas divididas e triangular e cada linha depende so da anterior.
    ///
    /// O polinomio resultante e o MESMO de Lagrange, pelo teorema de unicidade, e
    /// o teste confere exatamente isso.
    /// </summary>
    public static Racional Newton(IReadOnlyList<BigInteger> x, IReadOnlyList<BigInteger> y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        if (x.Count != y.Count) throw new ArgumentException("quantidades diferentes de abscissa e ordenada");
        ConferirDistintas(x);
        var n = x.Count;
        if (n == 0) return Racional.Zero;

        var dif = new Fracao[n];
        for (var i = 0; i < n; i++) dif[i] = new Fracao(y[i]);
        for (var ordem = 1; ordem < n; ordem++)
            for (var i = n - 1; i >= ordem; i--)
                dif[i] = (dif[i] - dif[i - 1]) / new Fracao(x[i] - x[i - ordem]);

        // Horner para tras sobre a base de Newton.
        var p = Racional.Zero;
        for (var k = n - 1; k >= 0; k--)
            p = p * Racional.De(new Fracao(-x[k]), Fracao.Um) + dif[k];
        return p;
    }

    /// <summary>
    /// As diferencas divididas em si. Elas tem significado: a de ordem k dividida
    /// por k! e a estimativa da k-esima derivada, e e por isso que elas aparecem
    /// no erro da interpolacao.
    /// </summary>
    public static Fracao[] DiferencasDivididas(IReadOnlyList<BigInteger> x, IReadOnlyList<BigInteger> y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        ConferirDistintas(x);
        var n = x.Count;
        var dif = new Fracao[n];
        for (var i = 0; i < n; i++) dif[i] = new Fracao(y[i]);
        for (var ordem = 1; ordem < n; ordem++)
            for (var i = n - 1; i >= ordem; i--)
                dif[i] = (dif[i] - dif[i - 1]) / new Fracao(x[i] - x[i - ordem]);
        return dif;
    }

    private static void ConferirDistintas(IReadOnlyList<BigInteger> x)
    {
        var vistos = new HashSet<BigInteger>();
        foreach (var v in x)
            if (!vistos.Add(v))
                throw new ArgumentException($"a abscissa {v} aparece duas vezes; sem distincao nao ha unicidade", nameof(x));
    }
}
