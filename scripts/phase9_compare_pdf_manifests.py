#!/usr/bin/env python3
import json
import math
import re
import sys
from pathlib import Path

if len(sys.argv) != 4:
    raise SystemExit("usage: phase9_compare_pdf_manifests.py BASELINE CANDIDATE REPORT")

baseline_path, candidate_path, report_path = map(Path, sys.argv[1:])
baseline = json.loads(baseline_path.read_text(encoding="utf-8"))
candidate = json.loads(candidate_path.read_text(encoding="utf-8"))

errors = []
differences = []

if not str(baseline.get("QuestPdfInformationalVersion", "")).startswith("2024.3.6"):
    errors.append("baseline QuestPDF no es 2024.3.6")
if not str(candidate.get("QuestPdfInformationalVersion", "")).startswith("2026.9.1"):
    errors.append("candidate QuestPDF no es 2026.9.1")

b_index = {(x["Formato"], x["Caso"]): x for x in baseline["Manifests"]}
c_index = {(x["Formato"], x["Caso"]): x for x in candidate["Manifests"]}
if set(b_index) != set(c_index):
    errors.append(f"superficies distintas baseline/candidate: {sorted(set(b_index) ^ set(c_index))}")

paper = {"a4", "carta", "legal", "oficio", "a5"}
thermal = {"pos58", "pos80"}

def family(font):
    return font.split("+", 1)[-1]

for key in sorted(set(b_index) & set(c_index)):
    b = b_index[key]
    c = c_index[key]
    formato, caso = key
    entry = {
        "Formato": formato,
        "Caso": caso,
        "BaselineBytes": b["Bytes"],
        "CandidateBytes": c["Bytes"],
        "BytesRatio": round(c["Bytes"] / max(1, b["Bytes"]), 4),
        "BaselinePages": b["PageCount"],
        "CandidatePages": c["PageCount"],
        "TextShaEqual": b["TextSha256"] == c["TextSha256"],
        "BaselineFonts": sorted({family(x) for x in b["Fonts"]}),
        "CandidateFonts": sorted({family(x) for x in c["Fonts"]}),
        "BaselinePdfUaMarker": b["PdfUaMarkerObserved"],
        "CandidatePdfUaMarker": c["PdfUaMarkerObserved"],
    }

    if not 0.45 <= entry["BytesRatio"] <= 2.25:
        errors.append(f"{key}: cambio de bytes fuera de frontera: {entry['BytesRatio']}")

    if formato in paper:
        if caso == "larga" and c["PageCount"] < 2:
            errors.append(f"{key}: perdió paginación múltiple")
        for p in c["Paginas"]:
            bpage = b["Paginas"][min(p["Numero"] - 1, len(b["Paginas"]) - 1)]
            if abs(p["Ancho"] - bpage["Ancho"]) > 2.5 or abs(p["Alto"] - bpage["Alto"]) > 2.5:
                errors.append(f"{key}: MediaBox cambió fuera de tolerancia en página {p['Numero']}")

    if formato in thermal:
        if c["PageCount"] != 1:
            errors.append(f"{key}: térmico dejó de ser página continua única")
        expected = 164.41 if formato == "pos58" else 226.77
        if abs(c["Paginas"][0]["Ancho"] - expected) > 2.5:
            errors.append(f"{key}: ancho térmico fuera de tolerancia")
        if caso == "larga":
            ratio = c["Paginas"][0]["Alto"] / max(1, b["Paginas"][0]["Alto"])
            entry["HeightRatio"] = round(ratio, 4)
            if not 0.70 <= ratio <= 1.30:
                errors.append(f"{key}: altura térmica cambió más de 30%")

    if not set(entry["BaselineFonts"]).issubset(set(entry["CandidateFonts"])):
        errors.append(f"{key}: familia de fuentes baseline no está preservada")

    differences.append(entry)

candidate_full = json.loads(candidate_path.with_name("manifest.json").read_text(encoding="utf-8"))
full_index = {(x["Formato"], x["Caso"]): x for x in candidate_full["Manifests"]}
unicode_tokens = [
    "José Núñez",
    "Peña & Compañía",
    "Crédito",
    "Información",
    "Dirección",
    "¿Gracias por su compra?",
    "¡Vuelva pronto!",
    "©",
    "×",
]
for formato in ["a4", "carta", "legal", "oficio", "a5", "pos58", "pos80"]:
    text = full_index[(formato, "unicode")]["TextoNormalizado"]
    for token in unicode_tokens:
        if token not in text:
            errors.append(f"{formato}/unicode: falta token {token!r}")
    if "□" in text:
        errors.append(f"{formato}/unicode: glyph tofu observado")

for formato in ["a4", "carta", "legal", "oficio", "a5"]:
    text = full_index[(formato, "larga")]["TextoNormalizado"]
    if "Producto largo 01" not in text or "Producto largo 72" not in text:
        errors.append(f"{formato}/larga: pérdida de extremos del detalle")
for formato in ["pos58", "pos80"]:
    text = full_index[(formato, "larga")]["TextoNormalizado"]
    if "Producto largo 01" not in text or "Producto largo 30" not in text:
        errors.append(f"{formato}/larga: pérdida de extremos del detalle")

settings = candidate.get("Settings", {})
if settings.get("UseSystemFonts") not in {"False", "false"}:
    errors.append(f"UseSystemFonts debe permanecer false; observado={settings.get('UseSystemFonts')}")
if settings.get("ThrowOnMissingFontFamilies") not in {"True", "true"}:
    errors.append(f"ThrowOnMissingFontFamilies debe permanecer true; observado={settings.get('ThrowOnMissingFontFamilies')}")
if settings.get("ThrowOnMissingTextGlyphs") not in {"True", "true"}:
    errors.append(f"ThrowOnMissingTextGlyphs debe permanecer true; observado={settings.get('ThrowOnMissingTextGlyphs')}")

report = {
    "BaselineVersion": baseline.get("QuestPdfInformationalVersion"),
    "CandidateVersion": candidate.get("QuestPdfInformationalVersion"),
    "BaselineLicense": baseline.get("License"),
    "CandidateLicense": candidate.get("License"),
    "BaselineSettings": baseline.get("Settings"),
    "CandidateSettings": candidate.get("Settings"),
    "Qr": "NOT_APPLICABLE_NO_EXISTING_PDF_QR",
    "EmbeddedAttachments": "NOT_APPLICABLE",
    "PdfUaBaseline": "PDFUA_NOT_ENABLED",
    "PdfUaCandidate": "NO_CHANGE_NOT_ENABLED" if not candidate.get("PdfUaConfiguredInProduct") else "REVIEW_REQUIRED",
    "Differences": differences,
    "Errors": errors,
    "Result": "PASS" if not errors else "FAIL",
}
report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps(report, ensure_ascii=False, indent=2))
if errors:
    raise SystemExit(1)
