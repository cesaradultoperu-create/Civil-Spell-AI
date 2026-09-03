# Fase 4: OpenAI con envío exclusivo de texto

Fecha de actualización: 2026-08-25

**Estado:** implementada; flujo feliz validado en revisión individual y por
lote, con pruebas de fallos reales y `UNDO` remoto pendientes.

## Alcance implementado

- `AISPELL` y `AISPELLALL` pueden usar OpenAI además de las reglas locales.
- El proveedor detecta y corrige textos en español, inglés o una combinación de
  ambos idiomas.
- OpenAI entrega alternativas mediante un esquema JSON estricto.
- Toda propuesta remota vuelve a pasar por el diff y el validador técnico local
  antes de habilitar **Aplicar**.
- La revisión global limita la concurrencia remota a dos solicitudes.

## Límite de privacidad

La única información variable extraída del dibujo e incluida en la solicitud es
`CorrectionRequest.Text`. No se serializan ni envían:

- el DWG o su nombre;
- geometría o coordenadas del objeto;
- capas, estilos o propiedades de Civil 3D;
- `DocumentId`, handle o tipo de entidad;
- el glosario local o personal.

La solicitud usa `store: false`. El consentimiento es versionado, está
desactivado por defecto y se administra mediante `AISPELLSETTINGS`.

## Credencial

La clave no se guarda en `settings.v3.json`, no se muestra en la interfaz y no
se registra en mensajes. Se lee de la variable de entorno de usuario
`OPENAI_API_KEY`.

## Protección local

El validador conserva números, estaciones, códigos, unidades, términos del
glosario y códigos de formato MText. Si una propuesta altera cualquiera de esos
elementos, permanece visible pero **Aplicar** queda bloqueado.

## Pruebas automatizadas

Las pruebas usan un transporte simulado y nunca llaman a Internet. Verifican:

- que el cuerpo contiene el texto y excluye metadatos del dibujo y glosario;
- que `store` está desactivado;
- que se interpreta una respuesta estructurada válida;
- que una respuesta sin `output_text` se rechaza;
- que los códigos de formato MText no pueden modificarse;
- que la configuración conserva consentimiento y modelo.

Resultado actual del runner global: 105 pruebas correctas de 105.

## Resultado manual

- El propietario confirmó el funcionamiento satisfactorio de OpenAI real en
  `AISPELL`.
- El propietario confirmó el funcionamiento satisfactorio de OpenAI real en
  `AISPELLALL`.

La confirmación cubre los flujos felices. No se usa para inferir los escenarios
de error o reversión enumerados a continuación.

## Pendiente manual

- probar el mensaje de clave inválida y una desconexión de red;
- comprobar `UNDO` después de un lote remoto.
- registrar idioma, entidad, versión de modelo y evidencia anonimizada en la
  próxima ejecución formal de la matriz.
