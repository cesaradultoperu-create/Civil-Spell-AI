"""Independent checks on exported before/after geometry, without editing DWGs."""
import argparse
import json
import runpy
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import Polygon
import numpy as np

read_model = runpy.run_path(str(Path(__file__).with_name("Analyze-CorteKinks.py")))["read_model"]


def crossing_count(points, closed):
    ring = np.vstack([points, points[0]]) if closed else points
    a, b = ring[:-1], ring[1:]
    count = 0
    cross = lambda x, y: x[..., 0] * y[..., 1] - x[..., 1] * y[..., 0]
    for i in range(len(a) - 2):
        j = np.arange(i + 2, len(a))
        if closed and i == 0:
            j = j[j != len(a) - 1]
        da, db = b[i] - a[i], b[j] - a[j]
        s = cross(da, a[j] - a[i]) * cross(da, b[j] - a[i])
        t = cross(db, a[i] - a[j]) * cross(db, b[i] - a[j])
        count += int(np.sum((s < 0) & (t < 0)))
    return count


def short_count(model, threshold):
    count = 0
    for poly in model.values():
        p = np.array(poly["points"])
        ring = np.vstack([p, p[0]]) if poly["closed"] else p
        count += int(np.sum(np.linalg.norm(np.diff(ring, axis=0), axis=1) <= threshold))
    return count


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("before", type=Path)
    parser.add_argument("after", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    before, after = read_model(args.before), read_model(args.after)
    assert before.keys() == after.keys(), "Source entity identifiers changed"
    changes = []
    limit = 0.0015
    for handle, old in before.items():
        new = after[handle]
        assert (old["closed"], old["z"], old["normal"]) == (new["closed"], new["z"], new["normal"])
        if old["points"] == new["points"]:
            assert old["bulges"] == new["bulges"]
            continue
        p, q = np.array(old["points"]), np.array(new["points"])
        # Match the ordered cyclic subsequence, including repeated coordinates.
        # A dictionary of coordinates loses distinct visits to the same point.
        retained = None
        for first in np.flatnonzero(np.all(p == q[0], axis=1)):
            candidate, cursor = [int(first)], int(first) + 1
            for point in q[1:]:
                stop = int(first) + len(p) if old["closed"] else len(p)
                while cursor < stop and not np.array_equal(p[cursor % len(p)], point):
                    cursor += 1
                if cursor == stop:
                    break
                candidate.append(cursor % len(p))
                cursor += 1
            if len(candidate) == len(q):
                retained = candidate
                break
        assert retained is not None, f"After is not an ordered source subsequence: {handle}"
        maximum = 0
        for i in range(len(q) if old["closed"] else len(q) - 1):
            a, b = retained[i], retained[(i + 1) % len(q)]
            steps = (b - a + len(p)) % len(p)
            chord = p[b] - p[a]
            squared = chord @ chord
            for offset in range(1, steps):
                point = p[(a + offset) % len(p)]
                t = np.clip((point - p[a]) @ chord / squared, 0, 1) if squared else 0
                maximum = max(maximum, float(np.linalg.norm(point - (p[a] + t * chord))))
        assert maximum <= limit + 1e-9, f"Displacement limit exceeded: {handle}"
        old_crossings, new_crossings = crossing_count(p, old["closed"]), crossing_count(q, new["closed"])
        if old_crossings == 0:
            assert new_crossings == 0, f"New self-crossing: {handle}"
        changes.append({"handle": handle, "before": len(p), "after": len(q),
                        "removed_original_indices_zero_based": sorted(set(range(len(p))) - set(retained)),
                        "maximum_displacement": maximum,
                        "crossings_before": old_crossings, "crossings_after": new_crossings})
    result = {"polylines": len(before), "changed": len(changes),
              "vertices_removed": sum(c["before"] - c["after"] for c in changes),
              "short_threshold": limit, "short_before": short_count(before, limit),
              "short_after": short_count(after, limit), "changes": changes}
    assert result["short_after"] == 0
    args.output.mkdir(parents=True, exist_ok=True)
    (args.output / "cleanup-comparison.json").write_text(json.dumps(result, indent=2), encoding="utf-8")

    p, q = np.array(before["5008F"]["points"]), np.array(after["5008F"]["points"])
    box = np.array(before["5008D"]["points"])
    origin = p[16]
    fig, axes = plt.subplots(1, 3, figsize=(13, 4.6), layout="constrained")
    for ax, title in zip(axes, ["Polilínea completa: 179 → 175 vértices", "Zona del cuadro rojo", "Microtramo retirado: 0.0001586 unidades"]):
        for points, color, label in [(p, "#2378ae", "Antes"), (q, "#d15823", "Después")]:
            ring = np.vstack([points, points[0]]) - origin
            factor = 1000 if ax is axes[2] else 1
            ax.plot(ring[:, 0] * factor, ring[:, 1] * factor, color=color, linewidth=1.5,
                    marker="o" if ax is axes[2] else None, markersize=4, label=label)
        ax.set_aspect("equal")
        ax.grid(alpha=0.2)
        ax.set_title(title, fontsize=10)
        ax.set_xlabel("X relativo (unidades del dibujo)" if ax is not axes[2] else "X relativo ×1000 (mm si metros)", fontsize=8)
        ax.set_ylabel("Y relativo (unidades del dibujo)" if ax is not axes[2] else "Y relativo ×1000 (mm si metros)", fontsize=8)
    for ax in axes[:2]:
        ax.add_patch(Polygon(box - origin, closed=True, fill=False, edgecolor="#bf2828", linewidth=1.2))
    axes[1].set_xlim(*(box[:, 0] - origin[0]).min() + np.array([-0.04, (box[:, 0].max() - box[:, 0].min()) + 0.04]))
    axes[1].set_ylim(*(box[:, 1] - origin[1]).min() + np.array([-0.04, (box[:, 1].max() - box[:, 1].min()) + 0.04]))
    axes[2].set_xlim(-0.3, 0.3)
    axes[2].set_ylim(-0.4, 0.3)
    axes[2].annotate("Vértice conservado", (0, 0), xytext=(0.03, 0.12), fontsize=8,
                     arrowprops={"arrowstyle": "->", "color": "#d15823"})
    removed = (p[17] - origin) * 1000
    axes[2].annotate("Vértice retirado", removed, xytext=(0.04, -0.30), fontsize=8,
                     arrowprops={"arrowstyle": "->", "color": "#2378ae"})
    axes[0].legend(loc="upper left", fontsize=8)
    fig.savefig(args.output / "comparacion-cuadro-antes-despues.png", dpi=180)
    plt.close(fig)
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
