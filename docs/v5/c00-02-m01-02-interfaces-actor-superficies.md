# C00-02.M01.02 — Entradas, salidas, actores y superficies del modelo V5

**Proyecto:** SOLQARYN, repositorio `solqaryn/Solqaryn`, integración ordinaria en `dev`.
**Fuente única de microtareas:** `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL`, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, 2.340 IDs de `C00-01.M01.01` a `C15-05.M06.06`.
**Registro institucional:** Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.
**Contrato de calidad:** V13 A0–A5 vigente en `docs/AUTOMATION_V13.md`, sin revivir FIFO por PR, ramas de trabajo ni proveedores retirados.

## Mapa de entradas, validaciones y salidas

| Entrada | Autoridad y verificación | Salida permitida |
| --- | --- | --- |
| ID V5, descripción, `RESULTADO_EXIGIDO`, dependencias | Google Doc V5 y `PLAN_MAESTRO!A:G`; no añadir IDs desde memoria o historias | Una unidad de trabajo trazable por ID, con aceptación medible |
| `PLAN_MAESTRO!H`, custodio `I`, evidencia `J:M` | Estado mutable sólo en PLAN; comprobar `PENDIENTE` y responsable vacío antes del claim | `EN_PROGRESO`, `VALIDANDO`, `LISTO` o bloqueo justificado y leíble |
| `COLA!E/G/O` y dependencias por fórmula | Leer valores y `userEnteredValue.formulaValue`; `G=SI`, `O=SI` para iniciar | Selección de una PENDIENTE elegible, sin sobrescribir `COLA!A:Q` |
| Identidad física TASK_ID, cuenta propietaria, conexión GitHub | Autenticación efectiva, allowlist, permiso real y horario físico; nombres no bastan | Actor y responsable reales conservados hasta cierre |
| HEAD vivo de `dev` y versión de Skill/arquitectura | Comparar Skill instalada con HEAD canónico, revisar Scope Lock y diferencias de código | Candidato verificado sobre base actual; commit sólo fast-forward sin force push |
| Código/documento/pruebas y resultados CI del SHA | A0 calidad, A1 funcional, A2 seguridad, A3 integración proporcionales; A4 exact-SHA tras publicar | Evidencia causal sin inventar PASS, LISTO DEV sólo después de gates pertinentes |
| `BITACORA!A:Z` y EVENT_ID | 26 columnas vigentes, evento único, validaciones y readback; nunca borrar historia | Ledger idempotente de cada transición y revisión |
| Resultado A5 de otra Task | TASK_ID distinto de autor, evidencia exact-SHA, permisos separados | Revisión independiente, **no** promoción automática a QA/main/PROD |

## Actores y límites de propiedad

- **Titular humano:** autoriza alcance, conexión y cambios sensibles; ni el prompt ni una cuenta alterna suplantan su decisión.
- **Task desarrolladora autorizada:** verifica preflight, elige un único scope libre, reclama con TASK_ID genuino, implementa, audita A0–A4 y registra transiciones. Escribe sólo `PLAN_MAESTRO!H:M` del ID propio y nuevos eventos `BITACORA`.
- **Task revisora independiente:** puede emitir A5 documentada para código ajeno, nunca autocertificar sus aportes, cambiar autoría ni conceder una promoción.
- **Cuenta GitHub efectiva:** publica exclusivamente en `solqaryn/Solqaryn` rama `dev` sin forzar refs; no crea PR ni ramas para el flujo vigente.
- **Google Sheets:** autoridad mutable del estado en PLAN; COLA es una proyección por fórmulas, y BITACORA es evidencia inmutable. Un rechazo del proveedor no es éxito.
- **GitHub Actions y runtimes de prueba:** verifican únicamente el código/HEAD y alcance exigido. Un estado `PENDING`, `SKIPPED`, `CANCELLED` o `FAILURE` no equivale a `PASS` requerido.

## Superficies y fronteras de confianza

| Superficie | Lectura/acción legítima | Riesgo y prueba de denegación |
| --- | --- | --- |
| Documento V5 / Google Drive | Leer autoridad y requisitos | Plan histórico o ID ajeno no autoriza claim |
| PLAN_MAESTRO | Actualizar H:M sólo del scope validado | Responsabilidad activa de otra Task impide escritura |
| COLA | Leer E/I/J/K/L/M/G/O y sus fórmulas | La Task nunca sustituye fórmulas derivadas por texto |
| BITACORA | Adjuntar evento de 26 campos, leerlo de vuelta | EVENT_ID existente impide un registro duplicado |
| GitHub dev | Publicación fast-forward secuencial con HEAD releído | Divergencia o Scope Lock no acreditado impide publicar |
| CI y proveedores de prueba | Leer jobs exact SHA e inspeccionar logs causales | Resultado de otro SHA o job SKIPPED no certifica el cambio |
| Frontend Angular / API .NET / MySQL / integraciones | Pruebas sólo según superficie efectivamente modificada | Aislamiento empresa A/B, JWT/RBAC, PII y secretos son controles del backend y seguridad |
| QA/main/PROD y configuración de Tasks | Fuera de una microtarea ordinaria de dev | A5_PASS y LISTO_DEV no conceden una autorización de despliegue |

## Intercambio mínimo verificable

1. **Entrada:** ID `PENDIENTE`, `COLA!G/O=SI`, sin responsable, HEAD canónico y permisos confirmados. **Salida:** `EN_PROGRESO` con TASK_ID auténtico y EVENT_ID único; readback positivo.
2. **Entrada:** candidato completo dentro del ID. **Salida:** `VALIDANDO` con el mismo responsable, huellas de fuente y evento; A0–A3 se ejecutan **después** de esta transición.
3. **Entrada:** A0–A3 conformes y HEAD rereleído. **Salida:** publicación directa fast-forward con SHA real (o evidencia-only cuando proceda, explicitando que no hubo cambios en runtime).
4. **Entrada:** resultado publicado, seguridad y CI pertinente en éxito. **Salida:** A4/readback y `LISTO`, con A5 pendiente de otro actor. Si falta un gate, conservar `VALIDANDO` y la siguiente acción.
5. **Entrada:** auditoría A5 con independencia comprobada. **Salida:** registro de revisión, **sin** tocar QA/main/PROD.

## Aceptación y exclusiones de esta microtarea

**Positivo:** todas las entradas y salidas tienen autoridad, actor, evidencia y superficie concretas; un observador puede reconstruir el ciclo de un ID sin consultar planes históricos.

**Negativo de duplicación:** dos Tasks ven la misma PENDIENTE, pero sólo una adquiere custodia genuina; la segunda, al releer, no escribe ni registra un claim ficticio.

**Negativo de seguridad:** una cuenta no autorizada o un cambio crítico de API/tenancy sin pruebas no obtiene permiso por estar habilitada; el scope queda bloqueado sin degradar controles.

**Negativo de integridad:** fórmula perdida, EVENT_ID duplicado o SHA/CI incorrectos impiden declarar LISTO.

Este documento identifica interfaces y actores, **no implementa** nuevos esquemas, workers, endpoints, permisos, conectores, formularios ni reglas de negocio. A0/A1/A2/A3 consisten aquí en validación de la coherencia documental y seguridad de límites; Node/Angular/.NET/MySQL/E2E son NO_APLICA por ausencia de cambios de runtime. A4 debe verificar el nuevo SHA y Scope Lock exacto y hacer readback PLAN/COLA/BITACORA. Rollback mediante `git revert` normal del commit documental, nunca con force-push ni borrado del ledger.

> **Corrección documental por A5_FAIL (2026-11-13):** lectura institucional del encabezado `BITACORA!A1:Z1` confirmó 26 columnas, desde `EVENT_ID` hasta `TIMESTAMP_UTC_ORIGINAL`. El formato heredado de 21 columnas no sirve como contrato para nuevas escrituras. Se conservan los eventos históricos, sus validaciones y autoría; cada nuevo evento debe comprobar esquema vigente y readback. Esta corrección no representa A5_PASS ni autorización para QA/main/PROD.
