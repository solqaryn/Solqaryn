"""Pruebas puras de clasificacion, sin Cloudinary, Aiven ni red."""

import unittest
from scripts.audit_cloudinary_dev_locations import classify_locator


class CloudinaryDevLocatorAuditTests(unittest.TestCase):
    def test_url_de_producto_dev_canonico(self):
        url = ("https://res.cloudinary.com/riyrzmob/image/upload/v1790367580/"
               "solqaryn_dev/solqaryn/productos/empresas/1/photo.jpg")
        public_id = "solqaryn_dev/solqaryn/productos/empresas/1/photo"
        self.assertEqual(classify_locator(url, public_id, "solqaryn/productos/"), "canonico")

    def test_url_dev_antigua_en_carpeta_no_canonica(self):
        public_id = "solqaryn_dev/origen-previo/productos/empresas/1/imagen-9"
        url = "https://res.cloudinary.com/riyrzmob/image/upload/v1790367580/" + public_id + ".jpg"
        self.assertEqual(classify_locator(url, public_id, "solqaryn/productos/"), "dev_carpeta_no_canonica")

    def test_prefijo_qa_y_prod_no_se_confunde_con_dev(self):
        for env in ("solqaryn_qa", "solqaryn_prod"):
            public_id = env + "/solqaryn/productos/empresas/1/photo"
            url = "https://res.cloudinary.com/riyrzmob/image/upload/v1/" + public_id + ".jpg"
            self.assertEqual(
                classify_locator(url, public_id, "solqaryn/productos/"),
                "origen_ajeno_o_entorno_cruzado",
            )

    def test_cloud_externo_no_es_compatible(self):
        public_id = "solqaryn_dev/solqaryn/productos/empresas/1/photo"
        url = "https://res.cloudinary.com/otra-cuenta/image/upload/v1/" + public_id + ".jpg"
        self.assertEqual(
            classify_locator(url, public_id, "solqaryn/productos/"),
            "origen_ajeno_o_entorno_cruzado",
        )

    def test_url_y_publicid_inconsistentes(self):
        url = ("https://res.cloudinary.com/riyrzmob/image/upload/v1/"
               "solqaryn_dev/solqaryn/productos/empresas/2/photo.jpg")
        public_id = "solqaryn_dev/solqaryn/productos/empresas/1/photo"
        self.assertEqual(
            classify_locator(url, public_id, "solqaryn/productos/"),
            "locator_incompleto_o_inconsistente",
        )

    def test_campos_vacios_e_incompletos(self):
        self.assertEqual(classify_locator("", "", "solqaryn/productos/"), "sin_referencia")
        self.assertEqual(
            classify_locator("https://res.cloudinary.com/riyrzmob/image/upload/v1/a.jpg", "", "solqaryn/productos/"),
            "locator_incompleto_o_inconsistente",
        )

    def test_perfiles_y_compras_usan_su_carpeta(self):
        for resource, suffix in (("perfiles", "jpg"), ("compras", "pdf")):
            public_id = f"solqaryn_dev/solqaryn/{resource}/resource-1"
            url = f"https://res.cloudinary.com/riyrzmob/raw/upload/v1/{public_id}.{suffix}"
            self.assertEqual(classify_locator(url, public_id, f"solqaryn/{resource}/"), "canonico")


if __name__ == "__main__":
    unittest.main()
