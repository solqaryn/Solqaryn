#!/usr/bin/env python3
"""C15-01.M05.03 isolated MySQL 8.4 proof: no Aiven, live Sheets or production."""
from collections import Counter
from pathlib import Path
import json
import os
import re
import subprocess

DB = "sq_v20_c1501_m0503"
CONTAINER = os.environ.get("MYSQL_CONTAINER", "")
SNAPSHOT = Path(__file__).resolve().parents[2] / "docs/v5/c15-01-m05-01-ids.snapshot.json"
ID_RE = re.compile(r"^C\d{2}-\d{2}\.M0[1-6]\.0[1-6]$")

def mysql(query, database=True, success=True):
    if not CONTAINER:
        raise RuntimeError("MYSQL_CONTAINER required; no external DB fallback")
    cmd = ["docker", "exec", "-i", CONTAINER, "mysql", "-uroot", "-proot", "-N", "-B"]
    if database:
        cmd += ["-D", DB]
    p = subprocess.run(cmd, input=query, text=True, capture_output=True, check=False)
    if success and p.returncode:
        raise RuntimeError("isolated MySQL query failed: " + p.stderr[-1500:])
    return p

def main():
    ids = json.loads(SNAPSHOT.read_text(encoding="utf8"))["ids"]
    groups = [f"C{major:02d}-{sub:02d}" for major in range(16)
              for sub in range(1, 6 if major == 15 else 5)]
    expected = {f"{group}.M{phase:02d}.{step:02d}" for group in groups
                for phase in range(1, 7) for step in range(1, 7)}
    assert len(ids) == 2340 and len(set(ids)) == 2340 and len(groups) == 65
    assert all(isinstance(x, str) and ID_RE.fullmatch(x) for x in ids)
    assert set(ids) == expected, "forged, missing or extra institutional IDs"
    assert all(n == 36 for n in Counter(x[:6] for x in ids).values())

    version = mysql("SELECT VERSION();", database=False).stdout.strip()
    assert version.startswith("8.4."), "Not isolated MySQL 8.4: " + version
    mysql(f"CREATE DATABASE {DB} CHARACTER SET utf8mb4 COLLATE utf8mb4_bin;", database=False)
    mysql("""
CREATE TABLE allowed_groups (code VARCHAR(6) COLLATE utf8mb4_bin PRIMARY KEY) ENGINE=InnoDB;
CREATE TABLE plan_ids (
 id VARCHAR(20) COLLATE utf8mb4_bin PRIMARY KEY,
 family VARCHAR(6) COLLATE utf8mb4_bin NOT NULL,
 CONSTRAINT fk_allowed_group FOREIGN KEY (family) REFERENCES allowed_groups(code),
 CONSTRAINT ck_family_matches_id CHECK (LEFT(id, 6) = family)
) ENGINE=InnoDB;
""")
    mysql("INSERT INTO allowed_groups(code) VALUES " +
          ",".join(f"('{g}')" for g in groups) + ";")
    mysql("INSERT INTO plan_ids(id,family) VALUES " +
          ",".join(f"('{x}','{x[:6]}')" for x in ids) + ";")
    persisted = mysql("SELECT id FROM plan_ids ORDER BY id;").stdout.strip().splitlines()
    assert persisted == sorted(ids), "MySQL roundtrip differs from snapshot"
    grouped = mysql("SELECT family, COUNT(*) FROM plan_ids GROUP BY family ORDER BY family;").stdout.strip().splitlines()
    assert grouped == [f"{g}\t36" for g in groups], "MySQL groups mismatch"
    assert mysql("SELECT COUNT(*) FROM plan_ids;").stdout.strip() == "2340"
    print("C1501_M0503_MYSQL84_REAL_POSITIVE_2340_IDS_65x36=PASS")

    duplicate = mysql(f"INSERT INTO plan_ids(id,family) VALUES ('{ids[0]}','{ids[0][:6]}');", success=False)
    assert duplicate.returncode != 0 and ("1062" in duplicate.stderr or "Duplicate entry" in duplicate.stderr)
    assert mysql("SELECT COUNT(*) FROM plan_ids;").stdout.strip() == "2340"
    print("C1501_M0503_DUPLICATE_PRIMARY_KEY_NEGATIVE=PASS")

    foreign = mysql("INSERT INTO plan_ids(id,family) VALUES ('C99-99.M01.01','C99-99');", success=False)
    assert foreign.returncode != 0 and ("1452" in foreign.stderr or "foreign key" in foreign.stderr.lower())
    assert mysql("SELECT COUNT(*) FROM plan_ids;").stdout.strip() == "2340"
    print("C1501_M0503_FOREIGN_GROUP_NEGATIVE=PASS")

    rollback = mysql(f"""START TRANSACTION;
DELETE FROM plan_ids WHERE id = '{ids[0]}';
SELECT COUNT(*) FROM plan_ids;
ROLLBACK;
SELECT COUNT(*) FROM plan_ids;
""").stdout.strip().splitlines()
    assert rollback == ["2339", "2340"], "failed InnoDB rollback"
    print("C1501_M0503_MISSING_ROW_ROLLBACK_NEGATIVE=PASS")
    print("C1501_M0503_REAL_MYSQL84_EPHEMERAL_INTEGRATION_PASS=TRUE")
    print("LIMIT: snapshot-only, no live Sheets/JWT/tenant/Aiven/PROD proof")

if __name__ == "__main__":
    main()
