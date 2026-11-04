# CREARFIGURA3D: reconocimiento del bloque inclinado

Esta actualización corrige el error «TOP contiene un recorrido de tubería sin
una correspondencia única en FRONT» del detalle actual.

El comando reconoce los bordes inclinados del tubo aunque sus extremos tengan
distintas coordenadas X y el eje de delineación no coincida exactamente con el
centro. Comprueba el diámetro con TOP y conserva la selección de componentes.
Las franjas del canal no se interpretan como una segunda tubería.

1. Guarde sus dibujos y cierre Civil 3D.
2. Ejecute `ACTUALIZAR.cmd` y acepte la solicitud de administrador de Windows.
3. Abra Civil 3D y ejecute `CREARFIGURA3D`, modo **Referencias**.
4. Seleccione la planta TOP completa y su sección FRONT completa, incluidos
   la tubería, los contornos y los hatches.
5. En la ventana, baje a **Tubería → Inclinación (°)**. Elija el extremo que
   conserva su cota e ingrese el ángulo. Pulse Tab para revisar la vista previa.
6. Pulse **Crear 3D** y coloque el modelo.

`-19` baja 19° desde el extremo inicial al final. El ángulo inicial de la
ventana procede de los bordes del dibujo; puede cambiarlo directamente.
`EDITARFIGURA3D` ofrece el mismo control para los modelos ya generados.

El actualizador conserva un respaldo y verifica por SHA-256 la DLL instalada.
No modifica sus DWG ni se ejecuta mientras Civil 3D está abierto.
