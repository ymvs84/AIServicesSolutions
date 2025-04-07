using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.AI.TextAnalytics;
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;

namespace sdk_client
{
    class Program
    {
        // Variables globales para almacenar las configuraciones de los servicios de Azure
        private static string AISvcEndpoint;
        private static string AISvcKey;
        private static string SpeechKey;
        private static string SpeechRegion;
        private static SpeechConfig speechConfig;

        // Ruta del archivo JSON donde se guardarán los textos registrados
        private static string jsonFilePath = "textos.json";

        static async Task Main(string[] args)
        {
            try
            {
                // Configuración inicial desde el archivo appsettings.json
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                AISvcEndpoint = configuration["AIServicesEndpoint"];
                AISvcKey = configuration["AIServicesKey"];
                SpeechKey = configuration["SpeechKey"];
                SpeechRegion = configuration["SpeechRegion"];

                // Configuración del servicio de voz de Azure
                speechConfig = SpeechConfig.FromSubscription(SpeechKey, SpeechRegion);

                string opcion = "";
                while (opcion != "4")
                {
                    // Menú principal del programa
                    Console.WriteLine("\n--- Menú ---");
                    Console.WriteLine("1. Ingresar texto (escrito o hablado)");
                    Console.WriteLine("2. Consultar textos por idioma");
                    Console.WriteLine("3. Detector de texto en imágenes");
                    Console.WriteLine("4. Salir");
                    Console.Write("Selecciona una opción: ");
                    opcion = Console.ReadLine();

                    switch (opcion)
                    {
                        case "1":
                            // Submenú para elegir entre texto escrito o hablado
                            Console.WriteLine("\n--- Submenú Entrada ---");
                            Console.WriteLine("1. Escribir texto");
                            Console.WriteLine("2. Hablar por micrófono");
                            Console.Write("Elige una opción: ");
                            string modoEntrada = Console.ReadLine();
                            string texto = "";

                            if (modoEntrada == "1")
                            {
                                Console.Write("Escribe el texto: ");
                                texto = Console.ReadLine();
                            }
                            else if (modoEntrada == "2")
                            {
                                texto = await TranscribeVoice();
                            }
                            else
                            {
                                Console.WriteLine("Opción inválida.");
                                break;
                            }

                            // Si el texto no está vacío, se procede a detectar el idioma
                            if (!string.IsNullOrWhiteSpace(texto))
                            {
                                var resultado = GetLanguage(texto);
                                Console.WriteLine($"Idioma detectado: {resultado.Idioma} ({resultado.Confianza * 100:F1}%)");

                                // Creación de un objeto TextoRegistrado con la información del texto
                                TextoRegistrado nuevoTexto = new TextoRegistrado
                                {
                                    Texto = texto,
                                    Idioma = resultado.Idioma,
                                    Confianza = resultado.Confianza,
                                    Fecha = DateTime.Now
                                };

                                // Guardar el texto en el archivo JSON
                                GuardarTexto(nuevoTexto);
                            }
                            break;

                        case "2":
                            // Consulta de textos por idioma
                            Console.Write("\nIntroduce el idioma a consultar (ej. English, Spanish): ");
                            string idiomaBuscado = Console.ReadLine();

                            // Leer todos los textos registrados
                            List<TextoRegistrado> textos = LeerTextos();
                            // Filtrar los textos por el idioma especificado
                            var filtrados = textos.FindAll(t =>
                                t.Idioma.Equals(idiomaBuscado, StringComparison.OrdinalIgnoreCase)
                            );

                            Console.WriteLine($"\nSe encontraron {filtrados.Count} texto(s) en {idiomaBuscado}:\n");

                            // Mostrar los textos filtrados
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
                            // Procesamiento de imágenes para detectar texto
                            await ProcesarImagenes();
                            break;

                        case "4":
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

        // Método para detectar el idioma de un texto dado
        static (string Idioma, double Confianza) GetLanguage(string text)
        {
            AzureKeyCredential credentials = new AzureKeyCredential(AISvcKey);
            Uri endpoint = new Uri(AISvcEndpoint);
            var client = new TextAnalyticsClient(endpoint, credentials);

            DetectedLanguage detectedLanguage = client.DetectLanguage(text);
            return (detectedLanguage.Name, detectedLanguage.ConfidenceScore);
        }

        // Método para guardar un texto registrado en el archivo JSON
        static void GuardarTexto(TextoRegistrado nuevo)
        {
            List<TextoRegistrado> textos = LeerTextos();
            textos.Add(nuevo);

            string json = JsonSerializer.Serialize(textos, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jsonFilePath, json);
        }

        // Método para leer los textos registrados desde el archivo JSON
        static List<TextoRegistrado> LeerTextos()
        {
            if (!File.Exists(jsonFilePath))
                return new List<TextoRegistrado>();

            string json = File.ReadAllText(jsonFilePath);
            return JsonSerializer.Deserialize<List<TextoRegistrado>>(json) ?? new List<TextoRegistrado>();
        }

        // Método para transcribir voz a texto usando el micrófono
        static async Task<string> TranscribeVoice()
        {
            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);
            Console.WriteLine("Habla ahora...");

            var result = await recognizer.RecognizeOnceAsync();

            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                Console.WriteLine($"Texto reconocido: {result.Text}");
                return result.Text;
            }
            else
            {
                Console.WriteLine($"No se erkannte texto. Razón: {result.Reason}");
                return "";
            }
        }

        // Método para procesar todas las imágenes en una carpeta dada
        static async Task ProcesarImagenes()
        {
            Console.Write("Introduce la ruta de la carpeta con las imágenes: ");
            string carpeta = Console.ReadLine();

            if (!Directory.Exists(carpeta))
            {
                Console.WriteLine("La carpeta no existe.");
                return;
            }

            var archivos = Directory.GetFiles(carpeta, "*.jpg").Concat(Directory.GetFiles(carpeta, "*.png"));

            foreach (var archivo in archivos)
            {
                Console.WriteLine($"Procesando {archivo}...");
                await ProcesarImagen(archivo);
            }
        }

        // Método para procesar una imagen individual y detectar texto en ella
        static async Task ProcesarImagen(string path)
        {
            // Crear cliente para el análisis de imágenes
            var client = new ImageAnalyzerClient(new Uri(AISvcEndpoint), new AzureKeyCredential(AISvcKey));
            using var imageSource = new FileStream(path, FileMode.Open);

            // Configurar opciones de análisis para detectar texto
            var analysisOptions = new ImageAnalysisOptions
            {
                Features = ImageAnalysisFeature.Read
            };

            // Analizar la imagen
            var result = await client.AnalyzeAsync(imageSource, analysisOptions);

            if (result.Reason == ImageAnalysisResultReason.Analyzed)
            {
                var readResult = result.ReadResult;
                if (readResult != null)
                {
                    // Unir todo el texto detectado en una sola cadena
                    string textoDetectado = string.Join(" ", readResult.Content.Select(c => c.Text));
                    var resultadoIdioma = GetLanguage(textoDetectado);
                    Console.WriteLine($"Idioma detectado: {resultadoIdioma.Idioma} ({resultadoIdioma.Confianza * 100:F1}%)");

                    // Encontrar la palabra más repetida en el texto detectado
                    var palabras = textoDetectado.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    var palabraMasRepetida = palabras.GroupBy(p => p)
                                                    .OrderByDescending(g => g.Count())
                                                    .First().Key;

                    // Crear un objeto TextoRegistrado con la información del texto de la imagen
                    TextoRegistrado nuevoTexto = new TextoRegistrado
                    {
                        Texto = textoDetectado,
                        Idioma = resultadoIdioma.Idioma,
                        Confianza = resultadoIdioma.Confianza,
                        Fecha = DateTime.Now,
                        PalabraMasRepetida = palabraMasRepetida
                    };

                    // Guardar el texto detectado en el archivo JSON
                    GuardarTexto(nuevoTexto);
                    Console.WriteLine($"Palabra más repetida: {palabraMasRepetida}");
                }
            }
            else
            {
                Console.WriteLine($"No se pudo analizar la imagen. Razón: {result.Reason}");
            }
        }
    }

    // Clase para representar un texto registrado con sus propiedades
    class TextoRegistrado
    {
        public string Texto { get; set; }
        public string Idioma { get; set; }
        public double Confianza { get; set; }
        public DateTime Fecha { get; set; }
        public string PalabraMasRepetida { get; set; }
    }
}
