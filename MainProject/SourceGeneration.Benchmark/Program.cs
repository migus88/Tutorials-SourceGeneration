using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SourceGeneration.ConsoleApp;

namespace SourceGeneration.Benchmark
{
    [MemoryDiagnoser]
    public class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Program>();
        }

        [Benchmark]
        public void GetValuesGeneric()
        {
            var values = Enum.GetValues<Animal>();
        }

        [Benchmark]
        public void GetValues()
        {
            var values = Enum.GetValues(typeof(Animal)).Cast<Animal>().ToArray();
        }

        [Benchmark]
        public void GetSourceGeneratedValues()
        {
            var values = AnimalExtension.Values;
        }
    }
}