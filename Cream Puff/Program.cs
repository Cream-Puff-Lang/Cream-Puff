using System;

namespace CreamPuff {
    class Program {
        /// <summary>
        /// Start a Cream Puff parser RPPL.
        /// </summary>
        /// <param name="args">Passed arguments. Currently unused.</param>
        static void Main(string[] args) {
            var parser = new Parser("");
            while (true) {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(parser.Finished ? "cream puff> " : "          > ");
                Console.ResetColor();
                parser.AddCode(Console.ReadLine() + '\n').Parse();
                if (parser.Finished) {
                    parser.Process();
                    Console.WriteLine(parser.Node);
                    parser.Clear();
                }
            }
        }
    }
}
