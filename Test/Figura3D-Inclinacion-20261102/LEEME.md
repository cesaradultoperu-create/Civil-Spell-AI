# Inclinación en CREARFIGURA3D — Civil 3D 2024

1. Guarde sus dibujos y cierre Civil 3D.
2. Ejecute `ACTUALIZAR.cmd`. Acepte la solicitud de administrador de Windows.
   El actualizador conserva un respaldo y verifica la DLL instalada.
3. Abra Civil 3D y ejecute `CREARFIGURA3D`, modo **Referencias**, con TOP y FRONT.
4. En la ventana, baje hasta **Tubería**. Elija **Mantener cota inicial** o
   **Mantener cota final** y escriba **Inclinación (°)**. Pulse Tab para revisar.
5. Pulse **Crear 3D** y coloque el modelo. Para modificar uno existente, use
   `EDITARFIGURA3D`, elija el modelo y cambie el mismo campo.

El sentido es del extremo inicial al final: `-19` baja 19°, `22` sube 22°,
`0` deja el eje horizontal. Se admite coma decimal. El ángulo sigue las
coordenadas y cotas si se editan directamente. La planta permanece fija;
la otra cota y la longitud real 3D cambian con el ángulo.

La opción corresponde a la tubería recta de los detalles de caja de drenaje,
como el de la captura. Los conjuntos corrugados con ramales/codos conservan
sus controles de cotas por punto.

Cancelar no aplica cambios al dibujo. `U` deshace una creación o edición.
Las referencias 2D seleccionadas se conservan.

La entrega incluye la DLL Release, el actualizador y una vista previa
obtenida de la geometría nativa de AutoCAD. No se instala mientras Civil 3D
está abierto. `Entrega.json` identifica la DLL por SHA-256.
