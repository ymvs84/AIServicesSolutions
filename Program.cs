using System;
using Azure;
using Microsoft.Extensions.Configuration;
using Azure.AI.TextAnalytics;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace sdk_client
{
    // Clase que representa el resultado de la detección del idioma
    public class LanguageDetectionResult
    {
        // Propiedad que contiene el nombre del idioma detectado
        public string Idioma { get; set; }
        // Propiedad que contiene el porcentaje de confianza (entre 0 y 1)
        public double Confianza { get; set; }
    }

    // Clase que se encarga de interactuar con el servicio Azure Text Analytics
    public class LanguageDetector
    {
        private readonly TextAnalyticsClient client;

        // Constructor que inicializa el cliente utilizando el endpoint y la clave del servicio
        public LanguageDetector(string endpoint, string apiKey)
        {
            AzureKeyCredential credentials = new AzureKeyCredential(apiKey);
            client = new TextAnalyticsClient(new Uri(endpoint), credentials);
        }

        // Método que realiza la detección del idioma y retorna un objeto LanguageDetectionResult
        public LanguageDetectionResult Detect(string text)
        {
            DetectedLanguage detectedLanguage = client.DetectLanguage(text);
            return new LanguageDetectionResult
            {
                Idioma = detectedLanguage.Name,
                Confianza = detectedLanguage.ConfidenceScore
            };
        }
    }

    // Clase que representa cada texto registrado junto a su información de detección
    public class TextoRegistrado
    {
        public string Texto { get; set; }
        public string Idioma { get; set; }
        public double Confianza { get; set; }
        public DateTime Fecha { get; set; }
    }

    // Clase principal de la aplicación
    class Program
    {
        // Variables globales para almacenar el endpoint y la clave del servicio
        private static string AISvcEndpoint;
        private static string AISvcKey;
        // Ruta del archivo JSON para persistir los textos registrados
        private static string jsonFilePath = "textos.json";

        static void Main(string[] args)
        {
            try
            {
                // Cargar la configuración desde el archivo appsettings.json
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                AISvcEndpoint = configuration["AIServicesEndpoint"];
                AISvcKey = configuration["AIServicesKey"];

                // Instancia del detector de idioma que encapsula la lógica de Azure AI Text Analytics
                LanguageDetector detector = new LanguageDetector(AISvcEndpoint, AISvcKey);

                // Bucle principal para el menú de la aplicación
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
                            // Opción para escribir un texto y realizar la detección del idioma
                            Console.Write("\nEscribe un texto: ");
                            string texto = Console.ReadLine();

                            // Se obtiene el resultado de la detección a través de la clase LanguageDetector
                            LanguageDetectionResult resultado = detector.Detect(texto);

                            // Mostrar el idioma detectado y el porcentaje de confianza formateado
                            Console.WriteLine($"Idioma detectado: {resultado.Idioma} ({resultado.Confianza * 100:F1}%)");

                            // Crear un objeto TextoRegistrado con la información obtenida y la fecha actual
                            TextoRegistrado nuevoTexto = new TextoRegistrado
                            {
                                Texto = texto,
                                Idioma = resultado.Idioma,
                                Confianza = resultado.Confianza,
                                Fecha = DateTime.Now
                            };

                            // Guardar el objeto en el archivo JSON
                            GuardarTexto(nuevoTexto);
                            break;

                        case "2":
                            // Opción para consultar los textos registrados en base a un idioma
                            Console.Write("\nIntroduce el idioma a consultar (ej. English, Spanish): ");
                            string idiomaBuscado = Console.ReadLine();

                            // Leer la lista total de textos registrados desde el archivo JSON
                            List<TextoRegistrado> textos = LeerTextos();

                            // Filtrar la lista de textos según el idioma ingresado (sin distinguir mayúsculas/minúsculas)
                            List<TextoRegistrado> filtrados = textos.FindAll(t =>
                                t.Idioma.Equals(idiomaBuscado, StringComparison.OrdinalIgnoreCase)
                            );

                            Console.WriteLine($"\nSe encontraron {filtrados.Count} texto(s) en {idiomaBuscado}:\n");

                            // Mostrar el listado de textos filtrados junto con su fecha y porcentaje de confianza
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
                            // Opción para salir del programa
                            Console.WriteLine("Saliendo del programa...");
                            break;

                        default:
                            // Opción inválida en caso de ingresar un valor distinto
                            Console.WriteLine("Opción inválida, intenta de nuevo.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                // En caso de error se muestra el mensaje correspondiente
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        // Método para guardar un objeto TextoRegistrado en el archivo JSON
        static void GuardarTexto(TextoRegistrado nuevo)
        {
            // Leer la lista actual de textos (si existe)
            List<TextoRegistrado> textos = LeerTextos();
            textos.Add(nuevo);

            // Serializar la lista a JSON con formato legible (indented)
            string json = JsonSerializer.Serialize(textos, new JsonSerializerOptions { WriteIndented = true });

            // Escribir el JSON en el archivo especificado
            File.WriteAllText(jsonFilePath, json);
        }

        // Método para leer la lista de textos registrados desde el archivo JSON
        static List<TextoRegistrado> LeerTextos()
        {
            // Si el archivo no existe, se retorna una lista vacía
            if (!File.Exists(jsonFilePath))
                return new List<TextoRegistrado>();

            // Leer el contenido del archivo
            string json = File.ReadAllText(jsonFilePath);
            // Deserializar el JSON y retornar la lista de textos
            return JsonSerializer.Deserialize<List<TextoRegistrado>>(json) ?? new List<TextoRegistrado>();
        }
    }
}
