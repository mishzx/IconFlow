# IconFlow Native

![Demostración de IconFlow WinUI 3](docs/media/iconflow-demo.gif)

IconFlow es un gestor de iconos para Windows, local por defecto y con cambios reversibles. Reúne en una interfaz nativa y ligera el cambio de iconos de carpetas y accesos directos, la organización de una biblioteca y el historial para deshacer.

El propósito de IconFlow va más allá de cambiar una imagen: los iconos de las carpetas son la primera capa visual para orientarse en una base de conocimiento personal, materiales de investigación y tareas diarias. Al gestionar iconos, categorías, favoritos e historial en el dispositivo, el proyecto ayuda a reducir el ruido visual y encontrar antes la información, manteniendo los nombres de archivos, las rutas y los recursos importados fuera de servicios externos. El proyecto se publica bajo la licencia MIT.

> Versión principal nativa actual: `0.8.1`. El repositorio público contiene únicamente la línea nativa mantenida.

## Idiomas

- [English](README.md)
- [简体中文](README.zh-CN.md)
- [日本語](README.ja.md)
- [Español](README.es.md)

## Por qué IconFlow

- **Interfaz nativa y ligera**: basada en .NET 8 y Windows App SDK / WinUI 3, con controles nativos de Windows y Mica; no necesita iniciar Chromium.
- **Local por defecto**: la importación, la vista previa, la edición, las copias de seguridad, el historial y la búsqueda se procesan en el equipo. No requiere cuenta, conexión de red ni subida de archivos por defecto.
- **Cambios reversibles**: crea una copia de seguridad antes de aplicar un icono, muestra el antes y el después y permite deshacer cada entrada o restaurar el icono predeterminado.
- **Integración con Explorer**: ofrece un menú contextual de primer nivel en Windows 11 y una entrada compatible que no requiere permisos de administrador.
- **Pensado para organizar**: biblioteca con carpetas, búsqueda, favoritos, detección de duplicados y elementos recientes, para un uso continuo y no solo para una modificación puntual.

## Funciones principales

- Cambiar el icono de carpetas normales y accesos directos `.lnk`.
- Importar PNG, JPG/JPEG, WEBP, SVG, BMP e ICO, además de extraer iconos de archivos EXE.
- Generar automáticamente ICO con siete tamaños: 16, 24, 32, 48, 64, 128 y 256.
- Biblioteca con búsqueda, favoritos, detección de duplicados, elementos recientes y carpetas para crear, renombrar y ordenar mediante arrastrar y soltar.
- Paquete integrado de iconos de carpetas Fluent para documentos, datos, código, laboratorio, imágenes, clínica, tareas, completados y archivo.
- Editor nativo con recorte libre o proporcional, escala, desplazamiento, margen transparente, esquinas redondeadas, forma inferior y color.
- Eliminar fondos conectados al borde mediante color y tolerancia, conservando los símbolos blancos internos.
- Ventana rápida del menú contextual: aparece cerca del puntero, evita los bordes de la pantalla, busca en tiempo real y admite arrastrar una imagen para aplicarla.
- Menú contextual de iconos: renombrar, editar, mover, marcar como favorito y eliminar de forma segura.
- Caché de previsualización versionada de 256×256 que prioriza la capa PNG más grande del ICO para evitar imágenes borrosas por cachés antiguos o capas pequeñas ampliadas.
- Historial, deshacer, restauración del valor predeterminado y actualización nativa del Explorador.
- Temas claro, oscuro y del sistema, con ajuste automático para DPI del 100% al 200%.
- Recursos multilingües nativos: chino simplificado y tradicional, inglés, español, francés, alemán, portugués, japonés, coreano, ruso y árabe. El árabe usa diseño RTL.

## Privacidad y datos

IconFlow está diseñado con una premisa sencilla: los archivos deben permanecer en tu equipo.

- Por defecto no se inicia con Windows ni permanece en la bandeja; al cerrar la última ventana, el proceso termina.
- No sube nombres de archivos, rutas, contenido del escritorio ni imágenes importadas. Por defecto no realiza conexiones de red.
- Antes de aplicar un icono crea una copia de seguridad y copia los iconos aplicados a una carpeta estable `managed-icons`; mover el recurso original o la biblioteca no lo rompe inmediatamente.
- Los datos se guardan por defecto en `%LocalAppData%\IconFlow`. No publiques en un issue archivos de diagnóstico que contengan nombres personales, rutas o iconos privados.

## Instalación y uso

### Ejecutar la versión portátil

1. Descarga `IconFlow-0.8.1-win-x64-FINAL.zip` desde GitHub Releases.
2. Extrae el archivo en una carpeta donde tengas permiso de escritura.
3. Ejecuta `IconFlow.exe`.

La versión publicada es un paquete nativo para Windows x64. Antes del primer inicio, comprueba que estén instalados **.NET 8 Desktop Runtime** y **Windows App Runtime 2.4**. En Windows 11 se puede activar el menú contextual de primer nivel.

### Menú contextual

- **Primer nivel de Windows 11**: ejecuta `Install-Win11Menu.ps1` desde la carpeta publicada o elige «Instalar / reparar el nuevo menú de Windows 11» en Ajustes. Solicita UAC una vez y registra un paquete sparse identity firmado localmente.
- **Entrada compatible**: activa «Menú contextual compatible del Explorador» en Ajustes. Solo modifica el registro del usuario actual, no requiere permisos de administrador y aparece en «Mostrar más opciones» de Windows 11.
- **Desinstalar el menú de primer nivel**: ejecuta `Uninstall-Win11Menu.ps1`; elimina el paquete del menú y el certificado de desarrollo correspondiente.

Para una distribución pública, el certificado de desarrollo solo sirve para pruebas y validación. Un producto final debe usar un certificado de firma de código confiable o Microsoft Store.

## Compilar desde el código fuente

Necesitas Windows, .NET 8 SDK, las dependencias de Windows App SDK y PowerShell:

```powershell
dotnet build native\IconFlow.WinUI\IconFlow.WinUI.csproj -c Release
dotnet run --project native\IconFlow.Native.Tests\IconFlow.Native.Tests.csproj -c Release
powershell -ExecutionPolicy Bypass -File scripts\build-native.ps1
```

`native\\IconFlow.Core` contiene la lógica de Windows independiente de la interfaz y `native\\IconFlow.WinUI` contiene la interfaz nativa. El script de compilación genera los iconos incluidos, ejecuta las pruebas de regresión del núcleo, publica los archivos WinUI y prepara la extensión del menú de Windows 11.


## Límites actuales

- Actualmente se dirige a Windows x64. Los iconos del escritorio del sistema, los iconos por tipo de archivo, la barra de tareas, el menú Inicio y las reglas por lotes todavía no forman parte del núcleo disponible.
- La compilación de desarrollo del menú contextual de primer nivel usa un certificado local y Windows puede mostrar una advertencia de confianza. Debe firmarse de nuevo antes de una distribución pública.
- El paquete publicado depende de .NET 8 Desktop Runtime y Windows App Runtime 2.4; las diferencias del entorno pueden afectar a la instalación y a la extensión del menú.
- IconFlow no sube automáticamente registros de fallos. Al informar de un problema, elimina nombres de usuario, organizaciones, rutas personales y recursos de iconos privados.

## Licencia

MIT License. Los runtimes de terceros y los componentes de Windows App SDK se distribuyen bajo sus respectivas licencias.
