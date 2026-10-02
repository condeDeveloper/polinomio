using System.Numerics;

namespace Conde.Polinomio;

/// <summary>
/// Polinomio de coeficientes inteiros grandes. E o juiz.
///
/// Toda conta aqui e exata, e e por isso que os coeficientes sao BigInteger e nao
/// long: multiplicar dois polinomios de grau mil com coeficientes de dez digitos
/// da coeficientes de uns vinte e tres digitos, e o estouro silencioso de um long
/// faria o juiz mentir sem avisar.
///
/// A representacao e densa, do termo constante para cima: `c[k]` e o coeficiente
/// de x elevado a k. A forma canonica tira os zeros do fim, de modo que o grau e
/// sempre o indice do ultimo coeficiente. Sem isso, o mesmo polinomio teria
/// representacoes diferentes e a igualdade passaria a depender de como ele foi
/// construido.
/// </summary>
public sealed class Inteiro : IEquatable<Inteiro>
{
    private readonly BigInteger[] _c;

    private Inteiro(BigInteger[] c) => _c = c;

    /// <summary>O polinomio zero. Por convencao o grau dele e -1.</summary>
    public static readonly Inteiro Zero = new([]);

    public static readonly Inteiro Um = De(1);

    /// <summary>Monta a partir dos coeficientes, do termo constante para cima.</summary>
    public static Inteiro De(params BigInteger[] coeficientes)
    {
        ArgumentNullException.ThrowIfNull(coeficientes);
        var fim = coeficientes.Length;
        while (fim > 0 && coeficientes[fim - 1].IsZero) fim--;
        if (fim == 0) return new Inteiro([]);
        var c = new BigInteger[fim];
        Array.Copy(coeficientes, c, fim);
        return new Inteiro(c);
    }

    public static Inteiro De(params long[] coeficientes)
    {
        ArgumentNullException.ThrowIfNull(coeficientes);
        return De(Array.ConvertAll(coeficientes, x => new BigInteger(x)));
    }

    /// <summary>x elevado a n.</summary>
    public static Inteiro Monomio(int n, BigInteger coeficiente)
    {
        if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
        if (coeficiente.IsZero) return Zero;
        var c = new BigInteger[n + 1];
        c[n] = coeficiente;
        return new Inteiro(c);
    }

    /// <summary>O grau. Vale -1 no polinomio zero, que e a convencao usual.</summary>
    public int Grau => _c.Length - 1;

    public bool EhZero => _c.Length == 0;

    /// <summary>O coeficiente de x^k, ou zero quando k passa do grau.</summary>
    public BigInteger this[int k] => k >= 0 && k < _c.Length ? _c[k] : BigInteger.Zero;

    public IReadOnlyList<BigInteger> Coeficientes => _c;

    /// <summary>O coeficiente do termo de maior grau.</summary>
    public BigInteger Lider => EhZero ? BigInteger.Zero : _c[^1];

    public static Inteiro operator +(Inteiro a, Inteiro b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var n = Math.Max(a._c.Length, b._c.Length);
        var c = new BigInteger[n];
        for (var k = 0; k < n; k++) c[k] = a[k] + b[k];
        return De(c);
    }

    public static Inteiro operator -(Inteiro a, Inteiro b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var n = Math.Max(a._c.Length, b._c.Length);
        var c = new BigInteger[n];
        for (var k = 0; k < n; k++) c[k] = a[k] - b[k];
        return De(c);
    }

    public static Inteiro operator -(Inteiro a)
    {
        ArgumentNullException.ThrowIfNull(a);
        return De(Array.ConvertAll(a._c, x => -x));
    }

    /// <summary>
    /// Multiplicacao ingenua: cada termo contra cada termo, O(n*m).
    ///
    /// E a definicao, e e por isso que ela fica aqui mesmo sendo lenta: e contra
    /// ela que a FFT e o Karatsuba sao julgados.
    /// </summary>
    public static Inteiro operator *(Inteiro a, Inteiro b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.EhZero || b.EhZero) return Zero;
        var c = new BigInteger[a._c.Length + b._c.Length - 1];
        for (var i = 0; i < a._c.Length; i++)
        {
            if (a._c[i].IsZero) continue;
            for (var j = 0; j < b._c.Length; j++) c[i + j] += a._c[i] * b._c[j];
        }
        return De(c);
    }

    public static Inteiro operator *(Inteiro a, BigInteger k)
    {
        ArgumentNullException.ThrowIfNull(a);
        return De(Array.ConvertAll(a._c, x => x * k));
    }

    /// <summary>
    /// Avaliacao por Horner: n multiplicacoes e n somas, contra as 2n-1 da forma
    /// ingenua com potencias.
    ///
    /// O ganho nao e so de contagem. Calcular x^k separadamente e depois
    /// multiplicar acumula erro em ponto flutuante e cresce rapido em inteiro
    /// grande; o Horner nunca forma a potencia.
    /// </summary>
    public BigInteger Avaliar(BigInteger x)
    {
        var y = BigInteger.Zero;
        for (var k = _c.Length - 1; k >= 0; k--) y = y * x + _c[k];
        return y;
    }

    /// <summary>A derivada formal, que nao precisa de limite nenhum para existir.</summary>
    public Inteiro Derivada()
    {
        if (_c.Length <= 1) return Zero;
        var d = new BigInteger[_c.Length - 1];
        for (var k = 1; k < _c.Length; k++) d[k - 1] = _c[k] * k;
        return De(d);
    }

    /// <summary>
    /// Divisao com resto: a = q*b + r, com grau(r) menor que grau(b).
    ///
    /// Em inteiro isso so fecha quando o divisor e MONICO (lider 1) ou quando a
    /// divisao de cada passo e exata. Devolve nulo quando nao fecha, em vez de
    /// arredondar: arredondar aqui daria um quociente plausivel e errado.
    /// </summary>
    public static (Inteiro Quociente, Inteiro Resto)? DividirComResto(Inteiro a, Inteiro b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (b.EhZero) throw new DivideByZeroException("divisao por polinomio zero");
        if (a.Grau < b.Grau) return (Zero, a);

        var resto = (BigInteger[])a._c.Clone();
        var q = new BigInteger[a.Grau - b.Grau + 1];
        for (var k = a.Grau - b.Grau; k >= 0; k--)
        {
            var cima = resto[k + b.Grau];
            if (cima.IsZero) continue;
            if (!(cima % b.Lider).IsZero) return null;   // nao fecha em inteiro
            var fator = cima / b.Lider;
            q[k] = fator;
            for (var j = 0; j <= b.Grau; j++) resto[k + j] -= fator * b[j];
        }
        return (De(q), De(resto));
    }

    public bool Equals(Inteiro? outro)
    {
        if (outro is null) return false;
        if (_c.Length != outro._c.Length) return false;
        for (var k = 0; k < _c.Length; k++) if (_c[k] != outro._c[k]) return false;
        return true;
    }

    public override bool Equals(object? o) => Equals(o as Inteiro);
    public override int GetHashCode()
    {
        var h = new HashCode();
        foreach (var c in _c) h.Add(c);
        return h.ToHashCode();
    }
    public static bool operator ==(Inteiro? a, Inteiro? b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(Inteiro? a, Inteiro? b) => !(a == b);

    /// <summary>O maior numero de digitos entre os coeficientes. Mede o crescimento.</summary>
    public int MaiorNumeroDeDigitos()
    {
        var maior = 1;
        foreach (var c in _c)
        {
            var d = BigInteger.Abs(c).ToString().Length;
            if (d > maior) maior = d;
        }
        return maior;
    }

    public override string ToString()
    {
        if (EhZero) return "0";
        var partes = new List<string>();
        for (var k = _c.Length - 1; k >= 0; k--)
        {
            if (_c[k].IsZero) continue;
            partes.Add(k == 0 ? _c[k].ToString() : k == 1 ? $"{_c[k]}x" : $"{_c[k]}x^{k}");
        }
        return string.Join(" + ", partes);
    }
}
