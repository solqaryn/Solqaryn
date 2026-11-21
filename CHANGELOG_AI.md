## 2026-11-20 — DEV06 V22 reparación proporcional C05-04.M05.06 teclado real Chromium
- Nuevo quinto escenario Playwright: Tab/Enter desde la navegacion real, summary y alternador de densidad, sin `.focus()`, con unica consulta GET y teclado como criterio falsable de accesibilidad; CI exact-SHA y estado VALIDANDO hasta resultado real.
- Workflow actualiza la exigencia 4→5 casos reales, sin bajar gates ni afirmar CWV de campo, dispositivos fisicos, JWT/HTTP/MySQL o A5. Riesgo R2 test-only, rollback con git revert normal.

