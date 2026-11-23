

## 2026-11-23 — SOLQARYN V26 / C01-03.M04.02 (reparación de prueba adversarial)
- Repara una falsa medición de overflow por `zoom:2` aplicado al HTML, observada en GitHub-hosted `45168772580` (13 PASS y 1 FAIL; ancho 640 en viewport 320).
- La nueva regresión Playwright usa ampliación de texto raíz al 200%, mantiene las negativas tenant A/B y verifica reflow en cinco tamaños. No cambia aplicación, API, JWT, permisos ni MySQL. CI del SHA nuevo pendiente; ni LISTO ni A5 certificados. Se preserva el historial y no se declara zoom nativo.
