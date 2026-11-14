# N8.16.A — PRE / gobierno de matrices

Estado: `LISTO_REAL`

Baseline de entrada: `a601929b5a2e366d7ac13f4375dbc4446a78aa4f` con `N8.15 = LISTO_REAL`.

## Objetivo

Congelar el contrato de identidad y cambio para las matrices de evaluación antes de ampliar o modificar plantillas. Esta etapa no altera producto, runtime, esquema, rutas ni datos.

## Identidad estable

Toda interfaz material certificada debe tener un `MATRIX_ID` estable e inmutable. El ID identifica el contrato semántico, no su posición visual, fila de Sheet, orden de menú ni número de tarea.

Formato canónico:

`VAEP-MX::<DOMAIN_SLUG>::<CONTRACT_SLUG>`

Reglas:

1. `DOMAIN_SLUG` y `CONTRACT_SLUG` usan mayúsculas ASCII, dígitos y guion bajo.
2. El ID se asigna una sola vez cuando el candidato se incorpora al catálogo canónico.
3. Reordenar menú, tabla, carpetas o backlog **no** cambia el ID.
4. Renombrar una etiqueta visible **no** cambia el ID si el contrato material es el mismo.
5. Si el contrato realmente se divide, fusiona o cambia de identidad semántica, se crea una relación explícita `SUPERSEDES`/`SPLIT_FROM`/`MERGED_FROM`; no se recicla un ID retirado.
6. Está prohibido usar identidad basada sólo en índice (`ROW-123`, `MATRIX-42`, posición en CSV/Sheet o equivalente).
7. Ningún candidato puede pasar a estado `MATERIAL` sin `MATRIX_ID` registrado.

## Identidad de cambio

Cada cambio material de una matriz usa `MATRIX_CHANGE_ID` independiente del `MATRIX_ID`:

`VAEP-MXC::<MATRIX_ID_SLUG>::<UTC_YYYYMMDDTHHMMSSZ>::<SHORT_SHA>`

El cambio se liga a commit/evidencia. Una corrección editorial sin impacto contractual puede compartir la misma `MATRIX_VERSION`; una modificación de contrato incrementa `MATRIX_VERSION` de forma monotónica.

## Tipos de contrato

Tipos permitidos: `FEATURE_GROUP`, `SCREEN`, `BUSINESS_DIALOG`, `EMBEDDED_INTERACTIVE`, `SHELL`, `SHARED_PRIMITIVE`.

`FEATURE_GROUP` es un contenedor de descubrimiento/ownership y no sustituye matrices hijas cuando N8.17 confirme que una pantalla, diálogo, widget o primitive es material. Un hijo confirmado material debe recibir su propio `MATRIX_ID` antes de certificarse.

## Fuente baseline

El inventario canónico N8.15 certificó 158 registros ancla arquitectónicos, incluyendo 48 feature roots frontend. N8.16 gobierna identidad y trazabilidad; **no** convierte automáticamente los 158 anchors en 158 contratos UI.

## Invariantes de gobierno

- una identidad semántica -> un `MATRIX_ID` canónico;
- un `MATRIX_ID` -> una entrada de catálogo activa o históricamente trazable;
- una matriz material -> un solo owner contractual;
- padre/hijo explícito y acíclico;
- no duplicados ocultos por alias de ruta/nombre;
- no materialidad implícita: un candidato interno permanece `DISCOVERED`/`UNKNOWN` hasta evaluación, y si se promueve a `MATERIAL`, primero se registra ID;
- cambios de gobierno requieren `MATRIX_CHANGE_ID`, REVIEW_FIRST y readback.

## REVIEW_FIRST

P0=0, P1=0. El esquema evita identidad por orden visual, no renumera activos existentes por posición y no inventa materialidad de contratos aún no inspeccionados.

## Resultado

`N8.16.A = LISTO_REAL`.

Siguiente dependency-valid: `N8.16.B — DOMAIN`.
