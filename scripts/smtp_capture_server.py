#!/usr/bin/env python3
"""Servidor SMTP efímero para certificar Fase 7 sin proveedores externos."""

from __future__ import annotations

import asyncio
import base64
import json
import os
import signal
import time
from pathlib import Path

HOST = os.environ.get("SMTP_CAPTURE_HOST", "127.0.0.1")
PORT = int(os.environ.get("SMTP_CAPTURE_PORT", "1025"))
USERNAME = os.environ.get("SMTP_CAPTURE_USERNAME", "smtp-user")
PASSWORD = os.environ.get("SMTP_CAPTURE_PASSWORD", "smtp-pass")
REQUIRE_AUTH = os.environ.get("SMTP_CAPTURE_REQUIRE_AUTH", "true").strip().lower() in {"1", "true", "yes", "on"}
OUTPUT_DIR = Path(os.environ.get("SMTP_CAPTURE_DIR", "/tmp/solqaryn-smtp"))
FAIL_FIRST = int(os.environ.get("SMTP_FAIL_FIRST_MESSAGES", "1"))

OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
STATE_FILE = OUTPUT_DIR / "state.json"

state = {
    "connections": 0,
    "data_attempts": 0,
    "messages_saved": 0,
    "transient_failures": 0,
}
failures_remaining = FAIL_FIRST
state_lock = asyncio.Lock()


def decode_b64(value: str) -> str:
    try:
        return base64.b64decode(value).decode("utf-8")
    except Exception:
        return ""


async def persist_state() -> None:
    async with state_lock:
        STATE_FILE.write_text(json.dumps(state, indent=2), encoding="utf-8")


async def send(writer: asyncio.StreamWriter, line: str) -> None:
    writer.write((line + "\r\n").encode("ascii"))
    await writer.drain()


async def handle_client(reader: asyncio.StreamReader, writer: asyncio.StreamWriter) -> None:
    global failures_remaining

    state["connections"] += 1
    await persist_state()
    authenticated = not REQUIRE_AUTH
    peer = writer.get_extra_info("peername")
    print(f"SMTP connection from {peer}", flush=True)

    try:
        await send(writer, "220 fake-smtp.solqaryn ESMTP ready")
        while True:
            raw = await reader.readline()
            if not raw:
                break
            line = raw.decode("ascii", errors="replace").rstrip("\r\n")
            upper = line.upper()

            if upper.startswith("EHLO"):
                await send(writer, "250-fake-smtp.solqaryn")
                if REQUIRE_AUTH:
                    await send(writer, "250-AUTH LOGIN")
                await send(writer, "250 SIZE 20971520")
            elif upper.startswith("HELO"):
                await send(writer, "250 fake-smtp.solqaryn")
            elif upper.startswith("AUTH LOGIN"):
                parts = line.split()
                if len(parts) >= 3:
                    username = decode_b64(parts[2])
                else:
                    await send(writer, "334 VXNlcm5hbWU6")
                    username = decode_b64((await reader.readline()).decode("ascii").strip())

                await send(writer, "334 UGFzc3dvcmQ6")
                password = decode_b64((await reader.readline()).decode("ascii").strip())
                authenticated = username == USERNAME and password == PASSWORD
                await send(
                    writer,
                    "235 2.7.0 Authentication successful"
                    if authenticated
                    else "535 5.7.8 Authentication failed",
                )
            elif upper.startswith("MAIL FROM") or upper.startswith("RCPT TO"):
                await send(
                    writer,
                    "250 2.1.0 OK"
                    if (authenticated or not REQUIRE_AUTH)
                    else "530 5.7.0 Authentication required",
                )
            elif upper == "DATA":
                state["data_attempts"] += 1
                await persist_state()
                await send(writer, "354 End data with <CR><LF>.<CR><LF>")
                message = bytearray()
                while True:
                    data_line = await reader.readline()
                    if not data_line or data_line in (b".\r\n", b".\n"):
                        break
                    if data_line.startswith(b".."):
                        data_line = data_line[1:]
                    message.extend(data_line)

                if failures_remaining > 0:
                    failures_remaining -= 1
                    state["transient_failures"] += 1
                    await persist_state()
                    await send(writer, "451 4.3.0 Temporary local problem")
                else:
                    filename = OUTPUT_DIR / f"message-{int(time.time() * 1000)}.eml"
                    filename.write_bytes(bytes(message))
                    state["messages_saved"] += 1
                    await persist_state()
                    await send(writer, "250 2.0.0 Queued")
            elif upper in {"RSET", "NOOP"}:
                await send(writer, "250 OK")
            elif upper == "QUIT":
                await send(writer, "221 2.0.0 Bye")
                break
            else:
                await send(writer, "250 OK")
    except (ConnectionError, asyncio.IncompleteReadError) as exc:
        print(f"SMTP client disconnected: {exc}", flush=True)
    finally:
        writer.close()
        try:
            await writer.wait_closed()
        except (ConnectionError, BrokenPipeError):
            pass


async def main() -> None:
    await persist_state()
    server = await asyncio.start_server(handle_client, HOST, PORT)
    print(f"Fake SMTP listening on {HOST}:{PORT}; output={OUTPUT_DIR}", flush=True)

    stop = asyncio.Event()
    loop = asyncio.get_running_loop()
    for sig in (signal.SIGINT, signal.SIGTERM):
        try:
            loop.add_signal_handler(sig, stop.set)
        except NotImplementedError:
            pass

    async with server:
        await stop.wait()


if __name__ == "__main__":
    asyncio.run(main())
