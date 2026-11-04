(vl-load-com)

(defun c:AISPELLPROBEDRAWING3D (/ ss index entity data type layer points xs ys closed counts entry blockEntity child childData childSolids childPolylines childHatches)
  (setq ss (ssget "_X" '((410 . "Model"))))
  (setq index 0 counts nil)
  (princ (strcat "\nDRAWING3D_ENTITY_TOTAL=" (itoa (if ss (sslength ss) 0))))
  (if ss
    (repeat (sslength ss)
      (setq entity (ssname ss index)
            data (entget entity)
            type (cdr (assoc 0 data))
            layer (cdr (assoc 8 data))
            entry (assoc type counts))
      (if entry
        (setq counts (subst (cons type (1+ (cdr entry))) entry counts))
        (setq counts (cons (cons type 1) counts)))
      (if (= type "LWPOLYLINE")
        (progn
          (setq points (mapcar 'cdr (vl-remove-if-not '(lambda (item) (= (car item) 10)) data))
                xs (mapcar 'car points)
                ys (mapcar 'cadr points)
                closed (= 1 (logand 1 (cdr (assoc 70 data)))))
          (if closed
            (princ
              (strcat
                "\nCLOSED_LWPOLYLINE handle=" (cdr (assoc 5 data))
                " layer=" layer
                " vertices=" (itoa (length points))
                " normal=" (vl-princ-to-string (cdr (assoc 210 data)))
                " points=" (vl-princ-to-string points)
                " minX=" (rtos (apply 'min xs) 2 6)
                " maxX=" (rtos (apply 'max xs) 2 6)
                " minY=" (rtos (apply 'min ys) 2 6)
                " maxY=" (rtos (apply 'max ys) 2 6))))))
      (if (= type "3DSOLID")
        (princ
          (strcat
            "\nSOLID3D handle=" (cdr (assoc 5 data))
            " layer=" layer)))
      (if (and (= type "INSERT") (wcmatch (cdr (assoc 2 data)) "CIVILSPELLAI_SECCION_*"))
        (progn
          (setq blockEntity (tblobjname "BLOCK" (cdr (assoc 2 data)))
                child (entnext blockEntity)
                childSolids 0
                childPolylines 0
                childHatches 0)
          (while child
            (setq childData (entget child))
            (cond
              ((= (cdr (assoc 0 childData)) "ENDBLK") (setq child nil))
              ((= (cdr (assoc 0 childData)) "3DSOLID") (setq childSolids (1+ childSolids) child (entnext child)))
              ((= (cdr (assoc 0 childData)) "LWPOLYLINE") (setq childPolylines (1+ childPolylines) child (entnext child)))
              ((= (cdr (assoc 0 childData)) "HATCH") (setq childHatches (1+ childHatches) child (entnext child)))
              (T (setq child (entnext child)))))
          (princ
            (strcat
              "\nPARAMETRIC_BLOCK handle=" (cdr (assoc 5 data))
              " name=" (cdr (assoc 2 data))
              " solids=" (itoa childSolids)
              " polylines=" (itoa childPolylines)
              " hatches=" (itoa childHatches)))))
      (setq index (1+ index))))
  (foreach item (vl-sort counts '(lambda (left right) (< (car left) (car right))))
    (princ (strcat "\nENTITY_COUNT " (car item) "=" (itoa (cdr item)))))
  (princ "\nDRAWING3D_PROBE_OK")
  (princ))

