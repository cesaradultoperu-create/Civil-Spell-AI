# Architecture Rules

- SpellEngine no depende de AutoCAD.
- Domain no depende de AutoCAD, WPF, disco, red ni proveedores concretos.
- Commands solo orquesta; Autodesk concentra objetos, documentos y transacciones.
- UI no abre transacciones ni escribe entidades.
- Servicios desacoplados mediante interfaces.
- Evitar dependencias circulares.
- Cerrar lectura antes de mostrar UI o llamar a red y abrir escritura únicamente
  después de una decisión explícita.
- Reutilizar coordinadores, diff y validadores para flujo individual y por lote;
  no crear rutas paralelas con garantías diferentes.
