# Requisitos para AIServicesSolutions

## Paquetes NuGet requeridos

```
Azure.AI.TextAnalytics (5.3.0)
Microsoft.CognitiveServices.Speech (1.31.0)
Azure.AI.Vision.ImageAnalysis (0.15.1-beta.1)
System.Drawing.Common
Microsoft.Extensions.Configuration
Microsoft.Extensions.Configuration.Json
Newtonsoft.Json
```

## Comando para instalar todas las dependencias

```
dotnet restore
```

Para instalar los paquetes individualmente:

```
dotnet add package Azure.AI.TextAnalytics --version 5.3.0
dotnet add package Microsoft.CognitiveServices.Speech --version 1.31.0
dotnet add package Azure.AI.Vision.ImageAnalysis --version 0.15.1-beta.1
dotnet add package System.Drawing.Common
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Newtonsoft.Json
```

## Requisitos de Azure

Es necesario tener configurados los siguientes servicios en Azure:
- Azure AI Language/Text Analytics
- Azure AI Speech Services
- Azure AI Vision

Consulte el archivo README.md para obtener instrucciones detalladas sobre la configuración del proyecto.
