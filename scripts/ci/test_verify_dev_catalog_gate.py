"""Offline positive and hostile fixtures for the DEV public catalog gate."""
import unittest
import json
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch
import verify_dev_catalog_gate as gate


class CatalogGateTests(unittest.TestCase):
    def test_curl_transport_preserves_http_code_without_leaks(self):
        def fake_run(args, **_kwargs):
            Path(args[args.index("--output") + 1]).write_text(
                json.dumps({"success": False, "code": gate.ISOLATION}))
            return SimpleNamespace(returncode=0, stdout=b"503")
        with patch.object(gate.subprocess, "run", side_effect=fake_run):
            status, payload = gate.get_json(gate.FRONTEND + "/api/tienda/categorias", "CATEGORIES")
        self.assertEqual(status, 503)
        with self.assertRaisesRegex(gate.GateError, "TENANT_GUARD"):
            gate.categories_from_response(status, payload)

    def test_curl_transport_error_is_explicit(self):
        with patch.object(gate.subprocess, "run", return_value=SimpleNamespace(
                returncode=6, stdout=b"000")):
            with self.assertRaisesRegex(gate.GateError, "TRANSPORT_UNAVAILABLE"):
                gate.get_json(gate.FRONTEND + "/api/tienda/categorias", "CATEGORIES")

    def test_valid_categories_and_products(self):
        self.assertEqual(gate.categories_from_response(200, {"success": True, "data": [{"id": 1, "nombre": "Demo"}]}), [{"id": 1, "nombre": "Demo"}])
        self.assertEqual(gate.products_from_response(200, {
            "success": True, "data": {"items": [{"imagenPrincipalUrl": "https://res.cloudinary.com/riyrzmob/image/upload/v1/solqaryn_dev/a.jpg"}]}
        }), [{"imagenPrincipalUrl": "https://res.cloudinary.com/riyrzmob/image/upload/v1/solqaryn_dev/a.jpg"}])

    def test_guard_is_not_a_pass(self):
        for fn in (gate.categories_from_response, gate.products_from_response):
            with self.subTest(fn=fn.__name__), self.assertRaisesRegex(gate.GateError, "TENANT_GUARD"):
                fn(503, {"success": False, "code": gate.ISOLATION})

    def test_other_503_does_not_impersonate_guard(self):
        for fn in (gate.categories_from_response, gate.products_from_response):
            with self.subTest(fn=fn.__name__), self.assertRaisesRegex(gate.GateError, "HTTP_503"):
                fn(503, {"code": "UPSTREAM_UNAVAILABLE"})

    def test_empty_categories_are_legitimate(self):
        self.assertEqual(gate.categories_from_response(
            200, {"success": True, "data": []}), [])

    def test_empty_products_are_legitimate(self):
        self.assertEqual(gate.products_from_response(
            200, {"success": True, "data": {"items": []}}), [])
        self.assertEqual(gate.images_from_products([]), [])

    def test_malformed_category_or_product_item_is_rejected(self):
        for entry in (1, {"id": True, "nombre": "x"},
                      {"id": 3, "nombre": ""}, {"id": 4}):
            with self.subTest(entry=entry), self.assertRaisesRegex(
                    gate.GateError, "CATEGORIES_INVALID_ITEM_SHAPE"):
                gate.categories_from_response(200, {"success": True, "data": [entry]})
        with self.assertRaisesRegex(gate.GateError, "PRODUCT_ITEM_INVALID_SHAPE"):
            gate.products_from_response(
                200, {"success": True, "data": {"items": ["invalid"]}})

    def test_empty_live_catalog_skips_image_http_but_rejects_bad_tenant(self):
        with patch.dict(gate.os.environ, {"FRONTEND": gate.FRONTEND}):
            with patch.object(gate, "get_json", side_effect=[
                (200, {"success": True, "data": []}),
                (200, {"success": True, "data": {"items": []}})
            ]) as probe, patch.object(gate, "verify_images") as image_probe, patch("builtins.print") as report:
                gate.main()
        self.assertEqual(probe.call_count, 2)
        image_probe.assert_not_called()
        self.assertIn("API_CATALOG_PRODUCTS_IMAGES=PASS_EMPTY_CONTRACT",
                      [call.args[0] for call in report.call_args_list])

        with patch.dict(gate.os.environ, {"FRONTEND": gate.FRONTEND}):
            with patch.object(gate, "get_json", side_effect=[
                (200, {"success": True, "data": []}),
                (503, {"success": False, "code": gate.ISOLATION})
            ]):
                with self.assertRaisesRegex(gate.GateError, "TENANT_GUARD"):
                    gate.main()

    def test_populated_fixture_still_checks_live_images(self):
        url = "https://res.cloudinary.com/riyrzmob/image/upload/v1/solqaryn_dev/a.jpg"
        with patch.dict(gate.os.environ, {"FRONTEND": gate.FRONTEND}):
            with patch.object(gate, "get_json", side_effect=[
                (200, {"success": True, "data": [{"id": 2, "nombre": "Empresa A"}]}),
                (200, {"success": True, "data": {"items": [{"imagenPrincipalUrl": url}]}})
            ]), patch.object(gate, "verify_images") as image_probe, patch("builtins.print") as report:
                gate.main()
        image_probe.assert_called_once_with([url])
        self.assertIn("API_CATALOG_PRODUCTS_IMAGES=PASS_WITH_LIVE_IMAGES",
                      [call.args[0] for call in report.call_args_list])

    def test_invalid_payloads_fail_closed(self):
        for data in (None, {}, "", 4, [1]):
            with self.subTest(data=data), self.assertRaises(gate.GateError):
                gate.products_from_response(200, {"success": True, "data": data})
        with self.assertRaisesRegex(gate.GateError, "INVALID_SUCCESS"):
            gate.categories_from_response(200, {"success": False, "data": [1]})

    def test_no_image_is_diagnostic_not_success(self):
        with self.assertRaisesRegex(gate.GateError, "IMAGES_EMPTY"):
            gate.images_from_products([{"imagenes": []}])

    def test_canonical_images(self):
        url = "https://res.cloudinary.com/riyrzmob/image/upload/v1/solqaryn_dev/a.jpg"
        self.assertTrue(gate.canonical_cloudinary_image(url))
        self.assertEqual(gate.images_from_products([{"imagenPrincipalUrl": url,
                                                     "imagenes": [{"url": url}]}]), [url])

    def test_cross_namespace_and_hosts_rejected(self):
        for url in (
            "http://res.cloudinary.com/riyrzmob/image/upload/v1/solqaryn_dev/a.jpg",
            "https://evil.invalid/riyrzmob/image/upload/v1/solqaryn_dev/a.jpg",
            "https://res.cloudinary.com/other/image/upload/v1/solqaryn_dev/a.jpg",
            "https://res.cloudinary.com/riyrzmob/image/upload/v1/other/a.jpg",
            "https://res.cloudinary.com@evil.invalid/riyrzmob/image/upload/v1/solqaryn_dev/a.jpg",
        ):
            with self.subTest(url=url), self.assertRaisesRegex(gate.GateError, "IMAGES_OUTSIDE"):
                gate.images_from_products([{"imagenPrincipalUrl": url}])

    def test_bad_gallery_shape_is_diagnostic(self):
        with self.assertRaisesRegex(gate.GateError, "PRODUCT_IMAGES_INVALID_SHAPE"):
            gate.images_from_products([{"imagenes": "nope"}])
        with self.assertRaisesRegex(gate.GateError, "PRODUCT_IMAGE_INVALID_ITEM"):
            gate.images_from_products([{"imagenes": [4]}])


if __name__ == "__main__":
    unittest.main()
