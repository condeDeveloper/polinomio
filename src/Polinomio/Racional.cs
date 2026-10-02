using System.Numerics;

namespace Conde.Polinomio;

/// <summary>
/// Polinomio de coeficientes racionais exatos.
///
/// Ele existe porque a divisao e a interpolacao saem do mundo dos inteiros: o
/// polinomio que passa por pontos inteiros quase nunca tem coeficiente inteiro, e
/// arredondar no meio faria a curva passar PERTO dos pontos em vez de passar por
/// eles. Com fracao exata, "passa pelo ponto" e uma igualdade e nao uma
/// tolerancia.
/// </summary>
public sealed class Racional : IEquatable<Racional>
{
    private readonly Fracao[] _c;

    private Racional(Fracao[] c) => _c = c;

    public static readonly Racional Zero = new([]);
    public static readonly Racional Um = De(Fracao.Um);

    public static Racional De(params Fracao[] coeficientes)
    {
        ArgumentNullException.ThrowIfNull(coeficientes);
        var fim = coeficientes.Length;
        while (fim > 0 && coeficientes[fim - 1].EhZero) fim--;
        if (fim == 0) return new Racional([]);
        var c = new Fracao[fim];
        Array.Copy(coeficientes, c, fim);
        return new Racional(c);
    }

    /// <summary>A versao racional de um polinomio inteiro.</summary>
    public static Racional De(Inteiro p)
    {
        ArgumentNullException.ThrowIfNull(p);
        var c = new Fracao[p.Grau + 1];
        for (var k = 0; k <= p.Grau; k++) c[k] = new Fracao(p[k]);
        return De(c);
    }

    public int Grau => _c.Length - 1;
    public bool EhZero => _c.Length == 0;
    public Fracao this[int k] => k >= 0 && k < _c.Length ? _c[k] : Fracao.Zero;
    public IReadOnlyList<Fracao> Coeficientes => _c;

    public static Racional operator +(Racional a, Racional b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var n = Math.Max(a._c.Length, b._c.Length);
        var c = new Fracao[n];
        for (var k = 0; k < n; k++) c[k] = a[k] + b[k];
        return De(c);
    }

    public static Racional operator -(Racional a, Racional b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var n = Math.Max(a._c.Length, b._c.Length);
        var c = new Fracao[n];
        for (var k = 0; k < n; k++) c[k] = a[k] - b[k];
        return De(c);
    }

    public static Racional operator *(Racional a, Racional b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.EhZero || b.EhZero) return Zero;
        var c = new Fracao[a._c.Length + b._c.Length - 1];
        for (var i = 0; i < c.Length; i++) c[i] = Fracao.Zero;
        for (var i = 0; i < a._c.Length; i++)
        {
            if (a._c[i].EhZero) continue;
            for (var j = 0; j < b._c.Length; j++) c[i + j] += a._c[i] * b._c[j];
        }
        return De(c);
    }

    public static Racional operator *(Racional a, Fracao k)
    {
        ArgumentNullException.ThrowIfNull(a);
        return De(Array.ConvertAll(a._c, x => x * k));
    }

    public static Racional operator +(Racional a, Fracao k)
    {
        ArgumentNullException.ThrowIfNull(a);
        return a + De(k);
    }

    /// <summary>Avaliacao por Horner, exata.</summary>
    public Fracao Avaliar(Fracao x)
    {
        var y = Fracao.Zero;
        for (var k = _c.Length - 1; k >= 0; k--) y = y * x + _c[k];
        return y;
    }

    public Fracao Avaliar(BigInteger x) => Avaliar(new Fracao(x));

    public Racional Derivada()
    {
        if (_c.Length <= 1) return Zero;
        var d = new Fracao[_c.Length - 1];
        for (var k = 1; k < _c.Length; k++) d[k - 1] = _c[k] * new Fracao(k);
        return De(d);
    }

    /// <summary>
    /// Divisao com resto. Sobre racionais isso SEMPRE fecha, porque todo
    /// coeficiente nao nulo tem inverso: e a diferenca entre um corpo e um anel, e
    /// e o que faz o algoritmo de Euclides para polinomios existir aqui e nao nos
    /// inteiros.
    /// </summary>
    public static (Racional Quociente, Racional Resto) DividirComResto(Racional a, Racional b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (b.EhZero) throw new DivideByZeroException("divisao por polinomio zero");
        if (a.Grau < b.Grau) return (Zero, a);

        var resto = (Fracao[])a._c.Clone();
        var q = new Fracao[a.Grau - b.Grau + 1];
        for (var i = 0; i < q.Length; i++) q[i] = Fracao.Zero;

        for (var k = a.Grau - b.Grau; k >= 0; k--)
        {
            var fator = resto[k + b.Grau] / b[b.Grau];
            q[k] = fator;
            if (fator.EhZero) continue;
            for (var j = 0; j <= b.Grau; j++) resto[k + j] -= fator * b[j];
        }
        return (De(q), De(resto));
    }

    /// <summary>
    /// O maximo divisor comum por Euclides, normalizado para lider 1.
    ///
    /// A normalizacao importa: sem ela o MDC sai a menos de uma constante, e dois
    /// chamadores com a mesma entrada poderiam receber 2x+2 e x+1, que sao o mesmo
    /// divisor com cara diferente.
    /// </summary>
    public static Racional Mdc(Racional a, Racional b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        while (!b.EhZero)
        {
            var (_, r) = DividirComResto(a, b);
            a = b;
            b = r;
        }
        return a.EhZero ? Zero : a.Monico();
    }

    /// <summary>O mesmo polinomio com coeficiente lider 1.</summary>
    public Racional Monico()
    {
        if (EhZero) return Zero;
        var lider = _c[^1];
        return De(Array.ConvertAll(_c, x => x / lider));
    }

    /// <summary>
    /// O menor multiplo que torna todos os coeficientes inteiros, e o polinomio
    /// inteiro correspondente. Util para mostrar o resultado sem fracao.
    /// </summary>
    public (BigInteger Multiplicador, Inteiro Inteiro) ComoInteiro()
    {
        if (EhZero) return (BigInteger.One, Inteiro.Zero);
        var mmc = BigInteger.One;
        foreach (var c in _c) mmc = mmc / BigInteger.GreatestCommonDivisor(mmc, c.Baixo) * c.Baixo;
        var inteiros = new BigInteger[_c.Length];
        for (var k = 0; k < _c.Length; k++) inteiros[k] = _c[k].Cima * (mmc / _c[k].Baixo);
        return (mmc, Inteiro.De(inteiros));
    }

    public bool Equals(Racional? outro)
    {
        if (outro is null) return false;
        if (_c.Length != outro._c.Length) return false;
        for (var k = 0; k < _c.Length; k++) if (_c[k] != outro._c[k]) return false;
        return true;
    }

    public override bool Equals(object? o) => Equals(o as Racional);
    public override int GetHashCode()
    {
        var h = new HashCode();
        foreach (var c in _c) h.Add(c);
        return h.ToHashCode();
    }
    public static bool operator ==(Racional? a, Racional? b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(Racional? a, Racional? b) => !(a == b);

    public override string ToString()
    {
        if (EhZero) return "0";
        var partes = new List<string>();
        for (var k = _c.Length - 1; k >= 0; k--)
        {
            if (_c[k].EhZero) continue;
            partes.Add(k == 0 ? _c[k].ToString() : k == 1 ? $"({_c[k]})x" : $"({_c[k]})x^{k}");
        }
        return string.Join(" + ", partes);
    }
}
