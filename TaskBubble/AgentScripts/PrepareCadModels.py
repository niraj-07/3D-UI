"""Split the supplied FreeCAD OBJ exports without changing coordinates or face winding."""
import hashlib
import json
import re
from pathlib import Path

PROJECT = Path(__file__).resolve().parents[1]
SOURCE = PROJECT / "Assets/CAD model/Converted"
OUTPUT = PROJECT / "Assets/TaskBubble/CAD/Models"
KINDS = ("v", "vt", "vn")


def component_name(name, coupling):
    if coupling:
        return "Coupling"
    if name.lower().endswith("_base.step"):
        return "Base"
    match = re.search(r"_finger([1-4])\.step(001)?$", name, re.IGNORECASE)
    if match:
        return f"Finger{match[1]}_{'B' if match[2] else 'A'}"
    match = re.search(r"_fingertips\.step(001)?$", name, re.IGNORECASE)
    if match:
        return f"Fingertip_{'B' if match[1] else 'A'}"
    raise ValueError(f"Unrecognized source component: {name}")


def split(source, material_name, coupling=False):
    geometry = {kind: [] for kind in KINDS}
    parts = []
    current = None
    active = {}
    for raw in source.read_text(encoding="utf-8-sig").splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        command, _, value = line.partition(" ")
        if command in KINDS:
            geometry[command].append(line)
        elif command == "o":
            current = {"source": value, "name": component_name(value, coupling),
                       "geometry": {k: [] for k in KINDS}, "indices": {k: {} for k in KINDS},
                       "commands": list(active.values()), "faces": 0, "triangles": 0}
            parts.append(current)
        elif command in ("usemtl", "s"):
            active[command] = line
            if current is not None:
                current["commands"].append(line)
        elif command == "f":
            if current is None:
                raise ValueError("Face found before an object declaration")
            vertices = []
            for token in value.split():
                references = token.split("/")
                if len(references) > 3:
                    raise ValueError(f"Invalid face reference: {token}")
                mapped = []
                for kind, reference in zip(KINDS, references):
                    if not reference:
                        mapped.append("")
                        continue
                    number = int(reference)
                    index = number - 1 if number > 0 else len(geometry[kind]) + number
                    if number == 0 or not 0 <= index < len(geometry[kind]):
                        raise ValueError(f"Invalid {kind} index: {reference}")
                    mapping = current["indices"][kind]
                    if index not in mapping:
                        mapping[index] = len(current["geometry"][kind]) + 1
                        current["geometry"][kind].append(geometry[kind][index])
                    mapped.append(str(mapping[index]))
                vertices.append("/".join(mapped))
            if len(vertices) < 3:
                raise ValueError("Face needs at least three vertices")
            current["commands"].append("f " + " ".join(vertices))
            current["faces"] += 1
            current["triangles"] += len(vertices) - 2
        elif command not in ("mtllib", "g"):
            raise ValueError(f"Unsupported OBJ record: {command}")

    if len(parts) != (1 if coupling else 11) or any(p["faces"] == 0 for p in parts):
        raise ValueError("Unexpected component count or empty geometry")
    results = []
    for part in parts:
        # Each original position/UV/normal record is copied verbatim; only indices are remapped.
        for kind in KINDS:
            for original, local in part["indices"][kind].items():
                assert geometry[kind][original] == part["geometry"][kind][local - 1]
        lines = [f"# Source: {source.name} / {part['source']}",
                 "# Coordinates remain in the original assembly frame, in millimetres.",
                 f"mtllib {material_name}", f"o {part['name']}", f"g {part['name']}"]
        for kind in KINDS:
            lines.extend(part["geometry"][kind])
        lines.extend(part["commands"])
        write_exact(OUTPUT / (part["name"] + ".obj"), "\n".join(lines) + "\n")
        results.append({"file": part["name"] + ".obj", "sourceObject": part["source"],
                        "faces": part["faces"], "triangles": part["triangles"]})
    return results


def write_exact(path, text):
    if path.exists():
        if path.read_text(encoding="utf-8") != text:
            raise FileExistsError(f"Refusing to overwrite modified output: {path}")
        return
    path.write_text(text, encoding="utf-8", newline="\n")


def main():
    inputs = [SOURCE / n for n in ("Gripper.obj", "Gripper.mtl", "coupling.obj", "coupling.mtl")]
    before = {p: hashlib.sha256(p.read_bytes()).digest() for p in inputs}
    OUTPUT.mkdir(parents=True, exist_ok=True)
    write_exact(OUTPUT / "Gripper.mtl", inputs[1].read_text(encoding="utf-8"))
    write_exact(OUTPUT / "Coupling.mtl", inputs[3].read_text(encoding="utf-8"))
    results = split(inputs[0], "Gripper.mtl") + split(inputs[2], "Coupling.mtl", coupling=True)
    assert len({r["file"] for r in results}) == 12
    assert all(hashlib.sha256(p.read_bytes()).digest() == before[p] for p in inputs)
    print(json.dumps({"sourceFilesUnchanged": True, "models": results}, indent=2))


if __name__ == "__main__":
    main()
