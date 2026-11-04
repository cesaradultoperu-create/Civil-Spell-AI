;;; SeparadorDeNivelesA.lsp
;;; Crea dos superficies TIN de Civil 3D a partir de dos capas de 3DFACE.
;;;   _face A -> dique superior
;;;   _face B -> terreno natural inferior
;;; Compatible con Civil 3D 2024.

(vl-load-com)

(defun sna:unique-points (points tolerance / point existing result)
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

(defun sna:face-points (entity tolerance)
  (sna:unique-points
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

(defun sna:selection-to-list (selection / index result)
  (setq index 0)
  (if selection
    (repeat (sslength selection)
      (setq result (cons (ssname selection index) result))
      (setq index (1+ index))
    )
  )
  (reverse result)
)

(defun sna:faces-on-layer (layer-name)
  (ssget "_X"
    (list
      '(0 . "3DFACE")
      (cons 8 layer-name)
      (cons 410 (getvar "CTAB"))
    )
  )
)

(defun sna:real-text (value)
  (rtos value 2 12)
)

(defun sna:point-key (point)
  (strcat
    (sna:real-text (car point))
    "|"
    (sna:real-text (cadr point))
    "|"
    (sna:real-text (caddr point))
  )
)

(defun sna:dictionary-value (dictionary key / value)
  (setq value (vlax-get-property dictionary 'Item key))
  (if (= (type value) 'VARIANT)
    (vlax-variant-value value)
    value
  )
)

(defun sna:replace-all (text old-text new-text / position start-position)
  (setq start-position 0)
  (while (setq position (vl-string-search old-text text start-position))
    (setq text (vl-string-subst new-text old-text text position))
    (setq start-position (+ position (strlen new-text)))
  )
  text
)

(defun sna:xml-escape (text)
  (setq text (sna:replace-all text "&" "&amp;"))
  (setq text (sna:replace-all text "<" "&lt;"))
  (setq text (sna:replace-all text ">" "&gt;"))
  (setq text (sna:replace-all text "\"" "&quot;"))
  (sna:replace-all text "'" "&apos;")
)

(defun sna:file-safe-name (text)
  (vl-string-translate "\\/:*?\"<>|" "_________" text)
)

(defun sna:source-directory (/ base-directory source-directory)
  (setq base-directory (getvar "DWGPREFIX"))
  (if (= base-directory "")
    (setq base-directory (getvar "TEMPPREFIX"))
  )
  (setq source-directory
    (strcat
      (vl-string-right-trim "\\/" base-directory)
      "\\Civil3D_SurfaceSources"
    )
  )
  (if (not (vl-file-directory-p source-directory))
    (vl-mkdir source-directory)
  )
  (if (vl-file-directory-p source-directory)
    source-directory
    (getvar "TEMPPREFIX")
  )
)

(defun sna:source-file-path (surface-name / directory)
  (setq directory (sna:source-directory))
  (vl-filename-mktemp
    (strcat (sna:file-safe-name surface-name) "-")
    directory
    ".xml"
  )
)

;;; Devuelve: (ruta cantidad-puntos cantidad-caras)
(defun sna:write-landxml
  (file-path surface-name entities tolerance
    / dictionary point-records face-records next-id entity points ids point
      key point-id point-count face-count stream record)

  (setq dictionary (vlax-create-object "Scripting.Dictionary"))
  (setq sna:*dictionary* dictionary)
  (setq next-id 1)

  (foreach entity entities
    (setq points (sna:face-points entity tolerance))
    (setq ids nil)
    (foreach point points
      (setq key (sna:point-key point))
      (if (= (vlax-invoke-method dictionary 'Exists key) :vlax-true)
        (setq point-id (sna:dictionary-value dictionary key))
        (progn
          (setq point-id next-id)
          (vlax-invoke-method dictionary 'Add key point-id)
          (setq point-records (cons (list point-id point) point-records))
          (setq next-id (1+ next-id))
        )
      )
      (setq ids (cons point-id ids))
    )
    (setq ids (reverse ids))

    (cond
      ((= (length ids) 3)
        (setq face-records (cons ids face-records))
      )
      ((= (length ids) 4)
        (setq face-records
          (cons
            (list (nth 0 ids) (nth 2 ids) (nth 3 ids))
            (cons
              (list (nth 0 ids) (nth 1 ids) (nth 2 ids))
              face-records
            )
          )
        )
      )
    )
  )

  (setq point-records (reverse point-records))
  (setq face-records (reverse face-records))
  (setq point-count (length point-records))
  (setq face-count (length face-records))

  (setq stream (open file-path "w" "utf8"))
  (setq sna:*stream* stream)
  (if stream
    (progn
      (write-line "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" stream)
      (write-line
        (strcat
          "<LandXML xmlns=\"http://www.landxml.org/schema/LandXML-1.2\""
          " version=\"1.2\""
          " date=\""
          (menucmd "M=$(edtime,$(getvar,date),YYYY-MM-DD)")
          "\" time=\""
          (menucmd "M=$(edtime,$(getvar,date),HH:mm:ss)")
          "\" language=\"English\" readOnly=\"false\">"
        )
        stream
      )
      (write-line
        "  <Units><Metric areaUnit=\"squareMeter\" linearUnit=\"meter\" volumeUnit=\"cubicMeter\" temperatureUnit=\"celsius\" pressureUnit=\"milliBars\" diameterUnit=\"millimeter\" angularUnit=\"decimal degrees\" directionUnit=\"decimal degrees\" /></Units>"
        stream
      )
      (write-line "  <Surfaces>" stream)
      (write-line
        (strcat
          "    <Surface name=\""
          (sna:xml-escape surface-name)
          "\" desc=\"Creada desde 3DFACE por SEPARADORNIVELESA\">"
        )
        stream
      )
      (write-line "      <Definition surfType=\"TIN\">" stream)
      (write-line "        <Pnts>" stream)
      (foreach record point-records
        (setq point (cadr record))
        (write-line
          (strcat
            "          <P id=\""
            (itoa (car record))
            "\">"
            (sna:real-text (cadr point))
            " "
            (sna:real-text (car point))
            " "
            (sna:real-text (caddr point))
            "</P>"
          )
          stream
        )
      )
      (write-line "        </Pnts>" stream)
      (write-line "        <Faces>" stream)
      (foreach record face-records
        (write-line
          (strcat
            "          <F>"
            (itoa (nth 0 record))
            " "
            (itoa (nth 1 record))
            " "
            (itoa (nth 2 record))
            "</F>"
          )
          stream
        )
      )
      (write-line "        </Faces>" stream)
      (write-line "      </Definition>" stream)
      (write-line "    </Surface>" stream)
      (write-line "  </Surfaces>" stream)
      (write-line "</LandXML>" stream)
      (close stream)
      (setq sna:*stream* nil)
    )
  )

  (vlax-release-object dictionary)
  (setq sna:*dictionary* nil)
  (if stream
    (list file-path point-count face-count)
    nil
  )
)

(defun sna:get-civil-application (/ acad version result application)
  (setq acad (vlax-get-acad-object))
  (if acad
    (foreach version
      '("13.9" "13.8" "13.7" "13.6" "13.5" "13.4" "13.3" "13.2" "13.1" "13.0")
      (if (null application)
        (progn
          (setq result
            (vl-catch-all-apply
              'vla-GetInterfaceObject
              (list
                acad
                (strcat "AeccXUiLand.AeccApplication." version)
              )
            )
          )
          (if (not (vl-catch-all-error-p result))
            (setq application result)
          )
        )
      )
    )
  )
  application
)

(defun sna:surface-by-name (surfaces surface-name / result)
  (setq result
    (vl-catch-all-apply
      'vlax-get-property
      (list surfaces 'Item surface-name)
    )
  )
  (if (vl-catch-all-error-p result)
    nil
    result
  )
)

(defun sna:import-surface (surfaces file-path / result)
  (setq result
    (vl-catch-all-apply
      'vlax-invoke-method
      (list surfaces 'ImportXML file-path)
    )
  )
  result
)

(defun sna:safe-put (object property value / result)
  (setq result
    (vl-catch-all-apply
      'vlax-put-property
      (list object property value)
    )
  )
  (not (vl-catch-all-error-p result))
)

(defun sna:configure-display-style
  (component display-property visible / display result)
  (setq result
    (vl-catch-all-apply
      'vlax-get-property
      (list component display-property)
    )
  )
  (if (vl-catch-all-error-p result)
    nil
    (progn
      (setq display result)
      (sna:safe-put display 'Visible visible)
      ;; La capa 0 hace que el componente use la capa de la superficie.
      (sna:safe-put display 'Layer "0")
      (sna:release-if-object display)
      T
    )
  )
)

(defun sna:configure-simple-component
  (surface-style component-property visible / component result)
  (setq result
    (vl-catch-all-apply
      'vlax-get-property
      (list surface-style component-property)
    )
  )
  (if (vl-catch-all-error-p result)
    nil
    (progn
      (setq component result)
      (sna:configure-display-style component 'DisplayStylePlan visible)
      (sna:configure-display-style component 'DisplayStyleModel visible)
      (sna:release-if-object component)
      T
    )
  )
)

(defun sna:configure-contours (surface-style / contour result property)
  (setq result
    (vl-catch-all-apply
      'vlax-get-property
      (list surface-style 'ContourStyle)
    )
  )
  (if (vl-catch-all-error-p result)
    nil
    (progn
      (setq contour result)
      (foreach property
        '(MinorContourDisplayStylePlan
          MajorContourDisplayStylePlan
          UserContourDisplayStylePlan
          MinorContourDisplayStyleModel
          MajorContourDisplayStyleModel
          UserContourDisplayStyleModel)
        (sna:configure-display-style contour property :vlax-false)
      )
      (sna:release-if-object contour)
      T
    )
  )
)

(defun sna:configure-boundaries (surface-style / boundary result)
  (setq result
    (vl-catch-all-apply
      'vlax-get-property
      (list surface-style 'BoundaryStyle)
    )
  )
  (if (vl-catch-all-error-p result)
    nil
    (progn
      (setq boundary result)
      (sna:safe-put boundary 'DisplayExteriorBoundaries :vlax-true)
      (sna:safe-put boundary 'DisplayInteriorBoundaries :vlax-false)
      (sna:configure-display-style
        boundary 'DisplayStylePlan :vlax-true
      )
      (sna:configure-display-style
        boundary 'DisplayStyleModel :vlax-true
      )
      (sna:release-if-object boundary)
      T
    )
  )
)

(defun sna:get-or-create-visible-style
  (civil-document style-name / styles style result property)
  (setq styles
    (vlax-get-property civil-document 'SurfaceStyles)
  )
  (setq result
    (vl-catch-all-apply
      'vlax-get-property
      (list styles 'Item style-name)
    )
  )
  (if (vl-catch-all-error-p result)
    (setq result
      (vl-catch-all-apply
        'vlax-invoke-method
        (list styles 'Add style-name)
      )
    )
  )
  (if (vl-catch-all-error-p result)
    (setq style nil)
    (progn
      (setq style result)
      ;; El estilo muestra solo triangulos y el borde, en planta y modelo.
      (sna:configure-simple-component
        style 'TriangleStyle :vlax-true
      )
      (sna:configure-boundaries style)
      (sna:configure-contours style)
      (foreach property
        '(PointStyle GridStyle DirectionStyle ElevationStyle
          SlopeArrowStyle SlopeStyle WatershedStyle)
        (sna:configure-simple-component
          style property :vlax-false
        )
      )
    )
  )
  (sna:release-if-object styles)
  style
)

(defun sna:ensure-layer
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
      (sna:safe-put layer 'Freeze :vlax-false)
      (sna:safe-put layer 'LayerOn :vlax-true)
      (sna:safe-put layer 'Lock :vlax-false)
      (sna:safe-put layer 'Color color)
    )
  )
  (sna:release-if-object layers)
  layer
)

(defun sna:assign-surface-appearance
  (surface surface-style style-name layer / result)
  (setq result
    (and surface surface-style layer
      (sna:safe-put surface 'Visible :vlax-true)
      (sna:safe-put surface 'Layer (vla-get-Name layer))
    )
  )
  (if result
    (progn
      (setq result
        (vl-catch-all-apply
          'vlax-put-property
          (list surface 'Style surface-style)
        )
      )
      (if (vl-catch-all-error-p result)
        (setq result
          (vl-catch-all-apply
            'vlax-put-property
            (list surface 'Style style-name)
          )
        )
      )
      (setq result (not (vl-catch-all-error-p result)))
    )
  )
  result
)

(defun sna:set-source-layer-visible
  (document layer-name visible / layers layer result)
  (setq layers (vla-get-Layers document))
  (setq result
    (vl-catch-all-apply 'vla-Item (list layers layer-name))
  )
  (if (not (vl-catch-all-error-p result))
    (progn
      (setq layer result)
      (sna:safe-put layer 'LayerOn visible)
      (sna:release-if-object layer)
    )
  )
  (sna:release-if-object layers)
)

(defun sna:release-if-object (object)
  (if (= (type object) 'VLA-OBJECT)
    (vl-catch-all-apply 'vlax-release-object (list object))
  )
)

(defun c:SEPARADORNIVELESA
  (/ *error* old-error document undo-open layer-a layer-b selection-a
    selection-b faces-a faces-b name-a name-b answer civil-application
    civil-document surfaces existing-a existing-b file-a file-b export-a
    export-b surface-a surface-b import-error surface-style style-name
    output-layer-a output-layer-b appearance-a appearance-b)

  (setq old-error *error*)
  (setq document (vla-get-ActiveDocument (vlax-get-acad-object)))
  (defun *error* (message)
    (if sna:*stream*
      (progn
        (close sna:*stream*)
        (setq sna:*stream* nil)
      )
    )
    (if sna:*dictionary*
      (progn
        (sna:release-if-object sna:*dictionary*)
        (setq sna:*dictionary* nil)
      )
    )
    (if undo-open
      (progn
        (vl-catch-all-apply 'vla-EndUndoMark (list document))
        (setq undo-open nil)
      )
    )
    (sna:release-if-object surfaces)
    (sna:release-if-object civil-document)
    (sna:release-if-object civil-application)
    (sna:release-if-object surface-style)
    (sna:release-if-object output-layer-a)
    (sna:release-if-object output-layer-b)
    (setq *error* old-error)
    (if (and message (not (wcmatch (strcase message) "*CANCEL*,*QUIT*,*EXIT*")))
      (princ (strcat "\nError: " message))
    )
    (princ)
  )

  (setq layer-a "_face A")
  (setq layer-b "_face B")
  (setq selection-a (sna:faces-on-layer layer-a))
  (setq selection-b (sna:faces-on-layer layer-b))

  (cond
    ((or (null selection-a) (= (sslength selection-a) 0))
      (princ (strcat "\nNo se encontraron 3DFACE en la capa " layer-a "."))
    )
    ((or (null selection-b) (= (sslength selection-b) 0))
      (princ (strcat "\nNo se encontraron 3DFACE en la capa " layer-b "."))
    )
    (T
      (setq faces-a (sna:selection-to-list selection-a))
      (setq faces-b (sna:selection-to-list selection-b))
      (princ
        (strcat
          "\nDatos encontrados: "
          (itoa (length faces-a))
          " cara(s) en "
          layer-a
          " y "
          (itoa (length faces-b))
          " cara(s) en "
          layer-b
          "."
        )
      )

      (setq name-a
        (getstring T
          "\nNombre de la superficie A <SEPARADOR DE NIVELES A>: "
        )
      )
      (if (= name-a "")
        (setq name-a "SEPARADOR DE NIVELES A")
      )
      (setq name-b
        (getstring T
          "\nNombre de la superficie B <TERRENO NATURAL B>: "
        )
      )
      (if (= name-b "")
        (setq name-b "TERRENO NATURAL B")
      )

      (cond
        ((= (strcase name-a) (strcase name-b))
          (princ "\nLas dos superficies deben tener nombres diferentes.")
        )
        (T
          (setq civil-application (sna:get-civil-application))
          (if (null civil-application)
            (princ
              "\nNo se pudo acceder a Civil 3D. Ejecute el comando dentro de Civil 3D 2024."
            )
            (progn
              (setq civil-document
                (vlax-get-property civil-application 'ActiveDocument)
              )
              (setq surfaces (vlax-get-property civil-document 'Surfaces))
              (setq existing-a (sna:surface-by-name surfaces name-a))
              (setq existing-b (sna:surface-by-name surfaces name-b))

              (cond
                (existing-a
                  (princ
                    (strcat
                      "\nYa existe una superficie llamada \""
                      name-a
                      "\". No se modifico nada."
                    )
                  )
                )
                (existing-b
                  (princ
                    (strcat
                      "\nYa existe una superficie llamada \""
                      name-b
                      "\". No se modifico nada."
                    )
                  )
                )
                (T
                  (initget "Si No")
                  (setq answer
                    (getkword
                      "\nCrear las dos superficies TIN conservando las caras [Si/No] <No>: "
                    )
                  )
                  (if (= answer "Si")
                    (progn
                      (princ "\nPreparando la superficie A. Espere...")
                      (setq file-a (sna:source-file-path name-a))
                      (setq export-a
                        (sna:write-landxml file-a name-a faces-a 1.0e-8)
                      )
                      (princ "\nPreparando la superficie B. Espere...")
                      (setq file-b (sna:source-file-path name-b))
                      (setq export-b
                        (sna:write-landxml file-b name-b faces-b 1.0e-8)
                      )

                      (if (or (null export-a) (null export-b))
                        (progn
                          (if file-a (vl-file-delete file-a))
                          (if file-b (vl-file-delete file-b))
                          (princ
                            "\nNo fue posible preparar los archivos de las superficies."
                          )
                        )
                        (progn
                          (vla-StartUndoMark document)
                          (setq undo-open T)
                          (princ "\nCreando la superficie TIN A...")
                          (setq surface-a
                            (sna:import-surface surfaces file-a)
                          )
                          (if (vl-catch-all-error-p surface-a)
                            (progn
                              (setq import-error
                                (vl-catch-all-error-message surface-a)
                              )
                              (setq surface-a nil)
                            )
                            (progn
                              (princ "\nCreando la superficie TIN B...")
                              (setq surface-b
                                (sna:import-surface surfaces file-b)
                              )
                              (if (vl-catch-all-error-p surface-b)
                                (progn
                                  (setq import-error
                                    (vl-catch-all-error-message surface-b)
                                  )
                                  (setq surface-b nil)
                                  (vl-catch-all-apply
                                    'vla-Delete
                                    (list surface-a)
                                  )
                                  (setq surface-a nil)
                                )
                              )
                            )
                          )

                          (if import-error
                            (progn
                              (vla-EndUndoMark document)
                              (setq undo-open nil)
                              (vl-file-delete file-a)
                              (vl-file-delete file-b)
                              (princ
                                (strcat
                                  "\nCivil 3D no pudo importar las superficies: "
                                  import-error
                                )
                              )
                            )
                            (progn
                              (setq style-name
                                "SNA - TRIANGULOS VISIBLES"
                              )
                              (setq surface-style
                                (sna:get-or-create-visible-style
                                  civil-document style-name
                                )
                              )
                              (setq output-layer-a
                                (sna:ensure-layer
                                  document "SNA-SUPERFICIE-A" 60
                                )
                              )
                              (setq output-layer-b
                                (sna:ensure-layer
                                  document "SNA-SUPERFICIE-B" 130
                                )
                              )
                              (setq appearance-a
                                (sna:assign-surface-appearance
                                  surface-a surface-style style-name
                                  output-layer-a
                                )
                              )
                              (setq appearance-b
                                (sna:assign-surface-appearance
                                  surface-b surface-style style-name
                                  output-layer-b
                                )
                              )
                              (vl-catch-all-apply
                                'vlax-invoke-method
                                (list surface-a 'Rebuild)
                              )
                              (vl-catch-all-apply
                                'vlax-invoke-method
                                (list surface-b 'Rebuild)
                              )
                              ;; Oculta solamente las caras fuente. Siguen en el
                              ;; dibujo y pueden volver a encenderse cuando se desee.
                              (if (and appearance-a appearance-b)
                                (progn
                                  (sna:set-source-layer-visible
                                    document layer-a :vlax-false
                                  )
                                  (sna:set-source-layer-visible
                                    document layer-b :vlax-false
                                  )
                                )
                              )
                              (vla-EndUndoMark document)
                              (setq undo-open nil)
                              (vla-Regen document 1)
                              (princ
                                (strcat
                                  "\nSuperficies creadas correctamente:"
                                  "\n  "
                                  name-a
                                  ": "
                                  (itoa (nth 2 export-a))
                                  " triangulo(s)."
                                  "\n  "
                                  name-b
                                  ": "
                                  (itoa (nth 2 export-b))
                                  " triangulo(s)."
                                )
                              )
                              (princ
                                (strcat
                                  "\nArchivos fuente LandXML conservados en: "
                                  (sna:source-directory)
                                )
                              )
                              (princ
                                "\nLas 3DFACE originales no fueron modificadas."
                              )
                              (if (and appearance-a appearance-b)
                                (princ
                                  (strcat
                                    "\nVisualizacion preparada automaticamente:"
                                    " estilo " style-name
                                    ", capas SNA-SUPERFICIE-A y "
                                    "SNA-SUPERFICIE-B."
                                    "\nLas capas _face A y _face B quedaron apagadas,"
                                    " pero sus objetos se conservaron."
                                  )
                                )
                                (princ
                                  "\nAviso: las superficies se crearon, pero Civil 3D no permitio configurar toda su visualizacion automaticamente."
                                )
                              )
                            )
                          )
                        )
                      )
                    )
                    (princ "\nOperacion cancelada. No se modifico nada.")
                  )
                )
              )
            )
          )
        )
      )
    )
  )

  (sna:release-if-object existing-a)
  (sna:release-if-object existing-b)
  (sna:release-if-object surface-a)
  (sna:release-if-object surface-b)
  (sna:release-if-object surfaces)
  (sna:release-if-object civil-document)
  (sna:release-if-object civil-application)
  (sna:release-if-object surface-style)
  (sna:release-if-object output-layer-a)
  (sna:release-if-object output-layer-b)
  (setq *error* old-error)
  (princ)
)

(princ
  "\nSeparador de niveles A cargado. Comando: SEPARADORNIVELESA"
)
(princ)
