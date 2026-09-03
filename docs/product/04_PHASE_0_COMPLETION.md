# Fase 0 — Base reproducible: resultado

Fecha: 2026-08-11

## Entregado

- `.gitignore` para artefactos de compilación, archivos de IDE, diagnósticos y
  configuraciones locales que podrían contener secretos.
- Exclusión de `bin/` y `obj/` del control de versiones, manteniendo los archivos
  locales para no alterar instalaciones ni builds existentes.
- Proyecto de pruebas autocontenido para .NET Framework 4.8 que compila el núcleo
  `Spell/` sin AutoCAD ni paquetes NuGet.
- Runner reproducible `scripts/Run-SpellCoreTests.ps1` con nueve pruebas de
  corrección, idioma, glosario y ausencia de cambios inseguros.
- Dataset JSON anonimizado de anotaciones y tokens técnicos protegidos para los
  validadores de las fases posteriores.
- Guía de desarrollo, carga `NETLOAD`, pruebas y diagnóstico de DLL bloqueada.

## Límites intencionales

Esta fase no modificó `AISPELL`, no añadió la GUI ni conectó ningún proveedor de
IA. El proyecto de pruebas cubre el núcleo actual; las pruebas de AutoCAD y la
interfaz llegan después de introducir sus adaptadores.

Después de cerrar esta fase se endureció `AISPELL` con confirmación explícita y
revalidación antes de escribir, sin adelantar la GUI ni la conexión de IA.

## Próximo paso

Iniciar la Fase 1: crear contratos de dominio, adaptar `SpellEngine` como
proveedor local y escribir el validador de tokens técnicos antes de construir la
ventana WPF.
