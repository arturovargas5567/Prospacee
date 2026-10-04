# Prospace

Organizador personal de escritorio para Windows, creado con C# y WPF.

## Ejecutar el proyecto desde el código

Requisitos: Windows y .NET 10 SDK. Desde esta carpeta ejecuta `dotnet run`.

## Publicación para Windows x64

El proyecto incluye un perfil Release, autocontenido y de archivo único. Desde la carpeta del proyecto ejecuta:

```powershell
dotnet publish .\Prospace.csproj -p:PublishProfile=WinX64
```

El perfil está en `Properties/PublishProfiles/WinX64.pubxml` y deja el resultado en `..\Prospace_Distribucion`. La publicación de archivo único requiere que NuGet pueda restaurar los runtime packs de Windows x64.

La versión portátil publicada está en [Releases de Prospace](https://github.com/arturovargas5567/Prospacee/releases/latest). Descarga el ZIP, extráelo y abre `Prospace.vbs`; incluye .NET 10 y WPF y no necesita instalar .NET en el otro PC.

## Estructura

- `MainWindow.xaml` y `MainWindow.xaml.cs`: interfaz y comportamiento de las pantallas.
- `Models.cs`: modelos de notas, asignaturas, tareas y eventos.
- `ProspaceDataStore.cs`: almacenamiento JSON local.
- `Assets/Prospace-logo.png` y `Assets/Prospace.ico`: logo original e icono multi-tamaño de Windows.

Los datos se guardan en `%LOCALAPPDATA%\Prospace\data.json`; los archivos importados, en `%LOCALAPPDATA%\Prospace\Files`.

