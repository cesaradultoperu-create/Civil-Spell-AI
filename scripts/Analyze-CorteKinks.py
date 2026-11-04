"""Read the CSV exported by Test/inspect-corte-model.scr; never write a DWG."""
import argparse
import json
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import Rectangle
import numpy as np


def read_model(path):
    model = {}
    current = None
    for line in path.read_text().splitlines():
        row = line.split("|")
        if row[0] == "P":
            current = {"closed": bool(int(row[2]) & 1), "z": float(row[3]),
                       "normal": tuple(map(float, row[4:7])), "points": [], "bulges": []}
            model[row[1]] = current
        elif row[0] == "V":
            current["points"].append(tuple(map(float, row[2:4])))
        elif row[0] == "B":
            current["bulges"].append(float(row[1]))
    return model


def segment_arrays(model):
    starts, ends, owners, indices = [], [], [], []
    for handle, poly in model.items():
        if poly["normal"] != (0.0, 0.0, 1.0):
            raise ValueError(f"Non-horizontal OCS needs a transform: {handle}")
        points = poly["points"]
        for index in range(len(points) if poly["closed"] else len(points) - 1):
            # Tiny stored bulges are treated as straight by the plugin too.
            if abs(poly["bulges"][index]) > 1e-12:
                raise ValueError(f"Arc requires separate geometry: {handle}/{index}")
            starts.append((*points[index], poly["z"]))
            ends.append((*points[(index + 1) % len(points)], poly["z"]))
            owners.append(handle)
            indices.append(index)
    return np.array(starts), np.array(ends), np.array(owners), np.array(indices)


def target_overlaps(starts, ends, owners, indices, handle, tolerance):
    lengths = np.linalg.norm(ends - starts, axis=1)
    pairs = set()
    for target in np.flatnonzero(owners == handle):
        if lengths[target] <= tolerance:
            continue
        unit = (ends[target] - starts[target]) / lengths[target]
        da, db = starts - starts[target], ends - starts[target]
        collinear = ((np.linalg.norm(np.cross(da, unit), axis=1) <= tolerance)
                     & (np.linalg.norm(np.cross(db, unit), axis=1) <= tolerance))
        ta, tb = da @ unit, db @ unit
        shared = np.minimum(lengths[target], np.maximum(ta, tb)) - np.maximum(0, np.minimum(ta, tb))
        matches = np.flatnonzero(collinear & (shared > tolerance) & (lengths > tolerance))
        for other in matches:
            if other != target:
                pairs.add(tuple(sorted(((owners[target], int(indices[target])),
                                        (owners[other], int(indices[other]))))))
    return sorted(pairs)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("run", type=Path)
    parser.add_argument("--handle", default="5008F")
    args = parser.parse_args()
    model = read_model(args.run / "model.csv")
    starts, ends, owners, indices = segment_arrays(model)
    target = model[args.handle]
    p = np.array(target["points"])
    relative = p - p[0]
    ring = np.vstack([relative, relative[0]])
    area = abs(np.sum(ring[:-1, 0] * ring[1:, 1] - ring[1:, 0] * ring[:-1, 1])) / 2
    perimeter = float(np.sum(np.linalg.norm(np.diff(ring, axis=0), axis=1)))
    tolerance = 1e-6
    pairs = target_overlaps(starts, ends, owners, indices, args.handle, tolerance)

    # Explicitly identified case region, not an inferred pixel-to-world mapping.
    selected_indices = list(range(172, 179)) + [0]
    region = relative[selected_indices]
    unit = (region[-1] - region[0]) / np.linalg.norm(region[-1] - region[0])
    along = (region - region[0]) @ unit
    lateral = (region[:, 0] - region[0, 0]) * unit[1] - (region[:, 1] - region[0, 1]) * unit[0]

    fig, (overview, detail) = plt.subplots(1, 2, figsize=(12, 5), layout="constrained")
    overview.fill(ring[:, 0], ring[:, 1], color="#d8eaf8", alpha=0.6)
    overview.plot(ring[:, 0], ring[:, 1], color="#1466a4", linewidth=1.7)
    overview.scatter(region[:, 0], region[:, 1], s=18, color="#c3511c")
    overview.add_patch(Rectangle((-0.15, -0.15), 1.2, 1.4, fill=False, color="#c3511c"))
    overview.set_aspect("equal")
    overview.set(xlabel="X respecto al primer vértice (unidades del dibujo)",
                 ylabel="Y respecto al primer vértice (unidades del dibujo)",
                 title=f"Entidad {args.handle}: contorno cerrado, {len(p)} vértices")
    overview.grid(alpha=0.2)
    detail.plot(along, lateral * 1000, "o-", color="#c3511c", label="Geometría guardada")
    detail.plot([along[0], along[-1]], [0, 0], "--", color="#1466a4", label="Referencia recta entre extremos")
    for i, x, y in zip(selected_indices, along, lateral * 1000):
        detail.annotate(f"P{i + 1}", (x, y), xytext=(0, 9 if i % 2 else -16),
                        textcoords="offset points", fontsize=8, ha="center")
    detail.set(xlabel="Distancia longitudinal (unidades del dibujo)",
               ylabel="Desvío lateral ×1000 (mm si la unidad es metro)",
               title="Detalle P173 → P1: escala lateral ampliada")
    detail.set_ylim(-16, 2)
    detail.legend(loc="lower center", fontsize=8)
    detail.grid(alpha=0.2)
    fig.savefig(args.run / "quiebres-5008F.png", dpi=180)
    plt.close(fig)

    result = {"handle": args.handle, "vertices": len(p), "closed": target["closed"],
              "elevation": target["z"], "area": float(area), "length": perimeter,
              "model_polylines": len(model), "model_segments": len(starts),
              "tolerance": tolerance, "target_overlap_pairs_internal_or_external": pairs,
              "region_indices_zero_based": selected_indices,
              "region_straight_chord_max_deviation": float(np.max(np.abs(lateral)))}
    (args.run / "measurements.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
