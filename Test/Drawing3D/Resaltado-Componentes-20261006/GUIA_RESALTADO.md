# Identificar los componentes en la vista 3D

Versión instalada 1.3.6.0, verificada 49/49 archivos después del aviso de cierre.
Para probarla ahora, comenzar por el paso 3. Los pasos 1 y 2 sirven para
volver a actualizar. El respaldo completo de 1.3.5.0 queda conservado.

1. Guardar los dibujos y cerrar Civil 3D.
2. Ejecutar ACTUALIZAR_DRAWING3D.cmd. Actualiza el paquete completo y conserva
   el respaldo de la versión anterior.
3. Abrir Civil 3D y ejecutar EDITARFIGURA3D sobre un modelo del comando.
4. Pulsar «Cota inferior» de «Mampostería: laterales»: se iluminan en verde las
   paredes del canal y aparece el nombre del campo encima de la vista.
5. Seleccionar otro campo o pulsar el nombre de otro componente cambia el
   resaltado. También funciona con Tab y con piezas sin campos editables.
6. Cambiar una cota y salir del campo actualiza la vista conservando el resaltado.
7. «Quitar resaltado» devuelve los colores originales de la vista previa.
8. Aplicar confirma los parámetros; Cancelar sale sin modificar el dibujo.

El mismo resaltado funciona en la ventana de CREARFIGURA3D. Las imágenes
LATERALES_RESALTADOS_A_B.png y LATERALES_RESALTADOS_C_D.png muestran el resultado
comprobado en AutoCAD Core Console. Ejemplos-A-B-y-C-D.dwg conserva la copia
etiquetada y sus modelos, en X=40 y X=46, del paquete anterior.

El resaltado es una ayuda de la vista previa. Los colores guardados en el DWG
siguen correspondiendo a sus materiales. Se ilumina el componente directo del
campo; el paso del tubo en la caja continúa actualizándose con la geometría.
