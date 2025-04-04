using System;
using Azure;
using Microsoft.Extensions.Configuration;
using System.Text;
using Azure.AI.TextAnalytics;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace sdk_client
{
    class Program
    {
        private static string AISvcEndpoint;
        private static string AISvcKey;
        private static string jsonFilePath = "textos.json";

        static void Main(string[] args)
        {
            try
            {
                // Leer configuración
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                AISvcEndpoint = configuration["AIServicesEndpoint"];
                AISvcKey = configuration["AIServicesKey"];

                string opcion = "";
                while (opcion != "3")
                {
                    Console.WriteLine("\n--- Menú ---");
                    Console.WriteLine("1. Escribir texto y detectar idioma");
                    Console.WriteLine("2. Consultar textos por idioma");
                    Console.WriteLine("3. Salir");
                    Console.Write("Selecciona una opción: ");
                    opcion = Console.ReadLine();

                    switch (opcion)
                    {
                        case "1":
                            Console.Write("\nEscribe un texto: ");
                            string texto = Console.ReadLine();
                            var resultado = GetLanguage(texto);

                            Console.WriteLine($"Idioma detectado: {resultado.Idioma} ({resultado.Confianza * 100:F1}%)");

                            TextoRegistrado nuevoTexto = new TextoRegistrado
                            {
                                Texto = texto,
                                Idioma = resultado.Idioma,
                                Confianza = resultado.Confianza,
                                Fecha = DateTime.Now
                            };

                            GuardarTexto(nuevoTexto);
                            break;

                        case "2":
                            Console.Write("\nIntroduce el idioma a consultar (ej. English, Spanish): ");
                            string idiomaBuscado = Console.ReadLine();

                            List<TextoRegistrado> textos = LeerTextos();
                            var filtrados = textos.FindAll(t =>
                                t.Idioma.Equals(idiomaBuscado, StringComparison.OrdinalIgnoreCase)
                            );

                            Console.WriteLine($"\nSe encontraron {filtrados.Count} texto(s) en {idiomaBuscado}:\n");

                            foreach (var t in filtrados)
                            {
                                Console.WriteLine($"- [{t.Fecha}] ({t.Confianza * 100:F1}%): {t.Texto}");
                            }

                            if (filtrados.Count == 0)
                            {
                                Console.WriteLine("No hay textos registrados para ese idioma.");
                            }
                            break;

                        case "3":
                            Console.WriteLine("Saliendo del programa...");
                            break;

                        default:
                            Console.WriteLine("Opción inválida, intenta de nuevo.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static (string Idioma, double Confianza) GetLanguage(string text)
        {
            AzureKeyCredential credentials = new AzureKeyCredential(AISvcKey);
            Uri endpoint = new Uri(AISvcEndpoint);
            var client = new TextAnalyticsClient(endpoint, credentials);

            DetectedLanguage detectedLanguage = client.DetectLanguage(text);
            return (detectedLanguage.Name, detectedLanguage.ConfidenceScore);
        }

        static void GuardarTexto(TextoRegistrado nuevo)
        {
            List<TextoRegistrado> textos = LeerTextos();
            textos.Add(nuevo);

            string json = JsonSerializer.Serialize(textos, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jsonFilePath, json);
        }

        static List<TextoRegistrado> LeerTextos()
        {
            if (!File.Exists(jsonFilePath))
                return new List<TextoRegistrado>();

            string json = File.ReadAllText(jsonFilePath);
            return JsonSerializer.Deserialize<List<TextoRegistrado>>(json) ?? new List<TextoRegistrado>();
        }
    }

    class TextoRegistrado
    {
        public string Texto { get; set; }
        public string Idioma { get; set; }
        public double Confianza { get; set; }
        public DateTime Fecha { get; set; }
    }
}
