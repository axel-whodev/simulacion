using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace HospitalSim
{
    // ─────────────────────────────────────────────────────────────────────────
    //  MODELOS DE DATA A USAR
    // ─────────────────────────────────────────────────────────────────────────
    
    // usamos 'record' para almacenar todos datos estáticos
    // representa cada tipo de servicio disponible en el hospital
    record Servicio(string Nombre, double Probabilidad, double Precio);

    // guarda el resumen estadístico y financiero de un día completo de simulación
    record ResultadoDia(
        int    Dia,
        double UPacientes,
        int    Pacientes,
        double IngresosDia,
        double IngresoAcumulado
    );
    class Program
    {
        // definir la paleta de colores para mantener la interfaz consistente siempre
        const ConsoleColor COLOR_TITULO   = ConsoleColor.Cyan;
        const ConsoleColor COLOR_HEADER   = ConsoleColor.Yellow;
        const ConsoleColor COLOR_POSITIVO = ConsoleColor.Green;
        const ConsoleColor COLOR_NEGATIVO = ConsoleColor.Red;
        const ConsoleColor COLOR_NEUTRO   = ConsoleColor.White;
        const ConsoleColor COLOR_ACCENT   = ConsoleColor.Magenta;
        const ConsoleColor COLOR_INFO     = ConsoleColor.DarkCyan;
        const ConsoleColor COLOR_TABLA    = ConsoleColor.DarkGray;

        // costo fijo mensual del hospital
        static readonly double CostoMensual = 5000.00;

        // la lista de servicios
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

        // ─────────────────────────────────────────────────────────────────────
        //  PUNTO DE ENTRADA (EL ENTRY POINT)
        // ─────────────────────────────────────────────────────────────────────
        static void Main()
        {
            // forzamos UTF8 para que se dibujen bien las tablas y símbolos dentro de la consola
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CursorVisible  = false;
            
            MostrarPantallaBienvenida();
            Console.ReadKey(true);
            MostrarMenuPrincipal();
        }

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
            CentrarTexto("Transformada Inversa  •  Números U ingresados manualmente", COLOR_INFO);
            CentrarTexto("", ConsoleColor.White);

            DibujarCaja(14, Console.CursorTop, 60, 5, COLOR_ACCENT);
            Console.SetCursorPosition(16, Console.CursorTop);
            Escribir("  Parámetros del Problema:", COLOR_HEADER); Console.WriteLine();
            Console.SetCursorPosition(16, Console.CursorTop);
            Escribir($"  ▸ Costo mensual  : ${CostoMensual:N2}", COLOR_NEUTRO); Console.WriteLine();
            Console.SetCursorPosition(16, Console.CursorTop);
            Escribir("  ▸ Triangular     : Min=10, Moda=28, Máx=40", COLOR_NEUTRO); Console.WriteLine();
            Console.SetCursorPosition(16, Console.CursorTop);
            Escribir("  ▸ Números U      : ingresados manualmente", COLOR_NEUTRO); Console.WriteLine();
            Console.WriteLine();
            CentrarTexto("Presiona cualquier tecla para continuar...", ConsoleColor.DarkYellow);
        }
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
                    case "1": 
                        EjecutarPolitica(1); 
                        break;
                    case "2": 
                        EjecutarPolitica(2); 
                        break;
                    case "3": 
                        CompararPoliticas(); 
                        break;
                    case "4": 
                        MostrarDistribuciones(); 
                        break;
                    case "0": 
                        SalirPrograma(); 
                        return;
                    default:
                        Escribir("  ⚠ Opción inválida.", COLOR_NEGATIVO);
                        Thread.Sleep(900); 
                        break;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  METODO ESTATICO PARA LA PREPARACIÓN Y CAPTURA DE DATOS PARA LA SIMULACIÓN DE TODO
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

            // solicitamos el multiplicador dependiendo de la política seleccionada
            double factor = 1.0;
            if (politica == 1)
            {
                Escribir("  Factor de servicios por paciente (ej. 1.5 = +50% servicios): ", COLOR_HEADER);
                // si el usuario da enter vacío o mete letras, usamos 1.5 por defecto
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
            Escribir("  ¿Cuántos números U deseas insertar? (= días a simular): ", COLOR_HEADER);
            if (!int.TryParse(Console.ReadLine(), out int cantidad) || cantidad <= 0) cantidad = 20;
            Escribir($"  ✔ Insertarás {cantidad} número(s) U.\n\n", COLOR_POSITIVO);

            // bucle de captura manual para los números pseudoaleatorios
            Escribir("  Ingresa cada número U (entre 0.0 y 1.0) y presiona Enter:\n", COLOR_HEADER);
            Escribir("  ──────────────────────────────────────────────────────────\n", COLOR_TABLA);

            var numerosU = new List<double>();
            for (int i = 0; i < cantidad; i++)
            {
                double u = -1;
                // validamos estrictamente que el valor esté en el rango de probabilidad
                while (u < 0 || u > 1)
                {
                    Escribir($"  U[{i + 1,2}] → ", COLOR_ACCENT);
                    string? entrada = Console.ReadLine()?.Trim().Replace(",", ".");
                    if (!double.TryParse(entrada,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out u) || u < 0 || u > 1)
                    {
                        Escribir("       Debe ser un número entre 0.0 y 1.0.\n", COLOR_NEGATIVO);
                        u = -1;
                    }
                }
                numerosU.Add(u);
            }

            Console.WriteLine();
            Escribir("  ¿Deseas ver el procedimiento paso a paso? [S/N]: ", COLOR_HEADER);
            string? resp = Console.ReadLine()?.Trim().ToUpper();
            Console.CursorVisible = false;
            bool verProcedimiento = resp == "S" || resp == "SI" || resp == "SÍ";

            Console.WriteLine();
            Escribir("  Procesando simulación", COLOR_INFO);
            AnimarCarga(20);
            Console.WriteLine("\n");

            // pasamos los números capturados al motor estadístico
            var resultados = Simular(numerosU, politica, factor);
            MostrarResultados(resultados, politica, factor, verProcedimiento);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  MOTOR DE SIMULACIÓN SEGUN MONTE CARLO 
        // ─────────────────────────────────────────────────────────────────────
        static List<ResultadoDia> Simular(List<double> numerosU, int politica, double factor)
        {
            var resultados  = new List<ResultadoDia>();
            double ingresoAcum = 0;
            
            // la semilla y nos genere valores duplicados en el proceso automático.
            Random rnd = new Random();

            for (int i = 0; i < numerosU.Count; i++)
            {
                // 1: Obtener cantidad de pacientes del día
                double uDia = numerosU[i];
                int pacientes = AplicarTriangular(uDia, 10, 28, 40);

                double ingresosDia = 0;

                // 2: Evaluar qué consumió cada paciente de forma independiente
                for (int p = 0; p < pacientes; p++)
                {
                    double uServicio = rnd.NextDouble(); 
                    var (servicio, _) = SeleccionarServicioConU(uServicio);

                    // aplicamos las reglas de negocio según la política seleccionada para la simulacion
                    double precioEfectivo = politica == 1 ? servicio.Precio : servicio.Precio * factor;
                    int serviciosPorPaciente = politica == 1 ? (int)Math.Round(factor) : 1;

                    ingresosDia += (precioEfectivo * serviciosPorPaciente);
                }

                ingresoAcum += ingresosDia;

                // registramos los resultados del día simulado
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
        //  METODO PARA RENDERIZAR TODOS LOS RESULTADOS  REGISTRADOS
        // ─────────────────────────────────────────────────────────────────────
        static void MostrarResultados(List<ResultadoDia> res, int politica, double factor, bool verProc)
        {
            Console.Clear();
            EncabezadoSeccion($"RESULTADOS — POLÍTICA {politica}");
            Console.WriteLine();

            // si el usuario lo pidió, mostramos el desglose matemático de todooo el procedimiento
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

            // mostramos los gráficos y resúmenes financieros del hospital
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

            // calculamos 'fc'
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

                Escribir("  │  PASO 2 y 3 — Servicios e Ingreso\n", COLOR_HEADER);
                Escribir($"  │    Se simularon {r.Pacientes} servicios de forma automatizada.\n", COLOR_NEUTRO);
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

            // calculamos cuánto debería generar el hospital diariamente para salir tablas al final de todo
            double costoRef = res.Count > 0 ? CostoMensual / res.Count : 0;
            
            foreach (var r in res)
            {
                bool cubrio  = r.IngresoAcumulado >= CostoMensual;
                bool diaOk   = r.IngresosDia >= costoRef; // Compara contra la meta diaria
                var  cDia    = diaOk ? COLOR_POSITIVO : COLOR_NEGATIVO;
                var  cAcum   = cubrio ? COLOR_POSITIVO : ConsoleColor.DarkYellow;

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
            
            // cálculos financieros finales
            double total     = res.Last().IngresoAcumulado;
            double prom      = total / res.Count;
            double promPac   = res.Average(r => r.Pacientes);
            double utilidad  = total - CostoMensual;
            bool   costeable = utilidad >= 0;

            // buscamos el primer día donde el ingreso acumulado superó los $5000
            var    diaCub    = res.FirstOrDefault(r => r.IngresoAcumulado >= CostoMensual);

            EncabezadoSeccion("RESUMEN FINANCIERO");
            Console.WriteLine();

            string lineaH = new string('─', 60);
            Escribir($"  ┌{lineaH}┐\n", COLOR_TABLA);

            // función helper para imprimir las filas uniformemente
            void Fila(string label, string val, ConsoleColor c)
            {
                Escribir("  │ ", COLOR_TABLA);
                Escribir($"{label,-32}", COLOR_NEUTRO);
                Escribir($"{val,25}", c);
                Escribir(" │\n", COLOR_TABLA);
            }

            Fila("Días simulados:",           $"{res.Count}",                   COLOR_NEUTRO);
            Fila("Costo mensual operativo:",  $"${CostoMensual:N2}",            COLOR_NEGATIVO);
            Fila("Total ingresos:",           $"${total:N2}",                   COLOR_POSITIVO);
            Fila("Promedio ingreso/día:",     $"${prom:N2}",                    COLOR_INFO);
            Fila("Promedio pacientes/día:",   $"{promPac:F1}",                  COLOR_ACCENT);
            Fila("Utilidad / Déficit:",       $"${utilidad:N2}",                costeable ? COLOR_POSITIVO : COLOR_NEGATIVO);
            Fila("¿Es costeable?:",           costeable ? "✔ SÍ" : "✘ NO",     costeable ? COLOR_POSITIVO : COLOR_NEGATIVO);
            Fila("Costo cubierto en:",        diaCub != null ? $"Día {diaCub.Dia}" : "No cubierto",
                                                                                diaCub != null ? COLOR_POSITIVO : COLOR_NEGATIVO);

            Escribir($"  └{lineaH}┘\n", COLOR_TABLA);
            Console.WriteLine();

            EncabezadoSeccion($"ANÁLISIS POLÍTICA {politica}");
            Console.WriteLine();
            
            // calculamos el valor monetario esperado de un servicio usando su probabilidad base
            double avgBase = Servicios.Sum(s => s.Probabilidad * s.Precio);

            if (politica == 1)
            {
                int spxp = (int)Math.Round(factor);
                Escribir($"  Factor de servicios     : {factor:F2}x ({spxp} serv/paciente)\n", COLOR_ACCENT);
                Escribir($"  Precio promedio base    : ${avgBase:N2}\n",                        COLOR_INFO);
        
                int pacNec = (int)Math.Ceiling(CostoMensual / (avgBase * spxp * res.Count));
                Escribir($"  Pacientes mín/día       : ~{pacNec}\n",                            COLOR_NEUTRO);
            }
            else
            {
                double ajust = avgBase * factor;
                Escribir($"  Factor de precio        : {factor:F2}x\n",          COLOR_ACCENT);
                Escribir($"  Precio promedio base    : ${avgBase:N2}\n",          COLOR_NEUTRO);
                Escribir($"  Precio promedio ajust.  : ${ajust:N2}\n",            COLOR_POSITIVO);
                int pacNec = (int)Math.Ceiling(CostoMensual / (ajust * res.Count));
                Escribir($"  Pacientes mín/día       : ~{pacNec}\n",              COLOR_NEUTRO);
            }
            Console.WriteLine();
        }

        static void MostrarGraficaBarras(List<ResultadoDia> res)
        {
            if (res.Count == 0) return;
            
            EncabezadoSeccion("GRÁFICA DE INGRESOS POR DÍA");
            Console.WriteLine();

            // obtenemos el ingreso máximo para normalizar el tamaño de las barras
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
        //  COMPARACIÓN EN PARALELO DE AMBAS POLITICAS
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
            Escribir("  ¿Cuántos números U deseas insertar? (= días): ", COLOR_HEADER);
            if (!int.TryParse(Console.ReadLine(), out int cantidad) || cantidad <= 0) cantidad = 20;
            Escribir($"  ✔ Insertarás {cantidad} número(s) U (compartidos por ambas políticas).\n\n", COLOR_POSITIVO);

            Escribir("  Ingresa cada número U (entre 0.0 y 1.0):\n", COLOR_HEADER);
            Escribir("  ──────────────────────────────────────────\n", COLOR_TABLA);

            var numerosU = new List<double>();
            for (int i = 0; i < cantidad; i++)
            {
                double u = -1;
                while (u < 0 || u > 1)
                {
                    Escribir($"  U[{i + 1,2}] → ", COLOR_ACCENT);
                    string? ent = Console.ReadLine()?.Trim().Replace(",", ".");
                    if (!double.TryParse(ent,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out u) || u < 0 || u > 1)
                    {
                        Escribir("       ⚠ Debe ser entre 0.0 y 1.0.\n", COLOR_NEGATIVO);
                        u = -1;
                    }
                }
                numerosU.Add(u);
            }
            Console.CursorVisible = false;

            Escribir("\n  Simulando política 1", COLOR_INFO); AnimarCarga(15);

            // simula Política 1
            var r1 = Simular(numerosU, 1, f1);
            
            Escribir("\n  Simulando política 2", COLOR_INFO); AnimarCarga(15);
            
            // simula Política 2 (Importante: usamos los mismos números U base para una comparación justa)
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

            FilaCmp("Total Ingresos:",      $"${t1:N2}",                       $"${t2:N2}",                       COLOR_POSITIVO);
            FilaCmp("¿Costeable?:",         c1 ? "SÍ ✔" : "NO ✘",             c2 ? "SÍ ✔" : "NO ✘",             c1 == c2 ? COLOR_NEUTRO : COLOR_ACCENT);
            FilaCmp("Utilidad:",            $"${t1 - CostoMensual:N2}",        $"${t2 - CostoMensual:N2}",        t1 >= t2 ? COLOR_POSITIVO : COLOR_INFO);
            FilaCmp("Prom. Pacientes/día:", $"{r1.Average(r => r.Pacientes):F1}", $"{r2.Average(r => r.Pacientes):F1}", COLOR_ACCENT);
            FilaCmp("Prom. Ingreso/día:",   $"${r1.Average(r => r.IngresosDia):N2}", $"${r2.Average(r => r.IngresosDia):N2}", COLOR_INFO);

            Escribir($"  ╚{sep}╝\n", COLOR_TABLA);
            Console.WriteLine();

            // aqui se recomienda automáticamente la política que haya generado mayor utilidad al final de la simulacion
            string rec = t1 >= t2 ? "POLÍTICA 1" : "POLÍTICA 2";
            Escribir("  ★ Política recomendada: ", COLOR_HEADER);
            Escribir($"{rec}\n", COLOR_POSITIVO);

            Console.WriteLine();
            Escribir("  Presiona cualquier tecla para volver...", COLOR_HEADER);
            Console.ReadKey(true);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PANTALLA ESTADÍSTICA
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

            // se calcula la probabilidad acumulada de forma dinámica
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

            // verificación matemática de si el negocio es sostenible sin aplicar políticas
            double precioEsp  = Servicios.Sum(s => s.Probabilidad * s.Precio);
            double ingEspDia  = precioEsp * 28; 
            double ingEspMes  = ingEspDia * 30; 
            bool   teorico    = ingEspMes >= CostoMensual;

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
        //  MÉTODOS ESTADÍSTICOS Y MATEMÁTICOS (CORE PRINCIPAL DE EL MOTOR MATEMATICO)
        // ─────────────────────────────────────────────────────────────────────
        static int AplicarTriangular(double u, double min, double moda, double max)
        {
            // 'fc' delimita el pico del triangulo. Separa la rama ascendente de la descendente.
            double fc = (moda - min) / (max - min);
            
            double x  = u < fc
                ? min + Math.Sqrt(u * (max - min) * (moda - min)) // rama izquierda
                : max - Math.Sqrt((1 - u) * (max - min) * (max - moda)); // rama derecha
            
            // retornamos redondeado porque no pueden existir pacientes fraccionarios
            return (int)Math.Round(x);
        }

        static (Servicio servicio, double cdfAlcanzada) SeleccionarServicioConU(double u)
        {
            double acum = 0;
            foreach (var s in Servicios)
            {
                acum += s.Probabilidad;

                // si el numero aleatorio cae en este rango acumulado, este es el servicio ganador
                if (u <= acum) return (s, acum);
            }
            // retorno por seguridad en caso de fallo de redondeo decimal en la lista 
            return (Servicios.Last(), 1.0);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HELPERS DE INTERFAZ DE USUARIO (PARA TODO EL DIBUJADO DE LA CONSOLA)
        // ─────────────────────────────────────────────────────────────────────
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
            // animación sencilla del spinner de carga
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