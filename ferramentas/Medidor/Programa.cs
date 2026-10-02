using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Conde.Polinomio;

// A ferramenta RODA para registrar numeros. Nao ha asseveracao de tempo: os
// numeros que entram no README saem daqui, nunca da minha cabeca.

// Com InvariantGlobalization a cultura pt-BR nao existe e lancaria. O formato vai
// montado na mao, que e o padrao ja adotado nos outros projetos.
var br = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };
string N(long x) => x.ToString("N0", br);
string D(double x, int casas = 2) => x.ToString("N" + casas, br);

var comando = args.Length > 0 ? args[0] : "tudo";
switch (comando)
{
    case "karatsuba": Karatsuba(); break;
    case "transformada": Transformada(); break;
    case "corte": Corte(); break;
    case "crescimento": Crescimento(); break;
    default: Karatsuba(); Console.WriteLine(); Corte(); Console.WriteLine(); Transformada(); Console.WriteLine(); Crescimento(); break;
}

return;

// Karatsuba contra a ingenua: o expoente cai de 2 para 1,585, e isso tem de
// aparecer na razao entre tamanhos sucessivos.
void Karatsuba()
{
    Console.WriteLine("karatsuba contra a multiplicacao ingenua");
    Console.WriteLine();
    Console.WriteLine("    grau   ingenua ms   karatsuba ms   razao   razao prevista");
    Aquecer();
    var r = new Random(1960);
    double anteriorIngenua = 0, anteriorKara = 0;
    foreach (var grau in new[] { 128, 256, 512, 1024, 2048 })
    {
        var a = Sortear(grau, r);
        var b = Sortear(grau, r);

        var msIngenua = Melhor(() => { var _ = a * b; }, 5);
        var msKara = Melhor(() => { var _ = Conde.Polinomio.Karatsuba.Multiplicar(a, b); }, 5);

        var crescimentoIngenua = anteriorIngenua > 0 ? msIngenua / anteriorIngenua : 0;
        var crescimentoKara = anteriorKara > 0 ? msKara / anteriorKara : 0;
        Console.WriteLine($"  {N(grau),6}   {D(msIngenua),10}   {D(msKara),12}   {D(msIngenua / msKara, 1),5}x   ingenua {D(crescimentoIngenua, 1)}x, kara {D(crescimentoKara, 1)}x");
        anteriorIngenua = msIngenua;
        anteriorKara = msKara;
    }
    Console.WriteLine();
    Console.WriteLine("  Dobrar o grau multiplica a ingenua por 2^2 = 4 e o karatsuba por 2^1,585 = 3.");
}

// Onde fica o corte: abaixo dele as tres chamadas recursivas custam mais que a
// dupla varredura.
void Corte()
{
    Console.WriteLine("onde o karatsuba passa a valer a pena");
    Console.WriteLine();
    Console.WriteLine("    grau   ingenua ms   karatsuba ms   quem ganha");
    Aquecer();
    var r = new Random(7);
    foreach (var grau in new[] { 8, 16, 32, 64, 128, 256 })
    {
        var a = Sortear(grau, r);
        var b = Sortear(grau, r);
        const int repeticoes = 200;

        var relogio = Stopwatch.StartNew();
        for (var k = 0; k < repeticoes; k++) { var _ = a * b; }
        relogio.Stop();
        var msIngenua = relogio.Elapsed.TotalMilliseconds / repeticoes;

        relogio.Restart();
        for (var k = 0; k < repeticoes; k++) { var _ = Conde.Polinomio.Karatsuba.Multiplicar(a, b); }
        relogio.Stop();
        var msKara = relogio.Elapsed.TotalMilliseconds / repeticoes;

        Console.WriteLine($"  {N(grau),6}   {D(msIngenua, 4),10}   {D(msKara, 4),12}   {(msKara < msIngenua ? "karatsuba" : "ingenua"),10}");
    }
}

// A transformada: exata, e com o custo caindo para n log n.
void Transformada()
{
    Console.WriteLine("transformada teorica dos numeros contra a ingenua modular");
    Console.WriteLine();
    Console.WriteLine("       n   ingenua ms   transformada ms   razao");
    Aquecer();
    var r = new Random(1965);
    foreach (var n in new[] { 256, 1024, 4096, 16384 })
    {
        var a = new long[n];
        var b = new long[n];
        for (var i = 0; i < n; i++) { a[i] = r.NextInt64(0, Modular.Primo); b[i] = r.NextInt64(0, Modular.Primo); }

        var relogio = Stopwatch.StartNew();
        var rapido = Ntt.Multiplicar(a, b);
        relogio.Stop();
        var msRapido = relogio.Elapsed.TotalMilliseconds;

        double msIngenua;
        if (n <= 4096)
        {
            relogio.Restart();
            var lento = Ntt.MultiplicarIngenuo(a, b);
            relogio.Stop();
            msIngenua = relogio.Elapsed.TotalMilliseconds;
            // Exata: qualquer diferenca e defeito, nao arredondamento.
            for (var i = 0; i < lento.Length; i++)
                if (lento[i] != rapido[i]) throw new InvalidOperationException($"discordaram na posicao {i}");
        }
        else msIngenua = double.NaN;

        var razao = double.IsNaN(msIngenua) ? "-" : D(msIngenua / msRapido, 1) + "x";
        var coluna = double.IsNaN(msIngenua) ? "(nao rodou)" : D(msIngenua);
        Console.WriteLine($"  {N(n),6}   {coluna,10}   {D(msRapido),15}   {razao,5}");
    }
    Console.WriteLine();
    Console.WriteLine("  Nao ha tolerancia nenhuma na comparacao: num corpo finito a transformada e");
    Console.WriteLine("  EXATA, e qualquer diferenca seria defeito e nao arredondamento.");
}

// O crescimento dos coeficientes: e por isso que os coeficientes sao BigInteger.
void Crescimento()
{
    Console.WriteLine("crescimento dos coeficientes no produto");
    Console.WriteLine();
    Console.WriteLine("    grau   digitos na entrada   digitos no produto   cabe em long");
    var r = new Random(1850);
    foreach (var grau in new[] { 1, 10, 100, 1000 })
    {
        var a = SortearGrande(grau, 20, r);
        var b = SortearGrande(grau, 20, r);
        var p = a * b;
        var cabe = p.MaiorNumeroDeDigitos() <= 18;
        Console.WriteLine($"  {N(grau),6}   {N(a.MaiorNumeroDeDigitos()),18}   {N(p.MaiorNumeroDeDigitos()),18}   {(cabe ? "sim" : "NAO"),12}");
    }
    Console.WriteLine();
    Console.WriteLine("  Com long isso estouraria em silencio, e um juiz que estoura em silencio e");
    Console.WriteLine("  pior que nenhum.");
}

Inteiro Sortear(int grau, Random r)
{
    var c = new BigInteger[grau + 1];
    for (var k = 0; k <= grau; k++) c[k] = r.Next(-1000, 1001);
    c[grau] = r.Next(1, 1001);
    return Inteiro.De(c);
}

// Coeficientes de verdade grandes, para a tabela de crescimento: com tres digitos
// o produto cabe em long folgado e a tabela nao mostraria nada.
Inteiro SortearGrande(int grau, int digitos, Random r)
{
    var c = new BigInteger[grau + 1];
    for (var k = 0; k <= grau; k++)
    {
        var v = BigInteger.Zero;
        for (var d = 0; d < digitos; d++) v = v * 10 + r.Next(0, 10);
        c[k] = r.Next(2) == 0 ? v : -v;
    }
    c[grau] = BigInteger.One + c[grau] * c[grau];
    return Inteiro.De(c);
}

// O melhor de varias passadas. Tempo de uma medida so, nestes tamanhos, oscila
// mais que o efeito que se quer mostrar.
double Melhor(Action acao, int passadas)
{
    var melhor = double.MaxValue;
    for (var k = 0; k < passadas; k++)
    {
        var relogio = Stopwatch.StartNew();
        acao();
        relogio.Stop();
        melhor = Math.Min(melhor, relogio.Elapsed.TotalMilliseconds);
    }
    return melhor;
}

// Aquece o JIT antes de medir. Sem isso a primeira linha mede a compilacao.
void Aquecer()
{
    var r = new Random(1);
    var a = Sortear(64, r);
    var b = Sortear(64, r);
    for (var k = 0; k < 50; k++) { var _ = a * b; var __ = Conde.Polinomio.Karatsuba.Multiplicar(a, b); }
    var x = new long[256];
    var y = new long[256];
    for (var i = 0; i < 256; i++) { x[i] = i + 1; y[i] = 256 - i; }
    for (var k = 0; k < 20; k++) { Ntt.Multiplicar(x, y); Ntt.MultiplicarIngenuo(x, y); }
}
