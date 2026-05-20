using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HospitalSim
{
    // ─────────────────────────────────────────────────────────────────────────
    //  MODELOS DE DATA
    // ─────────────────────────────────────────────────────────────────────────
    record Servicio(string Nombre, double Probabilidad, double Precio);

    record ResultadoDia(
        int    Dia,
        double UPacientes,
        int    Pacientes,
        double IngresosDia,
        double IngresoAcumulado
    );

    // ─────────────────────────────────────────────────────────────────────────
    //  GENERADOR CONGRUENCIAL MIXTO PROPIO  (sin usar System.Random)
    //  Fórmula: X_{n+1} = (A * X_n + C) mod M
    //  U_n = X_n / M   →   valor en [0, 1)
    // ─────────────────────────────────────────────────────────────────────────
    class Rnd
    {
        private long _x;          // semilla / estado actual
        private readonly long _a; // multiplicador
        private readonly long _c; // incremento
        private readonly long _m; // módulo

        public Rnd(long x0, long a, long c, long m)
        {
            _x = x0;
            _a = a;
            _c = c;
            _m = m;
        }

        /// <summary>Genera el siguiente número en [0, 1)</summary>
        public double NextDouble()
        {
            _x = (_a * _x + _c) % _m;
            return (double)_x / _m;
        }

        /// <summary>Genera una lista de 'n' números U en [0,1)</summary>
        public List<double> Generar(int n)
        {
            var lista = new List<double>(n);
            for (int i = 0; i < n; i++)
                lista.Add(NextDouble());
            return lista;
        }
    }

    class Program
    {
        // ── Paleta de colores ────────────────────────────────────────────────
        const ConsoleColor COLOR_TITULO   = ConsoleColor.Cyan;
        const ConsoleColor COLOR_HEADER   = ConsoleColor.Yellow;
        const ConsoleColor COLOR_POSITIVO = ConsoleColor.Green;
        const ConsoleColor COLOR_NEGATIVO = ConsoleColor.Red;
        const ConsoleColor COLOR_NEUTRO   = ConsoleColor.White;
        const ConsoleColor COLOR_ACCENT   = ConsoleColor.Magenta;
        const ConsoleColor COLOR_INFO     = ConsoleColor.DarkCyan;
        const ConsoleColor COLOR_TABLA    = ConsoleColor.DarkGray;

        static readonly double CostoMensual = 5000.00;

        static readonly List<Servicio> Servicios = new()
        {
            new("Atenc. Emergencia",       0.10,  75.00),
            new("Consulta Familiar",       0.20,  50.00),
            new("Serv. Laboratorio",       0.15, 150.00),
            new("Operaciones Menores",     0.13, 400.00),
            new("Operaciones Especializ.", 0.17, 950.00),
            new("Campaña Vacuna/Sanidad",  0.10,  20.00),
            new("Cuidados Intensivos",     0.15, 200.00),
        };

        // ── Generador global (se inicializa al arrancar) ─────────────────────
        static Rnd? _rnd;

        // ─────────────────────────────────────────────────────────────────────
        //  ENTRY POINT
        // ─────────────────────────────────────────────────────────────────────
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CursorVisible  = false;

            MostrarPantallaBienvenida();
            Console.ReadKey(true);

            // Antes del menú: configurar y validar el generador
            ConfigurarGeneradorCongruencial();

            MostrarMenuPrincipal();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  CONFIGURACIÓN DEL GENERADOR CONGRUENCIAL MIXTO
        // ─────────────────────────────────────────────────────────────────────
        static void ConfigurarGeneradorCongruencial()
        {
            while (true)
            {
                Console.Clear();
                EncabezadoSeccion("CONFIGURACIÓN — GENERADOR CONGRUENCIAL MIXTO");
                Console.WriteLine();
                Escribir("  Fórmula: X_{n+1} = (A * X_n + C) mod M\n", COLOR_INFO);
                Escribir("  U_n = X_n / M   →   número en [0, 1)\n\n", COLOR_INFO);
                Console.CursorVisible = true;

                long x0 = PedirEnteroPositivo("  Semilla  X0 : ");
                long a  = PedirEnteroPositivo("  Multiplicador A : ");
                long c  = PedirEnteroPositivo("  Incremento    C : ");
                long m  = PedirEnteroPositivo("  Módulo        M : ");

                Console.CursorVisible = false;

                // Crear generador temporal para pruebas
                var rndTemp = new Rnd(x0, a, c, m);

                // Pedimos cuántos números generar para las pruebas
                Console.WriteLine();
                Console.CursorVisible = true;
                Escribir("  ¿Cuántos números generar para las pruebas de uniformidad? (mín 20): ", COLOR_HEADER);
                if (!int.TryParse(Console.ReadLine(), out int nPrueba) || nPrueba < 20)
                    nPrueba = 20;
                Console.CursorVisible = false;

                var numerosU = rndTemp.Generar(nPrueba);

                // Mostrar los números generados en pantalla
                Console.Clear();
                EncabezadoSeccion($"NÚMEROS GENERADOS AUTOMÁTICAMENTE ({nPrueba})");
                Console.WriteLine();
                for (int i = 0; i < numerosU.Count; i++)
                {
                    Escribir($" {numerosU[i]:F4} ", COLOR_NEUTRO);
                    if ((i + 1) % 10 == 0) Console.WriteLine(); // Muestra 10 números por línea
                }
                Console.WriteLine("\n");
                Escribir("  Presiona cualquier tecla para someter la secuencia a pruebas estadísticas...", COLOR_HEADER);
                Console.ReadKey(true);

                // ── PRUEBA 1: Chi-cuadrada ───────────────────────────────────
                bool pasoChi = PruebaChiCuadrada(numerosU);

                // ── PRUEBA 2: Kolmogorov-Smirnov ────────────────────────────
                bool pasoKS = PruebaKolmogorovSmirnov(numerosU);

                Console.WriteLine();
                if (pasoChi && pasoKS)
                {
                    Escribir("  ✔ Ambas pruebas APROBADAS. Los parámetros son válidos.\n", COLOR_POSITIVO);
                    Escribir("  Presiona cualquier tecla para continuar al menú...", COLOR_HEADER);
                    Console.ReadKey(true);
                    // Recrear con la semilla original para que la simulación empiece desde X0
                    _rnd = new Rnd(x0, a, c, m);
                    return;
                }
                else
                {
                    Escribir("  ✘ Una o más pruebas FALLARON. Los números generados\n", COLOR_NEGATIVO);
                    Escribir("    no superan la prueba de uniformidad.\n", COLOR_NEGATIVO);
                    Escribir("    Ingresa nuevos parámetros.\n\n", COLOR_NEGATIVO);
                    Escribir("  Presiona cualquier tecla para reintentar...", COLOR_HEADER);
                    Console.ReadKey(true);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PRUEBA 1 — CHI-CUADRADA  (goodness-of-fit con k intervalos iguales)
        //  H0: los números siguen una distribución Uniforme(0,1)
        //  Si X² calculado < X² crítico → no se rechaza H0 → pasan la prueba
        // ─────────────────────────────────────────────────────────────────────
        static bool PruebaChiCuadrada(List<double> datos)
        {
            Console.Clear();
            EncabezadoSeccion("PRUEBA 1 — CHI-CUADRADA (Frecuencias Observadas vs Esperadas)");
            Console.WriteLine();
            Escribir("  Método: Chi-cuadrada de Pearson con k = 10 intervalos iguales.\n", COLOR_INFO);
            Escribir("  H0: Los números siguen distribución Uniforme(0,1).\n", COLOR_INFO);
            Escribir("  Nivel de significancia α = 0.05\n\n", COLOR_INFO);

            int n = datos.Count;
            int k = 10; // número de intervalos
            double esperado = (double)n / k;

            // Contar frecuencias observadas por intervalo [i/k, (i+1)/k)
            int[] obs = new int[k];
            foreach (var u in datos)
            {
                int idx = (int)(u * k);
                if (idx >= k) idx = k - 1;
                obs[idx]++;
            }

            // Calcular estadístico Chi²
            double chiCalc = 0;
            string sep = new string('─', 52);
            Escribir($"  ╔{sep}╗\n", COLOR_TABLA);
            Escribir("  ║", COLOR_TABLA);
            Escribir($"  {"Intervalo",-18}{"Observado",10}{"Esperado",10}{"(O-E)²/E",12}   ", COLOR_HEADER);
            Escribir("║\n", COLOR_TABLA);
            Escribir($"  ╠{sep}╣\n", COLOR_TABLA);

            for (int i = 0; i < k; i++)
            {
                double contrib = Math.Pow(obs[i] - esperado, 2) / esperado;
                chiCalc += contrib;
                string intervalo = $"[{i * 0.1:F1}, {(i + 1) * 0.1:F1})";
                Escribir("  ║ ", COLOR_TABLA);
                Escribir($"{intervalo,-18}", COLOR_NEUTRO);
                Escribir($"{obs[i],10}", COLOR_ACCENT);
                Escribir($"{esperado,10:F2}", COLOR_INFO);
                Escribir($"{contrib,12:F4}", COLOR_POSITIVO);
                Escribir("   ║\n", COLOR_TABLA);
            }
            Escribir($"  ╚{sep}╝\n", COLOR_TABLA);

            // Valor crítico Chi² con gl = k-1 = 9, α = 0.05 → 16.919
            double chiCritico = 16.919;
            int gl = k - 1;

            Console.WriteLine();
            Escribir($"  Chi² calculado : {chiCalc:F4}\n", COLOR_NEUTRO);
            Escribir($"  Chi² crítico   : {chiCritico:F4}  (gl={gl}, α=0.05)\n", COLOR_NEUTRO);

            bool pasa = chiCalc <= chiCritico;
            Console.WriteLine();
            if (pasa)
                Escribir("  Resultado: ✔ No se rechaza H0 → distribución UNIFORME.\n", COLOR_POSITIVO);
            else
                Escribir("  Resultado: ✘ Se rechaza H0 → distribución NO uniforme.\n", COLOR_NEGATIVO);

            Escribir("\n  Presiona cualquier tecla para ver la Prueba 2...", COLOR_HEADER);
            Console.ReadKey(true);
            return pasa;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PRUEBA 2 — KOLMOGOROV-SMIRNOV
        //  Compara la CDF empírica F_n(x) contra la CDF teórica F(x) = x
        //  D = max|F_n(x_i) - x_i|
        //  Si D calculado < D crítico → no se rechaza H0
        // ─────────────────────────────────────────────────────────────────────
        static bool PruebaKolmogorovSmirnov(List<double> datos)
        {
            Console.Clear();
            EncabezadoSeccion("PRUEBA 2 — KOLMOGOROV-SMIRNOV");
            Console.WriteLine();
            Escribir("  Método: Kolmogorov-Smirnov de una muestra.\n", COLOR_INFO);
            Escribir("  H0: Los números siguen distribución Uniforme(0,1).\n", COLOR_INFO);
            Escribir("  Nivel de significancia α = 0.05\n\n", COLOR_INFO);

            int n = datos.Count;
            var sorted = datos.OrderBy(x => x).ToList();

            double D = 0;
            double Dmas = 0;
            double Dmenos = 0;

            string sep = new string('─', 66);
            Escribir($"  ╔{sep}╗\n", COLOR_TABLA);
            Escribir("  ║", COLOR_TABLA);
            Escribir($"  {"i",4}{"U(i)",10}{"F(i)=i/n",10}{"F(i-1)",10}{"D+",10}{"D-",10}{"Di",10}   ", COLOR_HEADER);
            Escribir("║\n", COLOR_TABLA);
            Escribir($"  ╠{sep}╣\n", COLOR_TABLA);

            // Mostramos solo las primeras 15 filas para no saturar la pantalla
            int mostrar = Math.Min(n, 15);

            for (int i = 1; i <= n; i++)
            {
                double ui    = sorted[i - 1];
                double Fi    = (double)i / n;        // CDF empírica en i
                double Fi_1  = (double)(i - 1) / n;  // CDF empírica en i-1
                double dMas  = Math.Abs(Fi - ui);    // |F(i) - U(i)|
                double dMen  = Math.Abs(ui - Fi_1);  // |U(i) - F(i-1)|
                double di    = Math.Max(dMas, dMen);

                if (di > D) D = di;
                if (dMas > Dmas) Dmas = dMas;
                if (dMen > Dmenos) Dmenos = dMen;

                if (i <= mostrar)
                {
                    Escribir("  ║ ", COLOR_TABLA);
                    Escribir($"{i,4}", COLOR_NEUTRO);
                    Escribir($"{ui,10:F4}", COLOR_ACCENT);
                    Escribir($"{Fi,10:F4}", COLOR_INFO);
                    Escribir($"{Fi_1,10:F4}", COLOR_INFO);
                    Escribir($"{dMas,10:F4}", COLOR_NEUTRO);
                    Escribir($"{dMen,10:F4}", COLOR_NEUTRO);
                    Escribir($"{di,10:F4}", di == D ? COLOR_NEGATIVO : COLOR_NEUTRO);
                    Escribir("   ║\n", COLOR_TABLA);
                }
            }
            if (n > mostrar)
            {
                Escribir("  ║ ", COLOR_TABLA);
                Escribir($"  ... ({n - mostrar} filas omitidas para visualización) ...".PadRight(64), COLOR_TABLA);
                Escribir("   ║\n", COLOR_TABLA);
            }
            Escribir($"  ╚{sep}╝\n", COLOR_TABLA);

            // Valor crítico K-S para α=0.05: D_critico ≈ 1.36 / sqrt(n)
            double dCritico = 1.36 / Math.Sqrt(n);

            Console.WriteLine();
            Escribir($"  D calculado    : {D:F4}  (max entre D+={Dmas:F4} y D-={Dmenos:F4})\n", COLOR_NEUTRO);
            Escribir($"  D crítico      : {dCritico:F4}  (1.36/√{n}, α=0.05)\n", COLOR_NEUTRO);

            bool pasa = D <= dCritico;
            Console.WriteLine();
            if (pasa)
                Escribir("  Resultado: ✔ No se rechaza H0 → distribución UNIFORME.\n", COLOR_POSITIVO);
            else
                Escribir("  Resultado: ✘ Se rechaza H0 → distribución NO uniforme.\n", COLOR_NEGATIVO);

            Escribir("\n  Presiona cualquier tecla para continuar...", COLOR_HEADER);
            Console.ReadKey(true);
            return pasa;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PANTALLA DE BIENVENIDA
        // ─────────────────────────────────────────────────────────────────────
        static void MostrarPantallaBienvenida()
        {
            Console.Clear();
            string[] logo = {
                @"  ██╗  ██╗ ██████╗ ███████╗██████╗ ██╗████████╗ █████╗ ██╗     ",
                @"  ██║  ██║██╔═══██╗██╔════╝██╔══██╗██║╚══██╔══╝██╔══██╗██║     ",
                @"  ███████║██║   ██║███████╗██████╔╝██║   ██║   ███████║██║     ",
                @"  ██╔══██║██║   ██║╚════██║██╔═══╝ ██║   ██║   ██╔══██║██║     ",
                @"  ██║  ██║╚██████╔╝███████║██║     ██║   ██║   ██║  ██║███████╗",
                @"  ╚═╝  ╚═╝ ╚═════╝ ╚══════╝╚═╝     ╚═╝   ╚═╝   ╚═╝  ╚═╝╚══════╝",
            };
            CentrarTexto("", ConsoleColor.White);
            foreach (var l in logo) CentrarTexto(l, COLOR_TITULO);
            CentrarTexto("", ConsoleColor.White);
            CentrarTexto("S I M U L A D O R   D E   O P E R A C I O N E S", COLOR_HEADER);
            CentrarTexto("─────────────────────────────────────────────────", COLOR_TABLA);
            CentrarTexto("Simulación de Monte Carlo  •  Distribución Triangular", COLOR_INFO);
            CentrarTexto("Generador Congruencial Mixto  •  Pruebas Chi² y K-S", COLOR_INFO);
            Console.WriteLine();

            DibujarCaja(14, Console.CursorTop, 60, 6, COLOR_ACCENT);
            Console.SetCursorPosition(16, Console.CursorTop);
            Escribir("  Parámetros del Problema:", COLOR_HEADER); Console.WriteLine();
            Console.SetCursorPosition(16, Console.CursorTop);
            Escribir($"  ▸ Costo mensual  : ${CostoMensual:N2}", COLOR_NEUTRO); Console.WriteLine();
            Console.SetCursorPosition(16, Console.CursorTop);
            Escribir("  ▸ Triangular     : Min=10, Moda=28, Máx=40", COLOR_NEUTRO); Console.WriteLine();
            Console.SetCursorPosition(16, Console.CursorTop);
            Escribir("  ▸ Generador      : Congruencial Mixto propio", COLOR_NEUTRO); Console.WriteLine();
            Console.SetCursorPosition(16, Console.CursorTop);
            Escribir("  ▸ Pruebas        : Chi-cuadrada + Kolmogorov-Smirnov", COLOR_NEUTRO); Console.WriteLine();
            Console.WriteLine();
            CentrarTexto("Presiona cualquier tecla para continuar...", ConsoleColor.DarkYellow);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  MENÚ PRINCIPAL
        // ─────────────────────────────────────────────────────────────────────
        static void MostrarMenuPrincipal()
        {
            while (true)
            {
                Console.Clear();
                EncabezadoSeccion("MENÚ PRINCIPAL");
                Console.WriteLine();
                var opciones = new[]
                {
                    ("1", "Política 1 — Incrementar servicios (reducir calidad)"),
                    ("2", "Política 2 — Incrementar precio   (mantener calidad)"),
                    ("3", "Comparar ambas políticas"),
                    ("4", "Ver distribuciones del problema"),
                    ("5", "Reconfigurar generador congruencial"),
                    ("0", "Salir"),
                };
                foreach (var (key, desc) in opciones)
                {
                    Console.Write("  ");
                    Escribir($" [{key}] ", COLOR_ACCENT);
                    Escribir($" {desc}", COLOR_NEUTRO);
                    Console.WriteLine();
                }
                Console.WriteLine();
                Escribir("  Selecciona una opción: ", COLOR_HEADER);
                Console.CursorVisible = true;
                var input = Console.ReadLine()?.Trim();
                Console.CursorVisible = false;

                switch (input)
                {
                    case "1": EjecutarPolitica(1); break;
                    case "2": EjecutarPolitica(2); break;
                    case "3": CompararPoliticas(); break;
                    case "4": MostrarDistribuciones(); break;
                    case "5": ConfigurarGeneradorCongruencial(); break;
                    case "0": SalirPrograma(); return;
                    default:
                        Escribir("  ⚠ Opción inválida.", COLOR_NEGATIVO);
                        Thread.Sleep(900);
                        break;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PREPARACIÓN Y CAPTURA PARA LA SIMULACIÓN
        // ─────────────────────────────────────────────────────────────────────
        static void EjecutarPolitica(int politica)
        {
            Console.Clear();
            string titulo = politica == 1
                ? "POLÍTICA 1 — Incrementar Servicios (Reducir Calidad)"
                : "POLÍTICA 2 — Incrementar Precios (Mantener Calidad)";
            EncabezadoSeccion(titulo);
            Console.WriteLine();
            Console.CursorVisible = true;

            double factor = 1.0;
            if (politica == 1)
            {
                Escribir("  Factor de servicios por paciente (ej. 1.5 = +50% servicios): ", COLOR_HEADER);
                if (!double.TryParse(Console.ReadLine(), out factor) || factor <= 0) factor = 1.5;
                Escribir($"  ✔ Factor aplicado: {factor:F2}x\n", COLOR_POSITIVO);
            }
            else
            {
                Escribir("  Factor de incremento de precios (ej. 1.3 = +30% precio): ", COLOR_HEADER);
                if (!double.TryParse(Console.ReadLine(), out factor) || factor <= 0) factor = 1.3;
                Escribir($"  ✔ Factor aplicado: {factor:F2}x\n", COLOR_POSITIVO);
            }

            Console.WriteLine();
            Escribir("  ¿Cuántos DÍAS deseas simular?: ", COLOR_HEADER);
            if (!int.TryParse(Console.ReadLine(), out int cantidad) || cantidad <= 0) cantidad = 20;
            Escribir($"  ✔ Se simularán {cantidad} días usando el Generador Congruencial.\n\n", COLOR_POSITIVO);

            var numerosU = _rnd!.Generar(cantidad);

            Console.WriteLine();
            Escribir("  ¿Deseas ver el procedimiento paso a paso? [S/N]: ", COLOR_HEADER);
            string? resp = Console.ReadLine()?.Trim().ToUpper();
            Console.CursorVisible = false;
            bool verProcedimiento = resp == "S" || resp == "SI" || resp == "SÍ";

            Console.WriteLine();
            Escribir("  Procesando simulación", COLOR_INFO);
            AnimarCarga(20);
            Console.WriteLine("\n");

            var resultados = Simular(numerosU, politica, factor);
            MostrarResultados(resultados, politica, factor, verProcedimiento);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  MOTOR DE SIMULACIÓN — usa _rnd (congruencial mixto) para servicios
        // ─────────────────────────────────────────────────────────────────────
        static List<ResultadoDia> Simular(List<double> numerosU, int politica, double factor)
        {
            var resultados  = new List<ResultadoDia>();
            double ingresoAcum = 0;

            for (int i = 0; i < numerosU.Count; i++)
            {
                double uDia    = numerosU[i];
                int pacientes  = AplicarTriangular(uDia, 10, 28, 40);
                double ingresosDia = 0;

                for (int p = 0; p < pacientes; p++)
                {
                    // Usamos nuestro generador congruencial para el U de servicio
                    double uServicio = _rnd!.NextDouble();
                    var (servicio, _) = SeleccionarServicioConU(uServicio);

                    double precioEfectivo   = politica == 1 ? servicio.Precio : servicio.Precio * factor;
                    int serviciosPorPaciente = politica == 1 ? (int)Math.Round(factor) : 1;

                    ingresosDia += (precioEfectivo * serviciosPorPaciente);
                }

                ingresoAcum += ingresosDia;

                resultados.Add(new ResultadoDia(
                    Dia:              i + 1,
                    UPacientes:       uDia,
                    Pacientes:        pacientes,
                    IngresosDia:      ingresosDia,
                    IngresoAcumulado: ingresoAcum
                ));
            }
            return resultados;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  MOSTRAR RESULTADOS
        // ─────────────────────────────────────────────────────────────────────
        static void MostrarResultados(List<ResultadoDia> res, int politica, double factor, bool verProc)
        {
            Console.Clear();
            EncabezadoSeccion($"RESULTADOS — POLÍTICA {politica}");
            Console.WriteLine();

            if (verProc)
            {
                MostrarProcedimiento(res, politica, factor);
                Console.WriteLine();
                Escribir("  Presiona cualquier tecla para ver la tabla de resultados...", COLOR_HEADER);
                Console.ReadKey(true);
                Console.Clear();
                EncabezadoSeccion($"RESULTADOS — POLÍTICA {politica}");
                Console.WriteLine();
            }

            MostrarTabla(res);
            Console.WriteLine();
            MostrarResumen(res, politica, factor);
            Console.WriteLine();
            MostrarGraficaBarras(res);
            Console.WriteLine();
            Escribir("  Presiona cualquier tecla para volver al menú...", COLOR_HEADER);
            Console.ReadKey(true);
        }

        static void MostrarProcedimiento(List<ResultadoDia> res, int politica, double factor)
        {
            EncabezadoSeccion("PROCEDIMIENTO PASO A PASO");
            Console.WriteLine();

            double fc = (28.0 - 10.0) / (40.0 - 10.0);
            Escribir("  Fórmula Triangular (Min=10, Moda=28, Máx=40):\n", COLOR_HEADER);
            Escribir($"    fc = (28-10)/(40-10) = {fc:F4}\n", COLOR_NEUTRO);
            Escribir("    Si U < fc  →  X = 10 + √(U × 30 × 18)\n", COLOR_NEUTRO);
            Escribir("    Si U ≥ fc  →  X = 40 - √((1-U) × 30 × 12)\n", COLOR_NEUTRO);
            Console.WriteLine();
            Escribir("  ──────────────────────────────────────────────────────────\n\n", COLOR_TABLA);

            foreach (var r in res)
            {
                Escribir($"  ┌─ DÍA {r.Dia}", COLOR_TITULO);
                Escribir($"  (U = {r.UPacientes:F4})\n", COLOR_ACCENT);

                bool ramaIzq = r.UPacientes < fc;
                Escribir("  │  PASO 1 — Pacientes (Distribución Triangular)\n", COLOR_HEADER);
                Escribir($"  │    U = {r.UPacientes:F4}  {(ramaIzq ? "<" : "≥")}  fc = {fc:F4}  →  rama {(ramaIzq ? "izquierda" : "derecha")}\n", COLOR_NEUTRO);
                if (ramaIzq)
                    Escribir($"  │    X = 10 + √({r.UPacientes:F4} × 30 × 18)\n", COLOR_NEUTRO);
                else
                    Escribir($"  │    X = 40 - √((1 - {r.UPacientes:F4}) × 30 × 12)\n", COLOR_NEUTRO);
                Escribir($"  │    → Pacientes = ", COLOR_NEUTRO);
                Escribir($"{r.Pacientes}\n", COLOR_POSITIVO);

                Escribir("  │  PASO 2 y 3 — Servicios e Ingreso (U via Congruencial Mixto)\n", COLOR_HEADER);
                Escribir($"  │    Se simularon {r.Pacientes} servicios con el generador Rnd.\n", COLOR_NEUTRO);
                Escribir($"  │    → Ingreso del día: ", COLOR_NEUTRO);
                Escribir($"${r.IngresosDia:N2}\n", COLOR_POSITIVO);
                Escribir($"  │    Acumulado: ${r.IngresoAcumulado:N2}\n", COLOR_INFO);
                Escribir("  └──────────────────────────────────────────────────────\n\n", COLOR_TABLA);
            }
        }

        static void MostrarTabla(List<ResultadoDia> res)
        {
            EncabezadoSeccion("TABLA DE RESULTADOS (RESUMEN DIARIO)");
            Console.WriteLine();

            string sep = new string('─', 65);
            Escribir($"  ╔{sep}╗\n", COLOR_TABLA);
            string h = $"{"DÍA",4} │ {"U (Pacientes)",13} │ {"PACIENTES",9} │ {"INGRESO DÍA",13} │ {"ACUMULADO",13}";
            Escribir("  ║ ", COLOR_TABLA); Escribir(h, COLOR_HEADER); Escribir(" ║\n", COLOR_TABLA);
            Escribir($"  ╠{sep}╣\n", COLOR_TABLA);

            double costoRef = res.Count > 0 ? CostoMensual / res.Count : 0;

            foreach (var r in res)
            {
                bool cubrio = r.IngresoAcumulado >= CostoMensual;
                bool diaOk  = r.IngresosDia >= costoRef;
                var  cDia   = diaOk  ? COLOR_POSITIVO : COLOR_NEGATIVO;
                var  cAcum  = cubrio ? COLOR_POSITIVO : ConsoleColor.DarkYellow;

                Escribir("  ║ ", COLOR_TABLA);
                Escribir($"{r.Dia,4}",                   COLOR_NEUTRO);
                Escribir(" │ ", COLOR_TABLA);
                Escribir($"{r.UPacientes,13:F4}",        COLOR_ACCENT);
                Escribir(" │ ", COLOR_TABLA);
                Escribir($"{r.Pacientes,9}",             COLOR_ACCENT);
                Escribir(" │ ", COLOR_TABLA);
                Escribir($"${r.IngresosDia,12:N2}",      cDia);
                Escribir(" │ ", COLOR_TABLA);
                Escribir($"${r.IngresoAcumulado,12:N2}", cAcum);
                Escribir(" ║\n", COLOR_TABLA);
            }
            Escribir($"  ╚{sep}╝\n", COLOR_TABLA);
        }

        static void MostrarResumen(List<ResultadoDia> res, int politica, double factor)
        {
            if (res.Count == 0) return;

            double total    = res.Last().IngresoAcumulado;
            double prom     = total / res.Count;
            double promPac  = res.Average(r => r.Pacientes);
            double utilidad = total - CostoMensual;
            bool costeable  = utilidad >= 0;
            var  diaCub     = res.FirstOrDefault(r => r.IngresoAcumulado >= CostoMensual);

            EncabezadoSeccion("RESUMEN FINANCIERO");
            Console.WriteLine();

            string lineaH = new string('─', 60);
            Escribir($"  ┌{lineaH}┐\n", COLOR_TABLA);

            void Fila(string label, string val, ConsoleColor c)
            {
                Escribir("  │ ", COLOR_TABLA);
                Escribir($"{label,-32}", COLOR_NEUTRO);
                Escribir($"{val,25}", c);
                Escribir(" │\n", COLOR_TABLA);
            }

            Fila("Días simulados:",          $"{res.Count}",                   COLOR_NEUTRO);
            Fila("Costo mensual operativo:", $"${CostoMensual:N2}",            COLOR_NEGATIVO);
            Fila("Total ingresos:",          $"${total:N2}",                   COLOR_POSITIVO);
            Fila("Promedio ingreso/día:",    $"${prom:N2}",                    COLOR_INFO);
            Fila("Promedio pacientes/día:",  $"{promPac:F1}",                  COLOR_ACCENT);
            Fila("Utilidad / Déficit:",      $"${utilidad:N2}",                costeable ? COLOR_POSITIVO : COLOR_NEGATIVO);
            Fila("¿Es costeable?:",          costeable ? "✔ SÍ" : "✘ NO",     costeable ? COLOR_POSITIVO : COLOR_NEGATIVO);
            Fila("Costo cubierto en:",       diaCub != null ? $"Día {diaCub.Dia}" : "No cubierto",
                                                                               diaCub != null ? COLOR_POSITIVO : COLOR_NEGATIVO);

            Escribir($"  └{lineaH}┘\n", COLOR_TABLA);
            Console.WriteLine();

            EncabezadoSeccion($"ANÁLISIS POLÍTICA {politica}");
            Console.WriteLine();

            double avgBase = Servicios.Sum(s => s.Probabilidad * s.Precio);

            if (politica == 1)
            {
                int spxp = (int)Math.Round(factor);
                Escribir($"  Factor de servicios      : {factor:F2}x ({spxp} serv/paciente)\n", COLOR_ACCENT);
                Escribir($"  Precio promedio base     : ${avgBase:N2}\n",                        COLOR_INFO);
                int pacNec = (int)Math.Ceiling(CostoMensual / (avgBase * spxp * res.Count));
                Escribir($"  Pacientes mín/día        : ~{pacNec}\n",                            COLOR_NEUTRO);
            }
            else
            {
                double ajust = avgBase * factor;
                Escribir($"  Factor de precio         : {factor:F2}x\n",        COLOR_ACCENT);
                Escribir($"  Precio promedio base     : ${avgBase:N2}\n",        COLOR_NEUTRO);
                Escribir($"  Precio promedio ajust.   : ${ajust:N2}\n",          COLOR_POSITIVO);
                int pacNec = (int)Math.Ceiling(CostoMensual / (ajust * res.Count));
                Escribir($"  Pacientes mín/día        : ~{pacNec}\n",            COLOR_NEUTRO);
            }
            Console.WriteLine();
        }

        static void MostrarGraficaBarras(List<ResultadoDia> res)
        {
            if (res.Count == 0) return;

            EncabezadoSeccion("GRÁFICA DE INGRESOS POR DÍA");
            Console.WriteLine();

            double max      = res.Max(r => r.IngresosDia);
            double costoRef = CostoMensual / res.Count;

            Escribir($"  Referencia costo/día: ${costoRef:N2}  ", COLOR_HEADER);
            Escribir("(rojo = bajo meta | verde = sobre meta)\n\n", COLOR_INFO);

            foreach (var r in res)
            {
                int  barLen = max > 0 ? (int)Math.Round((r.IngresosDia / max) * 40) : 0;
                bool sobre  = r.IngresosDia >= costoRef;
                var  color  = sobre ? COLOR_POSITIVO : COLOR_NEGATIVO;

                Escribir($"  Día {r.Dia,2} │", COLOR_TABLA);
                Escribir(new string('█', barLen).PadRight(40), color);
                Escribir($"│ ${r.IngresosDia,10:N2}\n", COLOR_NEUTRO);
            }
            Console.WriteLine();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  COMPARAR POLÍTICAS
        // ─────────────────────────────────────────────────────────────────────
        static void CompararPoliticas()
        {
            Console.Clear();
            EncabezadoSeccion("COMPARACIÓN DE POLÍTICAS");
            Console.WriteLine();
            Console.CursorVisible = true;

            Escribir("  Factor para Política 1 (ej. 1.5): ", COLOR_HEADER);
            if (!double.TryParse(Console.ReadLine(), out double f1) || f1 <= 0) f1 = 1.5;
            Escribir("  Factor para Política 2 (ej. 1.3): ", COLOR_HEADER);
            if (!double.TryParse(Console.ReadLine(), out double f2) || f2 <= 0) f2 = 1.3;

            Console.WriteLine();
            Escribir("  ¿Cuántos DÍAS deseas simular para la comparación?: ", COLOR_HEADER);
            if (!int.TryParse(Console.ReadLine(), out int cantidad) || cantidad <= 0) cantidad = 20;
            Escribir($"  ✔ Se simularán {cantidad} días compartidos por ambas políticas.\n\n", COLOR_POSITIVO);

            var numerosU = _rnd!.Generar(cantidad);
            Console.CursorVisible = false;

            Escribir("\n  Simulando política 1", COLOR_INFO); AnimarCarga(15);
            var r1 = Simular(numerosU, 1, f1);
            Escribir("\n  Simulando política 2", COLOR_INFO); AnimarCarga(15);
            var r2 = Simular(numerosU, 2, f2);

            Console.Clear();
            EncabezadoSeccion("COMPARACIÓN DE POLÍTICAS");
            Console.WriteLine();

            double t1 = r1.Last().IngresoAcumulado;
            double t2 = r2.Last().IngresoAcumulado;
            bool   c1 = t1 >= CostoMensual;
            bool   c2 = t2 >= CostoMensual;

            string sep = new string('─', 56);
            Escribir($"  ╔{sep}╗\n", COLOR_TABLA);
            Escribir("  ║", COLOR_TABLA);
            Escribir($"  {"MÉTRICA",-28}{"POL. 1",12}{"POL. 2",12}   ", COLOR_HEADER);
            Escribir("║\n", COLOR_TABLA);
            Escribir($"  ╠{sep}╣\n", COLOR_TABLA);

            void FilaCmp(string label, string v1, string v2, ConsoleColor c)
            {
                Escribir("  ║ ", COLOR_TABLA);
                Escribir($"{label,-28}", COLOR_NEUTRO);
                Escribir($"{v1,12}", c);
                Escribir($"{v2,12}", c);
                Escribir("   ║\n", COLOR_TABLA);
            }

            FilaCmp("Total Ingresos:",       $"${t1:N2}",                           $"${t2:N2}",                           COLOR_POSITIVO);
            FilaCmp("¿Costeable?:",          c1 ? "SÍ ✔" : "NO ✘",                c2 ? "SÍ ✔" : "NO ✘",                c1 == c2 ? COLOR_NEUTRO : COLOR_ACCENT);
            FilaCmp("Utilidad:",             $"${t1 - CostoMensual:N2}",           $"${t2 - CostoMensual:N2}",           t1 >= t2 ? COLOR_POSITIVO : COLOR_INFO);
            FilaCmp("Prom. Pacientes/día:",  $"{r1.Average(r => r.Pacientes):F1}", $"{r2.Average(r => r.Pacientes):F1}", COLOR_ACCENT);
            FilaCmp("Prom. Ingreso/día:",    $"${r1.Average(r => r.IngresosDia):N2}", $"${r2.Average(r => r.IngresosDia):N2}", COLOR_INFO);

            Escribir($"  ╚{sep}╝\n", COLOR_TABLA);
            Console.WriteLine();

            string rec = t1 >= t2 ? "POLÍTICA 1" : "POLÍTICA 2";
            Escribir("  ★ Política recomendada: ", COLOR_HEADER);
            Escribir($"{rec}\n", COLOR_POSITIVO);

            Console.WriteLine();
            Escribir("  Presiona cualquier tecla para volver...", COLOR_HEADER);
            Console.ReadKey(true);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  DISTRIBUCIONES
        // ─────────────────────────────────────────────────────────────────────
        static void MostrarDistribuciones()
        {
            Console.Clear();
            EncabezadoSeccion("DISTRIBUCIONES DEL PROBLEMA");
            Console.WriteLine();

            Escribir("  ▸ TABLA CDF DE SERVICIOS\n\n", COLOR_HEADER);
            string sep = new string('─', 58);
            Escribir($"  ╔{sep}╗\n", COLOR_TABLA);
            Escribir("  ║", COLOR_TABLA);
            Escribir($"  {"SERVICIO",-26}{"PROB.",8}{"PRECIO",10}{"CDF",12}   ", COLOR_HEADER);
            Escribir("║\n", COLOR_TABLA);
            Escribir($"  ╠{sep}╣\n", COLOR_TABLA);

            double cdf = 0;
            foreach (var s in Servicios)
            {
                double antes = cdf;
                cdf += s.Probabilidad;
                Escribir("  ║ ", COLOR_TABLA);
                Escribir($"{s.Nombre,-26}", COLOR_INFO);
                Escribir($"{s.Probabilidad,8:P0}", COLOR_ACCENT);
                Escribir($"${s.Precio,9:N2}", COLOR_POSITIVO);
                Escribir($"({antes:F2},{cdf:F2}]", COLOR_NEUTRO);
                Escribir("   ║\n", COLOR_TABLA);
            }
            Escribir($"  ╚{sep}╝\n", COLOR_TABLA);
            Console.WriteLine();

            double fc = (28.0 - 10.0) / (40.0 - 10.0);
            Escribir("  ▸ DISTRIBUCIÓN TRIANGULAR (Min=10, Moda=28, Máx=40)\n\n", COLOR_HEADER);
            Escribir($"    fc = (Moda-Min)/(Máx-Min) = {fc:F4}\n", COLOR_NEUTRO);
            Escribir("    Si U < fc  →  X = 10 + √(U × 30 × 18)\n", COLOR_NEUTRO);
            Escribir("    Si U ≥ fc  →  X = 40 - √((1-U) × 30 × 12)\n\n", COLOR_NEUTRO);

            double precioEsp = Servicios.Sum(s => s.Probabilidad * s.Precio);
            double ingEspDia = precioEsp * 28;
            double ingEspMes = ingEspDia * 30;
            bool   teorico   = ingEspMes >= CostoMensual;

            Escribir($"  Precio esperado ponderado     : ${precioEsp:N2}\n", COLOR_POSITIVO);
            Escribir($"  Ingreso esperado/día (moda=28): ${ingEspDia:N2}\n", COLOR_INFO);
            Escribir($"  Ingreso esperado/mes (30 días): ${ingEspMes:N2}\n", COLOR_INFO);
            Console.WriteLine();
            Escribir("  Teóricamente costeable (sin ajuste): ", COLOR_HEADER);
            Escribir(teorico ? "SÍ ✔\n" : "NO ✘\n", teorico ? COLOR_POSITIVO : COLOR_NEGATIVO);

            Console.WriteLine();
            Escribir("  Presiona cualquier tecla para volver...", COLOR_HEADER);
            Console.ReadKey(true);
        }

        static void SalirPrograma()
        {
            Console.Clear();
            Console.WriteLine();
            CentrarTexto("╔══════════════════════════════════╗", COLOR_TITULO);
            CentrarTexto("║   Simulación finalizada.¡Gracias ║", COLOR_TITULO);
            CentrarTexto("║   por usar el programa           ║", COLOR_TITULO);
            CentrarTexto("╚══════════════════════════════════╝", COLOR_TITULO);
            Console.WriteLine();
            Console.CursorVisible = true;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  MÉTODOS ESTADÍSTICOS Y MATEMÁTICOS
        // ─────────────────────────────────────────────────────────────────────
        static int AplicarTriangular(double u, double min, double moda, double max)
        {
            double fc = (moda - min) / (max - min);
            double x  = u < fc
                ? min + Math.Sqrt(u * (max - min) * (moda - min))
                : max - Math.Sqrt((1 - u) * (max - min) * (max - moda));
            return (int)Math.Round(x);
        }

        static (Servicio servicio, double cdfAlcanzada) SeleccionarServicioConU(double u)
        {
            double acum = 0;
            foreach (var s in Servicios)
            {
                acum += s.Probabilidad;
                if (u <= acum) return (s, acum);
            }
            return (Servicios.Last(), 1.0);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────────────
        static long PedirEnteroPositivo(string prompt)
        {
            long valor = -1;
            while (valor <= 0)
            {
                Escribir(prompt, COLOR_HEADER);
                if (!long.TryParse(Console.ReadLine(), out valor) || valor <= 0)
                {
                    Escribir("  ⚠ Ingresa un entero positivo.\n", COLOR_NEGATIVO);
                    valor = -1;
                }
            }
            return valor;
        }

        static void Escribir(string texto, ConsoleColor color)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(texto);
            Console.ForegroundColor = prev;
        }

        static void CentrarTexto(string texto, ConsoleColor color)
        {
            int ancho = Console.WindowWidth > 0 ? Console.WindowWidth : 80;
            int pad   = Math.Max(0, (ancho - texto.Length) / 2);
            Escribir(new string(' ', pad) + texto + "\n", color);
        }

        static void EncabezadoSeccion(string titulo)
        {
            string sep = new string('═', titulo.Length + 4);
            Escribir($"\n  ╔{sep}╗\n", COLOR_TITULO);
            Escribir($"  ║  {titulo}  ║\n", COLOR_TITULO);
            Escribir($"  ╚{sep}╝\n", COLOR_TITULO);
        }

        static void DibujarCaja(int x, int y, int ancho, int alto, ConsoleColor color)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.SetCursorPosition(x, y);
            Console.Write("┌" + new string('─', ancho) + "┐");
            for (int i = 1; i < alto - 1; i++)
            {
                Console.SetCursorPosition(x, y + i);
                Console.Write("│" + new string(' ', ancho) + "│");
            }
            Console.SetCursorPosition(x, y + alto - 1);
            Console.Write("└" + new string('─', ancho) + "┘");
            Console.ForegroundColor = prev;
            Console.SetCursorPosition(0, y + alto);
        }

        static void AnimarCarga(int pasos)
        {
            string[] spinner = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
            for (int i = 0; i < pasos; i++)
            {
                Escribir($" {spinner[i % spinner.Length]}", COLOR_ACCENT);
                Thread.Sleep(60);
                Console.Write("\b\b");
            }
            Escribir(" ✔", COLOR_POSITIVO);
        }
    }
}