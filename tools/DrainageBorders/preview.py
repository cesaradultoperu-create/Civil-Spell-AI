"""Render actual AutoCAD exports; also compare Stage 1's manual junctions."""
import argparse
from pathlib import Path
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.collections import LineCollection
import numpy as np


def read(path):
    result = []
    for row in path.read_text(encoding='utf-8-sig').splitlines():
        kind, handle, z, closed, vertices = row.split('|')
        points = np.array([list(map(float, p.split(','))) for p in vertices.split(';')])
        if closed == 'True':
            points = np.vstack([points, points[0]])
        result.append(dict(kind=kind, handle=handle, z=float(z), closed=closed == 'True', points=points))
    return result


def draw(ax, data, limits=None, title=None, context=False):
    ax.set_facecolor('#202a32')
    for kind, color, width in [('context', '#63717b', .45), ('S', '#00c5ed', .65), ('B', '#4775ff', 1.0)]:
        paths = [item['points'] for item in data if (
            (kind == 'context' and item['kind'] == 'S' and (item['z'] != 0 or item['closed'])) or
            (kind == 'S' and item['kind'] == 'S' and item['z'] == 0 and not item['closed']) or
            (kind == 'B' and item['kind'] == 'B'))]
        if kind != 'context' or context:
            ax.add_collection(LineCollection(paths, colors=color, linewidths=width))
    ax.autoscale()
    if limits:
        ax.set_xlim(limits[0], limits[1]); ax.set_ylim(limits[2], limits[3])
    ax.set_aspect('equal', adjustable='box')
    ax.tick_params(colors='#91a4b3', labelsize=7)
    ax.ticklabel_format(useOffset=False, style='plain')
    for spine in ax.spines.values():
        spine.set_color('#344755')
    if title: ax.set_title(title, color='white', fontsize=12, pad=10)


def main(folder):
    reference = read(folder / 'stage-1-reference.txt')
    automatic = read(folder / 'stage-1-automatic.txt')
    errors = []
    fig, axes = plt.subplots(2, 2, figsize=(13, 10), facecolor='#121b22')
    for i, (cx, cy, name) in enumerate([(370422.73, 6964086.665, 'Union en T (ejemplo 3.2)'),
                                      (370219.675, 6964095.227, 'Encuentro multiple (ejemplo 4.2)')]):
        limits = [cx-8, cx+8, cy-5, cy+5]
        # Measure both directions inside each completed manual example.
        for left, right in [(reference, automatic), (automatic, reference)]:
            segments = np.concatenate([np.stack([p['points'][:-1], p['points'][1:]], axis=1)
                                       for p in right if p['kind'] == 'B'])
            a, b = segments[:, 0], segments[:, 1]
            delta = b-a
            lengths2 = (delta**2).sum(axis=1)
            worst = 0.0
            for item in left:
                if item['kind'] != 'B':
                    continue
                for p, q in zip(item['points'][:-1], item['points'][1:]):
                    samples = p + (q-p)*np.linspace(0, 1, max(2, int(np.linalg.norm(q-p)/.1)+1))[:, None]
                    samples = samples[(samples[:, 0] >= limits[0]) & (samples[:, 0] <= limits[1]) &
                                      (samples[:, 1] >= limits[2]) & (samples[:, 1] <= limits[3])]
                    for point in samples:
                        t = np.clip(((point-a)*delta).sum(axis=1)/lengths2, 0, 1)
                        distance = np.linalg.norm(a + delta*t[:, None] - point, axis=1).min()
                        worst = max(worst, distance)
            errors.append(worst)
            if worst > .00001:
                raise AssertionError(f'{name}: manual/automatic mismatch: {worst}')
        draw(axes[i, 0], reference, limits, 'Stage 1 manual - ' + name)
        draw(axes[i, 1], automatic, limits, 'Stage 1 automatico - ' + name)
    fig.suptitle('Referencia manual y resultado de la herramienta | offset 0.50 a cada lado', color='white', fontsize=14)
    fig.tight_layout(rect=[0, 0, 1, .95])
    fig.savefig(folder / 'Comparacion-Stage-1.png', dpi=170)
    plt.close(fig)
    fig, axes = plt.subplots(1, 2, figsize=(14, 9), facecolor='#121b22')
    for stage, ax in zip([2, 3], axes):
        draw(ax, read(folder / f'stage-{stage}.txt'), title=f'Drainage Stage {stage} | bordes generados', context=True)
    fig.suptitle('Ejes: celeste | 0_DCL_Bordes: azul | referencias conservadas: gris', color='white', fontsize=14)
    fig.tight_layout(rect=[0, 0, 1, .95])
    fig.savefig(folder / 'Resultado-Stage-2-y-3.png', dpi=170)
    plt.close(fig)
    (folder / 'reference-comparison.txt').write_text(
        'Stage 1, ejemplos 3.2 y 4.2: comparacion bidireccional de puntos cada 0.10.\n' +
        'Distancia maxima entre bordes manuales y automaticos: ' + str(max(errors)) + '\n', encoding='utf-8')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('folder', type=Path)
    main(parser.parse_args().folder)
