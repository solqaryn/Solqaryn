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

    def test_rejected_review_keeps_original_author_not_last_reviewer(self):
        origin = "6ad7bbf79c488191bf893e6c08ea9b5f"
        last_reviewer = "6ad7bc0d48748191aa1dd0ce2f18d22a"
        third_party = "6ad7bc1cc7b8819190665e5ed78610d4"
        commercial, runtime = tables((
            1, "REVIEW", "VALIDANDO", "", "",
            f"RELEASED|OWNER={last_reviewer}|ORIGINAL_AUTHOR={origin}|TOKEN=prev",
        ))
        runtime[1][5] = "https://github.com/solqaryn/Solqaryn/pull/3691"
        self.assertEqual(eligible_independent_reviews(
            commercial, runtime, expected_count=1, reviewer=origin), [])
        ready = eligible_independent_reviews(
            commercial, runtime, expected_count=1, reviewer=third_party)
        self.assertEqual(ready[0]["author"], origin)
        self.assertEqual(ready[0]["id"], "REVIEW")

    def test_missing_provenance_cannot_be_accepted_for_review(self):
        c, r = tables((1, "REVIEW", "VALIDANDO", "", "", "RELEASED|TOKEN=only"))
        r[1][5] = "https://github.com/solqaryn/Solqaryn/pull/3691"
        self.assertEqual(eligible_independent_reviews(c, r, expected_count=1), [])

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


    def test_owner_suspension_skips_only_explicitly_suspended_pendings(self):
        c, r = tables(
            (4, "P0-002.B", "PENDIENTE", "", "", "RECOVERED|FENCING_TOKEN=prior"),
            (21, "P0-011.A", "PENDIENTE", "P0-012.B", "", ""),
            (24, "P0-012.B", "LISTO", "", "", ""),
        )
        r[1][7] = "PENDIENTE SIN OWNER; SUSPENDIDO HASTA NUEVA ORDEN DEL PROPIETARIO"
        result = dispatch(c, r, expected_count=3)
        self.assertEqual(result["selected"]["id"], "P0-011.A")
        self.assertEqual(result["owner_suspended"][0]["id"], "P0-002.B")
        self.assertTrue(verify_claim_choice(result, "P0-011.A"))
        with self.assertRaisesRegex(UnsafeSnapshot, "OUT_OF_ORDER_CLAIM"):
            verify_claim_choice(result, "P0-002.B")

    def test_plain_pending_and_unrelated_next_action_remain_eligible(self):
        c, r = tables(
            (1, "A", "PENDIENTE", "", "", ""),
            (2, "B", "PENDIENTE", "", "", ""),
        )
        r[1][7] = "VALIDAR CONDICIONES, LOCK, OWNER Y HEAD ANTES DE TRABAJAR"
        r[2][7] = "SUSPENDIDO POR PRUEBA; revisar (no orden del propietario)"
        decision = dispatch(c, r, expected_count=2)
        self.assertEqual(decision["selected"]["id"], "A")
        self.assertEqual(decision["owner_suspended"], [])

    def test_owner_suspension_case_whitespace_and_lift_by_action(self):
        c, r = tables((1, "A", "PENDIENTE", "", "", ""),
                      (2, "B", "PENDIENTE", "", "", ""))
        r[1][7] = " pendiente sin owner;   suspendido hasta nueva orden del propietario "
        self.assertEqual(dispatch(c, r, expected_count=2)["selected"]["id"], "B")
        r[1][7] = "VALIDAR CONDICIONES, LOCK, OWNER Y HEAD ANTES DE TRABAJAR"
        self.assertEqual(dispatch(c, r, expected_count=2)["selected"]["id"], "A")

    def test_all_suspended_does_not_authorize_idle_claim(self):
        c, r = tables((1, "A", "PENDIENTE", "", "", ""))
        r[1][7] = "SUSPENDIDO HASTA NUEVA ORDEN DEL PROPIETARIO"
        result = dispatch(c, r, expected_count=1)
        self.assertIsNone(result["selected"])
        self.assertTrue(result["idle_permitted"])
        with self.assertRaisesRegex(UnsafeSnapshot, "NO_ELIGIBLE_TASK_FOR_CLAIM"):
            verify_claim_choice(result, "A")

    def test_suspension_cannot_bypass_dependencies_or_wip(self):
        c, r = tables(
            (1, "SUSPENDED", "PENDIENTE", "", "", ""),
            (2, "WAITING", "PENDIENTE", "DEPENDENCY", "", ""),
            (3, "DEPENDENCY", "PENDIENTE", "", "", ""),
            (4, "OWNED", "EN_PROGRESO", "", "worker-one", "ACTIVE"),
        )
        r[1][7] = "SUSPENDIDO HASTA NUEVA ORDEN DEL PROPIETARIO"
        result = dispatch(c, r, expected_count=4, candidate_owner="worker-one")
        self.assertIsNone(result["selected"])
        self.assertIn("CANDIDATE_OWNER_WIP_LIMIT_EXCEEDED", result["candidate_status"])
        self.assertEqual(result["dependency_wait"][0]["id"], "WAITING")
        self.assertEqual(result["owner_suspended"][0]["id"], "SUSPENDED")

    def test_suspension_does_not_allow_self_review(self):
        c, r = tables((1, "A", "PENDIENTE", "", "", ""),
                      (2, "B", "VALIDANDO", "", "",
                       "RELEASED|ORIGINAL_AUTHOR=6ad7bbf79c488191bf893e6c08ea9b5f"))
        r[1][7] = "SUSPENDIDO HASTA NUEVA ORDEN DEL PROPIETARIO"
        r[2][5] = "https://github.com/solqaryn/Solqaryn/pull/3706"
        self.assertEqual(
            eligible_independent_reviews(c, r, expected_count=2,
                reviewer="6ad7bbf79c488191bf893e6c08ea9b5f"), [])
        self.assertEqual(
            eligible_independent_reviews(c, r, expected_count=2,
                reviewer="6abd62255ae88191a2dba6e1b00d3b4d")[0]["id"], "B")


if __name__ == '__main__':
    unittest.main()
