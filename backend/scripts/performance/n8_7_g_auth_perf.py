#!/usr/bin/env python3
"""N8.7.G bounded authenticated performance harness.

Development/CI only. Uses synthetic credentials supplied by the ephemeral workflow,
never prints tokens/passwords, and exercises the real HTTP/JWT/DB renewal path.
"""
from __future__ import annotations

import argparse
import concurrent.futures
import json
import math
import os
import statistics
import threading
import time
import urllib.error
import urllib.request
import uuid
from dataclasses import dataclass, asdict


@dataclass
class ProfileResult:
    name: str
    requests: int
    concurrency: int
    duration_seconds: float
    successes: int
    failures: int
    error_rate: float
    rps: float
    p50_ms: float
    p95_ms: float
    p99_ms: float
    max_ms: float
    status_counts: dict[str, int]


def percentile(values: list[float], q: float) -> float:
    if not values:
        return 0.0
    ordered = sorted(values)
    pos = (len(ordered) - 1) * q
    lo = math.floor(pos)
    hi = math.ceil(pos)
    if lo == hi:
        return ordered[lo]
    return ordered[lo] * (hi - pos) + ordered[hi] * (pos - lo)


def request_json(url: str, method: str = "GET", payload: dict | None = None, token: str | None = None, timeout: float = 15.0) -> tuple[int, dict | None, float]:
    body = None if payload is None else json.dumps(payload).encode("utf-8")
    headers = {
        "Accept": "application/json",
        "X-Correlation-ID": f"n87g-{uuid.uuid4()}",
    }
    if body is not None:
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = f"Bearer {token}"
    req = urllib.request.Request(url, data=body, headers=headers, method=method)
    start = time.perf_counter()
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            raw = resp.read()
            elapsed = (time.perf_counter() - start) * 1000
            parsed = json.loads(raw.decode("utf-8")) if raw else None
            return resp.status, parsed, elapsed
    except urllib.error.HTTPError as exc:
        elapsed = (time.perf_counter() - start) * 1000
        try:
            raw = exc.read()
            parsed = json.loads(raw.decode("utf-8")) if raw else None
        except Exception:
            parsed = None
        return exc.code, parsed, elapsed
    except Exception:
        elapsed = (time.perf_counter() - start) * 1000
        return 0, None, elapsed


def login(base_url: str, username: str, password: str) -> str:
    status, data, _ = request_json(
        f"{base_url}/auth/login",
        method="POST",
        payload={"nombreUsuario": username, "password": password},
    )
    if status != 200 or not isinstance(data, dict):
        raise RuntimeError(f"Synthetic login failed with HTTP {status}")
    token = ((data.get("data") or {}).get("token"))
    if not token:
        raise RuntimeError("Synthetic login succeeded without a token")
    return token


def run_count_profile(base_url: str, token: str, name: str, requests: int, concurrency: int) -> ProfileResult:
    latencies: list[float] = []
    status_counts: dict[str, int] = {}
    lock = threading.Lock()

    def one(_: int) -> None:
        status, data, ms = request_json(f"{base_url}/auth/renovar", method="POST", token=token)
        ok = status == 200 and isinstance(data, dict) and bool((data.get("data") or {}).get("token"))
        with lock:
            latencies.append(ms)
            key = str(status if status else "transport")
            status_counts[key] = status_counts.get(key, 0) + 1
            if not ok and status == 200:
                status_counts["200-invalid-body"] = status_counts.get("200-invalid-body", 0) + 1

    start = time.perf_counter()
    with concurrent.futures.ThreadPoolExecutor(max_workers=concurrency) as pool:
        list(pool.map(one, range(requests)))
    duration = max(time.perf_counter() - start, 1e-9)
    successes = status_counts.get("200", 0) - status_counts.get("200-invalid-body", 0)
    failures = requests - successes
    return ProfileResult(
        name=name,
        requests=requests,
        concurrency=concurrency,
        duration_seconds=round(duration, 3),
        successes=successes,
        failures=failures,
        error_rate=round(failures / requests, 6),
        rps=round(requests / duration, 2),
        p50_ms=round(percentile(latencies, 0.50), 2),
        p95_ms=round(percentile(latencies, 0.95), 2),
        p99_ms=round(percentile(latencies, 0.99), 2),
        max_ms=round(max(latencies) if latencies else 0.0, 2),
        status_counts=status_counts,
    )


def run_endurance_profile(base_url: str, token: str, seconds: int, concurrency: int) -> ProfileResult:
    deadline = time.monotonic() + seconds
    latencies: list[float] = []
    status_counts: dict[str, int] = {}
    lock = threading.Lock()

    def worker() -> None:
        while time.monotonic() < deadline:
            status, data, ms = request_json(f"{base_url}/auth/renovar", method="POST", token=token)
            ok = status == 200 and isinstance(data, dict) and bool((data.get("data") or {}).get("token"))
            with lock:
                latencies.append(ms)
                key = str(status if status else "transport")
                status_counts[key] = status_counts.get(key, 0) + 1
                if not ok and status == 200:
                    status_counts["200-invalid-body"] = status_counts.get("200-invalid-body", 0) + 1

    start = time.perf_counter()
    with concurrent.futures.ThreadPoolExecutor(max_workers=concurrency) as pool:
        futures = [pool.submit(worker) for _ in range(concurrency)]
        for future in futures:
            future.result()
    duration = max(time.perf_counter() - start, 1e-9)
    requests = len(latencies)
    successes = status_counts.get("200", 0) - status_counts.get("200-invalid-body", 0)
    failures = requests - successes
    return ProfileResult(
        name="endurance",
        requests=requests,
        concurrency=concurrency,
        duration_seconds=round(duration, 3),
        successes=successes,
        failures=failures,
        error_rate=round((failures / requests) if requests else 1.0, 6),
        rps=round(requests / duration, 2),
        p50_ms=round(percentile(latencies, 0.50), 2),
        p95_ms=round(percentile(latencies, 0.95), 2),
        p99_ms=round(percentile(latencies, 0.99), 2),
        max_ms=round(max(latencies) if latencies else 0.0, 2),
        status_counts=status_counts,
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base-url", required=True)
    parser.add_argument("--output", default="n8_7_g_perf_results.json")
    args = parser.parse_args()

    username = os.environ.get("N87_PERF_USER")
    password = os.environ.get("N87_PERF_PASSWORD")
    if not username or not password:
        raise RuntimeError("Synthetic CI credentials are missing")

    base_url = args.base_url.rstrip("/")
    token = login(base_url, username, password)

    # Warm-up is intentionally small and excluded from measurements.
    warm = run_count_profile(base_url, token, "warmup", 12, 2)
    if warm.failures:
        raise RuntimeError(f"Warm-up failed: {warm.status_counts}")

    results = [
        run_count_profile(base_url, token, "load", 160, 4),
        run_count_profile(base_url, token, "stress", 240, 12),
        run_endurance_profile(base_url, token, 20, 4),
        run_count_profile(base_url, token, "volume", 800, 8),
    ]

    total_requests = sum(r.requests for r in results)
    total_failures = sum(r.failures for r in results)
    evidence = {
        "schemaVersion": "vaep.n8.7.g.performance.v1",
        "scope": "Development/ephemeral-CI",
        "target": "real ASP.NET HTTP pipeline + JWT auth + MySQL 8.4",
        "flow": "POST /auth/renovar using a synthetic active relational-role user",
        "profiles": [asdict(r) for r in results],
        "totalRequests": total_requests,
        "totalFailures": total_failures,
        "overallErrorRate": round(total_failures / total_requests, 6),
        "security": {
            "productionTraffic": 0,
            "productionData": 0,
            "secretValuesLogged": 0,
            "authBypass": False,
            "rateLimitDisabled": False,
            "syntheticDataOnly": True,
        },
        "acceptance": {
            "functionalCriterion": "zero HTTP/transport/body failures across bounded profiles",
            "latencySlaClaimed": False,
        },
    }
    with open(args.output, "w", encoding="utf-8") as fh:
        json.dump(evidence, fh, indent=2, sort_keys=True)

    print(json.dumps({
        "profiles": [{"name": r.name, "requests": r.requests, "failures": r.failures, "rps": r.rps, "p95_ms": r.p95_ms} for r in results],
        "totalRequests": total_requests,
        "totalFailures": total_failures,
    }, indent=2))

    return 0 if total_failures == 0 else 2


if __name__ == "__main__":
    raise SystemExit(main())
