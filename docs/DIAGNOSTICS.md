# Diagnóstico local seguro

Fecha de definición: 2026-08-27.

## Alcance y privacidad

El diagnóstico está desactivado por defecto. El usuario puede activarlo desde
`AISPELLSETTINGS`. Los eventos se guardan bajo
`%LOCALAPPDATA%\CivilSpellAI\diagnostics` y nunca se envían automáticamente.

Cada línea JSON admite exclusivamente:

- fecha UTC;
- versión del complemento;
- comando (`AISPELL`, `AISPELLALL` o `AISPELLSETTINGS`);
- código estable;
- severidad;
- duración en milisegundos;
- cantidad agregada de elementos.

El modelo no contiene campos libres para texto, mensajes de excepción, prompts,
respuestas, credenciales, DWG, rutas, capas, geometría, handles ni metadatos.
Una falla del propio registro se ignora y nunca interrumpe el comando o abre una
escritura sobre el dibujo.

## Códigos estables

| Código | Clasificación |
| --- | --- |
| `CMD-000` | Comando completado o sin cambios. |
| `SEL-001` | Selección cancelada. |
| `SEL-002` | Selección o conjunto compatible vacío. |
| `VAL-001` | Propuesta bloqueada por validación local. |
| `CAN-001` | Revisión cancelada o cerrada. |
| `CON-001` | Texto obsoleto o conflicto antes de escribir. |
| `DOC-001` | Documento cerrado o distinto del capturado. |
| `CFG-001` | Configuración o credencial requerida ausente. |
| `CFG-002` | Fallo de lectura/escritura de configuración. |
| `NET-001` | Red o proveedor no disponible. |
| `AUT-001` | Credencial rechazada. |
| `TMO-001` | Tiempo de espera agotado. |
| `RSP-001` | Respuesta remota inválida o incompatible. |
| `WRT-001` | Objeto de escritura inexistente o de tipo distinto. |
| `WRT-002` | Fallo general de escritura. |
| `GEN-001` | Fallo no clasificado. |

Los nombres y significados no deben reutilizarse. Un caso nuevo obtiene un
código nuevo; no se cambia la interpretación de un código ya publicado.

## Severidad y triage

- `Information`: operación normal, cancelación explícita o ausencia de cambios.
- `Warning`: degradación recuperable, conflicto, documento distinto, red,
  timeout, autenticación o respuesta inválida sin escritura.
- `Error`: configuración irrecuperable en esa ejecución, escritura inesperada o
  fallo sin clasificar.

Para soporte, registrar versión, comando, código, severidad, duración y conteo.
No solicitar el DWG ni copiar el texto afectado. `CON-001`, `DOC-001` y
`WRT-001` requieren confirmar que no hubo escritura. `AUT-001`, `NET-001`,
`TMO-001` y `RSP-001` requieren confirmar que las reglas locales siguieron
disponibles.

## Conservación, exportación y borrado

El archivo activo `events.jsonl` rota al alcanzar aproximadamente 2 MiB. Se
conserva una sola generación anterior (`events.previous.jsonl`), por lo que el
máximo normal es cercano a 4 MiB. No existe telemetría remota.

Desde `AISPELLSETTINGS`, el usuario puede:

1. activar o desactivar nuevos eventos;
2. exportar ambas generaciones a un único `.jsonl` revisable;
3. borrar permanentemente los eventos locales tras una confirmación explícita.

La copia exportada queda bajo control del usuario y no se borra al limpiar el
registro interno. Debe revisarse antes de compartirla. La sesión de
`AISPELLSETTINGS` que ejecuta el borrado no genera un evento nuevo al cerrarse,
por lo que la decisión del usuario deja realmente vacío el registro interno.
