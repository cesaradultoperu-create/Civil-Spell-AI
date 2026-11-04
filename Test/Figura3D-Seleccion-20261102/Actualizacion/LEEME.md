# CREARFIGURA3D: corrección de la selección TOP actual

Se corrigió el rechazo del detalle de 47 objetos TOP y 42 FRONT. Las líneas
interiores de la caja ya no ocultan los bordes originales de la tubería al
unir segmentos alineados. Se comprueban el diámetro y la correspondencia
única entre ambas vistas.

1. Guarde sus dibujos y cierre Civil 3D.
2. Ejecute `ACTUALIZAR.cmd`. Se conserva un respaldo y se verifica la copia.
3. Abra Civil 3D y ejecute `CREARFIGURA3D`, modo **Referencias**.
4. Seleccione TOP completo y el FRONT del mismo detalle, con contornos,
   tubería y hatches.
5. En la ventana, baje hasta **Tubería → Inclinación (°)**. Ingrese `-19`
   para bajar 19° del extremo inicial al final. Elija la cota que se conserva
   y pulse Tab para actualizar la vista previa.

También disponible en `EDITARFIGURA3D`. La ventana y el campo de inclinación
se comprobaron en la sesión real con la DLL candidata de esta entrega.
La prueba se canceló sin insertar geometría. Pasaron además las pruebas
nativas sobre copias, incluida la selección anterior de 54/42 y los detalles
de regresión.
