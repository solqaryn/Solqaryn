#!/usr/bin/env python3
"""Read-only deterministic dispatch selection for the current SOLQARYN plan.

Input: live, aligned _COMERCIAL_SOURCE and _RUNTIME_SOURCE Sheets rows,
including their header rows. This code NEVER claims a lease or mutates a Sheet.
The authorized runtime MUST use an atomic single-writer claim and recheck the
selection immediately before the claim. A plain Sheets update is not a CAS.
"""

import argparse
import json
from pathlib import Path

ALLOWED_STATES = frozenset({
    'PENDIENTE', 'EN_PROGRESO', 'VALIDANDO', 'LISTO', 'BLOQUEADO', 'CANCELADO',
})
COMMERCIAL_HEADERS = ('ID', 'PROGRAMA_PRIORIDAD', 'MICROTAREA', 'CRITERIO_EXIGIDO', 'ORDEN')
RUNTIME_HEADERS = ('ID', 'ESTADO', 'OWNER_AUTOMATION_ID', 'LEASE_STATUS')


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


def dispatch(commercial_rows, runtime_rows, expected_count=1123):
    """Pick lowest-order free eligible work globally (all ten actors).

    Claimed task means owner or non-RELEASED lease; even a stale lease needs
    separately verified takeover. No side effects or lease fabrication.
    """
    tasks = snapshot(commercial_rows, runtime_rows, expected_count)
    available, held, dep_wait, review = [], [], [], []
    for t in sorted(tasks.values(), key=lambda x: x['order']):
        if t['state'] == 'VALIDANDO':
            review.append(t['id'])
        if t['state'] != 'PENDIENTE':
            continue
        missing = [d for d in t['dependencies'] if tasks[d]['state'] != 'LISTO']
        if missing:
            dep_wait.append({'id': t['id'], 'order': t['order'], 'missing': missing})
            continue
        if t['owner'] or (t['lease'] and not t['lease'].startswith('RELEASED')):
            held.append({'id': t['id'], 'order': t['order'], 'reason': 'OWNER_OR_UNVERIFIED_LEASE'})
            continue
        available.append({'id': t['id'], 'order': t['order']})
    return {
        'policy': 'GLOBAL_ASCENDING_ORDER_ELIGIBLE_ONLY',
        'selected': available[0] if available else None,
        'available': available,
        'held': held,
        'dependency_wait': dep_wait,
        'independent_review': review,
        'idle_permitted': not available and not review,
        'note': 'Read-only decision. Writer MUST atomically claim and revalidate; never infer activity from this result.',
    }


def verify_claim_choice(result, chosen_id):
    """Fail if a worker plans to skip a lower-order, unclaimed eligible task."""
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
