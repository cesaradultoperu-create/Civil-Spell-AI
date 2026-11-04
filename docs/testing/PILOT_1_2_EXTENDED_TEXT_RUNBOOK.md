# Regresión del piloto CivilSpellAI 1.3.7.0

Fecha del candidato: 2026-09-11.

## Objetivo

Confirmar en Civil 3D 2024 x64 que `AISPELLALL` conserva el comportamiento de
1.1.5.0 y amplía la revisión a `MLeader`, celdas simples de `Table` y textos o
atributos de bloques locales sin perder aprobación humana, atomicidad ni `UNDO`.

## Evidencia automatizada

Ejecutar con Civil 3D cerrado, desde el repositorio:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Validate-Mvp.ps1 -Configuration Release
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-BatchUndoIntegration.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-ExtendedTextIntegration.ps1
```

Resultados requeridos:

- 138 pruebas correctas y cero fallos;
- prueba C# real de LanguageTool local correcta;
- `DICT-01` carga `es_PE` dentro de AutoCAD y restaura una tilde sin conexión;
- las pruebas de núcleo cargan `en_US` y corrigen inglés sin conexión;
- `INTEGRACION UNDO AISPELLALL CORRECTA`;
- `INTEGRACION DE TEXTOS EXTENDIDOS CORRECTA`.

## Primera configuración por usuario

Esta prueba usa otra computadora o un perfil de Windows desechable sin
configuración previa de CivilSpellAI. Nunca se borra ni se mueve la
configuración real para simular el primer inicio.

1. Ejecutar `AISPELLALL` y confirmar que aparece una sola vez la explicación de
   primera configuración antes de escanear el dibujo.
2. Elegir **No** y comprobar que continúa con el diccionario local, sin pedir
   clave ni conectarse a OpenAI.
3. Ejecutar `AISPELLALL` otra vez y confirmar que la explicación no se repite.
4. Abrir `AISPELLSETTINGS`: el estado debe indicar si `OPENAI_API_KEY` está
   disponible, pero el campo enmascarado debe aparecer siempre vacío.
5. Con una clave desechable y limitada, pegarla en el campo, pulsar **Guardar
   clave** y comprobar que el estado cambia a «detectada» sin mostrar el valor.
6. Autorizar el texto fijo y pulsar **Probar conexión**. Confirmar que el fallo
   de red o autenticación es seguro, o que la conexión correcta no guarda la
   respuesta.
7. Pulsar **Quitar clave**, confirmar que el estado cambia a «no detectada» y
   verificar que la revisión local continúa disponible.

No pegar una clave personal en capturas, registros, incidencias ni dibujos.

Desde la carpeta extraída del ZIP, generar el fixture empaquetado con:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\New-PilotFixture.ps1 -Force
```

`EXTENDED-01` crea destinos sintéticos en una copia temporal: un `MLeader`, dos
celdas ordinarias de la misma tabla, un `DBText` dentro de una definición de
bloque y un `AttributeReference`. Aplica los cinco cambios en un lote, ejecuta
una sola `U` y exige la restauración exacta. El fixture base no se modifica.

## Prueba visual en una copia del DWG

1. Cerrar Civil 3D, instalar o actualizar el bundle 1.3.7.0 y volver a abrirlo.
2. Abrir exclusivamente una copia de prueba del dibujo y ejecutar
   `AISPELLSETTINGS` para comprobar la versión cargada.
3. Ejecutar `AISPELLALL` y esperar a que termine el análisis.
4. Confirmar que las filas identifican los tipos `MLeader`, `TableCell`,
   `DBText`, `MText` o `AttributeReference` esperados.
5. Revisar cada propuesta. Desmarcar cualquier cambio dudoso y pulsar
   **Aplicar seleccionados**.
6. Sin hacer zoom, desplazarse, seleccionar ni ejecutar otro comando, usar una
   sola `U` inmediatamente y comprobar que todo el lote vuelve al estado
   anterior. Si la línea de comandos muestra `INTELLIPAN`, todavía se está
   deshaciendo un movimiento de vista y la prueba debe repetirse desde cero.
7. Repetir y aplicar solo las correcciones aprobadas; guardar únicamente la
   copia de prueba.
8. Reinstalar 1.3.7.0 para continuar la matriz con el candidato autorizado. Si
   la prueba falla, ejecutar antes un rollback de diagnóstico para restaurar y
   comprobar 1.2.10.0.

## Resultado del candidato 1.3.1.0

Revalidación interactiva adicional PASS el 2026-09-14. En la copia desechable
`22 PRUEBA_CivilSpellIAI_antes.dwg`, con OpenAI y LanguageTool desactivados,
`AISPELLALL` revisó un MText y mostró una propuesta de reglas locales con 16
cambios, cero fallos y validación técnica aprobada. Se inspeccionaron y aplicaron
las 16 correcciones; una sola `U` ejecutada inmediatamente restauró los errores
originales. Las validaciones Debug y Release del mismo día pasaron 138/138
pruebas sin advertencias; Release reprodujo la huella de DLL esperada. La integración C# real de
LanguageTool también pasó en Release; el servicio temporal se detuvo al
terminar.

PASS Debug y Release el 2026-09-11. Los dos MText completos del piloto quedan
como regresiones con 20 correcciones españolas y 7 inglesas exactas. Se
conservan `LLDPE`, `HDPE`, `SST`, `PD`, números y unidades. La DLL Release tiene
SHA-256 `61FF7C02C2977BE21BFCA54E4D9F17979D81987016371C674AB052900D0C38B5`.
El empaquetado y el verificador independiente pasan y la huella vigente se
entrega únicamente en el archivo `.zip.sha256` para evitar una referencia
circular dentro del ZIP. La instalación coincide 17/17 con el bundle y deja
1.2.10.0 como único rollback estable. `UNDO-04`, `DICT-01` y `EXTENDED-01`
pasan. La revisión visual mostró 20 cambios españoles y 7 ingleses con cero
fallos; los siete ingleses fueron inspeccionados y aprobados. La inspección
visual de las últimas entradas españolas queda pospuesta para no interrumpir el
trabajo del propietario; la regresión automática completa permanece en PASS.

## Resultado del candidato 1.2.12.0

PASS automatizado inicial el 2026-09-11. El paquete incorpora `es_PE` y
`en_US`; `AISPELL` y `AISPELLALL` detectan español, inglés y texto mixto antes
de consultar proveedores, y LanguageTool recibe el código de idioma correcto o
`auto`. Las 135 pruebas pasan en Debug y Release y el verificador independiente
acepta el ZIP. La huella vigente se entrega en el archivo `.zip.sha256` para no
introducir una referencia circular dentro del propio ZIP. La instalación y las
regresiones de Core Console pasaron. La revisión visual bilingüe se canceló sin
escrituras al revelar los falsos positivos corregidos por 1.2.13.0.

## Resultado del candidato 1.2.11.0

PASS automatizado el 2026-09-10. Las reglas que coinciden con un texto ya
correcto, como `FINALIDAD → FINALIDAD`, dejan de registrar cambios inexistentes;
por tanto no generan filas bloqueadas sin diferencias. Las 131 pruebas pasan en
Debug y Release. Pendiente: instalación y comprobación visual en `C1.dwg`.

## Resultado del candidato 1.2.10.0

PASS el 2026-09-10. Incorpora los dos errores omitidos en la prueba
controlada de doce casos: `PROTEJE → PROTEGE` y `DEVE → DEBE`, tanto en
mayúsculas como en minúsculas. Las 131 pruebas pasan en Debug y Release sin
advertencias. La instalación contiene 13/13 archivos idénticos al bundle. La
prueba visual local mostró 12/12 correcciones intencionales, cero fallos,
validación técnica aprobada y conservó `LLDPE`, `SST`, `GCL` y `CQA`. El lote se
aplicó correctamente y una sola `U` restauró el texto de prueba.

## Resultado del candidato 1.2.9.0

PASS automatizado el 2026-09-10. El diccionario local trata con el mismo
criterio las erratas seguras en minúsculas, con inicial mayúscula y en
mayúsculas completas, y conserva la capitalización original de la propuesta.
También protege nombres precedidos por inicial, términos técnicos ya corregidos,
códigos, glosario y resultados ambiguos. La primera comprobación visual confirmó
las correcciones en mayúsculas, pero detectó el falso positivo
`LLDPE → LLAPE`; se canceló sin escribir. La fuente corregida conserva `LLDPE`,
protege esa sigla también en el validador y corrige `EXESO → EXCESO`. Debug y
Release pasan 131/131 pruebas sin advertencias. Pendiente: reinstalar y repetir
la comprobación visual en Civil 3D.

## Resultado del candidato 1.2.8.0

PASS el 2026-09-10. El candidato quedó instalado con 13/13 archivos idénticos
al bundle y 1.2.7.0 disponible como rollback. La fuente usa una frase sintética
fija, permite cancelar y clasifica los fallos sin exponer detalles internos;
131/131 pruebas pasan en Debug y Release. La integración C# real atraviesa el
nuevo servicio. En Civil 3D, `AISPELLSETTINGS` mostró **Probar servicio local**,
el estado cambió a **Servicio local disponible** y el mensaje confirmó que
LanguageTool respondió usando únicamente la frase fija y sin guardar la
respuesta. La ventana conservó desplazamiento y la nueva sección quedó visible
sin recortes. No se escribió el fixture.

## Resultado del candidato 1.2.7.0

PASS el 2026-09-10. El candidato quedó instalado con 13/13 archivos idénticos
al bundle y 1.2.6.0 disponible como rollback. La fuente separa
`LanguageTool local` de `Reglas locales`, conserva la prioridad de las reglas
integradas y somete LanguageTool a la protección estricta del glosario. Debug,
Release, la integración C# real, `DICT-01`, `UNDO-04` y `EXTENDED-01` pasan.
En Civil 3D, `AISPELLALL` revisó cuatro textos, mostró propuestas en las cuatro
filas y reportó cero fallos. Tres filas conservaron `Reglas locales`; la cuarta
mostró `Alternativa 2 · LanguageTool local` y el mismo origen bajo el tipo de
entidad. La propuesta quedó técnicamente aprobada. Se canceló sin escribir el
fixture; aplicación y `UNDO` permanecen cubiertos por Core Console.

## Resultado del candidato 1.2.6.0

PASS parcial controlado el 2026-09-10. El candidato quedó instalado con 13/13
archivos idénticos al bundle y 1.2.5.0 disponible como rollback. Debug y Release
pasan 128/128 pruebas; la integración C# real con LanguageTool 6.6, `DICT-01`,
`UNDO-04` y `EXTENDED-01` están en PASS. En Civil 3D, `AISPELLSETTINGS` guardó
LanguageTool habilitado con timeout de 10 segundos; el servidor recibió cuatro
solicitudes y `AISPELLALL` mostró 4/4 filas con propuestas, cero fallos y
validación técnica aprobada. La ventana se canceló sin aplicar para que el
usuario pudiera abrir su trabajo. Por tanto, la aplicación y la `U` no se
repitieron interactivamente en esta sesión, aunque ambas quedaron verificadas
en AutoCAD Core Console sobre copias aisladas.

## Resultado del candidato 1.2.5.0

PASS el 2026-09-09 en Civil 3D 2024 sobre un dibujo desechable. El complemento
cargó sin `NETLOAD`; `AISPELLALL` revisó un MText y mostró una propuesta de
`Reglas locales`, dos cambios de concordancia, cero fallos y validación técnica
aprobada. Después de aplicar la fila seleccionada, una sola `U` inmediata
restauró exactamente el texto original. Las regresiones automatizadas
`DICT-01`, `UNDO-04` y `EXTENDED-01` también permanecen en PASS.

## Límites intencionales

- Los textos fijos de un bloque se corrigen una vez en su definición y el
  resultado aparece en todas las inserciones del bloque.
- Los atributos editables se revisan por cada `BlockReference`.
- Se omiten Xrefs y overlays, etiquetas nativas de Civil 3D, celdas combinadas
  que no sean su celda superior izquierda y celdas con fórmulas, campos,
  vínculos de datos, valores no textuales o contenido de bloque.
- El modo local solo corrige errores cubiertos por reglas seguras. OpenAI puede
  proponer más correcciones cuando está habilitado con consentimiento, pero
  ninguna corrección se aplica automáticamente ni se garantiza detectar todos
  los errores del idioma.

Un cambio no confirmado, una alteración de números/unidades/códigos, una
escritura parcial o un `UNDO` incompleto detiene la prueba y debe registrarse
como incidencia antes de usar el candidato en un DWG de trabajo.
