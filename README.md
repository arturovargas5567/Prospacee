# Prospace

Organizador personal de escritorio para Windows, creado con C# y WPF.

## Ejecutar desde el código

Necesitas Windows y el .NET 10 SDK. Abre esta carpeta en Visual Studio o ejecuta:

```powershell
dotnet run
```

## Publicar para Windows x64

El proyecto incluye un perfil Release autocontenido y de archivo único. Desde esta carpeta:

```powershell
dotnet publish .\Prospace.csproj -p:PublishProfile=WinX64
```

El perfil `Properties/PublishProfiles/WinX64.pubxml` crea el resultado en `..\Prospace_Distribucion`. La publicación de archivo único necesita acceso a NuGet para descargar los runtime packs de Windows x64.

## Versión portátil

La versión portátil incluye .NET 10 y WPF, por lo que no requiere instalar .NET ni Visual Studio. Está disponible en la carpeta local `outputs/Prospace_Windows_x64_Portable_Release_v2.zip` de la entrega original. Extrae el ZIP y abre `Prospace.vbs`.

## Estructura

- `MainWindow.xaml` y `MainWindow.xaml.cs`: interfaz y comportamiento.
- `Models.cs`: modelos de notas, asignaturas, tareas y eventos.
- `ProspaceDataStore.cs`: almacenamiento JSON local.
- `Assets/`: logo e icono de Windows.

Los datos se guardan en `%LOCALAPPDATA%\Prospace\data.json`; los archivos importados, en `%LOCALAPPDATA%\Prospace\Files`.
