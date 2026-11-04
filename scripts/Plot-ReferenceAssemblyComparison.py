"""Plot native TOP projections and central FRONT sections from the copy test."""
import argparse
import json
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np
from matplotlib.collections import LineCollection, PolyCollection
from matplotlib.lines import Line2D
from matplotlib.colors import to_rgb


def faces(body):
    vertices = np.asarray(body["Vertices"])
    indices = body["Faces"]
    offset = 0
    while offset < len(indices):
        count = indices[offset]
        yield vertices[indices[offset + 1:offset + 1 + count]]
        offset += count + 1


def color(name):
    if "Mampostería" in name:
        return "#af8656"
    if "arena" in name or "Solado" in name:
        return "#aaa881"
    if "Tubería" in name:
        return "#2986ad"
    return "#8ca5b3"


def top_projection(ax, bodies):
    polygons = []
    for body in bodies:
        low, high = body["Minimum"][2], body["Maximum"][2]
        for face in faces(body):
            area = np.cross(face[1] - face[0], face[2] - face[0])
            if abs(area[2]) > 1e-12:
                elevation = face[:, 2].mean()
                blend = 0.65 * (1 - (elevation - low) / max(high - low, 1e-12))
                shade = tuple(value + (1 - value) * blend for value in to_rgb(color(body["Name"])))
                polygons.append((elevation, face[:, :2], shade))
    polygons.sort(key=lambda entry: entry[0])
    ax.add_collection(PolyCollection([entry[1] for entry in polygons],
                                    facecolors=[entry[2] for entry in polygons],
                                    edgecolors="none"))


def front_section(ax, bodies, y=0.65):
    for body in bodies:
        segments = []
        for face in faces(body):
            hits = []
            for a, b in zip(face, np.roll(face, -1, axis=0)):
                da, db = a[1] - y, b[1] - y
                if abs(da) < 1e-9:
                    hits.append(a[[0, 2]])
                if da * db < 0:
                    point = a + (b - a) * (-da / (db - da))
                    hits.append(point[[0, 2]])
            unique = []
            for hit in hits:
                if not any(np.linalg.norm(hit - point) < 1e-8 for point in unique):
                    unique.append(hit)
            if len(unique) == 2:
                segments.append(unique)
        ax.add_collection(LineCollection(segments, colors=color(body["Name"]), linewidths=2.5))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("comparison_json", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    data = json.loads(args.comparison_json.read_text(encoding="utf-8-sig"))
    if not data["Reconstructed"] or not data["Approximate"]:
        raise ValueError("The native reconstruction and approximate extrusion must both be present.")
    fig, axes = plt.subplots(2, 2, figsize=(14, 9), constrained_layout=True)
    for col, key in enumerate(("Reconstructed", "Approximate")):
        bodies = data[key]
        top_projection(axes[0, col], bodies)
        front_section(axes[1, col], bodies)
        for row, reference in enumerate(("Top", "Front")):
            axes[row, col].add_collection(LineCollection(data[reference], colors="#30343b",
                                                        linewidths=0.7, linestyles="dashed", alpha=0.7))
            axes[row, col].set(xlim=(-0.6, 2.3), ylim=((-0.12, 1.42) if row == 0 else (-0.45, 1.05)),
                              xlabel="X (m)", ylabel=("Y (m)" if row == 0 else "Z (m)"), aspect="equal")
            axes[row, col].grid(alpha=0.15)
        label = "Reconstrucción TOP + FRONT" if col == 0 else "Extrusión aproximada de César"
        axes[0, col].set_title(label + " · planta")
        axes[1, col].set_title(label + " · corte central Y=0,65 m")
    legend = [Line2D([0], [0], color="#30343b", ls="--", label="Referencias 2D seleccionadas"),
              Line2D([0], [0], color="#8ca5b3", lw=3, label="Concreto / extrusión aproximada"),
              Line2D([0], [0], color="#af8656", lw=3, label="Mampostería"),
              Line2D([0], [0], color="#aaa881", lw=3, label="Solado / cama de arena"),
              Line2D([0], [0], color="#2986ad", lw=3, label="Tubería")]
    fig.legend(handles=legend, loc="outside lower center", ncol=3, frameon=False)
    fig.suptitle("Comparación geométrica · copia de las referencias guardadas el 05/10/2026\n"
                 "FRONT se muestra como corte central; las paredes laterales pueden aparecer en la referencia.", fontsize=13)
    fig.supxlabel("Extrusión aproximada alineada Z=−0,05 m. Contornos entre cortes: longitud visible, pendiente geométrica conservada.", fontsize=9)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    fig.savefig(args.output.with_suffix(".png"), dpi=170)
    fig.savefig(args.output.with_suffix(".pdf"))
    plt.close(fig)
    print(args.output.with_suffix(".png"))


if __name__ == "__main__":
    main()
