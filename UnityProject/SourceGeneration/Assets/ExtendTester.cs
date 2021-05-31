using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SourceGeneration.ConsoleApp
{
    public class ExtendTester : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("Who is going to be the next prime minister?");
            var random = Random.Range(0, PrimeMinisterExtension.Values.Length);
            var primeMinister = PrimeMinisterExtension.Values[random];
            Debug.Log(primeMinister);
        }
    }
}