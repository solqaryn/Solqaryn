#!/usr/bin/env python3
import json
import math
import subprocess
import sys
import tempfile
from pathlib import Path

if len(sys.argv) != 4:
    raise SystemExit("usage: phase9_compare_pdf_renders.py BASELINE_DIR CANDIDATE_DIR REPORT")

baseline_dir = Path(sys.argv[1])
candidate_dir = Path(sys.argv[2])
report_path = Path(sys.argv[3])

def read_ppm(path: Path):
    data = path.read_bytes()
    index = 0
    def token():
        nonlocal index
        while index < len(data) and chr(data[index]).isspace():
            index += 1
        while index < len(data) and data[index] == 35:
            while index < len(data) and data[index] != 10:
                index += 1
            while index < len(data) and chr(data[index]).isspace():
                index += 1
        start = index
        while index < len(data) and not chr(data[index]).isspace():
            index += 1
        return data[start:index]
    magic = token()
    width = int(token())
    height = int(token())
    max_value = int(token())
    while index < len(data) and chr(data[index]).isspace():
        index += 1
    pixels = data[index:]
    if magic != b"P6" or max_value != 255 or len(pixels) != width * height * 3:
        raise RuntimeError(f"PPM inválido: {path}")
    return width, height, pixels

def crop(pixels, width, height, target_width, target_height):
    result = bytearray()
    stride = width * 3
    for y in range(target_height):
        start = y * stride
        result.extend(pixels[start:start + target_width * 3])
    return bytes(result)

def nonwhite_ratio(pixels):
    count = 0
    total = len(pixels) // 3
    for i in range(0, len(pixels), 3):
        if min(pixels[i:i+3]) < 245:
            count += 1
    return count / max(1, total)

def render(pdf: Path, directory: Path):
    prefix = directory / pdf.stem
    subprocess.run(
        ["pdftoppm", "-r", "72", str(pdf), str(prefix)],
        check=True,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.PIPE,
    )
    return sorted(directory.glob(pdf.stem + "-*.ppm"))

baseline_pdfs = {x.name: x for x in baseline_dir.glob("*.pdf")}
candidate_pdfs = {x.name: x for x in candidate_dir.glob("*.pdf")}
errors = []
rows = []

if set(baseline_pdfs) != set(candidate_pdfs):
    errors.append(f"PDF set mismatch: {sorted(set(baseline_pdfs) ^ set(candidate_pdfs))}")

with tempfile.TemporaryDirectory(prefix="phase9-render-") as temp:
    temp = Path(temp)
    base_render = temp / "base"
    cand_render = temp / "candidate"
    base_render.mkdir()
    cand_render.mkdir()

    for name in sorted(set(baseline_pdfs) & set(candidate_pdfs)):
        base_pages = render(baseline_pdfs[name], base_render)
        cand_pages = render(candidate_pdfs[name], cand_render)
        if len(base_pages) != len(cand_pages):
            errors.append(f"{name}: render page count {len(base_pages)} != {len(cand_pages)}")
            continue

        for page_index, (base_page, cand_page) in enumerate(zip(base_pages, cand_pages), 1):
            bw, bh, bp = read_ppm(base_page)
            cw, ch, cp = read_ppm(cand_page)
            min_w, min_h = min(bw, cw), min(bh, ch)
            b_crop = crop(bp, bw, bh, min_w, min_h)
            c_crop = crop(cp, cw, ch, min_w, min_h)
            mse = sum((x - y) ** 2 for x, y in zip(b_crop, c_crop)) / max(1, len(b_crop))
            rmse = math.sqrt(mse) / 255.0
            base_ink = nonwhite_ratio(b_crop)
            cand_ink = nonwhite_ratio(c_crop)
            ink_delta = abs(base_ink - cand_ink)
            width_delta = abs(bw - cw)
            height_ratio = ch / max(1, bh)

            if width_delta > 2:
                errors.append(f"{name} p{page_index}: render width delta {width_delta}px")
            if name.startswith("factura-pos"):
                if not 0.65 <= height_ratio <= 1.35:
                    errors.append(f"{name} p{page_index}: thermal render height ratio {height_ratio:.4f}")
            elif abs(bh - ch) > 2:
                errors.append(f"{name} p{page_index}: render height delta {abs(bh-ch)}px")
            if rmse > 0.35:
                errors.append(f"{name} p{page_index}: visual RMSE {rmse:.4f} > 0.35")
            if ink_delta > 0.04:
                errors.append(f"{name} p{page_index}: nonwhite density delta {ink_delta:.4f} > 0.04")

            rows.append({
                "Pdf": name,
                "Page": page_index,
                "BaselinePixels": f"{bw}x{bh}",
                "CandidatePixels": f"{cw}x{ch}",
                "RmseNormalized": round(rmse, 6),
                "BaselineInkRatio": round(base_ink, 6),
                "CandidateInkRatio": round(cand_ink, 6),
                "InkDelta": round(ink_delta, 6),
                "HeightRatio": round(height_ratio, 6),
            })

report = {
    "Renderer": "pdftoppm 72dpi",
    "PdfCount": len(candidate_pdfs),
    "PageComparisons": len(rows),
    "Rows": rows,
    "Errors": errors,
    "Result": "PASS" if not errors else "FAIL",
}
report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(report, ensure_ascii=False, indent=2))
if errors:
    raise SystemExit(1)
