"""Tests for global, non-idling, dependency-safe and non-skipping VAEP dispatch."""
import unittest
from commercial_dispatch import UnsafeSnapshot, dispatch, verify_claim_choice, audit_wip_violations, eligible_independent_reviews


COMMERCIAL_HEADER = ['ID','PROGRAMA_PRIORIDAD','MICROTAREA','CRITERIO_EXIGIDO','ORDEN','PLAN_COMERCIAL_DOC_ID','VERSION_FUENTE','UNIDAD_ORIGEN_LANE']
RUNTIME_HEADER = ['ID','ESTADO','OWNER_AUTOMATION_ID','LEASE_STATUS','HEAD','EVIDENCIA','BLOQUEO','NEXT_ACTION','UPDATED_AT','DEPENDENCIAS_ACTUALES','DEPENDENCIAS_OK','PRIORIDAD_ACTUAL','P0','P1','ROLLBACK','RESULTADO','NOTA_RUNTIME','RESERVED']


def tables(*items):
    commercial, runtime = [COMMERCIAL_HEADER], [RUNTIME_HEADER]
    for order, ident, state, deps, owner, lease in items:
        commercial.append([ident, '', ident, '', str(order), '', '', 'NO_LANE_RESTRICTIONS'])
        runtime.append([ident, state, owner, lease, '', '', '', '', '', deps, '', '', '', '', '', '', '', ''])
    return commercial, runtime


def pick(*items):
    return dispatch(*tables(*items), expected_count=len(items))


class CommercialDispatchTests(unittest.TestCase):
    def test_reviewable_validando_comes_before_pending(self):
        from commercial_dispatch import eligible_independent_reviews
        author = "6ad7bbf79c488191bf893e6c08ea9b5f"
        reviewer = "6ad7bc0d48748191aa1dd0ce2f18d22a"
        comm, runtime = tables(
            (1, "PENDING", "PENDIENTE", "", "", ""),
            (2, "REVIEW", "VALIDANDO", "", "", "RELEASED|OWNER=" + author),
        )
        runtime[2][5] = "https://github.com/solqaryn/Solqaryn/pull/3691"
        first = eligible_independent_reviews(comm, runtime, expected_count=2, reviewer=reviewer)
        self.assertEqual(first[0]["id"], "REVIEW")
        self.assertEqual(eligible_independent_reviews(
            comm, runtime, expected_count=2, reviewer=author), [])
        runtime[2][2] = author
        self.assertEqual(eligible_independent_reviews(
            comm, runtime, expected_count=2, reviewer=reviewer), [])

    def test_bootstrap_forward_dependency_is_valid(self):
        result = pick(
            (1, 'P0-001.A', 'PENDIENTE', 'P0-012.B', '', ''),
            (2, 'P0-001.B', 'PENDIENTE', 'P0-001.A | P0-012.B', '', ''),
            (23, 'P0-012.A', 'PENDIENTE', '', '', ''),
            (24, 'P0-012.B', 'PENDIENTE', 'P0-012.A', '', ''),
        )
        self.assertEqual(result['selected']['id'], 'P0-012.A')
        self.assertEqual(len(result['dependency_wait']), 3)

    def test_32_eligible_workers_take_ten_distinct_lowest_orders(self):
        items = [(i, f'T-{i:03d}', 'PENDIENTE', '', '', '') for i in range(1, 33)]
        claimed = []
        for slot in range(10):
            result = pick(*items)
            winner = result['selected']
            self.assertEqual(winner['order'], slot + 1)
            self.assertEqual(len(result['available']), 32-slot)
            self.assertTrue(verify_claim_choice(result, winner['id']))
            claimed.append(winner['id'])
            items[slot] = (slot + 1, winner['id'], 'EN_PROGRESO', '', f'worker-{slot}', 'ACTIVE')
        self.assertEqual(len(set(claimed)), 10)

    def test_lowest_order_overrides_priority_label_and_lane(self):
        result = pick(
            (30, 'HIGH_PRIORITY', 'PENDIENTE', '', '', ''),
            (3, 'LOW_PRIORITY', 'PENDIENTE', '', '', ''),
        )
        self.assertEqual(result['selected']['id'], 'LOW_PRIORITY')
        with self.assertRaisesRegex(UnsafeSnapshot, 'OUT_OF_ORDER_CLAIM'):
            verify_claim_choice(result, 'HIGH_PRIORITY')

    def test_busy_first_work_goes_to_next_free_not_idle(self):
        result = pick((1,'A','PENDIENTE','','other','ACTIVE'), (2,'B','PENDIENTE','','',''))
        self.assertEqual(result['selected']['id'], 'B')
        self.assertFalse(result['idle_permitted'])

    def test_lease_unknown_is_never_claimable(self):
        result = pick((1,'A','PENDIENTE','','','UNKNOWN'), (2,'B','PENDIENTE','','',''))
        self.assertEqual(result['selected']['id'], 'B')

    def test_review_is_useful_even_if_no_build_ready(self):
        result = pick((1,'A','VALIDANDO','','','RELEASED'), (2,'B','PENDIENTE','A','',''))
        self.assertIsNone(result['selected'])
        self.assertEqual(result['independent_review'], ['A'])
        self.assertFalse(result['idle_permitted'])

    def test_blocked_chain_allows_other_independent_work(self):
        result = pick((1,'A','PENDIENTE','B','',''), (2,'B','VALIDANDO','','',''), (3,'C','PENDIENTE','','',''))
        self.assertEqual(result['selected']['id'], 'C')

    def test_no_tasks_ready_permits_wait(self):
        result = pick((1,'A','PENDIENTE','B','',''), (2,'B','BLOQUEADO','','',''))
        self.assertTrue(result['idle_permitted'])

    def test_long_dependency_chain_no_recursion_limit(self):
        items = [(i, f'T-{i:04d}', 'PENDIENTE', f'T-{i-1:04d}' if i > 1 else '', '', '') for i in range(1, 1124)]
        result = pick(*items)
        self.assertEqual(result['selected']['id'], 'T-0001')
        self.assertEqual(len(result['dependency_wait']), 1122)

    def test_unknown_dependency_fails_closed(self):
        with self.assertRaisesRegex(UnsafeSnapshot, 'UNKNOWN_DEPENDENCY'):
            pick((1,'A','PENDIENTE','MISSING','',''))

    def test_cycle_fails_closed(self):
        with self.assertRaisesRegex(UnsafeSnapshot, 'CYCLIC_DEPENDENCY'):
            pick((1,'A','PENDIENTE','B','',''), (2,'B','PENDIENTE','A','',''))

    def test_mismatch_and_duplicate_fails_closed(self):
        commercial, runtime = tables((1,'A','PENDIENTE','','',''), (2,'B','PENDIENTE','','',''))
        runtime[2][0] = 'C'
        with self.assertRaisesRegex(UnsafeSnapshot, 'ID_MISMATCH'):
            dispatch(commercial, runtime, expected_count=2)
        commercial, runtime = tables((1,'A','PENDIENTE','','',''), (1,'B','PENDIENTE','','',''))
        with self.assertRaisesRegex(UnsafeSnapshot, 'DUPLICATE_ORDER'):
            dispatch(commercial, runtime, expected_count=2)

    def test_invalid_status_fails_closed(self):
        with self.assertRaisesRegex(UnsafeSnapshot, 'INVALID_STATE'):
            pick((1,'A','HACER','','',''))

    def test_mismatched_count_fails_closed(self):
        commercial, runtime = tables((1,'A','PENDIENTE','','',''))
        with self.assertRaisesRegex(UnsafeSnapshot, 'PLAN_RUNTIME_COUNT_MISMATCH'):
            dispatch(commercial, runtime)

    def test_audit_wip_violations_detects_multiple_assignments(self):
        _, runtime = tables(
            (1, 'A', 'EN_PROGRESO', '', 'sup17', 'ACTIVE'),
            (2, 'B', 'VALIDANDO', '', 'sup17', 'ACTIVE_REVIEW'),
            (3, 'C', 'EN_PROGRESO', '', 'sup41', 'ACTIVE'),
            (4, 'D', 'PENDIENTE', '', '', ''),
        )
        violations = audit_wip_violations(runtime)
        self.assertIn('sup17', violations)
        self.assertEqual(violations['sup17'], ['A', 'B'])
        self.assertNotIn('sup41', violations)

    def test_candidate_owner_with_active_task_blocked_from_new_claim(self):
        result = dispatch(
            *tables(
                (1, 'A', 'EN_PROGRESO', '', 'worker1', 'ACTIVE'),
                (2, 'B', 'PENDIENTE', '', '', ''),
            ),
            expected_count=2,
            candidate_owner='worker1',
        )
        self.assertIsNone(result['selected'])
        self.assertIn('CANDIDATE_OWNER_WIP_LIMIT_EXCEEDED:A', result['candidate_status'])

    def test_candidate_owner_without_active_task_is_eligible(self):
        result = dispatch(
            *tables(
                (1, 'A', 'EN_PROGRESO', '', 'worker1', 'ACTIVE'),
                (2, 'B', 'PENDIENTE', '', '', ''),
            ),
            expected_count=2,
            candidate_owner='worker2',
        )
        self.assertEqual(result['selected']['id'], 'B')
        self.assertEqual(result['candidate_status'], 'ELIGIBLE')


if __name__ == '__main__':
    unittest.main()
