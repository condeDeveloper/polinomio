using System.Numerics;

namespace Conde.Polinomio;

/// <summary>
/// Uma fracao exata com inteiro grande em cima e embaixo.
///
/// Ela existe para que a interpolacao saia EXATA. O polinomio que passa por
/// pontos inteiros quase nunca tem coeficientes inteiros, e arredonda-los no
/// meio do caminho faria o resultado passar PERTO dos pontos em vez de passar
/// por eles. O numerador e o denominador sao BigInteger porque as diferencas
/// divididas crescem rapido: com doze pontos os denominadores ja passam de
/// oito digitos, e um long estouraria em silencio.
///
/// A forma canonica (reduzida, com o sinal no numerador) e mantida em TODA
/// operacao. Sem ela, duas fracoes iguais poderiam ter representacoes diferentes
/// e a comparacao passaria a depender de como o valor foi construido.
/// </summary>
public readonly struct Fracao : IEquatable<Fracao>, IComparable<Fracao>
{
    public BigInteger Cima { get; }
    public BigInteger Baixo { get; }

    public Fracao(BigInteger cima, BigInteger baixo)
    {
        if (baixo.IsZero) throw new DivideByZeroException("denominador zero");
        if (baixo.Sign < 0) { cima = -cima; baixo = -baixo; }
        var mdc = BigInteger.GreatestCommonDivisor(BigInteger.Abs(cima), baixo);
        if (!mdc.IsOne && !mdc.IsZero) { cima /= mdc; baixo /= mdc; }
        if (cima.IsZero) baixo = BigInteger.One;
        Cima = cima;
        Baixo = baixo;
    }

    public Fracao(BigInteger inteiro) : this(inteiro, BigInteger.One) { }

    public static readonly Fracao Zero = new(BigInteger.Zero);
    public static readonly Fracao Um = new(BigInteger.One);

    public bool EhZero => Cima.IsZero;
    public int Sinal => Cima.Sign;

    public static Fracao operator +(Fracao a, Fracao b) => new(a.Cima * b.Baixo + b.Cima * a.Baixo, a.Baixo * b.Baixo);
    public static Fracao operator -(Fracao a, Fracao b) => new(a.Cima * b.Baixo - b.Cima * a.Baixo, a.Baixo * b.Baixo);
    public static Fracao operator *(Fracao a, Fracao b) => new(a.Cima * b.Cima, a.Baixo * b.Baixo);
    public static Fracao operator /(Fracao a, Fracao b)
    {
        if (b.EhZero) throw new DivideByZeroException("divisao por fracao zero");
        return new Fracao(a.Cima * b.Baixo, a.Baixo * b.Cima);
    }
    public static Fracao operator -(Fracao a) => new(-a.Cima, a.Baixo);

    public static implicit operator Fracao(long n) => new(n);

    /// <summary>
    /// A fracao EXATA que um double representa.
    ///
    /// Esta e a peca que faz o juiz funcionar. Todo double finito e, por
    /// definicao do IEEE 754, exatamente uma fracao com denominador potencia de
    /// dois: mantissa vezes 2 elevado ao expoente. Nao ha aproximacao nenhuma
    /// nesta conversao, e e isso que permite comparar uma conta em ponto
    /// flutuante com a mesma conta feita sem erro.
    /// </summary>
    public static Fracao Exata(double x)
    {
        if (double.IsNaN(x) || double.IsInfinity(x)) throw new ArgumentOutOfRangeException(nameof(x), "nao e um numero finito");
        if (x == 0) return Zero;

        var bits = BitConverter.DoubleToInt64Bits(x);
        var negativo = bits < 0;
        var expoente = (int)((bits >> 52) & 0x7FF);
        var mantissa = bits & 0xFFFFFFFFFFFFFL;

        if (expoente == 0) expoente++;                       // subnormal
        else mantissa |= 1L << 52;                           // o bit implicito
        expoente -= 1075;                                    // 1023 de vies + 52 da mantissa

        var cima = new BigInteger(mantissa);
        if (negativo) cima = -cima;
        return expoente >= 0
            ? new Fracao(cima * BigInteger.Pow(2, expoente), BigInteger.One)
            : new Fracao(cima, BigInteger.Pow(2, -expoente));
    }

    /// <summary>
    /// O double mais proximo. Usado so para mostrar e para medir erro, nunca para
    /// decidir.
    ///
    /// A versao obvia, `(double)Cima / (double)Baixo`, esta ERRADA nas pontas: a
    /// fracao exata de um double pequeno tem denominador 2^1074, que nao cabe em
    /// double e vira infinito, e a divisao devolve ZERO. O teste de ida e volta
    /// pegou isso em 7,34e-308, que nem subnormal e.
    ///
    /// O jeito certo e dividir em inteiro grande com uns 64 bits de sobra e so
    /// depois escalar por potencia de dois, com ScaleB, que e exato e nao estoura
    /// no caminho.
    /// </summary>
    public double ComoDouble()
    {
        if (Cima.IsZero) return 0;
        var negativo = Cima.Sign < 0;
        var cima = BigInteger.Abs(Cima);
        var baixo = Baixo;

        var escala = (int)baixo.GetBitLength() - (int)cima.GetBitLength() + 64;
        if (escala > 0) cima <<= escala; else baixo <<= -escala;

        var q = (double)(cima / baixo);
        return Math.ScaleB(negativo ? -q : q, -escala);
    }

    public bool Equals(Fracao outra) => Cima == outra.Cima && Baixo == outra.Baixo;
    public override bool Equals(object? o) => o is Fracao f && Equals(f);
    public override int GetHashCode() => HashCode.Combine(Cima, Baixo);
    public static bool operator ==(Fracao a, Fracao b) => a.Equals(b);
    public static bool operator !=(Fracao a, Fracao b) => !a.Equals(b);

    public int CompareTo(Fracao outra) => (Cima * outra.Baixo).CompareTo(outra.Cima * Baixo);
    public static bool operator <(Fracao a, Fracao b) => a.CompareTo(b) < 0;
    public static bool operator >(Fracao a, Fracao b) => a.CompareTo(b) > 0;
    public static bool operator <=(Fracao a, Fracao b) => a.CompareTo(b) <= 0;
    public static bool operator >=(Fracao a, Fracao b) => a.CompareTo(b) >= 0;

    public Fracao Abs() => Cima.Sign < 0 ? -this : this;

    public override string ToString() => Baixo.IsOne ? Cima.ToString() : $"{Cima}/{Baixo}";
}
