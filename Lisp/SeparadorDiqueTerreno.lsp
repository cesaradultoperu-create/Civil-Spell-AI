;;; SeparadorDiqueTerreno.lsp
;;; Prototipo independiente para separar una malla mezclada de 3DFACE.
;;;
;;; Flujo:
;;;   1. Selecciona todas las 3DFACE que forman el conjunto mezclado.
;;;   2. El usuario marca varias caras representativas del dique.
;;;   3. El usuario marca varias caras representativas del terreno natural.
;;;   4. El programa compara la forma y las caras superpuestas verticalmente.
;;;      La envolvente superior es candidata a dique y la inferior a terreno.
;;;   5. Resalta la propuesta del dique antes de modificar el dibujo.
;;;   6. Si se acepta, separa las caras en dos capas. UNDO revierte el cambio.
;;;
;;; Esta primera version NO crea superficies Civil 3D. Primero se verifica que
;;; la clasificacion sea correcta. Despues se conectara con la creacion TIN.
;;; El archivo SeparadorDeNivelesA.lsp no se modifica ni se reemplaza.

(vl-load-com)

(defun sdt:release-if-object (object)
  (if (= (type object) 'VLA-OBJECT)
    (vl-catch-all-apply 'vlax-release-object (list object))
  )
)

(defun sdt:safe-put (object property value / result)
  (setq result
    (vl-catch-all-apply
      'vlax-put-property
      (list object property value)
    )
  )
  (not (vl-catch-all-error-p result))
)

(defun sdt:unique-points (points tolerance / point existing result)
  (foreach point points
    (setq existing
      (vl-some
        '(lambda (candidate) (equal point candidate tolerance))
        result
      )
    )
    (if (not existing)
      (setq result (cons point result))
    )
  )
  (reverse result)
)

(defun sdt:face-points (entity tolerance)
  (sdt:unique-points
    (mapcar
      'cdr
      (vl-remove-if-not
        '(lambda (item) (member (car item) '(10 11 12 13)))
        (entget entity)
      )
    )
    tolerance
  )
)

(defun sdt:vector (from to)
  (mapcar '- to from)
)

(defun sdt:cross (a b)
  (list
    (- (* (cadr a) (caddr b)) (* (caddr a) (cadr b)))
    (- (* (caddr a) (car b)) (* (car a) (caddr b)))
    (- (* (car a) (cadr b)) (* (cadr a) (car b)))
  )
)

(defun sdt:length-3d (vector)
  (sqrt
    (+
      (* (car vector) (car vector))
      (* (cadr vector) (cadr vector))
      (* (caddr vector) (caddr vector))
    )
  )
)

(defun sdt:distance-3d (a b)
  (sdt:length-3d (sdt:vector a b))
)

(defun sdt:log10-safe (value)
  (/ (log (max value 1.0e-12)) (log 10.0))
)

;;; Devuelve cinco caracteristicas geometricas de una cara triangular:
;;; log(area), log(arista mayor), log(proporcion), pendiente y elevacion media.
(defun sdt:face-features
  (entity / points p0 p1 p2 u v normal normal-length area
    edge-a edge-b edge-c longest shortest aspect slope centroid-z)

  (setq points (sdt:face-points entity 1.0e-8))
  (if (< (length points) 3)
    nil
    (progn
      (setq p0 (nth 0 points))
      (setq p1 (nth 1 points))
      (setq p2 (nth 2 points))
      (setq u (sdt:vector p0 p1))
      (setq v (sdt:vector p0 p2))
      (setq normal (sdt:cross u v))
      (setq normal-length (sdt:length-3d normal))
      (setq area (/ normal-length 2.0))
      (setq edge-a (sdt:distance-3d p0 p1))
      (setq edge-b (sdt:distance-3d p1 p2))
      (setq edge-c (sdt:distance-3d p2 p0))
      (setq longest (max edge-a edge-b edge-c))
      (setq shortest (max 1.0e-9 (min edge-a edge-b edge-c)))
      (setq aspect (/ longest shortest))
      (if (> normal-length 1.0e-12)
        (setq slope
          (*
            180.0
            (/ (atan
                 (sqrt
                   (+
                     (* (car normal) (car normal))
                     (* (cadr normal) (cadr normal))
                   )
                 )
                 (abs (caddr normal))
               )
               pi
            )
          )
        )
        (setq slope 0.0)
      )
      (setq centroid-z
        (/ (+ (caddr p0) (caddr p1) (caddr p2)) 3.0)
      )
      (list
        (sdt:log10-safe area)
        (sdt:log10-safe longest)
        (sdt:log10-safe aspect)
        slope
        centroid-z
      )
    )
  )
)

(defun sdt:face-record
  (entity / points features p0 p1 p2 centroid bounds)
  (setq points (sdt:face-points entity 1.0e-8))
  (setq features (sdt:face-features entity))
  (if (and features (>= (length points) 3))
    (progn
      (setq p0 (nth 0 points))
      (setq p1 (nth 1 points))
      (setq p2 (nth 2 points))
      (setq centroid
        (list
          (/ (+ (car p0) (car p1) (car p2)) 3.0)
          (/ (+ (cadr p0) (cadr p1) (cadr p2)) 3.0)
          (/ (+ (caddr p0) (caddr p1) (caddr p2)) 3.0)
        )
      )
      (setq bounds
        (list
          (min (car p0) (car p1) (car p2))
          (min (cadr p0) (cadr p1) (cadr p2))
          (max (car p0) (car p1) (car p2))
          (max (cadr p0) (cadr p1) (cadr p2))
        )
      )
      ;; entidad, caracteristicas, vertices, centroide y limites XY
      (list entity features (list p0 p1 p2) centroid bounds)
    )
  )
)

(defun sdt:floor-int (value / integer)
  (setq integer (fix value))
  (if (and (< value 0.0) (/= value integer))
    (1- integer)
    integer
  )
)

(defun sdt:grid-key (x y cell-size)
  (strcat
    (itoa (sdt:floor-int (/ x cell-size)))
    "|"
    (itoa (sdt:floor-int (/ y cell-size)))
  )
)

(defun sdt:lwpolyline-points
  (entity / end-parameter parameter point result)
  ;; VLAX-CURVE devuelve directamente los vertices en WCS. Esto evita las
  ;; diferencias de formato que puede presentar el grupo DXF 10 de una
  ;; LWPOLYLINE segun el UCS y la version de AutoCAD/Civil 3D.
  (setq end-parameter
    (vl-catch-all-apply 'vlax-curve-getEndParam (list entity))
  )
  (if (vl-catch-all-error-p end-parameter)
    nil
    (progn
      (setq parameter 0)
      ;; En una polilinea cerrada, EndParam corresponde nuevamente al primer
      ;; vertice. Por eso se toman los parametros enteros menores que el final.
      (while (< parameter (fix end-parameter))
        (setq point
          (vl-catch-all-apply
            'vlax-curve-getPointAtParam
            (list entity parameter)
          )
        )
        (if (and (not (vl-catch-all-error-p point))
                 (= (type point) 'LIST)
                 (>= (length point) 2))
          (setq result (cons point result))
        )
        (setq parameter (1+ parameter))
      )
      (reverse result)
    )
  )
)

(defun sdt:valid-polygon-p (points / valid point)
  (setq valid (and (= (type points) 'LIST) (>= (length points) 3)))
  (if valid
    (foreach point points
      (if (not
            (and (= (type point) 'LIST)
                 (>= (length point) 2)
                 (numberp (car point))
                 (numberp (cadr point))))
        (setq valid nil)
      )
    )
  )
  valid
)

(defun sdt:select-dike-boundary
  (/ picked entity data entity-type flags points finished)
  (princ
    (strcat
      "\nSeleccione una POLILINEA CERRADA que siga el limite exterior"
      " del dique visto en planta."
    )
  )
  (while (not finished)
    (setq picked
      (entsel "\nPolilinea cerrada del limite del dique (Enter cancela): ")
    )
    (cond
      ((null picked)
        (setq finished T)
      )
      (T
        (setq entity (car picked))
        (setq data (entget entity))
        (setq entity-type (cdr (assoc 0 data)))
        (setq flags (if (assoc 70 data) (cdr (assoc 70 data)) 0))
        (cond
          ((/= entity-type "LWPOLYLINE")
            (princ
              "\nDebe seleccionar una polilinea 2D creada con PLINE."
            )
          )
          ((= (logand flags 1) 0)
            (princ
              "\nLa polilinea no esta cerrada. Cierrela y repita."
            )
          )
          (T
            (setq points (sdt:lwpolyline-points entity))
            (if (not (sdt:valid-polygon-p points))
              (princ
                "\nNo se pudieron leer correctamente sus vertices."
              )
              (progn
                (princ
                  (strcat "\nLimite reconocido: "
                    (itoa (length points)) " vertices."))
                (setq finished T)
              )
            )
          )
        )
      )
    )
  )
  points
)

(defun sdt:point-on-segment-2d-p
  (point first second tolerance / cross dot length-squared)
  (setq cross
    (-
      (* (- (car point) (car first)) (- (cadr second) (cadr first)))
      (* (- (cadr point) (cadr first)) (- (car second) (car first)))
    )
  )
  (setq dot
    (+
      (* (- (car point) (car first)) (- (car second) (car first)))
      (* (- (cadr point) (cadr first)) (- (cadr second) (cadr first)))
    )
  )
  (setq length-squared
    (+
      (* (- (car second) (car first)) (- (car second) (car first)))
      (* (- (cadr second) (cadr first)) (- (cadr second) (cadr first)))
    )
  )
  (and
    (<= (abs cross) (* tolerance (max 1.0 (sqrt length-squared))))
    (>= dot (- tolerance))
    (<= dot (+ length-squared tolerance))
  )
)

;;; Prueba de punto dentro de poligono por cruce de rayo horizontal.
(defun sdt:point-in-polygon-2d-p
  (point polygon tolerance / inside on-boundary previous current
    x-intersection)
  ;; En AutoLISP, LAST devuelve directamente el ultimo elemento de la lista.
  (setq previous (last polygon))
  (foreach current polygon
    (if (not on-boundary)
      (cond
        ((sdt:point-on-segment-2d-p point previous current tolerance)
          (setq on-boundary T)
        )
        ((and
           (not
             (equal
               (> (cadr previous) (cadr point))
               (> (cadr current) (cadr point))
             )
           )
           (/= (cadr current) (cadr previous)))
          (setq x-intersection
            (+
              (car previous)
              (*
                (/ (- (cadr point) (cadr previous))
                   (- (cadr current) (cadr previous)))
                (- (car current) (car previous))
              )
            )
          )
          (if (> x-intersection (car point))
            (setq inside (not inside))
          )
        )
      )
    )
    (setq previous current)
  )
  (or on-boundary inside)
)

(defun sdt:recommended-cell-size
  (records / first bounds minimum-x minimum-y maximum-x maximum-y
    width height target)
  (setq first T)
  (foreach record records
    (setq bounds (nth 4 record))
    (if first
      (progn
        (setq minimum-x (nth 0 bounds))
        (setq minimum-y (nth 1 bounds))
        (setq maximum-x (nth 2 bounds))
        (setq maximum-y (nth 3 bounds))
        (setq first nil)
      )
      (progn
        (setq minimum-x (min minimum-x (nth 0 bounds)))
        (setq minimum-y (min minimum-y (nth 1 bounds)))
        (setq maximum-x (max maximum-x (nth 2 bounds)))
        (setq maximum-y (max maximum-y (nth 3 bounds)))
      )
    )
  )
  (setq width (max 1.0 (- maximum-x minimum-x)))
  (setq height (max 1.0 (- maximum-y minimum-y)))
  ;; Aproximadamente diez triangulos pequenos por celda.
  (setq target
    (sqrt (/ (* width height) (max 1.0 (/ (length records) 10.0))))
  )
  ;; Diez metros evita que las caras largas generen demasiadas celdas.
  (max 10.0 (min 50.0 target))
)

(defun sdt:grid-entry-less-p (first second)
  (cond
    ((< (nth 0 first) (nth 0 second)) T)
    ((> (nth 0 first) (nth 0 second)) nil)
    ((< (nth 1 first) (nth 1 second)) T)
    ((> (nth 1 first) (nth 1 second)) nil)
    (T (< (nth 2 first) (nth 2 second)))
  )
)

(defun sdt:build-grid
  (records cell-size / entries sorted grid record bounds minimum-ix
    minimum-iy maximum-ix maximum-iy ix iy key count entry
    current-key bucket)
  (setq count 0)
  (foreach record records
    (setq bounds (nth 4 record))
    (setq minimum-ix
      (sdt:floor-int (/ (nth 0 bounds) cell-size))
    )
    (setq minimum-iy
      (sdt:floor-int (/ (nth 1 bounds) cell-size))
    )
    (setq maximum-ix
      (sdt:floor-int (/ (nth 2 bounds) cell-size))
    )
    (setq maximum-iy
      (sdt:floor-int (/ (nth 3 bounds) cell-size))
    )
    (setq ix minimum-ix)
    (while (<= ix maximum-ix)
      (setq iy minimum-iy)
      (while (<= iy maximum-iy)
        ;; El contador identifica la cara de manera numerica y evita que
        ;; VL-SORT elimine caras que pertenecen a la misma celda.
        (setq entries (cons (list ix iy count record) entries))
        (setq iy (1+ iy))
      )
      (setq ix (1+ ix))
    )
    (setq count (1+ count))
    (if (= (rem count 2500) 0)
      (princ
        (strcat "\rReconstruyendo la forma: " (itoa count) " caras...")
      )
    )
  )
  (princ "\nOrdenando las relaciones verticales...")
  (setq sorted (vl-sort entries 'sdt:grid-entry-less-p))
  (setq entries nil)
  (foreach entry sorted
    (setq key
      (strcat (itoa (nth 0 entry)) "|" (itoa (nth 1 entry)))
    )
    (if (or (null current-key) (not (equal key current-key)))
      (progn
        (if current-key
          (setq grid (cons (cons current-key (reverse bucket)) grid))
        )
        (setq current-key key)
        (setq bucket (list (nth 3 entry)))
      )
      (setq bucket (cons (nth 3 entry) bucket))
    )
  )
  (if current-key
    (setq grid (cons (cons current-key (reverse bucket)) grid))
  )
  grid
)

;;; Elevacion del plano triangular en una coordenada XY.
;;; Devuelve nil cuando el punto esta fuera del triangulo proyectado.
(defun sdt:z-at-xy
  (points x y tolerance / p0 p1 p2 denominator a b c)
  (setq p0 (nth 0 points))
  (setq p1 (nth 1 points))
  (setq p2 (nth 2 points))
  (setq denominator
    (+
      (* (- (cadr p1) (cadr p2)) (- (car p0) (car p2)))
      (* (- (car p2) (car p1)) (- (cadr p0) (cadr p2)))
    )
  )
  (if (< (abs denominator) 1.0e-12)
    nil
    (progn
      (setq a
        (/
          (+
            (* (- (cadr p1) (cadr p2)) (- x (car p2)))
            (* (- (car p2) (car p1)) (- y (cadr p2)))
          )
          denominator
        )
      )
      (setq b
        (/
          (+
            (* (- (cadr p2) (cadr p0)) (- x (car p2)))
            (* (- (car p0) (car p2)) (- y (cadr p2)))
          )
          denominator
        )
      )
      (setq c (- 1.0 a b))
      (if (and (>= a (- tolerance))
               (>= b (- tolerance))
               (>= c (- tolerance)))
        (+
          (* a (caddr p0))
          (* b (caddr p1))
          (* c (caddr p2))
        )
        nil
      )
    )
  )
)

;;; Separa por envolvente vertical local. Cada triangulo queda exclusivamente
;;; en un grupo. Las muestras elegidas por el usuario siempre se respetan.
(defun sdt:classify-upper-envelope
  (grid cell-size vertical-tolerance boundary-points
    / dike-set terrain-set cell key bucket record entity centroid
      candidate candidate-z maximum-z inside-boundary count)
  (setq dike-set (ssadd))
  (setq terrain-set (ssadd))
  (setq count 0)
  ;; Se recorre cada celda una sola vez. Cada cara se procesa solamente en
  ;; la celda que contiene su centroide, aunque atraviese varias celdas.
  (foreach cell grid
    (setq key (car cell))
    (setq bucket (cdr cell))
    (foreach record bucket
      (setq centroid (nth 3 record))
      (if (equal
            (sdt:grid-key (car centroid) (cadr centroid) cell-size)
            key
          )
        (progn
          (setq entity (car record))
          (setq inside-boundary
            (sdt:point-in-polygon-2d-p
              centroid boundary-points 0.001
            )
          )
          (setq maximum-z nil)
          (foreach candidate bucket
            (setq candidate-z
              (sdt:z-at-xy
                (nth 2 candidate)
                (car centroid)
                (cadr centroid)
                1.0e-7
              )
            )
            (if (and candidate-z
                     (or (null maximum-z) (> candidate-z maximum-z)))
              (setq maximum-z candidate-z)
            )
          )
          (cond
            ((and inside-boundary
                  (ssmemb entity sdt:*sample-dike-selection*))
              (ssadd entity dike-set)
            )
            ((ssmemb entity sdt:*sample-terrain-selection*)
              (ssadd entity terrain-set)
            )
            ((and inside-boundary maximum-z
                  (<= (- maximum-z (caddr centroid)) vertical-tolerance))
              (ssadd entity dike-set)
            )
            (T
              (ssadd entity terrain-set)
            )
          )
          (setq count (1+ count))
          (if (= (rem count 2500) 0)
            (princ
              (strcat "\rComparando nivel superior/inferior: "
                (itoa count) " caras...")
            )
          )
        )
      )
    )
  )
  (list dike-set terrain-set)
)

(defun sdt:selection-to-list (selection / index result)
  (setq index 0)
  (if selection
    (repeat (sslength selection)
      (setq result (cons (sdt:ssname-at selection index) result))
      (setq index (1+ index))
    )
  )
  (reverse result)
)

;;; AutoLISP exige indices reales para posiciones superiores a 32767.
(defun sdt:ssname-at (selection index)
  (ssname selection (if (> index 32767) (float index) index))
)

(defun sdt:all-faces-current-space ()
  (ssget "_X"
    (list
      '(0 . "3DFACE")
      (cons 410 (getvar "CTAB"))
    )
  )
)

(defun sdt:select-source-faces (/ mode selection)
  (initget "Seleccionar Todas")
  (setq mode
    (getkword
      "\nOrigen de las caras [Seleccionar/Todas] <Seleccionar>: "
    )
  )
  (if (= mode "Todas")
    (setq selection (sdt:all-faces-current-space))
    (progn
      (princ "\nSeleccione todas las 3DFACE mezcladas y pulse Enter: ")
      (setq selection (ssget '((0 . "3DFACE"))))
    )
  )
  selection
)

(defun sdt:selection-intersection
  (selection source / result index entity)
  (setq result (ssadd))
  (setq index 0)
  (if selection
    (repeat (sslength selection)
      (setq entity (sdt:ssname-at selection index))
      (if (ssmemb entity source)
        (ssadd entity result)
      )
      (setq index (1+ index))
    )
  )
  result
)

(defun sdt:select-samples
  (source label / result picked entity entity-data count finished)
  ;; La seleccion anterior no debe convertirse automaticamente en muestra.
  ;; Esto es indispensable cuando PICKFIRST esta activo en AutoCAD.
  (sssetfirst nil nil)
  (setq result (ssadd))
  (setq count 0)
  (princ
    (strcat
      "\nNO seleccione todo nuevamente. Haga clic solamente"
      " en una cara CLARA del "
      label
      "."
    )
  )
  (while (and (< count 1) (not finished))
    (setq picked
      (entsel
        (strcat
          "\n"
          label
          " - cara "
          (itoa (1+ count))
          " (Enter para terminar): "
        )
      )
    )
    (if (null picked)
      (setq finished T)
      (progn
        (setq entity (car picked))
        (setq entity-data (entget entity))
        (cond
          ((/= (cdr (assoc 0 entity-data)) "3DFACE")
            (princ "\nEse objeto no es una 3DFACE.")
          )
          ((not (ssmemb entity source))
            (princ "\nEsa cara no pertenece al conjunto inicial.")
          )
          ((ssmemb entity result)
            (princ "\nEsa cara ya estaba marcada.")
          )
          (T
            (ssadd entity result)
            (setq count (1+ count))
            (princ (strcat "  Marcadas: " (itoa count)))
          )
        )
      )
    )
  )
  result
)

(defun sdt:selections-overlap-p (first second / index found)
  (setq index 0)
  (while (and (< index (sslength first)) (not found))
    (if (ssmemb (sdt:ssname-at first index) second)
      (setq found T)
    )
    (setq index (1+ index))
  )
  found
)

;;; Sustituir por posicion sin depender de que los valores sean diferentes.
(defun sdt:replace-nth (index value values / current item result)
  (setq current 0)
  (foreach item values
    (if (= current index)
      (setq result (cons value result))
      (setq result (cons item result))
    )
    (setq current (1+ current))
  )
  (reverse result)
)

(defun sdt:update-bounds-safe
  (features minimums maximums / index value)
  (if (null minimums)
    (list features features)
    (progn
      (setq index 0)
      (while (< index (length features))
        (setq value (nth index features))
        (if (< value (nth index minimums))
          (setq minimums (sdt:replace-nth index value minimums))
        )
        (if (> value (nth index maximums))
          (setq maximums (sdt:replace-nth index value maximums))
        )
        (setq index (1+ index))
      )
      (list minimums maximums)
    )
  )
)

(defun sdt:normalize-features
  (features minimums maximums / index low high value result)
  (setq index 0)
  (while (< index (length features))
    (setq low (nth index minimums))
    (setq high (nth index maximums))
    (setq value (nth index features))
    (if (> (- high low) 1.0e-12)
      (setq value (/ (- value low) (- high low)))
      (setq value 0.0)
    )
    (setq result (cons value result))
    (setq index (1+ index))
  )
  (reverse result)
)

;;; Pesos: la longitud y el area son las pistas mas fuertes en estas mallas.
;;; La pendiente, la proporcion y la elevacion sirven como apoyo.
(defun sdt:feature-distance (a b / weights index delta result)
  (setq weights '(2.0 3.0 0.75 1.75 0.50))
  (setq index 0)
  (setq result 0.0)
  (while (< index (length weights))
    (setq delta (- (nth index a) (nth index b)))
    (setq result
      (+ result (* (nth index weights) delta delta))
    )
    (setq index (1+ index))
  )
  result
)

(defun sdt:sample-feature-list
  (selection records minimums maximums / index entity record result)
  (setq index 0)
  (repeat (sslength selection)
    (setq entity (sdt:ssname-at selection index))
    (setq record (assoc entity records))
    (if record
      (setq result
        (cons
          (sdt:normalize-features (cadr record) minimums maximums)
          result
        )
      )
    )
    (setq index (1+ index))
  )
  result
)

(defun sdt:nearest-distance (features samples / value best)
  (foreach sample samples
    (setq value (sdt:feature-distance features sample))
    (if (or (null best) (< value best))
      (setq best value)
    )
  )
  best
)

(defun sdt:classify
  (records sample-dike sample-terrain minimums maximums
    / dike-set terrain-set count entity features normalized
      distance-dike distance-terrain)

  (setq dike-set (ssadd))
  (setq terrain-set (ssadd))
  (setq count 0)
  (foreach record records
    (setq entity (car record))
    (setq features (cadr record))
    (setq normalized
      (sdt:normalize-features features minimums maximums)
    )
    (cond
      ((ssmemb entity sdt:*sample-dike-selection*)
        (ssadd entity dike-set)
      )
      ((ssmemb entity sdt:*sample-terrain-selection*)
        (ssadd entity terrain-set)
      )
      (T
        (setq distance-dike
          (sdt:nearest-distance normalized sample-dike)
        )
        (setq distance-terrain
          (sdt:nearest-distance normalized sample-terrain)
        )
        (if (<= distance-dike distance-terrain)
          (ssadd entity dike-set)
          (ssadd entity terrain-set)
        )
      )
    )
    (setq count (1+ count))
    (if (= (rem count 2500) 0)
      (princ (strcat "\rClasificando: " (itoa count) " caras..."))
    )
  )
  (list dike-set terrain-set)
)

(defun sdt:toggle-selection
  (changes dike-set terrain-set / index entity changed)
  (setq index 0)
  (if changes
    (repeat (sslength changes)
      (setq entity (sdt:ssname-at changes index))
      (cond
        ((ssmemb entity dike-set)
          (ssdel entity dike-set)
          (ssadd entity terrain-set)
          (setq changed (1+ (if changed changed 0)))
        )
        ((ssmemb entity terrain-set)
          (ssdel entity terrain-set)
          (ssadd entity dike-set)
          (setq changed (1+ (if changed changed 0)))
        )
      )
      (setq index (1+ index))
    )
  )
  (if changed changed 0)
)

(defun sdt:review-classification
  (dike-set terrain-set / answer changes changed finished accepted)
  (while (not finished)
    ;; Solo el dique propuesto queda resaltado. El terreno no se modifica.
    (sssetfirst nil dike-set)
    (princ
      (strcat
        "\nVista previa: DIQUE resaltado = "
        (itoa (sslength dike-set))
        " caras; TERRENO = "
        (itoa (sslength terrain-set))
        " caras."
      )
    )
    (initget "Si Ajustar No")
    (setq answer
      (getkword
        "\n¿El dique resaltado esta correcto? [Si/Ajustar/No] <No>: "
      )
    )
    (cond
      ((= answer "Si")
        (setq accepted T)
        (setq finished T)
      )
      ((= answer "Ajustar")
        (sssetfirst nil nil)
        (princ
          "\nSeleccione caras o zonas para cambiarlas al grupo contrario y pulse Enter: "
        )
        (setq changes (ssget '((0 . "3DFACE"))))
        (setq changed
          (sdt:toggle-selection changes dike-set terrain-set)
        )
        (princ (strcat "\nSe cambiaron " (itoa changed) " cara(s)."))
      )
      (T
        (setq accepted nil)
        (setq finished T)
      )
    )
  )
  (sssetfirst nil nil)
  accepted
)

(defun sdt:ensure-layer
  (document layer-name color / layers layer result)
  (setq layers (vla-get-Layers document))
  (setq result
    (vl-catch-all-apply 'vla-Item (list layers layer-name))
  )
  (if (vl-catch-all-error-p result)
    (setq result
      (vl-catch-all-apply 'vla-Add (list layers layer-name))
    )
  )
  (if (vl-catch-all-error-p result)
    (setq layer nil)
    (progn
      (setq layer result)
      (sdt:safe-put layer 'Freeze :vlax-false)
      (sdt:safe-put layer 'LayerOn :vlax-true)
      (sdt:safe-put layer 'Lock :vlax-false)
      (sdt:safe-put layer 'Color color)
    )
  )
  (sdt:release-if-object layers)
  layer
)

(defun sdt:move-selection-to-layer
  (selection layer-name / index entity object result moved failed)
  (setq index 0)
  (setq moved 0)
  (setq failed 0)
  (repeat (sslength selection)
    (setq entity (sdt:ssname-at selection index))
    (setq object (vlax-ename->vla-object entity))
    (setq result
      (vl-catch-all-apply 'vla-put-Layer (list object layer-name))
    )
    (if (vl-catch-all-error-p result)
      (setq failed (1+ failed))
      (progn
        ;; 256 = ByLayer. Asi se ven con el color de la capa creada.
        (vl-catch-all-apply 'vla-put-Color (list object 256))
        (setq moved (1+ moved))
      )
    )
    (sdt:release-if-object object)
    (setq index (1+ index))
  )
  (list moved failed)
)

(defun c:SEPARARDIQUETERRENO
  (/ *error* old-error document undo-open source sample-dike-selection
    sample-terrain-selection boundary-points entities records count entity
    record cell-size grid groups dike-set terrain-set accepted layer-dike
    layer-terrain move-dike move-terrain)

  (setq old-error *error*)
  (setq document (vla-get-ActiveDocument (vlax-get-acad-object)))

  (defun *error* (message)
    (sssetfirst nil nil)
    (if undo-open
      (progn
        (vl-catch-all-apply 'vla-EndUndoMark (list document))
        (setq undo-open nil)
      )
    )
    (sdt:release-if-object layer-dike)
    (sdt:release-if-object layer-terrain)
    (setq sdt:*sample-dike-selection* nil)
    (setq sdt:*sample-terrain-selection* nil)
    (setq *error* old-error)
    (if (and message (not (wcmatch (strcase message) "*CANCEL*,*QUIT*,*EXIT*")))
      (princ (strcat "\nError: " message))
    )
    (princ)
  )

  (princ
    "\nSEPARARDIQUETERRENO - dique superior y terreno natural inferior."
  )
  (setq source (sdt:select-source-faces))

  (cond
    ((or (null source) (= (sslength source) 0))
      (princ "\nNo se seleccionaron 3DFACE. No se modifico nada.")
    )
    (T
      (princ
        (strcat
          "\nConjunto inicial: "
          (itoa (sslength source))
          " cara(s). Ahora la seleccion general se limpiara."
        )
      )
      (sssetfirst nil nil)
      (setq sample-dike-selection
        (sdt:select-samples source "DIQUE")
      )
      (cond
        ((= (sslength sample-dike-selection) 0)
          (princ "\nFalta marcar al menos una cara del dique.")
        )
        (T
          (setq sample-terrain-selection
            (sdt:select-samples source "TERRENO NATURAL")
          )
          (if (> (sslength sample-terrain-selection) 0)
            (setq boundary-points (sdt:select-dike-boundary))
          )
          (cond
            ((= (sslength sample-terrain-selection) 0)
              (princ "\nFalta marcar al menos una cara del terreno.")
            )
            ((null boundary-points)
              (princ
                "\nFalta la polilinea cerrada del dique. No se modifico nada."
              )
            )
            ((sdt:selections-overlap-p
               sample-dike-selection sample-terrain-selection)
              (princ
                "\nUna cara fue marcada en ambos grupos. Repita el comando."
              )
            )
            (T
              (setq sdt:*sample-dike-selection* sample-dike-selection)
              (setq sdt:*sample-terrain-selection* sample-terrain-selection)
              (princ "\nAnalizando geometria. Espere...")
              (setq entities (sdt:selection-to-list source))
              (setq count 0)
              (foreach entity entities
                (setq record (sdt:face-record entity))
                (if record
                  (setq records (cons record records))
                )
                (setq count (1+ count))
                (if (= (rem count 2500) 0)
                  (princ (strcat "\rAnalizando: " (itoa count) " caras..."))
                )
              )
              (setq records (reverse records))
              (setq cell-size (sdt:recommended-cell-size records))
              (princ
                (strcat
                  "\nReconstruyendo la forma superior e inferior"
                  " (celda " (rtos cell-size 2 2) ")..."
                )
              )
              (setq grid (sdt:build-grid records cell-size))
              (setq groups
                (sdt:classify-upper-envelope
                  grid cell-size 0.001 boundary-points
                )
              )
              (setq dike-set (car groups))
              (setq terrain-set (cadr groups))
              ;; Libera la estructura geometrica antes de mostrar la vista.
              (setq grid nil)
              (setq records nil)
              (gc)
              (setq accepted
                (sdt:review-classification dike-set terrain-set)
              )
              (if accepted
                (progn
                  (vla-StartUndoMark document)
                  (setq undo-open T)
                  (setq layer-dike
                    (sdt:ensure-layer document "SDT-DIQUE-FUENTE" 60)
                  )
                  (setq layer-terrain
                    (sdt:ensure-layer document "SDT-TERRENO-FUENTE" 130)
                  )
                  (if (and layer-dike layer-terrain)
                    (progn
                      (setq move-dike
                        (sdt:move-selection-to-layer
                          dike-set "SDT-DIQUE-FUENTE"
                        )
                      )
                      (setq move-terrain
                        (sdt:move-selection-to-layer
                          terrain-set "SDT-TERRENO-FUENTE"
                        )
                      )
                      (vla-EndUndoMark document)
                      (setq undo-open nil)
                      (vla-Regen document 1)
                      (princ
                        (strcat
                          "\nSeparacion aplicada:"
                          "\n  SDT-DIQUE-FUENTE: "
                          (itoa (car move-dike))
                          " cara(s)."
                          "\n  SDT-TERRENO-FUENTE: "
                          (itoa (car move-terrain))
                          " cara(s)."
                        )
                      )
                      (if (> (+ (cadr move-dike) (cadr move-terrain)) 0)
                        (princ
                          (strcat
                            "\nAviso: "
                            (itoa (+ (cadr move-dike) (cadr move-terrain)))
                            " cara(s) no pudieron cambiar de capa."
                          )
                        )
                      )
                      (princ
                        "\nEsta version verifica la separacion. Aun no crea superficies TIN."
                      )
                      (princ
                        "\nSi el resultado no es correcto, ejecute UNDO una vez."
                      )
                    )
                    (progn
                      (vla-EndUndoMark document)
                      (setq undo-open nil)
                      (princ "\nNo se pudieron preparar las capas de salida.")
                    )
                  )
                )
                (princ "\nPrueba cancelada. No se modifico ninguna cara.")
              )
            )
          )
        )
      )
    )
  )

  (sssetfirst nil nil)
  (sdt:release-if-object layer-dike)
  (sdt:release-if-object layer-terrain)
  (setq sdt:*sample-dike-selection* nil)
  (setq sdt:*sample-terrain-selection* nil)
  (setq *error* old-error)
  (princ)
)

;;; ------------------------------------------------------------------------
;;; Modo por referencia exacta
;;; ------------------------------------------------------------------------
;;; Este modo sirve para una copia de una malla que ya fue separada
;;; correctamente en otro dibujo. Lee los LandXML fuente creados por
;;; SEPARADORNIVELESA y reconoce cada 3DFACE por sus tres coordenadas.

(defun sdt:dictionary-value (dictionary key / value)
  (setq value (vlax-get-property dictionary 'Item key))
  (if (= (type value) 'VARIANT)
    (vlax-variant-value value)
    value
  )
)

(defun sdt:dictionary-exists-p (dictionary key)
  (= (vlax-invoke-method dictionary 'Exists key) :vlax-true)
)

(defun sdt:point-less-p (first second)
  (cond
    ((< (car first) (car second)) T)
    ((> (car first) (car second)) nil)
    ((< (cadr first) (cadr second)) T)
    ((> (cadr first) (cadr second)) nil)
    (T (< (caddr first) (caddr second)))
  )
)

(defun sdt:reference-coordinate-text (value)
  ;; Seis decimales equivalen a una tolerancia submilimetrica y absorben
  ;; pequenas diferencias de escritura entre DWG y LandXML.
  (rtos value 2 6)
)

(defun sdt:point-signature (point)
  (strcat
    (sdt:reference-coordinate-text (car point)) ","
    (sdt:reference-coordinate-text (cadr point)) ","
    (sdt:reference-coordinate-text (caddr point))
  )
)

(defun sdt:face-signature-from-point-keys (point-keys / ordered)
  (if (>= (length point-keys) 3)
    (progn
      ;; ACAD_STRLSORT esta disponible en AutoCAD y conserva las claves.
      (setq ordered (acad_strlsort point-keys))
      (apply 'strcat
        (mapcar '(lambda (part) (strcat part "|")) ordered))
    )
  )
)

(defun sdt:face-signature-from-points (points)
  (sdt:face-signature-from-point-keys
    (mapcar 'sdt:point-signature points)
  )
)

(defun sdt:tag-text (line closing-tag / opening-position closing-position)
  (setq opening-position (vl-string-search ">" line))
  (setq closing-position (vl-string-search closing-tag line))
  (if (and opening-position closing-position
           (> closing-position opening-position))
    (substr
      line
      (+ opening-position 2)
      (- closing-position opening-position 1)
    )
  )
)

(defun sdt:xml-point-id (line / marker start-position end-position)
  (setq marker (vl-string-search "id=\"" line))
  (if marker
    (progn
      (setq start-position (+ marker 4))
      (setq end-position (vl-string-search "\"" line start-position))
      (if end-position
        (substr
          line
          (+ start-position 1)
          (- end-position start-position)
        )
      )
    )
  )
)

;;; Devuelve (puntos caras duplicadas errores).
(defun sdt:load-reference-landxml
  (file-path face-dictionary label
    / stream point-dictionary line text values point-id point
      ids p0 p1 p2 signature point-count face-count duplicate-count
      error-count)
  (setq stream (open file-path "r"))
  (if (null stream)
    nil
    (progn
      (setq point-dictionary (vlax-create-object "Scripting.Dictionary"))
      (while (setq line (read-line stream))
        (cond
          ((and (vl-string-search "<P id=\"" line)
                (setq point-id (sdt:xml-point-id line))
                (setq text (sdt:tag-text line "</P>")))
            (setq values
              (vl-catch-all-apply 'read (list (strcat "(" text ")")))
            )
            (if (and (not (vl-catch-all-error-p values))
                     (= (type values) 'LIST)
                     (>= (length values) 3))
              (progn
                ;; LandXML escribe Norte, Este, Elevacion. AutoCAD usa X,Y,Z.
                (setq point
                  (sdt:point-signature
                    (list (cadr values) (car values) (caddr values))))
                (vlax-invoke-method
                  point-dictionary 'Add point-id point)
                (setq point-count (1+ (if point-count point-count 0)))
              )
              (setq error-count (1+ (if error-count error-count 0)))
            )
          )
          ((setq text (sdt:tag-text line "</F>"))
            (setq ids
              (vl-catch-all-apply 'read (list (strcat "(" text ")")))
            )
            (if (and (not (vl-catch-all-error-p ids))
                     (= (type ids) 'LIST)
                     (>= (length ids) 3)
                     (sdt:dictionary-exists-p
                       point-dictionary (itoa (fix (nth 0 ids))))
                     (sdt:dictionary-exists-p
                       point-dictionary (itoa (fix (nth 1 ids))))
                     (sdt:dictionary-exists-p
                       point-dictionary (itoa (fix (nth 2 ids)))))
              (progn
                (setq p0
                  (sdt:dictionary-value
                    point-dictionary (itoa (fix (nth 0 ids)))))
                (setq p1
                  (sdt:dictionary-value
                    point-dictionary (itoa (fix (nth 1 ids)))))
                (setq p2
                  (sdt:dictionary-value
                    point-dictionary (itoa (fix (nth 2 ids)))))
                (setq signature
                  (sdt:face-signature-from-point-keys (list p0 p1 p2)))
                (if (sdt:dictionary-exists-p face-dictionary signature)
                  (setq duplicate-count
                    (1+ (if duplicate-count duplicate-count 0)))
                  (vlax-invoke-method
                    face-dictionary 'Add signature label)
                )
                (setq face-count (1+ (if face-count face-count 0)))
              )
              (setq error-count (1+ (if error-count error-count 0)))
            )
          )
        )
      )
      (close stream)
      (sdt:release-if-object point-dictionary)
      (list
        (if point-count point-count 0)
        (if face-count face-count 0)
        (if duplicate-count duplicate-count 0)
        (if error-count error-count 0)
      )
    )
  )
)

(defun sdt:classify-from-reference
  (source face-dictionary
    / dike-set terrain-set unmatched-set index entity points signature label)
  (setq dike-set (ssadd))
  (setq terrain-set (ssadd))
  (setq unmatched-set (ssadd))
  (setq index 0)
  (repeat (sslength source)
    (setq entity (sdt:ssname-at source index))
    (setq points (sdt:face-points entity 1.0e-8))
    (setq signature (sdt:face-signature-from-points points))
    (if (and signature
             (sdt:dictionary-exists-p face-dictionary signature))
      (progn
        (setq label (sdt:dictionary-value face-dictionary signature))
        (if (= label "A")
          (ssadd entity dike-set)
          (ssadd entity terrain-set)
        )
      )
      (ssadd entity unmatched-set)
    )
    (setq index (1+ index))
    (if (= (rem index 2500) 0)
      (princ
        (strcat "\rComparando con la referencia: "
          (itoa index) " caras..."))
    )
  )
  (list dike-set terrain-set unmatched-set)
)

(defun c:SEPARARDIQUETERRENOREF
  (/ *error* old-error document undo-open source file-a file-b
    face-dictionary result-a result-b groups dike-set terrain-set
    unmatched-set accepted layer-dike layer-terrain move-dike move-terrain)
  (setq old-error *error*)
  (setq document (vla-get-ActiveDocument (vlax-get-acad-object)))

  (defun *error* (message)
    (sssetfirst nil nil)
    (if undo-open
      (progn
        (vl-catch-all-apply 'vla-EndUndoMark (list document))
        (setq undo-open nil)
      )
    )
    (sdt:release-if-object layer-dike)
    (sdt:release-if-object layer-terrain)
    (sdt:release-if-object face-dictionary)
    (setq *error* old-error)
    (if (and message
             (not (wcmatch (strcase message) "*CANCEL*,*QUIT*,*EXIT*")))
      (princ (strcat "\nError: " message))
    )
    (princ)
  )

  (princ
    "\nSEPARARDIQUETERRENOREF - separacion exacta mediante LandXML."
  )
  (setq source (sdt:select-source-faces))
  (if (and source (> (sslength source) 0))
    (progn
      (setq file-a
        (getfiled
          "Seleccione el LandXML correcto del DIQUE A"
          (getvar "DWGPREFIX") "xml" 0))
      (if file-a
        (setq file-b
          (getfiled
            "Seleccione el LandXML correcto del TERRENO B"
            (strcat (vl-filename-directory file-a) "\\") "xml" 0)))
      (if (and file-a file-b)
        (progn
          (setq face-dictionary
            (vlax-create-object "Scripting.Dictionary"))
          (princ "\nLeyendo la referencia del dique...")
          (setq result-a
            (sdt:load-reference-landxml file-a face-dictionary "A"))
          (princ "\nLeyendo la referencia del terreno...")
          (setq result-b
            (sdt:load-reference-landxml file-b face-dictionary "B"))
          (if (and result-a result-b
                   (= (nth 3 result-a) 0)
                   (= (nth 3 result-b) 0))
            (progn
              (princ
                (strcat
                  "\nReferencia cargada: DIQUE "
                  (itoa (nth 1 result-a))
                  " caras; TERRENO "
                  (itoa (nth 1 result-b)) " caras."
                )
              )
              (setq groups
                (sdt:classify-from-reference source face-dictionary))
              (setq dike-set (nth 0 groups))
              (setq terrain-set (nth 1 groups))
              (setq unmatched-set (nth 2 groups))
              (princ
                (strcat
                  "\nCoincidencias: DIQUE " (itoa (sslength dike-set))
                  "; TERRENO " (itoa (sslength terrain-set))
                  "; SIN COINCIDENCIA " (itoa (sslength unmatched-set)) "."
                )
              )
              (if (> (sslength unmatched-set) 0)
                (progn
                  (sssetfirst nil unmatched-set)
                  (princ
                    "\nNo se aplico nada. Las caras sin coincidencia quedaron resaltadas."
                  )
                )
                (progn
                  (setq accepted
                    (sdt:review-classification dike-set terrain-set))
                  (if accepted
                    (progn
                      (vla-StartUndoMark document)
                      (setq undo-open T)
                      (setq layer-dike
                        (sdt:ensure-layer document "_face A" 60))
                      (setq layer-terrain
                        (sdt:ensure-layer document "_face B" 130))
                      (setq move-dike
                        (sdt:move-selection-to-layer dike-set "_face A"))
                      (setq move-terrain
                        (sdt:move-selection-to-layer terrain-set "_face B"))
                      (vla-EndUndoMark document)
                      (setq undo-open nil)
                      (princ
                        (strcat
                          "\nSeparacion aplicada: DIQUE "
                          (itoa (car move-dike))
                          "; TERRENO " (itoa (car move-terrain)) "."
                        )
                      )
                      (princ
                        "\nAhora puede usar SEPARADORNIVELESA para crear las superficies TIN."
                      )
                    )
                    (princ "\nPrueba cancelada. No se modifico ninguna cara.")
                  )
                )
              )
            )
            (princ
              "\nNo se pudo leer correctamente uno de los LandXML. No se aplico nada."
            )
          )
        )
        (princ "\nOperacion cancelada. No se modifico nada.")
      )
    )
    (princ "\nNo se seleccionaron 3DFACE. No se modifico nada.")
  )

  (sdt:release-if-object layer-dike)
  (sdt:release-if-object layer-terrain)
  (sdt:release-if-object face-dictionary)
  (setq *error* old-error)
  (princ)
)

(princ
  (strcat
    "\nSeparador Dique-Terreno cargado. Comandos:"
    " SEPARARDIQUETERRENO y SEPARARDIQUETERRENOREF"
  )
)
(princ)
