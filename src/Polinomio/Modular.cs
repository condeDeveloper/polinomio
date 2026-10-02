namespace Conde.Polinomio;

/// <summary>
/// Aritmetica modulo um primo, em long.
///
/// Serve de corpo finito para a transformada teorica dos numeros. A diferenca
/// para o complexo e a razao de ela existir: aqui NAO HA ERRO. A raiz da unidade
/// e exata, a transformada e exata, a inversa devolve o original bit a bit. Com
/// numero complexo em double, nada disso e verdade.
/// </summary>
public static class Modular
{
    /// <summary>
    /// 998.244.353 = 119 * 2^23 + 1. E primo e tem 2^23 dividindo p-1, o que da
    /// raizes da unidade de ordem ate 8.388.608.
    ///
    /// Esse primo nao e arbitrario: um primo so serve para a transformada se
    /// p-1 tiver uma potencia de dois grande como fator, porque a transformada
    /// precisa de uma raiz primitiva de ordem 2^k. E e por isso que o mesmo
    /// punhado de primos aparece em todo codigo de competicao do mundo.
    /// </summary>
    public const long Primo = 998_244_353;

    /// <summary>Uma raiz primitiva de 998.244.353.</summary>
    public const long RaizPrimitiva = 3;

    public static long Normalizar(long x, long p = Primo)
    {
        x %= p;
        return x < 0 ? x + p : x;
    }

    public static long Somar(long a, long b, long p = Primo)
    {
        var s = a + b;
        return s >= p ? s - p : s;
    }

    public static long Subtrair(long a, long b, long p = Primo)
    {
        var s = a - b;
        return s < 0 ? s + p : s;
    }

    public static long Multiplicar(long a, long b, long p = Primo) => a * b % p;

    /// <summary>Exponenciacao por quadrados sucessivos: log(e) multiplicacoes.</summary>
    public static long Potencia(long b, long e, long p = Primo)
    {
        if (e < 0) throw new ArgumentOutOfRangeException(nameof(e));
        b = Normalizar(b, p);
        long r = 1;
        while (e > 0)
        {
            if ((e & 1) == 1) r = r * b % p;
            b = b * b % p;
            e >>= 1;
        }
        return r;
    }

    /// <summary>
    /// O inverso multiplicativo pelo pequeno teorema de Fermat: a^(p-2) mod p.
    ///
    /// So vale com p primo, e a conta inteira depende disso. Usar essa formula com
    /// modulo composto devolve um numero, e o numero esta errado.
    /// </summary>
    public static long Inverso(long a, long p = Primo)
    {
        a = Normalizar(a, p);
        if (a == 0) throw new DivideByZeroException("zero nao tem inverso");
        return Potencia(a, p - 2, p);
    }

    /// <summary>
    /// A raiz primitiva de ordem n, com n potencia de dois que divide p-1.
    ///
    /// E o analogo exato de e^(2*pi*i/n) no corpo finito: elevada a n da 1, e
    /// nenhuma potencia menor da. Essa e a unica propriedade que a transformada
    /// usa, e e por isso que ela funciona tanto no complexo quanto aqui.
    /// </summary>
    public static long RaizDaUnidade(int n, long p = Primo, long g = RaizPrimitiva)
    {
        if (n <= 0 || (n & (n - 1)) != 0) throw new ArgumentException("n precisa ser potencia de dois", nameof(n));
        if ((p - 1) % n != 0) throw new ArgumentException($"{n} nao divide {p - 1}", nameof(n));
        return Potencia(g, (p - 1) / n, p);
    }
}
