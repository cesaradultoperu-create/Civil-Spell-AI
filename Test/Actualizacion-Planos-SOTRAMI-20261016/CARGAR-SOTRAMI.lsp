;;; Abra primero la copia de revision y cargue este archivo con APPLOAD.
(vl-load-com)
(defun c:CARGARSOTRAMI (/ version nombre dll resultado)
  (setq version (substr (getvar "ACADVER") 1 4))
  (cond
    ((/= (strcase (getvar "DWGNAME")) "1107.001.00.14-200-00-P-001 - REVISION EXCEL.DWG")
     (princ "\nABRA PRIMERO EL DWG DE REVISION DEL PAQUETE."))
    ((not (member version '("24.2" "24.3" "25.0")))
     (princ "\nESTE PAQUETE ESTA PREPARADO PARA CIVIL 3D 2023, 2024 Y 2025."))
    (T
     (setq nombre (if (= version "25.0") "VinculoPlanosSOTRAMI.v5.2025.dll" "VinculoPlanosSOTRAMI.v5.2023-2024.dll"))
     (setq dll (strcat (getvar "DWGPREFIX") nombre))
     (cond
       ((not (findfile dll))
        (princ (strcat "\nFALTA EL DLL JUNTO AL DWG: " nombre)))
       ((and (boundp '*sotrami-dll-cargado*) (= *sotrami-dll-cargado* dll))
        (princ "\nVINCULO YA CARGADO EN ESTA SESION. USE SOTRAMIEXCEL."))
       (T
        (setq resultado (vl-catch-all-apply '(lambda () (command "_.NETLOAD" dll))))
        (if (vl-catch-all-error-p resultado)
          (princ (strcat "\nNO SE PUDO CARGAR: " (vl-catch-all-error-message resultado)))
          (progn
            (setq *sotrami-dll-cargado* dll)
            (princ (strcat "\nDLL SELECCIONADO: " nombre ". EJECUTE SOTRAMIEXCEL."))))))))
  (princ))
(c:CARGARSOTRAMI)
(princ)
