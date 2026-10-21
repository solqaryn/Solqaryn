#!/usr/bin/env python3
"""Read-only deterministic dispatch selection for the current SOLQARYN plan.

Input: live, aligned _COMERCIAL_SOURCE and _RUNTIME_SOURCE Sheets rows,
including their header rows. This code NEVER claims a lease or mutates a Sheet.
The authorized runtime MUST use an atomic single-writer claim and recheck the
selection immediately before the claim. A plain Sheets update is not a CAS.
"""

import argparse
import json
import re
from pathlib import Path

ALLOWED_STATES = frozenset({
    'PENDIENTE', 'EN_PROGRESO', 'VALIDANDO', 'LISTO', 'BLOQUEADO', 'CANCELADO',
})
COMMERCIAL_HEADERS = ('ID', 'PROGRAMA_PRIORIDAD', 'MICROTAREA', 'CRITERIO_EXIGIDO', 'ORDEN')
RUNTIME_HEADERS = ('ID', 'ESTADO', 'OWNER_AUTOMATION_ID', 'LEASE_STATUS')
OWNER_SUSPENSION = re.compile(
    r'^(?:PENDIENTE SIN OWNER;\s*)?SUSPENDIDO HASTA NUEVA ORDEN DEL PROPIETARIO$',
    re.IGNORECASE,
)


def _owner_suspended(next_action):
    # Only the owner's explicit suspension (not unrelated notes) excludes admission.
    return bool(OWNER_SUSPENSION.fullmatch(' '.join(str(next_action or '').split())))

class UnsafeSnapshot(ValueError):
    """An incomplete or contradictory snapshot cannot authorize dispatch."""


def _value(row, index):
    return row[index] if len(row) > index and row[index] is not None else ''


def _rows(table, header, label):
    if not isinstance(table, list) or not table:
        raise UnsafeSnapshot(f'{label}:MISSING_ROWS')
    if not isinstance(table[0], list) or tuple(table[0][:len(header)]) != header:
        raise UnsafeSnapshot(f'{label}:WRONG_HEADER')
    if not all(isinstance(row, list) and isinstance(_value(row, 0), str) for row in table[1:]):
        raise UnsafeSnapshot(f'{label}:INVALID_ROW')
    return table[1:]


def _order(raw, task_id):
    try:
        order = int(raw)
    except (ValueError, TypeError) as error:
        raise UnsafeSnapshot(f'INVALID_ORDER:{task_id}') from error
    if order <= 0 or str(order) != str(raw).strip():
        raise UnsafeSnapshot(f'INVALID_ORDER:{task_id}')
    return order


def _dependencies(raw, task_id):
    if not isinstance(raw, str):
        raise UnsafeSnapshot(f'INVALID_DEPENDENCIES:{task_id}')
    deps = [x.strip() for x in raw.split('|') if x.strip()]
    if len(deps) != len(set(deps)) or task_id in deps:
        raise UnsafeSnapshot(f'INVALID_DEPENDENCIES:{task_id}')
    return deps


def _validate_dag(nodes):
    # Iterative Kahn check: supports a valid chain spanning all 1123 tasks.
    remaining = {name: len(task['dependencies']) for name, task in nodes.items()}
    followers = {name: [] for name in nodes}
    for task_id, item in nodes.items():
        for dependency in item['dependencies']:
            if dependency not in nodes:
                raise UnsafeSnapshot(f'UNKNOWN_DEPENDENCY:{task_id}:{dependency}')
            followers[dependency].append(task_id)
    queue = [ident for ident, count in remaining.items() if count == 0]
    visited = 0
    while queue:
        parent = queue.pop()
        visited += 1
        for child in followers[parent]:
            remaining[child] -= 1
            if remaining[child] == 0:
                queue.append(child)
    if visited != len(nodes):
        raise UnsafeSnapshot('CYCLIC_DEPENDENCY')


def snapshot(commercial_rows, runtime_rows, expected_count=1123):
    """Return a validated, immutable-in-spirit in-memory view.

    Sheet rows must correspond to the EXACT approved plan. This function
    cannot independently authenticate a caller-provided export; the caller
    must acquire a fresh, authorized readback, HEAD, and gate before use.
    """
    commercial = _rows(commercial_rows, COMMERCIAL_HEADERS, 'COMMERCIAL')
    runtime = _rows(runtime_rows, RUNTIME_HEADERS, 'RUNTIME')
    if len(commercial) != len(runtime) or (expected_count is not None and len(runtime) != expected_count):
        raise UnsafeSnapshot('PLAN_RUNTIME_COUNT_MISMATCH')
    tasks, used_orders = {}, set()
    for index, (src, run) in enumerate(zip(commercial, runtime), 1):
        task_id = _value(src, 0)
        if not task_id or task_id in tasks or task_id != _value(run, 0):
            raise UnsafeSnapshot(f'ID_MISMATCH_OR_DUPLICATE:{index}')
        order = _order(_value(src, 4), task_id)
        if order in used_orders:
            raise UnsafeSnapshot(f'DUPLICATE_ORDER:{order}')
        used_orders.add(order)
        state = _value(run, 1)
        if state not in ALLOWED_STATES:
            raise UnsafeSnapshot(f'INVALID_STATE:{task_id}:{state}')
        tasks[task_id] = {
            'id': task_id,
            'order': order,
            'state': state,
            'owner': str(_value(run, 2)).strip(),
            'lease': str(_value(run, 3)).strip(),
            'dependencies': _dependencies(_value(run, 9), task_id),
            'owner_suspended': _owner_suspended(_value(run, 7)),
        }
    _validate_dag(tasks)
    return tasks


def eligible_independent_reviews(commercial_rows, runtime_rows, expected_count=1123, reviewer=None):
    """Find reviewable VALIDANDO scopes in order, never stealing leases.

    This is admission *selection*, not REVIEW_FIRST certification. The writer
    still verifies the original author, ownership metadata, CI and readback.
    A reviewer may not select their own delivered work.
    """
    tasks = snapshot(commercial_rows, runtime_rows, expected_count)
    rows = {row[0]: row for row in runtime_rows[1:]}
    chosen = []
    for item in sorted(tasks.values(), key=lambda task: task['order']):
        if item['state'] != 'VALIDANDO' or item['owner']:
            continue
        if not item['lease'].startswith('RELEASED|'):
            continue
        if any(tasks[dependency]['state'] != 'LISTO' for dependency in item['dependencies']):
            continue
        # If a review was rejected, OWNER is the previous reviewer, not
        # the implementation author. ORIGINAL_AUTHOR survives every handoff.
        pieces = item['lease'].split('|')[1:]
        originals = [part.split('=', 1)[1] for part in pieces
                     if part.startswith('ORIGINAL_AUTHOR=')]
        owners = [part.split('=', 1)[1] for part in pieces
                  if part.startswith('OWNER=')]
        authors = originals if originals else owners
        if len(authors) != 1 or not re.fullmatch(r'[0-9a-f]{32}', authors[0]):
            continue
        if reviewer is not None and reviewer == authors[0]:
            continue
        evidence = _value(rows[item['id']], 5)
        if not isinstance(evidence, str) or not evidence.startswith('https://'):
            continue
        chosen.append({'id': item['id'], 'order': item['order'], 'author': authors[0]})
    return chosen


def audit_wip_violations(runtime_rows):
    """Detect and return any automation owner holding multiple active assignments.

    Returns a dict mapping owner -> list of task_ids with active ownership.
    An assignment is active if state in ('EN_PROGRESO', 'VALIDANDO') and owner is set
    and lease is not RELEASED.
    """
    rows = runtime_rows[1:] if runtime_rows and runtime_rows[0] and runtime_rows[0][0] == 'ID' else runtime_rows
    active_by_owner = {}
    for r in rows:
        if not isinstance(r, list) or len(r) < 3:
            continue
        task_id = str(r[0]).strip()
        state = str(r[1]).strip() if len(r) > 1 else ''
        owner = str(r[2]).strip() if len(r) > 2 else ''
        lease = str(r[3]).strip() if len(r) > 3 else ''
        if owner and state in ('EN_PROGRESO', 'VALIDANDO') and not lease.startswith('RELEASED'):
            active_by_owner.setdefault(owner, []).append(task_id)
    return {owner: tasks for owner, tasks in active_by_owner.items() if len(tasks) > 1}


def dispatch(commercial_rows, runtime_rows, expected_count=1123, candidate_owner=None):
    """Pick lowest-order free eligible work globally (all ten actors).

    Claimed task means owner or non-RELEASED lease; even a stale lease needs
    separately verified takeover. No side effects or lease fabrication.
    If candidate_owner is provided, enforces that this executor does not already hold
    an active assignment (WIP limit = 1).
    """
    tasks = snapshot(commercial_rows, runtime_rows, expected_count)
    wip_violations = audit_wip_violations(runtime_rows)
    available, held, dep_wait, review, suspended = [], [], [], [], []
    candidate_active_task = None
    if candidate_owner:
        candidate_owner = str(candidate_owner).strip()
        for t in tasks.values():
            if t['owner'] == candidate_owner and t['state'] in ('EN_PROGRESO', 'VALIDANDO') and not t['lease'].startswith('RELEASED'):
                candidate_active_task = t['id']
                break

    for t in sorted(tasks.values(), key=lambda x: x['order']):
        if t['state'] == 'VALIDANDO':
            review.append(t['id'])
        if t['state'] != 'PENDIENTE':
            continue
        if t['owner_suspended']:
            suspended.append({'id': t['id'], 'order': t['order'],
                              'reason': 'OWNER_SUSPENDED_UNTIL_NEW_ORDER'})
            continue
        missing = [d for d in t['dependencies'] if tasks[d]['state'] != 'LISTO']
        if missing:
            dep_wait.append({'id': t['id'], 'order': t['order'], 'missing': missing})
            continue
        if t['owner'] or (t['lease'] and not t['lease'].startswith(('RELEASED', 'RECOVERED'))):
            held.append({'id': t['id'], 'order': t['order'], 'reason': 'OWNER_OR_UNVERIFIED_LEASE'})
            continue
        available.append({'id': t['id'], 'order': t['order']})

    if candidate_owner and candidate_active_task:
        selected = None
        candidate_status = f'CANDIDATE_OWNER_WIP_LIMIT_EXCEEDED:{candidate_active_task}'
    else:
        selected = available[0] if available else None
        candidate_status = 'ELIGIBLE' if candidate_owner else None

    return {
        'policy': 'GLOBAL_ASCENDING_ORDER_ELIGIBLE_ONLY',
        'selected': selected,
        'available': available,
        'held': held,
        'dependency_wait': dep_wait,
        'owner_suspended': suspended,
        'independent_review': review,
        'idle_permitted': not available and not review,
        'candidate_owner': candidate_owner,
        'candidate_status': candidate_status,
        'wip_violations': wip_violations,
        'note': 'Read-only decision. Writer MUST atomically claim and revalidate; never infer activity from this result.',
    }


def verify_claim_choice(result, chosen_id):
    """Fail if a worker plans to skip a lower-order, unclaimed eligible task or has WIP limit exceeded."""
    candidate_status = str(result.get('candidate_status') or '')
    if candidate_status.startswith('CANDIDATE_OWNER_WIP_LIMIT_EXCEEDED'):
        raise UnsafeSnapshot(candidate_status)
    winner = result['selected']
    if winner is None:
        raise UnsafeSnapshot('NO_ELIGIBLE_TASK_FOR_CLAIM')
    if chosen_id != winner['id']:
        raise UnsafeSnapshot(f'OUT_OF_ORDER_CLAIM:{chosen_id}:EXPECTED:{winner["id"]}')
    return True


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--snapshot', type=Path, required=True, help='JSON with commercial_rows and runtime_rows')
    parser.add_argument('--expected-count', type=int, default=1123)
    parser.add_argument('--assert-claim', help='Fail if chosen task is not the lowest-order free eligible task')
    args = parser.parse_args()
    try:
        data = json.loads(args.snapshot.read_text(encoding='utf-8'))
        result = dispatch(data['commercial_rows'], data['runtime_rows'], args.expected_count)
        if args.assert_claim:
            verify_claim_choice(result, args.assert_claim)
    except (KeyError, OSError, ValueError, TypeError) as error:
        print(json.dumps({'status': 'NO_GO', 'error': str(error)}))
        return 2
    print(json.dumps({'status': 'READ_ONLY_DISPATCH_VERIFIED', **result}, indent=2, ensure_ascii=False))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
,
    re.IGNORECASE,
)


def _owner_suspended(next_action):
    # Only the owner's explicit suspension (not arbitrary notes) restricts admission.
    return bool(OWNER_SUSPENSION.fullmatch(' '.join(str(next_action or '').split())))


class UnsafeSnapshot(ValueError):
    """An incomplete or contradictory snapshot cannot authorize dispatch."""


def _value(row, index):
    return row[index] if len(row) > index and row[index] is not None else ''


def _rows(table, header, label):
    if not isinstance(table, list) or not table:
        raise UnsafeSnapshot(f'{label}:MISSING_ROWS')
    if not isinstance(table[0], list) or tuple(table[0][:len(header)]) != header:
        raise UnsafeSnapshot(f'{label}:WRONG_HEADER')
    if not all(isinstance(row, list) and isinstance(_value(row, 0), str) for row in table[1:]):
        raise UnsafeSnapshot(f'{label}:INVALID_ROW')
    return table[1:]


def _order(raw, task_id):
    try:
        order = int(raw)
    except (ValueError, TypeError) as error:
        raise UnsafeSnapshot(f'INVALID_ORDER:{task_id}') from error
    if order <= 0 or str(order) != str(raw).strip():
        raise UnsafeSnapshot(f'INVALID_ORDER:{task_id}')
    return order


def _dependencies(raw, task_id):
    if not isinstance(raw, str):
        raise UnsafeSnapshot(f'INVALID_DEPENDENCIES:{task_id}')
    deps = [x.strip() for x in raw.split('|') if x.strip()]
    if len(deps) != len(set(deps)) or task_id in deps:
        raise UnsafeSnapshot(f'INVALID_DEPENDENCIES:{task_id}')
    return deps


def _validate_dag(nodes):
    # Iterative Kahn check: supports a valid chain spanning all 1123 tasks.
    remaining = {name: len(task['dependencies']) for name, task in nodes.items()}
    followers = {name: [] for name in nodes}
    for task_id, item in nodes.items():
        for dependency in item['dependencies']:
            if dependency not in nodes:
                raise UnsafeSnapshot(f'UNKNOWN_DEPENDENCY:{task_id}:{dependency}')
            followers[dependency].append(task_id)
    queue = [ident for ident, count in remaining.items() if count == 0]
    visited = 0
    while queue:
        parent = queue.pop()
        visited += 1
        for child in followers[parent]:
            remaining[child] -= 1
            if remaining[child] == 0:
                queue.append(child)
    if visited != len(nodes):
        raise UnsafeSnapshot('CYCLIC_DEPENDENCY')


def snapshot(commercial_rows, runtime_rows, expected_count=1123):
    """Return a validated, immutable-in-spirit in-memory view.

    Sheet rows must correspond to the EXACT approved plan. This function
    cannot independently authenticate a caller-provided export; the caller
    must acquire a fresh, authorized readback, HEAD, and gate before use.
    """
    commercial = _rows(commercial_rows, COMMERCIAL_HEADERS, 'COMMERCIAL')
    runtime = _rows(runtime_rows, RUNTIME_HEADERS, 'RUNTIME')
    if len(commercial) != len(runtime) or (expected_count is not None and len(runtime) != expected_count):
        raise UnsafeSnapshot('PLAN_RUNTIME_COUNT_MISMATCH')
    tasks, used_orders = {}, set()
    for index, (src, run) in enumerate(zip(commercial, runtime), 1):
        task_id = _value(src, 0)
        if not task_id or task_id in tasks or task_id != _value(run, 0):
            raise UnsafeSnapshot(f'ID_MISMATCH_OR_DUPLICATE:{index}')
        order = _order(_value(src, 4), task_id)
        if order in used_orders:
            raise UnsafeSnapshot(f'DUPLICATE_ORDER:{order}')
        used_orders.add(order)
        state = _value(run, 1)
        if state not in ALLOWED_STATES:
            raise UnsafeSnapshot(f'INVALID_STATE:{task_id}:{state}')
        tasks[task_id] = {
            'id': task_id,
            'order': order,
            'state': state,
            'owner': str(_value(run, 2)).strip(),
            'lease': str(_value(run, 3)).strip(),
            'dependencies': _dependencies(_value(run, 9), task_id),
        }
    _validate_dag(tasks)
    return tasks


def eligible_independent_reviews(commercial_rows, runtime_rows, expected_count=1123, reviewer=None):
    """Find reviewable VALIDANDO scopes in order, never stealing leases.

    This is admission *selection*, not REVIEW_FIRST certification. The writer
    still verifies the original author, ownership metadata, CI and readback.
    A reviewer may not select their own delivered work.
    """
    tasks = snapshot(commercial_rows, runtime_rows, expected_count)
    rows = {row[0]: row for row in runtime_rows[1:]}
    chosen = []
    for item in sorted(tasks.values(), key=lambda task: task['order']):
        if item['state'] != 'VALIDANDO' or item['owner']:
            continue
        if not item['lease'].startswith('RELEASED|'):
            continue
        if any(tasks[dependency]['state'] != 'LISTO' for dependency in item['dependencies']):
            continue
        # If a review was rejected, OWNER is the previous reviewer, not
        # the implementation author. ORIGINAL_AUTHOR survives every handoff.
        pieces = item['lease'].split('|')[1:]
        originals = [part.split('=', 1)[1] for part in pieces
                     if part.startswith('ORIGINAL_AUTHOR=')]
        owners = [part.split('=', 1)[1] for part in pieces
                  if part.startswith('OWNER=')]
        authors = originals if originals else owners
        if len(authors) != 1 or not re.fullmatch(r'[0-9a-f]{32}', authors[0]):
            continue
        if reviewer is not None and reviewer == authors[0]:
            continue
        evidence = _value(rows[item['id']], 5)
        if not isinstance(evidence, str) or not evidence.startswith('https://'):
            continue
        chosen.append({'id': item['id'], 'order': item['order'], 'author': authors[0]})
    return chosen


def audit_wip_violations(runtime_rows):
    """Detect and return any automation owner holding multiple active assignments.

    Returns a dict mapping owner -> list of task_ids with active ownership.
    An assignment is active if state in ('EN_PROGRESO', 'VALIDANDO') and owner is set
    and lease is not RELEASED.
    """
    rows = runtime_rows[1:] if runtime_rows and runtime_rows[0] and runtime_rows[0][0] == 'ID' else runtime_rows
    active_by_owner = {}
    for r in rows:
        if not isinstance(r, list) or len(r) < 3:
            continue
        task_id = str(r[0]).strip()
        state = str(r[1]).strip() if len(r) > 1 else ''
        owner = str(r[2]).strip() if len(r) > 2 else ''
        lease = str(r[3]).strip() if len(r) > 3 else ''
        if owner and state in ('EN_PROGRESO', 'VALIDANDO') and not lease.startswith('RELEASED'):
            active_by_owner.setdefault(owner, []).append(task_id)
    return {owner: tasks for owner, tasks in active_by_owner.items() if len(tasks) > 1}


def dispatch(commercial_rows, runtime_rows, expected_count=1123, candidate_owner=None):
    """Pick lowest-order free eligible work globally (all ten actors).

    Claimed task means owner or non-RELEASED lease; even a stale lease needs
    separately verified takeover. No side effects or lease fabrication.
    If candidate_owner is provided, enforces that this executor does not already hold
    an active assignment (WIP limit = 1).
    """
    tasks = snapshot(commercial_rows, runtime_rows, expected_count)
    wip_violations = audit_wip_violations(runtime_rows)
    available, held, dep_wait, review = [], [], [], []
    candidate_active_task = None
    if candidate_owner:
        candidate_owner = str(candidate_owner).strip()
        for t in tasks.values():
            if t['owner'] == candidate_owner and t['state'] in ('EN_PROGRESO', 'VALIDANDO') and not t['lease'].startswith('RELEASED'):
                candidate_active_task = t['id']
                break

    for t in sorted(tasks.values(), key=lambda x: x['order']):
        if t['state'] == 'VALIDANDO':
            review.append(t['id'])
        if t['state'] != 'PENDIENTE':
            continue
        missing = [d for d in t['dependencies'] if tasks[d]['state'] != 'LISTO']
        if missing:
            dep_wait.append({'id': t['id'], 'order': t['order'], 'missing': missing})
            continue
        if t['owner'] or (t['lease'] and not t['lease'].startswith(('RELEASED', 'RECOVERED'))):
            held.append({'id': t['id'], 'order': t['order'], 'reason': 'OWNER_OR_UNVERIFIED_LEASE'})
            continue
        available.append({'id': t['id'], 'order': t['order']})

    if candidate_owner and candidate_active_task:
        selected = None
        candidate_status = f'CANDIDATE_OWNER_WIP_LIMIT_EXCEEDED:{candidate_active_task}'
    else:
        selected = available[0] if available else None
        candidate_status = 'ELIGIBLE' if candidate_owner else None

    return {
        'policy': 'GLOBAL_ASCENDING_ORDER_ELIGIBLE_ONLY',
        'selected': selected,
        'available': available,
        'held': held,
        'dependency_wait': dep_wait,
        'independent_review': review,
        'idle_permitted': not available and not review,
        'candidate_owner': candidate_owner,
        'candidate_status': candidate_status,
        'wip_violations': wip_violations,
        'note': 'Read-only decision. Writer MUST atomically claim and revalidate; never infer activity from this result.',
    }


def verify_claim_choice(result, chosen_id):
    """Fail if a worker plans to skip a lower-order, unclaimed eligible task or has WIP limit exceeded."""
    candidate_status = str(result.get('candidate_status') or '')
    if candidate_status.startswith('CANDIDATE_OWNER_WIP_LIMIT_EXCEEDED'):
        raise UnsafeSnapshot(candidate_status)
    winner = result['selected']
    if winner is None:
        raise UnsafeSnapshot('NO_ELIGIBLE_TASK_FOR_CLAIM')
    if chosen_id != winner['id']:
        raise UnsafeSnapshot(f'OUT_OF_ORDER_CLAIM:{chosen_id}:EXPECTED:{winner["id"]}')
    return True


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--snapshot', type=Path, required=True, help='JSON with commercial_rows and runtime_rows')
    parser.add_argument('--expected-count', type=int, default=1123)
    parser.add_argument('--assert-claim', help='Fail if chosen task is not the lowest-order free eligible task')
    args = parser.parse_args()
    try:
        data = json.loads(args.snapshot.read_text(encoding='utf-8'))
        result = dispatch(data['commercial_rows'], data['runtime_rows'], args.expected_count)
        if args.assert_claim:
            verify_claim_choice(result, args.assert_claim)
    except (KeyError, OSError, ValueError, TypeError) as error:
        print(json.dumps({'status': 'NO_GO', 'error': str(error)}))
        return 2
    print(json.dumps({'status': 'READ_ONLY_DISPATCH_VERIFIED', **result}, indent=2, ensure_ascii=False))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
