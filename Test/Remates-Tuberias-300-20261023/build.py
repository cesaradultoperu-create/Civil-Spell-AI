"""Prepare this drawing's checked cap placement manifest and AutoLISP command."""
import json, math
from pathlib import Path
ROOT=Path(__file__).resolve().parent
a=json.loads((ROOT/'analysis.json').read_text())
sample=a['sample']; anchor=a['anchor']
ref=next(r for r in a['records'] if r['handle']==a['nearest_sample'][1])
refend=a['nearest_sample'][2]
def angle(p,q): return math.atan2(q[1]-p[1],q[0]-p[0])
refangle=angle(ref['points'][refend],ref['points'][1-refend])
specs=[]
for r in a['records']:
    connected=[j for j,d in enumerate(r['nearest']) if d[0]<=.15]
    assert len(connected)==1,r
    end=1-connected[0]; p=r['points'][end]
    rotation=angle(p,r['points'][1-end])-refangle
    c,s=math.cos(rotation),math.sin(rotation)
    vs=[[p[0]+c*(v[0]-anchor[0])-s*(v[1]-anchor[1]),p[1]+s*(v[0]-anchor[0])+c*(v[1]-anchor[1])] for v in sample]
    if r['handle']==ref['handle']: vs=sample
    specs.append(dict(handle=r['handle'],pipe=r['points'],end=end,vertices=vs,elevation=r['z']))
(ROOT/'placement-manifest.json').write_text(json.dumps(specs,indent=2))
def lisp(v):
    if isinstance(v,str): return json.dumps(v)
    if isinstance(v,list): return '('+' '.join(lisp(x) for x in v)+')'
    return format(v,'.12f') if isinstance(v,float) else str(v)
data='(setq rt300-specs \'('+ '\n'.join(lisp([r['handle'],r['pipe'],r['vertices'],r['elevation']]) for r in specs)+'))\n'
code=r'''; Remates para Xref_Tuberias de coleccion_copy.dwg.
; Comando: REMATES300. No guarda el archivo. Incluye comprobacion de duplicados.
(defun rt300-points (data / ps)
  (foreach item data (if (= (car item) 10) (setq ps (cons (cdr item) ps))))
  (reverse ps))
(defun rt300-same (a b / ok)
  (setq ok (= (length a) (length b)))
  (while (and ok a) (setq ok (equal (car a) (car b) 0.000001) a (cdr a) b (cdr b))) ok)
(defun rt300-existing (/ ss i e data found)
  (setq ss (ssget "_X" '((0 . "LWPOLYLINE") (8 . "WWW_Tub. 300 Perf.") (410 . "Model"))) i 0)
  (if ss (repeat (sslength ss)
    (setq e (ssname ss i) i (1+ i) data (entget e))
    (if (= (cdr (assoc 90 data)) 4)
      (setq found (cons (list (rt300-points data) (cdr (assoc 38 data))) found))))) found)
(defun rt300-has (ps z caps / found)
  (foreach cap caps
    (if (and (equal z (cadr cap) 0.000001)
      (or (rt300-same ps (car cap)) (rt300-same ps (reverse (car cap))))) (setq found T))) found)
(defun rt300-entity (template ps z / result)
  (foreach item template
    (cond ((member (car item) '(-1 5 330)) nil)
      ((= (car item) 10) (setq result (cons (cons 10 (car ps)) result) ps (cdr ps)))
      ((= (car item) 38) (setq result (cons (cons 38 z) result)))
      (T (setq result (cons item result)))))
  (if (entmake (reverse result)) (entlast)))
(defun c:REMATES300 (/ *error* template e data valid caps made started item)
  (defun *error* (msg)
    (foreach e made (if (entget e) (entdel e)))
    (if started (command-s "_.UNDO" "_End"))
    (princ (strcat "\nREMATES300 cancelado: " msg)) (princ))
  (setq rt300-created 0 rt300-skipped 0 valid T)
  (if (and (setq e (handent "1A491"))
           (= (cdr (assoc 8 (entget e))) "WWW_Tub. 300 Perf.")
           (= (cdr (assoc 90 (entget e))) 4))
    (setq template (entget e)) (setq valid nil))
  (foreach spec rt300-specs
    (if (not (and (setq e (handent (car spec)))
      (= (cdr (assoc 8 (setq data (entget e)))) "WWW_Tub. 300 Perf.")
      (= (cdr (assoc 90 data)) 2)
      (rt300-same (rt300-points data) (cadr spec))
      (equal (cdr (assoc 38 data)) (cadddr spec) 0.000001))) (setq valid nil)))
  (if (not valid)
    (princ "\nREMATES300: el dibujo no coincide con la copia revisada. No se hizo ningun cambio.")
    (progn
      (setq caps (rt300-existing))
      (command-s "_.UNDO" "_Begin") (setq started T)
      (foreach spec rt300-specs
        (if (rt300-has (caddr spec) (cadddr spec) caps)
          (setq rt300-skipped (1+ rt300-skipped))
          (if (setq e (rt300-entity template (caddr spec) (cadddr spec)))
            (setq made (cons e made) rt300-created (1+ rt300-created)
              caps (cons (list (caddr spec) (cadddr spec)) caps))
            (error "No se pudo crear un remate."))))
      (command-s "_.UNDO" "_End") (setq started nil)
      (princ (strcat "\nREMATES300_OK: creados=" (itoa rt300-created) "; existentes=" (itoa rt300-skipped)))))
  (princ))
'''
(ROOT/'Remates300.lsp').write_text(data+code+'\n(princ "\\nComando disponible: REMATES300")\n(princ)\n',encoding='ascii')
root=ROOT.as_posix()
script=f'''(setvar "SECURELOAD" 0)
(load "{root}/Remates300.lsp")
REMATES300
(if (and (= rt300-created 85) (= rt300-skipped 1)) (princ "FIRST_RUN_OK") (princ "FIRST_RUN_FAILED"))
REMATES300
(if (and (= rt300-created 0) (= rt300-skipped 86)) (princ "DUPLICATE_CHECK_OK") (princ "DUPLICATE_CHECK_FAILED"))
_.QSAVE
_.QUIT

'''
(ROOT/'apply.scr').write_text(script,encoding='ascii')
print('Manifest:',len(specs),'caps; expected additions:',len(specs)-1)
