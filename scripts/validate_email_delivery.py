#!/usr/bin/env python3
"""Valida el correo capturado por el SMTP efímero de Fase 7."""

from __future__ import annotations

import json
import os
import sys
from email import policy
from email.parser import BytesParser
from pathlib import Path

capture_dir = Path(os.environ.get("SMTP_CAPTURE_DIR", "/tmp/solqaryn-smtp"))
state_file = capture_dir / "state.json"
messages = sorted(capture_dir.glob("message-*.eml"))

if not state_file.exists():
    raise SystemExit("No existe state.json del servidor SMTP.")

state = json.loads(state_file.read_text(encoding="utf-8"))
if state.get("data_attempts", 0) < 2:
    raise SystemExit(f"Se esperaban al menos dos intentos SMTP por el reintento: {state}")
if state.get("transient_failures") != 1:
    raise SystemExit(f"Se esperaba exactamente un fallo transitorio: {state}")
if state.get("messages_saved") != 1 or len(messages) != 1:
    raise SystemExit(f"La idempotencia debía producir un solo correo: state={state}, files={len(messages)}")

message = BytesParser(policy=policy.default).parsebytes(messages[0].read_bytes())
recipient = str(message.get("To", ""))
subject = str(message.get("Subject", ""))
message_id = str(message.get("X-Solqaryn-Message-Id", ""))

if "fase7@example.com" not in recipient.lower():
    raise SystemExit(f"Destinatario inesperado: {recipient}")
if "Factura" not in subject:
    raise SystemExit(f"Asunto inesperado: {subject}")
if not message_id.startswith("solqaryn-"):
    raise SystemExit(f"Falta X-Solqaryn-Message-Id: {message_id}")

plain = []
html = []
pdf_attachments = []
for part in message.walk():
    content_type = part.get_content_type()
    disposition = part.get_content_disposition()
    if disposition == "attachment" and content_type == "application/pdf":
        pdf_attachments.append((part.get_filename(), part.get_payload(decode=True) or b""))
    elif content_type == "text/plain" and disposition != "attachment":
        plain.append(part.get_content())
    elif content_type == "text/html" and disposition != "attachment":
        html.append(part.get_content())

if not plain or "Factura" not in "\n".join(plain):
    raise SystemExit("El correo no contiene alternativa text/plain válida.")
if not html or "PDF oficial A4" not in "\n".join(html):
    raise SystemExit("La plantilla HTML no describe el PDF oficial A4.")
if len(pdf_attachments) != 1:
    raise SystemExit(f"Se esperaba un PDF adjunto y se encontraron {len(pdf_attachments)}.")

filename, pdf = pdf_attachments[0]
if not filename or not filename.lower().endswith(".pdf"):
    raise SystemExit(f"Nombre de adjunto inválido: {filename}")
if not pdf.startswith(b"%PDF") or len(pdf) < 5_000:
    raise SystemExit(f"PDF adjunto inválido: bytes={len(pdf)}, header={pdf[:4]!r}")

summary = {
    "recipient": recipient,
    "subject": subject,
    "message_id": message_id,
    "attachment": filename,
    "attachment_bytes": len(pdf),
    "smtp_state": state,
}
(capture_dir / "validation-summary.json").write_text(
    json.dumps(summary, indent=2, ensure_ascii=False),
    encoding="utf-8",
)
print(json.dumps(summary, indent=2, ensure_ascii=False))
