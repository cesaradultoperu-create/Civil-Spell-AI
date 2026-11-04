;;; Dibujo 2D, en metros, reconstruido a partir del croquis de descarga de quebrada.
;;; Comando: QUEBRADADWG

(defun q:pt (x y)
  (list x y 0.0))

(defun q:layer (name color lineweight)
  (if (not (tblsearch "LAYER" name))
    (entmakex
      (list
        '(0 . "LAYER")
        '(100 . "AcDbSymbolTableRecord")
        '(100 . "AcDbLayerTableRecord")
        (cons 2 name)
        '(70 . 0)
        (cons 62 color)
        '(6 . "Continuous")
        (cons 370 lineweight)))))

(defun q:line (layer start end)
  (entmakex
    (list
      '(0 . "LINE")
      '(100 . "AcDbEntity")
      (cons 8 layer)
      '(100 . "AcDbLine")
      (cons 10 start)
      (cons 11 end))))

(defun q:polyline (layer points closed / data point)
  (setq data
    (list
      '(0 . "LWPOLYLINE")
      '(100 . "AcDbEntity")
      (cons 8 layer)
      '(100 . "AcDbPolyline")
      (cons 90 (length points))
      (cons 70 (if closed 1 0))))
  (foreach point points
    (setq data (append data (list (cons 10 (list (car point) (cadr point)))))))
  (entmakex data))

(defun q:circle (layer center radius)
  (entmakex
    (list
      '(0 . "CIRCLE")
      '(100 . "AcDbEntity")
      (cons 8 layer)
      '(100 . "AcDbCircle")
      (cons 10 center)
      (cons 40 radius))))

(defun q:text (layer insertion height value justification / horizontal vertical)
  (setq horizontal 0)
  (setq vertical 0)
  (cond
    ((= justification "MC") (setq horizontal 1 vertical 2))
    ((= justification "MR") (setq horizontal 2 vertical 2))
    ((= justification "ML") (setq horizontal 0 vertical 2)))
  (entmakex
    (list
      '(0 . "TEXT")
      '(100 . "AcDbEntity")
      (cons 8 layer)
      '(100 . "AcDbText")
      (cons 10 insertion)
      (cons 11 insertion)
      (cons 40 height)
      (cons 1 value)
      (cons 7 "Standard")
      (cons 50 0.0)
      (cons 72 horizontal)
      (cons 73 vertical))))

(defun q:dim (layer first second location / previous-layer)
  (setq previous-layer (getvar "CLAYER"))
  (setvar "CLAYER" layer)
  (command "_.DIMLINEAR" "_non" first "_non" second "_non" location)
  (setvar "CLAYER" previous-layer))

(defun q:arrow (layer start end / dx dy magnitude ux uy size half-width left right)
  (q:line layer start end)
  (setq dx (- (car start) (car end)))
  (setq dy (- (cadr start) (cadr end)))
  (setq magnitude (sqrt (+ (* dx dx) (* dy dy))))
  (setq ux (/ dx magnitude))
  (setq uy (/ dy magnitude))
  (setq size 0.10)
  (setq half-width 0.045)
  (setq left
    (q:pt
      (+ (car end) (* ux size) (* (- uy) half-width))
      (+ (cadr end) (* uy size) (* ux half-width))))
  (setq right
    (q:pt
      (+ (car end) (* ux size) (* uy half-width))
      (+ (cadr end) (* uy size) (* (- ux) half-width))))
  (q:polyline layer (list left end right) nil))

(defun q:masonry-marks (layer / x)
  ;; Símbolo esquemático de piedra sobre el fondo de la sección.
  (setq x 0.99)
  (while (<= x 2.21)
    (q:circle layer (q:pt x -3.37) 0.035)
    (setq x (+ x 0.115))))

(defun q:draw-plan ()
  ;; Llegada: 0.30 m libres y dos muros de 0.15 m.
  (q:line "ESTRUCTURA" (q:pt 0.00  0.30) (q:pt 0.80  0.30))
  (q:line "ESTRUCTURA" (q:pt 0.00  0.15) (q:pt 0.80  0.15))
  (q:line "ESTRUCTURA" (q:pt 0.00 -0.15) (q:pt 0.80 -0.15))
  (q:line "ESTRUCTURA" (q:pt 0.00 -0.30) (q:pt 0.80 -0.30))
  (q:line "ESTRUCTURA" (q:pt 0.00  0.30) (q:pt 0.00  0.15))
  (q:line "ESTRUCTURA" (q:pt 0.00 -0.15) (q:pt 0.00 -0.30))

  ;; Transición de 1.20 m hacia una caja de 1.20 m libres.
  (q:line "ESTRUCTURA" (q:pt 0.80  0.30) (q:pt 2.00  0.75))
  (q:line "ESTRUCTURA" (q:pt 0.80  0.15) (q:pt 2.00  0.60))
  (q:line "ESTRUCTURA" (q:pt 0.80 -0.15) (q:pt 2.00 -0.60))
  (q:line "ESTRUCTURA" (q:pt 0.80 -0.30) (q:pt 2.00 -0.75))

  ;; Caja de mampostería: 1.20 x 1.20 m libres y muros de 0.15 m.
  (q:line "ESTRUCTURA" (q:pt 2.00  0.75) (q:pt 3.20  0.75))
  (q:line "ESTRUCTURA" (q:pt 2.00  0.60) (q:pt 3.05  0.60))
  (q:line "ESTRUCTURA" (q:pt 2.00 -0.60) (q:pt 3.05 -0.60))
  (q:line "ESTRUCTURA" (q:pt 2.00 -0.75) (q:pt 3.20 -0.75))
  (q:line "ESTRUCTURA" (q:pt 3.20  0.75) (q:pt 3.20 -0.75))
  (q:line "ESTRUCTURA" (q:pt 3.05  0.60) (q:pt 3.05 -0.60))

  ;; Eje y sentido del flujo.
  (q:line "EJES" (q:pt -0.15 0.0) (q:pt 3.35 0.0))
  (q:arrow "TEXTO" (q:pt 0.18 0.0) (q:pt 0.52 0.0))

  ;; Línea de corte A-A'.
  (q:line "CORTE" (q:pt 2.08 1.00) (q:pt 2.08 -1.02))
  (q:text "TEXTO" (q:pt 2.08 1.10) 0.13 "A" "MC")
  (q:text "TEXTO" (q:pt 2.08 -1.12) 0.13 "A'" "MC")

  ;; Cotas de planta.
  (q:dim "COTAS" (q:pt 0.00 0.15) (q:pt 0.00 -0.15) (q:pt -0.25 0.00))
  (q:dim "COTAS" (q:pt 0.00 0.30) (q:pt 0.00 0.15) (q:pt -0.46 0.225))
  (q:dim "COTAS" (q:pt 0.00 -0.15) (q:pt 0.00 -0.30) (q:pt -0.46 -0.225))
  (q:dim "COTAS" (q:pt 0.80 -0.30) (q:pt 2.00 -0.30) (q:pt 1.40 -1.06))
  (q:dim "COTAS" (q:pt 2.00 -0.75) (q:pt 3.20 -0.75) (q:pt 2.60 -1.35))
  (q:dim "COTAS" (q:pt 3.05 0.60) (q:pt 3.05 -0.60) (q:pt 3.48 0.00))
  (q:dim "COTAS" (q:pt 3.20 0.75) (q:pt 3.20 0.60) (q:pt 3.72 0.675))
  (q:dim "COTAS" (q:pt 3.20 -0.60) (q:pt 3.20 -0.75) (q:pt 3.72 -0.675))

  ;; Rótulos.
  (q:text "TEXTO" (q:pt 0.40 0.58) 0.12 "DESCARGA DE QUEBRADA" "MC")
  (q:text "TEXTO" (q:pt 3.45 0.28) 0.12 "MAMPOSTERIA" "ML")
  (q:text "TEXTO" (q:pt 1.60 1.43) 0.18 "PLANTA" "MC"))

(defun q:draw-section ()
  ;; Sección A-A': canal revestido, 1.20 m de fondo y 0.30 m de altura.
  (q:polyline "ESTRUCTURA"
    (list
      (q:pt 0.70 -2.92)
      (q:pt 0.90 -3.40)
      (q:pt 2.30 -3.40)
      (q:pt 2.50 -2.92))
    nil)
  (q:polyline "ESTRUCTURA"
    (list
      (q:pt 0.85 -3.00)
      (q:pt 1.00 -3.30)
      (q:pt 2.20 -3.30)
      (q:pt 2.35 -3.00))
    nil)
  (q:line "ESTRUCTURA" (q:pt 0.70 -2.92) (q:pt 0.85 -3.00))
  (q:line "ESTRUCTURA" (q:pt 2.35 -3.00) (q:pt 2.50 -2.92))
  (q:masonry-marks "HATCH")

  ;; Cotas de la sección.
  (q:dim "COTAS" (q:pt 1.00 -3.30) (q:pt 2.20 -3.30) (q:pt 1.60 -3.72))
  (q:dim "COTAS" (q:pt 2.20 -3.30) (q:pt 2.20 -3.00) (q:pt 2.70 -3.15))
  (q:dim "COTAS" (q:pt 2.35 -3.00) (q:pt 2.50 -3.00) (q:pt 2.425 -2.70))
  (q:text "TEXTO" (q:pt 2.63 -3.46) 0.11 "e = 0.10 m" "ML")
  (q:text "TEXTO" (q:pt 1.60 -2.48) 0.18 "SECCION A-A'" "MC"))

(defun q:draw-notes ()
  (q:text "TEXTO" (q:pt -0.45 -4.22) 0.10 "NOTAS:" "ML")
  (q:text "TEXTO" (q:pt -0.45 -4.40) 0.09 "1. TODAS LAS DIMENSIONES ESTAN EN METROS." "ML")
  (q:text "TEXTO" (q:pt -0.45 -4.55) 0.09 "2. GEOMETRIA DIBUJADA A ESCALA 1:1." "ML")
  (q:text "TEXTO" (q:pt -0.45 -4.70) 0.09 "3. LOS TALUDES NO ACOTADOS SON REPRESENTATIVOS SEGUN EL CROQUIS." "ML"))

(defun c:QUEBRADADWG ()
  (setvar "CMDECHO" 0)
  (setvar "INSUNITS" 6)
  (setvar "LUNITS" 2)
  (setvar "LUPREC" 2)
  (setvar "DIMDEC" 2)
  (setvar "DIMZIN" 0)
  (setvar "DIMTXT" 0.10)
  (setvar "DIMASZ" 0.07)
  (setvar "DIMGAP" 0.025)

  (q:layer "ESTRUCTURA" 7 35)
  (q:layer "COTAS" 2 18)
  (q:layer "TEXTO" 7 18)
  (q:layer "EJES" 4 18)
  (q:layer "CORTE" 1 35)
  (q:layer "HATCH" 8 13)

  (q:draw-plan)
  (q:draw-section)
  (q:draw-notes)
  (command "_.REGEN")
  (princ "\nQUEBRADADWG_CREATED")
  (princ))

(princ "\nComando QUEBRADADWG cargado.")
(princ)
