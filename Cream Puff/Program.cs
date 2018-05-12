using System;

namespace CreamPuff {
    class Program {
        static void Main(string[] args) {
            // TODO: everything
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
