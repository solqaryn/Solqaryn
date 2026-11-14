"""Offline positive and hostile fixtures for DEV tenant identity certification."""
import unittest
import verify_dev_final_tenant_identity as gate


class DevFinalTenantIdentityTests(unittest.TestCase):
    def setUp(self):
        self.a = ("tienda-a", 2, "Empresa A")
        self.b = ("tienda-b", 7, "Empresa B")
        self.raw = "\n".join(
            key + "\t" + str(company) + "\t" + name.encode().hex()
            for key, company, name in (self.a, self.b)
        )

    def test_two_distinct_bindings_positive(self):
        self.assertEqual(gate.read_bindings(self.raw), [self.a, self.b])

    def test_duplicate_company_cannot_replace_second_tenant(self):
        duplicate = "otra-tienda\t2\t" + "Empresa A".encode().hex()
        with self.assertRaises(gate.GateError):
            gate.read_bindings(self.raw.splitlines()[0] + "\n" + duplicate)

    def test_invalid_store_key_fails_closed(self):
        with self.assertRaises(gate.GateError):
            gate.read_bindings(self.raw.replace("tienda-a", "../tienda", 1))

    def test_invalid_company_encoding_fails_closed(self):
        with self.assertRaises(gate.GateError):
            gate.read_bindings(self.raw.replace("456d70726573612041", "FF", 1))

    def test_unscoped_503_only_exact_isolation(self):
        valid = {"code": gate.ISOLATION, "success": False}
        self.assertTrue(gate.isolated(503, valid))
        for code, payload in [
            (200, valid),
            (503, {"code": "API_UPSTREAM_UNAVAILABLE"}),
            (503, {"code": gate.ISOLATION, "success": True}),
            (403, valid),
            (503, None),
        ]:
            with self.subTest(code=code, payload=payload):
                self.assertFalse(gate.isolated(code, payload))

    def test_binding_company_must_match_verified_server(self):
        valid = {"success": True, "data": {"empresaId": 2, "verificada": True}}
        self.assertTrue(gate.verified_binding(200, valid, 2))
        self.assertFalse(gate.verified_binding(200, valid, 7))
        self.assertFalse(gate.verified_binding(503, valid, 2))
        self.assertFalse(gate.verified_binding(200, {
            "success": True, "data": {"empresaId": True, "verificada": True},
        }, 1))
        self.assertFalse(gate.verified_binding(200, {
            "success": True, "data": {"empresaId": 2, "verificada": False},
        }, 2))

    def test_identity_must_match_mysql_company_name(self):
        valid = {"success": True, "data": {"nombreComercial": "Empresa A"}}
        self.assertTrue(gate.verified_identity(200, valid, "Empresa A"))
        self.assertFalse(gate.verified_identity(200, valid, "Empresa B"))
        self.assertFalse(gate.verified_identity(503, valid, "Empresa A"))
        self.assertFalse(gate.verified_identity(200, {
            "success": True, "data": {"nombreComercial": ""},
        }, "Empresa A"))
        self.assertFalse(gate.verified_identity(200, {
            "success": False, "data": {"nombreComercial": "Empresa A"},
        }, "Empresa A"))


if __name__ == "__main__":
    unittest.main()
