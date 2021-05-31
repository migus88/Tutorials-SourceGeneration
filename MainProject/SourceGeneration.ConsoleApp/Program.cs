using System;
using System.Linq;

namespace SourceGeneration.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var vehicle = VehicleExtension.Values[1];
            Console.WriteLine(AnimalExtension.Values[0]);
        }

        public class someClassName
        {
            
        }
    }
}