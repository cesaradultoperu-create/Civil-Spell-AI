# Grupo de planos de la etapa final en Sheets

El usuario señaló que los planos .16-300- aparecían mezclados con GENERAL y
DISEÑO CIVIL. Solicitó un grupo separado visible al abrir Sheets.

Se implementó e instaló v35 para 2023/2024 y 2025. En `SheetSetSync` se usa
un grupo raíz `ETAPA FINAL - 16-300` para altas nuevas y para las hojas ya
registradas. Se trasladan los mismos componentes AcSm: no se recrean hojas,
se conservan GUID, rutas, presentaciones, handles y campos de rótulo.
SOTRAMIEXCEL mantiene esa separación. SOTRAMIETAPAS hace solo el traslado
de las hojas existentes; no guarda ni edita dibujos.

Prueba nativa AutoCAD/Civil 2024 aprobada en Ensayo4: 52 hojas, tres finales
ordenadas, un único grupo final, repetición estable, identidades y referencias
conservadas, SHA-256 de todos los DWG sin cambios y persistencia al reabrir DST.
Se compilaron ambas familias sin advertencias. Prueba pura de lectura aprobada.
Evidencia: `Test/Sotrami-Sheets-v35-20261030/Ensayo4/subset-test.json`.

Aplicado al proyecto real Q: con SOTRAMIETAPAS. Antes se respaldó el DST:
`Test/Sotrami-Sheets-v35-20261030/Aplicado/SOTRAMI-antes-grupo-final.dst`.
Verificado: GENERAL 3, GEOLOGÍA 8, GEOTECNIA 2, HIDRÁULICA 12,
DISEÑO CIVIL 24 y ETAPA FINAL - 16-300 con los tres planos P-001/002/003.
Total 52; cero diferencias, archivos faltantes, presentaciones pendientes
y planos finales mal ubicados. Los GUID finales de producción se conservaron:

- P-001: gA506EAAC-0C78-47AF-AC82-7085CD682A3F
- P-002: g3193084B-7A7C-44EF-BD3E-82B710BA13DA
- P-003: gC1517075-FA9E-414F-87BD-8F884866BC52

v35 instalada y cargada en la sesión actual. Respaldo del bundle:
`SOTRAMI_Respaldos/Antes_v35_20261030_134515`.
Paquete local: `Test/Sotrami-Sheets-v35-20261030/Paquete-v35`.
No se cambió ningún ajuste de seguridad de Civil 3D ni se guardaron DWG.

## Continuación mientras el usuario trabaja

El usuario pidió aviso antes de cualquier acción sobre Civil 3D y después
indicó continuar con la mejora. Se respetó su sesión y se continuó solo con
fuentes, paquete y pruebas en procesos/perfiles y carpetas aislados.

Prueba adicional aprobada: creación de dos marcos finales ausentes, registro
directo de los tres planos en ETAPA FINAL, tabla/conjunto con 52 entradas y
repetición sin duplicados. Evidencia:
`Test/Sotrami-16-300-20261029/Ensayo-v35-NewSheets/native-test.json`.

Instalador probado en modo ValidationRoot, instalación nueva y reinstalación:
conservó configuración personalizada, registros y otro plugin de prueba;
los dos DLL coinciden por SHA-256. Evidencia:
`Test/Sotrami-Sheets-v35-20261030/installer-test.json`.

La comprobación visual mostró el grupo final guardado pero contraído. No se
insistió en desplegarlo al detectarse que el usuario estaba trabajando. No es
necesario tocar su sesión para completar la entrega del paquete.

Entrega final archivada y verificada mediante SHA-256 (40 archivos), sin
reemplazar versiones anteriores:
`G:/01. PERSONALES/Cesar/Programas con el Visual Studio para el CIVIL3D/08_SOTRAMI_Lista_de_planos_con_Excel/v35_20261030_EtapaFinal`.
Incluye `SOTRAMI-v35.zip`, su suma SHA-256, instalador y fuentes.
ZIP íntegro: 37 archivos de manifiesto comprobados. Evidencia local del
archivo: `Test/Sotrami-Sheets-v35-20261030/archive-copy.json`.
Después de pedir el usuario que se le avise, no se realizaron más acciones
sobre su sesión de Civil 3D. La v35 instalada previamente sigue vigente.
