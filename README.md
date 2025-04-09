# Azure AI Developers - Proyecto 04: AI Services Solutions

## Alcance

Este proyecto requiere la implementación de varios prototipos para integrar aplicaciones de cliente con los servicios de IA de Azure.  Los prototipos se desarrollarán en C#, priorizando la reutilización de código mediante la implementación de clases.

En esta fase inicial, el almacenamiento de datos se realizará localmente.  La integración con almacenamiento en la nube se implementará en etapas posteriores de pre-producción.  Los prototipos se implementarán en releases sucesivas, manteniendo la reutilización de código a través de las clases.

## Releases del Proyecto:

**Release 1: Detector de Idioma Escrito**

* **Funcionalidad:** El usuario introduce textos en diferentes idiomas.  El sistema identifica el idioma de cada texto.  Se permite consultar la cantidad de textos introducidos para un idioma específico, con un porcentaje mínimo de probabilidad.

**Release 2: Detector de Idioma Hablado**

* **Funcionalidad:**  Similar a la Release 1, pero el usuario introduce el texto mediante un micrófono.

**Release 3: Detector de Texto en Imágenes**

* **Funcionalidad:**  Se especifica una carpeta con imágenes que contienen texto. Para cada imagen:
    * Se detecta el texto.
    * Se determina el idioma del texto.
    * Se almacena la palabra más repetida en el texto.
    * Se permite consultar la cantidad de textos introducidos para un idioma específico, con un porcentaje mínimo de probabilidad, así como las palabras más utilizadas en esos textos.

## Requisitos de Instalación

### Requisitos Previos

1. **Visual Studio 2022** o **.NET SDK 6.0 o superior**
   - Descargar desde: [Visual Studio](https://visualstudio.microsoft.com/) o [.NET SDK](https://dotnet.microsoft.com/download)

2. **Cuenta de Azure** con los siguientes servicios configurados:
   - Azure AI Language/Text Analytics
   - Azure AI Speech Services
   - Azure AI Vision

### Paquetes NuGet Necesarios

Para instalar los paquetes necesarios desde la consola:

```
dotnet add package Azure.AI.TextAnalytics --version 5.3.0
dotnet add package Microsoft.CognitiveServices.Speech --version 1.31.0
dotnet add package Azure.AI.Vision.ImageAnalysis --version 0.15.1-beta.1
dotnet add package System.Drawing.Common
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Newtonsoft.Json
```

## Configuración del Proyecto

1. **Configurar los servicios de Azure**:
   - Crear o modificar el archivo `appsettings.json` en la raíz del proyecto:

   ```json
   {
     "LanguageService": {
       "Endpoint": "https://[TU-SERVICIO].cognitiveservices.azure.com/",
       "Key": "[TU-CLAVE]"
     },
     "SpeechService": {
       "Endpoint": "https://[TU-REGIÓN].api.cognitive.microsoft.com/sts/v1.0/issuetoken",
       "Key": "[TU-CLAVE]",
       "Region": "[TU-REGIÓN]"
     },
     "VisionService": {
       "Endpoint": "https://[TU-SERVICIO].cognitiveservices.azure.com/",
       "Key": "[TU-CLAVE]"
     }
   }
   ```

2. **Estructura de carpetas**:
   - Asegurarse de tener una carpeta `img` en la raíz del proyecto con imágenes que contengan texto para probar la Release 3

## Compilación y Ejecución

Para compilar y ejecutar el proyecto desde la consola:

```
dotnet restore
dotnet build
dotnet run
```

Al ejecutar la aplicación, se mostrará un menú con las opciones disponibles para cada release.

## Notas Adicionales

- Para la Release 2, asegúrese de que su micrófono esté configurado y funcionando correctamente
- Las imágenes para la Release 3 deben contener texto visible y legible
- Los resultados del análisis se almacenan en el archivo `textos.json` en la raíz del proyecto