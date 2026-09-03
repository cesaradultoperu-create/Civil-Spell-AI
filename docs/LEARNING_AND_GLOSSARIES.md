# Memoria local y glosarios

## Memoria explícita

CivilSpellAI solo recuerda una corrección cuando el usuario marca **Recordar
esta decisión localmente** y la escritura termina correctamente. El archivo es
`%LOCALAPPDATA%\CivilSpellAI\learning.v1.json`, usa el esquema 1 y conserva hasta
500 decisiones por usuario.

Una coincidencia reaparece como alternativa identificada como **Memoria local**.
No queda seleccionada ni aplicada fuera del flujo normal de revisión: siempre
pasa por diff, validación técnica y confirmación humana. Desde
`AISPELLSETTINGS` se puede buscar, desactivar, exportar o borrar cada recuerdo,
o vaciar la memoria completa. La exportación contiene los textos recordados y
debe manejarse como información potencialmente sensible.

La configuración, la memoria y las exportaciones no se incluyen en el paquete
ni se eliminan al desinstalarlo.

## Glosarios

Los términos protegidos se unen en este orden conceptual:

1. glosario integrado de CivilSpellAI;
2. glosario organizacional de solo lectura;
3. glosario personal del usuario.

Duplicados se eliminan sin distinguir mayúsculas. El glosario organizacional es
un archivo UTF-8 con un término por línea en
`%PROGRAMDATA%\CivilSpellAI\organizational-glossary.txt`. Líneas vacías se
ignoran. CivilSpellAI solo lo lee; su distribución y permisos pertenecen al
administrador. Los glosarios se usan en la validación local y no se envían a
OpenAI.

