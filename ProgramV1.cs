using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Extensions.Configuration;

namespace Proyecto04_AI_Services
{
  // Representa cada entrada de texto y su información detectada.
  public class TextEntry
  {
    public string Texto { get; set; }
    public string IdiomaDetectado { get; set; }
    public double Probabilidad { get; set; } // Valor entre 0 y 1
    public TextEntry(string texto, string idiomaDetectado, double probabilidad)
    {
      Texto = texto;
      IdiomaDetectado = idiomaDetectado;
      Probabilidad = probabilidad;
    }
  }

  // Clase que se encarga de detectar el idioma utilizando Azure AI Text Analytics.
  public class LanguageDetector
  {
    private readonly TextAnalyticsClient client;

    // Constructor: se requiere el endpoint y la clave del servicio.
    public LanguageDetector(string endpoint, string apiKey)
    {
      AzureKeyCredential credentials = new AzureKeyCredential(apiKey);
      Uri serviceEndpoint = new Uri(endpoint);
      client = new TextAnalyticsClient(serviceEndpoint, credentials);
    }

    // Llama al servicio para detectar el idioma del texto.
    public TextEntry DetectLanguage(string texto)
    {
      // Llamada al servicio de Azure AI para detectar el idioma.
      DetectedLanguage detectedLanguage = client.DetectLanguage(texto);

      // Se retorna el resultado en un objeto TextEntry.
      return new TextEntry(texto, detectedLanguage.Name, detectedLanguage.Score);
    }
  }

  // Clase que almacena localmente las entradas de texto y permite consultas.
  public class TextRepository
  {
    private List<TextEntry> entradas;

    public TextRepository()
    {
      entradas = new List<TextEntry>();
    }

    // Permite agregar una entrada.
    public void Add(TextEntry entry)
    {
      entradas.Add(entry);
    }

    // Consulta la cantidad de textos que tienen un idioma determinado y una probabilidad mínima.
    public int CountByLanguage(string idioma, double minProbabilidad)
    {
      return entradas.Count(te => te.IdiomaDetectado.Equals(idioma, StringComparison.OrdinalIgnoreCase)
                                && te.Probabilidad >= minProbabilidad);
    }
  }

  // Clase principal que gestiona la interacción con el usuario.
  class Program
  {
    private static string AISvcEndpoint;
    private static string AISvcKey;

    static void Main(string[] args)
    {
      try
      {
        // 1. Cargar las configuraciones desde appsettings.json
        IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
        IConfigurationRoot configuration = builder.Build();
        AISvcEndpoint = configuration["AIServicesEndpoint"];
        AISvcKey = configuration["AIServicesKey"];

        // 2. Crear las instancias de LanguageDetector y TextRepository.
        LanguageDetector detector = new LanguageDetector(AISvcEndpoint, AISvcKey);
        TextRepository repository = new TextRepository();

        // 3. Menú interactivo:
        bool exit = false;
        while (!exit)
        {
          Console.WriteLine("\n--- Menú del Prototipo Detector de Idioma ---");
          Console.WriteLine("1. Introducir texto");
          Console.WriteLine("2. Consultar cantidad de textos por idioma y probabilidad mínima");
          Console.WriteLine("3. Salir");
          Console.Write("Seleccione una opción: ");
          string opcion = Console.ReadLine();

          switch (opcion)
          {
            case "1":
              Console.WriteLine("\nIngrese el texto (o 'quit' para regresar al menú):");
              string userText = Console.ReadLine();

              // Si el usuario no desea salir, se procesa el texto.
              if (!string.Equals(userText, "quit", StringComparison.OrdinalIgnoreCase))
              {
                TextEntry entry = detector.DetectLanguage(userText);
                repository.Add(entry);
                Console.WriteLine($"\nResultado de la detección:");
                Console.WriteLine($"Texto: {entry.Texto}");
                Console.WriteLine($"Idioma detectado: {entry.IdiomaDetectado}");
                Console.WriteLine($"Probabilidad (score): {entry.Probabilidad:P0}");
              }
              break;

            case "2":
              // Se consulta la cantidad de textos correspondientes a un idioma y un umbral de probabilidad.
              Console.Write("\nIngrese el código/nombre del idioma (ejemplo: Spanish, English, French): ");
              string idiomaConsulta = Console.ReadLine();
              Console.Write("Ingrese el valor mínimo de probabilidad (0 a 1): ");

              if (double.TryParse(Console.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture, out double minProb))
              {
                int count = repository.CountByLanguage(idiomaConsulta, minProb);
                Console.WriteLine($"\nCantidad de textos en '{idiomaConsulta}' con probabilidad >= {minProb:P0}: {count}");
              }
              else
              {
                Console.WriteLine("Valor de probabilidad no válido.");
              }
              break;

            case "3":
              exit = true;
              break;

            default:
              Console.WriteLine("Opción no válida. Intente nuevamente.");
              break;
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error: {ex.Message}");
      }
    }
  }
}
// Fin del código
