#!/usr/bin/env python3
"""Fail-closed DEV public catalog probe with redacted, deterministic errors.

This gate never changes tenant bindings, products, images or authorization.
Empty 200 catalogs are valid; positive product/image coverage is provided by
isolated CI fixtures, while live nonempty image references remain strict.
Every response is inspected in-memory; no business names/IDs/URLs are logged.
"""
import json
import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path
from urllib.parse import urlsplit

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
    for item in data:
        require(isinstance(item, dict) and type(item.get("id")) is int
                and item["id"] > 0 and isinstance(item.get("nombre"), str)
                and bool(item["nombre"].strip()), "CATEGORIES_INVALID_ITEM_SHAPE")
    return data


def products_from_response(status, payload):
    data = expect_success("PRODUCTS", status, payload)
    require(isinstance(data, dict) and isinstance(data.get("items"), list),
            "PRODUCTS_INVALID_ITEMS_SHAPE")
    require(all(isinstance(item, dict) for item in data["items"]),
            "PRODUCT_ITEM_INVALID_SHAPE")
    return data["items"]


def canonical_cloudinary_image(url):
    if not isinstance(url, str) or not url or any(c.isspace() for c in url):
        return False
    parts = urlsplit(url)
    return (parts.scheme == "https" and parts.netloc == "res.cloudinary.com"
            and not parts.username and not parts.password and not parts.fragment
            and CLOUDINARY_PATH.search(parts.path) is not None)


def images_from_products(items):
    if not items:
        return []  # No product means no live image is expected.
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
    # Preserve the original GitHub runner curl transport and its timeouts.
    # Only the response is parsed in Python; body, URL and headers stay private.
    with tempfile.TemporaryDirectory(prefix="solqaryn-dev-catalog-") as directory:
        body_path = str(Path(directory) / "response.json")
        args = ["curl", "--silent", "--show-error", "--connect-timeout", "8",
                "--max-time", "30", "--output", body_path,
                "--write-out", "%{http_code}", url]
        try:
            result = subprocess.run(args, capture_output=True, timeout=36)
        except (OSError, subprocess.TimeoutExpired):
            raise GateError(label + "_TRANSPORT_UNAVAILABLE") from None
        require(result.returncode == 0, label + "_TRANSPORT_UNAVAILABLE")
        raw_code = result.stdout.strip()
        require(re.fullmatch(rb"[0-9]{3}", raw_code) is not None,
                label + "_INVALID_HTTP_CODE")
        status = int(raw_code)
        try:
            with open(body_path, "rb") as response:
                data = response.read(2_000_001)
        except OSError:
            raise GateError(label + "_RESPONSE_NOT_SAVED") from None
        require(len(data) <= 2_000_000, label + "_BODY_TOO_LARGE")
        try:
            return status, json.loads(data)
        except (ValueError, UnicodeDecodeError):
            raise GateError(label + "_NON_JSON_HTTP_" + str(status)) from None


def verify_images(urls):
    for url in urls:
        args = ["curl", "--location", "--silent", "--show-error",
                "--connect-timeout", "8", "--max-time", "30", "--output",
                "/dev/null", "--write-out", "%{http_code}\n%{url_effective}", url]
        try:
            result = subprocess.run(args, capture_output=True, timeout=36)
        except (OSError, subprocess.TimeoutExpired):
            raise GateError("IMAGE_TRANSPORT_UNAVAILABLE") from None
        require(result.returncode == 0, "IMAGE_TRANSPORT_UNAVAILABLE")
        parts = result.stdout.decode("utf-8", "replace").splitlines()
        require(len(parts) == 2 and re.fullmatch(r"[0-9]{3}", parts[0]) is not None,
                "IMAGE_INVALID_HTTP_CODE")
        require(canonical_cloudinary_image(parts[1]),
                "IMAGE_REDIRECT_OUTSIDE_CANONICAL_NAMESPACE")
        require(parts[0] == "200", "IMAGE_HTTP_" + parts[0])


def main():
    require(os.getenv("FRONTEND") == FRONTEND, "FRONTEND_NOT_CANONICAL_DEV")
    category_status, category_payload = get_json(FRONTEND + "/api/tienda/categorias", "CATEGORIES")
    categories = categories_from_response(category_status, category_payload)
    print("DEV_CATEGORIES_SCHEMA=PASS")
    product_status, product_payload = get_json(
        FRONTEND + "/api/tienda/productos?pagina=1&tamano=100", "PRODUCTS")
    products = products_from_response(product_status, product_payload)
    print("DEV_PRODUCTS_SCHEMA=PASS")
    if not products:
        print("DEV_CATALOG_PRODUCTS=EMPTY_VALID")
        print("DEV_IMAGE_HTTP=NOT_APPLICABLE_EMPTY_PRODUCTS")
        print("API_CATALOG_PRODUCTS_IMAGES=PASS_EMPTY_CONTRACT")
        return
    images = images_from_products(products)
    print("DEV_CLOUDINARY_NAMESPACE=PASS")
    verify_images(images)
    print("API_CATALOG_PRODUCTS_IMAGES=PASS_WITH_LIVE_IMAGES")


if __name__ == "__main__":
    try:
        main()
    except GateError as exc:
        print("::error title=DEV catalog/products/images::" + str(exc), file=sys.stderr)
        sys.exit(1)
