# AudienciasApp

Aplicación WPFUI (C#) para gestionar registros de audiencias judiciales y generar/editar archivos Excel a partir de una plantilla.

Estructura:

- Assets/template/ (poner aquí tu plantilla Excel: plantilla_audiencias.xlsx)
- ArchivosCreados/ (los archivos generados se guardan aquí)

Comandos útiles:

- Build: dotnet build
- Run: dotnet run
- Publicar self-contained (Windows x64):

  dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true

Notas:

- La plantilla debe ser colocada en `Assets/template/plantilla_audiencias.xlsx`.
- El proyecto usa EPPlus, WPF-UI y CommunityToolkit.Mvvm.
