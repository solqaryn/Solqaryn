#!/usr/bin/env python3
"""Fail-closed DEV public catalog probe with redacted, deterministic errors.

This gate never changes tenant bindings, products, images or authorization.
Every response is inspected in-memory; no business names/IDs/URLs are logged.
"""
import json
import os
import re
import sys
from urllib.error import HTTPError, URLError
from urllib.parse import urlsplit
from urllib.request import Request, urlopen

FRONTEND = "https://solqaryn-dev.vercel.app"
ISOLATION = "TENANT_DATA_ISOLATION_REQUIRED"
CLOUDINARY_PATH = re.compile(r"/riyrzmob/.+/solqaryn_dev/[^/]+")


class GateError(RuntimeError):
    pass


def require(condition, code):
    if not condition:
        raise GateError(code)


def expect_success(kind, status, payload):
    if (status == 503 and isinstance(payload, dict)
            and payload.get("code") == ISOLATION):
        raise GateError(kind + "_TENANT_GUARD_REQUIRES_SCOPED_POSITIVE")
    require(status == 200, kind + "_HTTP_" + str(status))
    require(isinstance(payload, dict) and payload.get("success") is True,
            kind + "_INVALID_SUCCESS_ENVELOPE")
    return payload.get("data")


def categories_from_response(status, payload):
    data = expect_success("CATEGORIES", status, payload)
    require(isinstance(data, list), "CATEGORIES_INVALID_DATA_SHAPE")
    require(len(data) > 0, "CATEGORIES_EMPTY")
    return data


def products_from_response(status, payload):
    data = expect_success("PRODUCTS", status, payload)
    require(isinstance(data, dict) and isinstance(data.get("items"), list),
            "PRODUCTS_INVALID_ITEMS_SHAPE")
    require(len(data["items"]) > 0, "PRODUCTS_EMPTY")
    return data["items"]


def canonical_cloudinary_image(url):
    if not isinstance(url, str) or not url or any(c.isspace() for c in url):
        return False
    parts = urlsplit(url)
    return (parts.scheme == "https" and parts.netloc == "res.cloudinary.com"
            and not parts.username and not parts.password and not parts.fragment
            and CLOUDINARY_PATH.search(parts.path) is not None)


def images_from_products(items):
    urls = set()
    for item in items:
        require(isinstance(item, dict), "PRODUCT_ITEM_INVALID_SHAPE")
        gallery = item.get("imagenes")
        if gallery is None:
            gallery = []
        require(isinstance(gallery, list), "PRODUCT_IMAGES_INVALID_SHAPE")
        choices = [item.get("imagenPrincipalUrl")]
        for image in gallery:
            require(isinstance(image, dict), "PRODUCT_IMAGE_INVALID_ITEM")
            choices.append(image.get("url"))
        for url in choices:
            if url is not None and url != "":
                require(canonical_cloudinary_image(url),
                        "IMAGES_OUTSIDE_CANONICAL_NAMESPACE")
                urls.add(url)
    require(bool(urls), "IMAGES_EMPTY")
    return sorted(urls)


def get_json(url, label):
    req = Request(url, headers={"Cache-Control": "no-store",
                                "User-Agent": "SOLQARYN-DEV-CatalogGate/1.0"})
    try:
        with urlopen(req, timeout=30) as response:
            status, data = response.status, response.read(2_000_001)
            final_url = response.geturl()
    except HTTPError as response:
        status, data = response.code, response.read(2_000_001)
        final_url = response.geturl()
    except (URLError, TimeoutError, OSError):
        raise GateError(label + "_TRANSPORT_UNAVAILABLE") from None
    require(final_url.startswith(FRONTEND + "/api/"),
            label + "_UNEXPECTED_REDIRECT")
    require(len(data) <= 2_000_000, label + "_BODY_TOO_LARGE")
    try:
        return status, json.loads(data)
    except (ValueError, UnicodeDecodeError):
        raise GateError(label + "_NON_JSON_HTTP_" + str(status)) from None


def verify_images(urls):
    for url in urls:
        req = Request(url, headers={"User-Agent": "SOLQARYN-DEV-CatalogGate/1.0"})
        try:
            with urlopen(req, timeout=30) as response:
                status, final_url = response.status, response.geturl()
        except HTTPError as response:
            status, final_url = response.code, response.geturl()
        except (URLError, TimeoutError, OSError):
            raise GateError("IMAGE_TRANSPORT_UNAVAILABLE") from None
        require(canonical_cloudinary_image(final_url),
                "IMAGE_REDIRECT_OUTSIDE_CANONICAL_NAMESPACE")
        require(status == 200, "IMAGE_HTTP_" + str(status))


def main():
    require(os.getenv("FRONTEND") == FRONTEND, "FRONTEND_NOT_CANONICAL_DEV")
    category_status, category_payload = get_json(FRONTEND + "/api/tienda/categorias", "CATEGORIES")
    categories = categories_from_response(category_status, category_payload)
    print("DEV_CATEGORIES_SCHEMA_AND_NONEMPTY=PASS count=" + str(len(categories)))
    product_status, product_payload = get_json(
        FRONTEND + "/api/tienda/productos?pagina=1&tamano=100", "PRODUCTS")
    products = products_from_response(product_status, product_payload)
    print("DEV_PRODUCTS_SCHEMA_AND_NONEMPTY=PASS count=" + str(len(products)))
    images = images_from_products(products)
    print("DEV_CLOUDINARY_NAMESPACE=PASS count=" + str(len(images)))
    verify_images(images)
    print("API_CATALOG_PRODUCTS_IMAGES=PASS")


if __name__ == "__main__":
    try:
        main()
    except GateError as exc:
        print("::error title=DEV catalog/products/images::" + str(exc), file=sys.stderr)
        sys.exit(1)
