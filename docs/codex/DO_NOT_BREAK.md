# Do Not Break

- `AISPELL`, `AISPELLALL` y `AISPELLSETTINGS` deben seguir funcionando en Civil
  3D 2024, .NET Framework 4.8 y x64.
- `SpellEngine` y `Domain` deben permanecer independientes de Autodesk, WPF,
  disco, red y proveedores concretos.
- No mantener transacciones de AutoCAD abiertas mientras una ventana espera al
  usuario o una petición remota está activa.
- Ninguna propuesta se aplica sin confirmación humana explícita.
- Toda propuesta debe pasar por diff y validación local; no confiar en cambios o
  explicaciones declarados por un proveedor.
- Revalidar documento, objeto, tipo y texto original antes de escribir. Un
  conflicto individual o de lote no puede producir una escritura parcial.
- Cancelar, cerrar o fallar debe conservar el dibujo y terminar solicitudes
  pendientes sin escrituras posteriores.
- OpenAI solo puede recibir el contenido del texto autorizado. No enviar DWG,
  geometría, metadatos, glosarios, handles, rutas ni secretos.
- No registrar texto del plano, prompts, respuestas ni credenciales por defecto.
- Las reglas locales deben continuar disponibles si la IA está deshabilitada o
  falla.
- `AISPELLTESTCONFLICT`, `AISPELLTESTBATCHCONFLICT` y
  `AISPELLTESTDOCUMENTSWITCH` son diagnósticos de regresión y deben permanecer
  ausentes de compilaciones Release.
