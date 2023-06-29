using System;
using System.IO;


namespace InxNoJIT
{

   public static class Program
    {
        public static string path = "";
    
        static void Main(string[] args)
        {
            Console.Title = "InxNoJIT > github.com/0x58B1337 <";
            path = args[0];
            byte[] nojit = JIT.RestoreMethods();
            File.WriteAllBytes(Path.GetFileNameWithoutExtension(path) + "-NoJIT" + Path.GetExtension(path), nojit);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Done!");
            Console.ReadLine();
        }
        
    }
}
