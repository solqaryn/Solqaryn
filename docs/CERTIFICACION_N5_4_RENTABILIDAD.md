# N5.4 — Rentabilidad · certificación canónica

Autoridad operativa: `docs/VAEP_AUTHORITY.md`.

## Alcance certificado

N5.4 establece rentabilidad absoluta de ventas usando únicamente datos y contratos existentes. La cadena material cerrada es `N5.4.A → N5.4.B → N5.4.C → N5.4.D → N5.4.E → N5.4.F → N5.4.G`. `N5.4.H` documenta y certifica ese resultado sin reabrir scopes ya aceptados.

La semántica aceptada es deliberadamente acotada: vendedor y cliente usan total de venta persistido, costo persistido y utilidad bruta; producto y categoría usan subtotal de línea, costo histórico y utilidad bruta. El porcentaje de margen continúa explícitamente no expuesto porque su denominador contractual no está resuelto, y no se inventa asignación de descuento de cabecera por línea.

## Superficie resultante

- Backend: DTO/servicio/API de rentabilidad absoluta con agrupaciones `Vendedor`, `Cliente`, `Producto` y `Categoria` y validación de filtros fail-closed.
- Datos: la persistencia existente ya contiene los insumos absolutos necesarios; N5.4 no introdujo una migración nueva para esta capacidad.
- Frontend: página y componentes de rentabilidad bajo Centro de Reportes, filtros y estados de carga/vacío/error, sin fabricar porcentaje de margen.
- Seguridad: autenticación y `Ventas/Ver`; los filtros no pueden ampliar el alcance efectivo autorizado.
- Auditoría/observabilidad: las lecturas válidas del controller de rentabilidad registran auditoría acotada con agrupación y `HttpContext.TraceIdentifier` como correlación, sin payload sensible. Las consultas inválidas fallan antes de servicio/auditoría.
- QA: pruebas de contrato preservan `[Authorize]`, `Ventas/Ver` en los cuatro endpoints, orden audit-before-result para consultas válidas y fail-closed para query inválida.

## Evidencia de cierre

- `N5.4.A`: `vaep/evidence/fragments/N5.4.A_LISTO_REAL_20260910T0447Z.json`.
- `N5.4.B`: `vaep/evidence/fragments/N5.4.B_LISTO_REAL_20260910T0520Z.json`.
- `N5.4.C`: `vaep/evidence/fragments/N5.4.C_LISTO_REAL_20260910T0544Z.json`.
- `N5.4.D`: `vaep/evidence/fragments/N5.4.D_LISTO_REAL_20260910T0626Z.json`.
- `N5.4.E`: `vaep/evidence/fragments/N5.4.E_LISTO_REAL_20260910T0732Z.json`.
- `N5.4.F`: `vaep/evidence/fragments/N5.4.F_LISTO_REAL_20260910T0754Z.json`.
- `N5.4.G`: `vaep/evidence/fragments/N5.4.G_LISTO_REAL_20260910T0758Z.json`.
- `N5.4.F REVIEW_FIRST`: `vaep/evidence/reviews/N5.4.F_REVIEW_FIRST_20260910T0753Z.json`.
- `N5.4.G QA/CI REVIEW_FIRST`: `vaep/evidence/fragments/N5.4.G_REVIEW_FIRST_20260910T0757Z.json`.

## Recovery y ownership

F1/J1 y F2/J2 superaron el umbral de diez minutos sin `sessionId` correlacionado ni actividad útil verificable. Se transfirió ownership a `CHATGPT_VAEP` antes de escribir sobre sus scopes; issues `#3268` y `#3270` conservan la evidencia de supersession. Los cambios controller/test fueron resueltos por takeover acotado y los resultados Jules tardíos quedaron `EVIDENCE_ONLY`.

Un manifest R2 de F1 fue generado por autorefill después de la transferencia de ownership, pero no produjo workflow run y fue eliminado antes de cualquier segunda ejecución material. Por tanto no existe un segundo writer aceptado ni un R2 de contenido consumido; R3 no se utilizó.

## Gates causales

El functional head de N5.4.F/G es `df5eca6fa7ccd66f06031a050141ac247dcffec4`.

En ese SHA, `Desarrollo - Compilación y pruebas` run `34451766432` verificó como `SUCCESS` los gates causales aplicables, incluyendo:

- `Backend Release y pruebas` — Release build + pruebas backend no integración `SUCCESS`.
- `Frontend producción` — lint/build de regresión `SUCCESS`.
- `Docker y aislamiento de entornos` — `SUCCESS`.
- `Higiene del repositorio` — `SUCCESS`.

El workflow de auditoría/API/MySQL `34451771184` alcanzó backend build/tests y arranque API con MySQL en `SUCCESS` antes de ser cancelado durante su tramo frontend por churn de control-plane; ese tail no se convierte en un blocker causal de N5.4.F/G. De igual forma, un fallo de `VAEP engine` por receipts históricos inválidos no se atribuye al delta funcional de N5.4.

No se exige una migración nueva ni E2E nuevo para F/G: F no modificó persistencia ni flujo frontend; la superficie frontend ya había sido certificada en N5.4.E y el build de producción permanece verde.

## DoD y deuda

- REVIEW_FIRST: aceptado para los scopes materiales y para QA/CI.
- P0 abierto atribuible a N5.4: `0`.
- P1 abierto atribuible a N5.4: `0`.
- R3: prohibido y no utilizado.
- Filler/busywork: no utilizado; F tuvo exactamente dos scopes source-backed y G cerró desde evidencia causal existente en lugar de fabricar pruebas redundantes.
- `main`, Producción, secretos y merge de PR #2 permanecen fuera de alcance.

## Rollback

No se aplicó ningún cambio a Producción. Un rollback de código se realiza únicamente mediante un changeset posterior en `Desarrollo`, revirtiendo el commit causal correspondiente y repitiendo gates aplicables. No existe rollback de esquema específico para F/G porque no hubo delta de persistencia.

## Resultado

N5.4 queda materialmente listo para cerrar `N5.4.H` cuando este documento pase REVIEW_FIRST contra los receipts A–G y se confirme que la evidencia causal sigue vigente. El siguiente parent ordenado es `N5.5.A — Reportes de compras — Auditoría y preflight`; no debe publicarse como CURRENT antes de `N5.4.H LISTO_REAL`.
