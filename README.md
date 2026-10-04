# Prospace

Organizador personal de escritorio para Windows, creado con C# y WPF.

## Ejecutar desde el código

Requisitos: Windows y .NET 10 SDK. Abre esta carpeta en Visual Studio o ejecuta:

```powershell
dotnet run
```

## Publicar para Windows x64

El perfil Release autocontenido y de archivo único está en `Properties/PublishProfiles/WinX64.pubxml`. Desde esta carpeta ejecuta:

```powershell
dotnet publish .\Prospace.csproj -p:PublishProfile=WinX64
```

La publicación necesita acceso a NuGet para restaurar los runtime packs de Windows x64. Este repositorio contiene el código fuente; no contiene el paquete binario portable.

Los datos de usuario se guardan en `%LOCALAPPDATA%\Prospace\data.json`; los archivos importados, en `%LOCALAPPDATA%\Prospace\Files`.

## Estructura

- `MainWindow.xaml` y `MainWindow.xaml.cs`: interfaz y comportamiento.
- `Models.cs`: modelos de notas, asignaturas, tareas y eventos.
- `ProspaceDataStore.cs`: almacenamiento JSON local.
- `Assets/`: logo e icono de Windows.
