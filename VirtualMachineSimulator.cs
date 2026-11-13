using System;
using System.IO;
using System.Linq;

namespace VirtualMachineSimulator
{
    internal class Program
    {
        private const int TotalRegisters = 256;
        private static readonly int[] Registers = new int[TotalRegisters];

        private static void Main(string[] args)
        {
            Console.BackgroundColor= ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Clear();
            Console.Title = "Virtual Machine Simulator - 256 Registers";
            Console.WriteLine("==============================================");
            Console.WriteLine("       VIRTUAL MACHINE SIMULATOR");
            Console.WriteLine("       256 Registers (R0: Accumulator)");
            Console.WriteLine("==============================================");
            Console.WriteLine();

            string filePath = args.FirstOrDefault(a => !a.StartsWith("--"));

            if (string.IsNullOrWhiteSpace(filePath))
            {
                Console.Write("Input script file (.txt): ");
                filePath = Console.ReadLine();
            }

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Console.WriteLine("Error: Input file not found.");
                return;
            }

            bool stepMode = args.Any(a => a.Equals("--step", StringComparison.OrdinalIgnoreCase));

            string[] executionLines;

            try
            {
                executionLines = File.ReadAllLines(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading input file: " + ex.Message);
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Loaded script file: " + filePath);
            Console.WriteLine("Lines: " + executionLines.Length);
            Console.WriteLine(stepMode ? "Execution mode: STEP" : "Execution mode: RUN ALL");
            Console.WriteLine();
            Console.WriteLine("----- SCREEN OUTPUT -----");

            for (int i = 0; i < executionLines.Length; i++)
            {
                string line = executionLines[i];
                string cleanLine = line.Trim();

                // Ignore empty lines and comments.
                if (cleanLine.Length == 0 ||
                    cleanLine.StartsWith("#") ||
                    cleanLine.StartsWith("//"))
                {
                    continue;
                }

                if (stepMode)
                {
                    Console.WriteLine();
                    Console.WriteLine("[Line " + (i + 1) + "] " + cleanLine);
                    Console.Write("Press ENTER to execute, or Q to quit: ");

                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        Console.WriteLine("Execution stopped.");
                        break;
                    }
                }

                ExecuteLine(line);
            }

            Console.WriteLine();
            Console.WriteLine("----- REGISTER BANK -----");
            ShowRegisters();

            Console.WriteLine();
            Console.WriteLine("Accumulator (R0): " + Registers[0]);
            Console.WriteLine("Modified registers: " +
                Registers.Count(value => value != 0));

            Console.WriteLine();
            Console.WriteLine("Execution finished.");
        }

        private static void ExecuteLine(string rawLine)
        {
            string cleanLine = rawLine.Trim();

            // Ignore empty lines and comments.
            if (cleanLine.Length == 0 ||
                cleanLine.StartsWith("#") ||
                cleanLine.StartsWith("//"))
            {
                return;
            }

            // Split comma-separated parameters.
            string[] parts = cleanLine
                .Split(',')
                .Select(part => part.Trim())
                .ToArray();

            string keyword = parts[0].ToLowerInvariant();

            switch (keyword)
            {
                case "move":
                {
                    if (parts.Length < 3)
                    {
                        PrintError("\"move\" requires register and integer value (format: move, reg, val)");
                        return;
                    }

                    int reg;
                    int value;

                    if (!int.TryParse(parts[1], out reg) ||
                        reg < 0 || reg >= TotalRegisters ||
                        !int.TryParse(parts[2], out value))
                    {
                        PrintError("Invalid arguments for move. Register must be 0-255, value must be integer.");
                        return;
                    }

                    Registers[reg] = value;
                    Console.WriteLine("register " + reg + " = " + value);
                    break;
                }

                case "store":
                {
                    if (parts.Length < 2)
                    {
                        PrintError("\"store\" requires source register (format: store, reg)");
                        return;
                    }

                    int reg;

                    if (!int.TryParse(parts[1], out reg) ||
                        reg < 0 || reg >= TotalRegisters)
                    {
                        PrintError("Invalid register " + parts[1] + " for store.");
                        return;
                    }

                    Registers[0] = Registers[reg];
                    Console.WriteLine("register 0 = " + Registers[0]);
                    break;
                }

                case "load":
                {
                    if (parts.Length < 3)
                    {
                        PrintError("\"load\" requires destination and source registers (format: load, dest, src)");
                        return;
                    }

                    int dest;
                    int src;

                    if (!int.TryParse(parts[1], out dest) ||
                        !int.TryParse(parts[2], out src) ||
                        dest < 0 || dest >= TotalRegisters ||
                        src < 0 || src >= TotalRegisters)
                    {
                        PrintError("Invalid registers for load. Must be between 0 and 255.");
                        return;
                    }

                    Registers[dest] = Registers[src];
                    Console.WriteLine("register " + dest + " = " + Registers[dest]);
                    break;
                }

                case "print":
                {
                    // Print the value held in the accumulator (register 0).
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(Registers[0]);
                    Console.ResetColor();
                    break;
                }

                case "reset":
                {
                    // Set all registers to zero and display "reset".
                    Array.Clear(Registers, 0, Registers.Length);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("reset");
                    Console.ResetColor();
                    break;
                }

                case "help":
                {
                    ShowHelp();
                    break;
                }

                default:
                {
                    PrintError("Unknown keyword: \"" + keyword + "\"");
                    break;
                }
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("--- Virtual Machine ISA Help ---");
            Console.WriteLine("move, reg, val    : Assigns integer [val] to [reg] (0-255).");
            Console.WriteLine("store, reg        : Loads value from [reg] into accumulator (Reg 0).");
            Console.WriteLine("load, dest, src   : Copies value from [src] register to [dest] register.");
            Console.WriteLine("print             : Outputs value currently in accumulator (Reg 0).");
            Console.WriteLine("reset             : Sets all registers to 0 and displays \"reset\".");
            Console.WriteLine("help              : Displays this documentation.");
        }

        private static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: " + message);
            Console.ResetColor();
        }

        private static void ShowRegisters()
        {
            int modified = 0;

            for (int i = 0; i < TotalRegisters; i++)
            {
                if (Registers[i] != 0)
                {
                    Console.WriteLine("R" + i.ToString().PadLeft(3) + " = " + Registers[i]);
                    modified++;
                }
            }

            if (modified == 0)
            {
                Console.WriteLine("All registers are zero.");
            }
        }
    }
}
