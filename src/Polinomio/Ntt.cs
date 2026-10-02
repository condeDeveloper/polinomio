namespace Conde.Polinomio;

/// <summary>
/// Transformada teorica dos numeros: a FFT num corpo finito.
///
/// E literalmente o mesmo algoritmo de Cooley-Tukey, com e^(2*pi*i/n) trocado
/// pela raiz da unidade de ordem n modulo p. A transformada so precisa de UMA
/// propriedade da raiz: elevada a n da 1, e nenhuma potencia menor da. O corpo
/// finito tem isso, e de brinde nao tem erro de arredondamento.
///
/// A diferenca pratica e enorme e vale dizer em voz alta. Com numero complexo em
/// double, multiplicar dois polinomios de grau grande com coeficientes grandes
/// devolve valores que PRECISAM ser arredondados no fim, e a partir de certo
/// tamanho o arredondamento erra. Aqui a resposta e exata por construcao: se os
/// coeficientes do produto cabem no modulo, eles saem certos, e ponto.
///
/// O preco e o modulo: a resposta sai reduzida. Para coeficientes maiores que o
/// primo, usa-se mais de um primo e o teorema chines do resto.
/// </summary>
public static class Ntt
{
    /// <summary>
    /// A transformada no lugar. `inversa` troca a raiz pelo inverso dela e divide
    /// por n no fim, que e exatamente a mesma simetria da FFT.
    /// </summary>
    public static void Transformar(long[] a, bool inversa, long p = Modular.Primo)
    {
        ArgumentNullException.ThrowIfNull(a);
        var n = a.Length;
        if (n == 0) return;
        if ((n & (n - 1)) != 0) throw new ArgumentException("o tamanho precisa ser potencia de dois", nameof(a));

        // Permutacao por inversao de bits: a recursao de Cooley-Tukey separa pares
        // e impares, e fazer isso ate o fim e o mesmo que inverter os bits do
        // indice. Com a permutacao feita antes, o resto do algoritmo e um laco.
        for (int i = 1, j = 0; i < n; i++)
        {
            var bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if (i < j) (a[i], a[j]) = (a[j], a[i]);
        }

        for (var tamanho = 2; tamanho <= n; tamanho <<= 1)
        {
            var raiz = Modular.RaizDaUnidade(tamanho, p);
            if (inversa) raiz = Modular.Inverso(raiz, p);
            for (var i = 0; i < n; i += tamanho)
            {
                long w = 1;
                for (var k = 0; k < tamanho / 2; k++)
                {
                    var u = a[i + k];
                    var v = Modular.Multiplicar(a[i + k + tamanho / 2], w, p);
                    a[i + k] = Modular.Somar(u, v, p);
                    a[i + k + tamanho / 2] = Modular.Subtrair(u, v, p);
                    w = Modular.Multiplicar(w, raiz, p);
                }
            }
        }

        if (!inversa) return;
        var inversoDeN = Modular.Inverso(n, p);
        for (var i = 0; i < n; i++) a[i] = Modular.Multiplicar(a[i], inversoDeN, p);
    }

    /// <summary>
    /// Multiplicacao por convolucao: transforma os dois, multiplica ponto a
    /// ponto, e volta.
    ///
    /// E o teorema da convolucao, e ele e a razao de a transformada existir para
    /// quem multiplica polinomio: o produto de polinomios e a convolucao dos
    /// coeficientes, e a transformada troca convolucao por multiplicacao ponto a
    /// ponto. O custo cai de O(n^2) para O(n log n).
    /// </summary>
    public static long[] Multiplicar(long[] a, long[] b, long p = Modular.Primo)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.Length == 0 || b.Length == 0) return [];

        var precisa = a.Length + b.Length - 1;
        var n = 1;
        while (n < precisa) n <<= 1;

        var fa = new long[n];
        var fb = new long[n];
        for (var i = 0; i < a.Length; i++) fa[i] = Modular.Normalizar(a[i], p);
        for (var i = 0; i < b.Length; i++) fb[i] = Modular.Normalizar(b[i], p);

        Transformar(fa, false, p);
        Transformar(fb, false, p);
        for (var i = 0; i < n; i++) fa[i] = Modular.Multiplicar(fa[i], fb[i], p);
        Transformar(fa, true, p);

        var saida = new long[precisa];
        Array.Copy(fa, saida, precisa);
        return saida;
    }

    /// <summary>Multiplicacao ingenua modular, para servir de juiz da transformada.</summary>
    public static long[] MultiplicarIngenuo(long[] a, long[] b, long p = Modular.Primo)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.Length == 0 || b.Length == 0) return [];
        var c = new long[a.Length + b.Length - 1];
        for (var i = 0; i < a.Length; i++)
            for (var j = 0; j < b.Length; j++)
                c[i + j] = Modular.Somar(c[i + j], Modular.Multiplicar(Modular.Normalizar(a[i], p), Modular.Normalizar(b[j], p), p), p);
        return c;
    }
}
