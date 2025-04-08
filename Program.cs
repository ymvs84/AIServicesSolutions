using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.AI.TextAnalytics;
using Azure.AI.Vision.ImageAnalysis; // Espacio de nombres para el SDK de Azure AI Vision
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using System.Drawing; // Se requiere el paquete System.Drawing.Common

namespace sdk_client
{
    class Program
    {
        // Variables globales para almacenar las configuraciones de los servicios de Azure.
        // Estas se cargarán desde appsettings.json en tiempo de ejecución.
        private static string AISvcEndpoint;
        private static string AISvcKey;
        private static string SpeechKey;
        private static string SpeechRegion;
        private static SpeechConfig speechConfig;

        // Ruta del archivo JSON donde se guardarán los textos registrados.
        private static string jsonFilePath = "textos.json";

        // Función Main: bucle del menú principal.
        static async Task Main(string[] args)
        {
            try
            {
                // Configuración inicial desde el archivo appsettings.json.
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                AISvcEndpoint = configuration["AIServicesEndpoint"];
                AISvcKey = configuration["AIServicesKey"];
                SpeechKey = configuration["SpeechKey"];
                SpeechRegion = configuration["SpeechRegion"];

                // Configuración del servicio de voz de Azure.
                speechConfig = SpeechConfig.FromSubscription(SpeechKey, SpeechRegion);

                string opcion = "";
                // Bucle principal del menú, se continúa hasta que el usuario elija "4" para salir.
                while (opcion != "4")
                {
                    Console.WriteLine("\n--- Menú ---");
                    Console.WriteLine("1. Ingresar texto (escrito o hablado)");
                    Console.WriteLine("2. Consultar textos por idioma");
                    Console.WriteLine("3. Detector de texto en imágenes");
                    Console.WriteLine("4. Salir");
                    Console.Write("Selecciona una opción: ");
                    opcion = Console.ReadLine();

                    switch (opcion)
                    {
                        // Caso 1: Ingresar texto manualmente o mediante el micrófono.
                        case "1":
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
                            // Si se ingresó algún texto, se detecta el idioma y se guarda junto con la información.
                            if (!string.IsNullOrWhiteSpace(texto))
                            {
                                var resultadoIdioma = GetLanguage(texto);
                                Console.WriteLine($"Idioma detectado: {resultadoIdioma.Idioma} ({resultadoIdioma.Confianza * 100:F1}%)");

                                TextoRegistrado nuevoTexto = new TextoRegistrado
                                {
                                    Texto = texto,
                                    Idioma = resultadoIdioma.Idioma,
                                    Confianza = resultadoIdioma.Confianza,
                                    Fecha = DateTime.Now,
                                    PalabraMasRepetida = "" // Puedes implementar el cálculo si lo requieres.
                                };
                                GuardarTexto(nuevoTexto);
                            }
                            break;
                        // Caso 2: Consultar textos guardados por idioma.
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
                        // Caso 3: Procesar imágenes para detectar texto.
                        case "3":
                            await ProcesarImagenes();
                            break;
                        // Caso 4: Salir del programa.
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
                Console.WriteLine("Error en el programa: " + ex.Message);
            }
        }

        // Método para detectar el idioma de un texto dado usando Azure Text Analytics.
        static (string Idioma, double Confianza) GetLanguage(string text)
        {
            AzureKeyCredential credentials = new AzureKeyCredential(AISvcKey);
            Uri endpoint = new Uri(AISvcEndpoint);
            var client = new TextAnalyticsClient(endpoint, credentials);
            DetectedLanguage detectedLanguage = client.DetectLanguage(text);
            return (detectedLanguage.Name, detectedLanguage.ConfidenceScore);
        }

        // Método para guardar un objeto TextoRegistrado en el archivo JSON.
        static void GuardarTexto(TextoRegistrado nuevo)
        {
            List<TextoRegistrado> textos = LeerTextos();
            textos.Add(nuevo);
            string json = JsonSerializer.Serialize(textos, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jsonFilePath, json);
        }

        // Método para leer los textos registrados desde el archivo JSON.
        static List<TextoRegistrado> LeerTextos()
        {
            if (!File.Exists(jsonFilePath))
                return new List<TextoRegistrado>();
            string json = File.ReadAllText(jsonFilePath);
            return JsonSerializer.Deserialize<List<TextoRegistrado>>(json) ?? new List<TextoRegistrado>();
        }

        // Método para transcribir voz a texto usando el micrófono y el servicio de voz de Azure.
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
                Console.WriteLine($"No se reconoció el texto. Razón: {result.Reason}");
                return "";
            }
        }

        // Método para procesar todas las imágenes de una carpeta.
        // Se envuelve en try/catch para capturar posibles errores y permitir volver al menú.
        static async Task ProcesarImagenes()
        {
            try
            {
                Console.Write("Introduce la ruta de la carpeta con las imágenes: ");
                string carpeta = Console.ReadLine();
                if (!Directory.Exists(carpeta))
                {
                    Console.WriteLine("La carpeta no existe.");
                    return;
                }
                // Se buscan archivos JPEG y PNG en la carpeta.
                var archivos = Directory.GetFiles(carpeta, "*.jpg").Concat(Directory.GetFiles(carpeta, "*.png"));
                foreach (var archivo in archivos)
                {
                    Console.WriteLine($"Procesando {archivo}...");
                    await ProcesarImagen(archivo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al procesar las imágenes: " + ex.Message);
            }
        }

        // Método para procesar una imagen individual y extraer el texto.
        // Incluye manejo de excepciones para que, si ocurre un error, se muestre el mensaje y continúe.
        static async Task ProcesarImagen(string path)
        {
            try
            {
                // Se crea el cliente de Azure AI Vision para analizar la imagen.
                ImageAnalysisClient client = new ImageAnalysisClient(
                    new Uri(AISvcEndpoint),
                    new AzureKeyCredential(AISvcKey));

                // Se abre el stream de la imagen y se analiza con la función Analyze,
                // indicando que se requiere el feature VisualFeatures.Read.
                using var imageStream = File.OpenRead(path);
                ImageAnalysisResult result = client.Analyze(BinaryData.FromStream(imageStream), VisualFeatures.Read);

                // Si se detectó texto en la imagen:
                if (result.Read != null)
                {
                    // Se une todo el texto extraído en una sola cadena.
                    string textoDetectado = string.Join(" ", result.Read.Blocks.SelectMany(block => block.Lines.Select(line => line.Text)));
                    var resultadoIdioma = GetLanguage(textoDetectado);
                    Console.WriteLine($"Idioma detectado: {resultadoIdioma.Idioma} ({resultadoIdioma.Confianza * 100:F1}%)");

                    // Se abre la imagen usando System.Drawing para dibujar bounding boxes.
                    System.Drawing.Image imagen = System.Drawing.Image.FromFile(path);
                    Graphics graphics = Graphics.FromImage(imagen);
                    Pen pen = new Pen(Color.Cyan, 3);

                    // Recorrer cada bloque y cada línea detectada.
                    foreach (var block in result.Read.Blocks)
                    {
                        foreach (var line in block.Lines)
                        {
                            Console.WriteLine($"   '{line.Text}'");
                            Console.WriteLine($"   Bounding Polygon: [{string.Join(" ", line.BoundingPolygon)}]");

                            // Se dibuja un polígono alrededor de la línea.
                            Point[] polygonLine = {
                                new Point((int)line.BoundingPolygon[0].X, (int)line.BoundingPolygon[0].Y),
                                new Point((int)line.BoundingPolygon[1].X, (int)line.BoundingPolygon[1].Y),
                                new Point((int)line.BoundingPolygon[2].X, (int)line.BoundingPolygon[2].Y),
                                new Point((int)line.BoundingPolygon[3].X, (int)line.BoundingPolygon[3].Y)
                            };
                            graphics.DrawPolygon(pen, polygonLine);

                            // Se recorre cada palabra de la línea y se dibujan sus bounding boxes.
                            foreach (var word in line.Words)
                            {
                                Console.WriteLine($"     Word: '{word.Text}', Confidence {word.Confidence:F4}, Bounding Polygon: [{string.Join(" ", word.BoundingPolygon)}]");
                                Point[] polygonWord = {
                                    new Point((int)word.BoundingPolygon[0].X, (int)word.BoundingPolygon[0].Y),
                                    new Point((int)word.BoundingPolygon[1].X, (int)word.BoundingPolygon[1].Y),
                                    new Point((int)word.BoundingPolygon[2].X, (int)word.BoundingPolygon[2].Y),
                                    new Point((int)word.BoundingPolygon[3].X, (int)word.BoundingPolygon[3].Y)
                                };
                                graphics.DrawPolygon(pen, polygonWord);
                            }
                        }
                    }

                    // Se guarda la imagen con anotaciones en un nuevo archivo.
                    string output_file = "text.jpg";
                    imagen.Save(output_file);
                    Console.WriteLine($"Resultados guardados en {output_file}\n");

                    // Se crea un objeto TextoRegistrado y se guarda en el JSON.
                    TextoRegistrado nuevoTexto = new TextoRegistrado
                    {
                        Texto = textoDetectado,
                        Idioma = resultadoIdioma.Idioma,
                        Confianza = resultadoIdioma.Confianza,
                        Fecha = DateTime.Now,
                        PalabraMasRepetida = "" // Opcional: se puede calcular la palabra más repetida.
                    };
                    GuardarTexto(nuevoTexto);
                }
                else
                {
                    Console.WriteLine("No se pudo analizar la imagen.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al procesar la imagen {path}: {ex.Message}");
            }
            await Task.CompletedTask;
        }
    }

    // Clase que representa un texto registrado con sus propiedades.
    class TextoRegistrado
    {
        public string Texto { get; set; }
        public string Idioma { get; set; }
        public double Confianza { get; set; }
        public DateTime Fecha { get; set; }
        public string PalabraMasRepetida { get; set; }
    }
}
