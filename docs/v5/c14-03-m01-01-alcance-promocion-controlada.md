# SOLQARYN V5 FINAL — C14-03.M01.01: objetivo, exclusiones y no-regresión de promoción controlada

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 02 JAVIER (`6af16bb9492c8191b8e8c7d606f11507`). **Repositorio/entorno autorizado:** `solqaryn/Solqaryn@dev` con `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** preparación documental de alcance M01.01. **No es** autorización para desplegar, modificar QA/main/PROD ni certificar paridad operativa.

**Autoridades:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md` y Skill canónica. Preflight de este ID: PLAN fila 2090 H=PENDIENTE/I vacío, COLA G/O=SI, DAG_TECNICO F=VALIDADO/G=SI, claim condicional y lectura de owner. Las dependencias de C12, C14-01 y C14-02 son referencias del contrato preparatorio; **no** implican autorización física de promoción.

## 1. Objetivo y resultado futuro demostrable

Diseñar un protocolo de promoción reproducible que mantenga separados **DEV**, la validación de **QA** y cualquier eventual autorización para **producción**. Toda transición futura debe relacionar un SHA Git exacto, árboles y artefactos inmutables, configuración por entorno sin secretos, versiones .NET/Angular/MySQL, pruebas pertinentes, evidencias de seguridad y tenancy, plan de rollback ensayado, aprobador competente y lectura posterior de salud y consistencia.

El resultado requerido del lote C14-03 será un **checklist auditable de promoción y evidencia de paridad**, donde paridad significa compatibilidad demostrada de contratos, configuraciones y comportamiento relevante sin copiar secretos ni bases productivas. Esta microtarea únicamente define el no-alcance y las propiedades que no pueden degradarse. Los workflows existentes no prueban automáticamente equivalencia de ambientes.

Quedan expresamente excluidos de este cambio: commits a `main` o `qa`, merge, deploy, migración SQL, DNS/TLS, modificación de dominios, secretos o proveedores, entrada a Hostinger/Cloudflare, activación de servicios de pago, manipulación de bases productivas, reejecución arbitraria de CI, creación de Tasks, PR/branches y un dispatcher/eventos. Solo se añaden este Markdown y su entrada autocontenida en CHANGELOG.

## 2. Invariantes de no-regresión NR01–NR16

| ID | Control a preservar | Condición que debe bloquear una promoción futura |
| --- | --- | --- |
| NR01 | Identidad del artefacto | SHA del artefacto publicado no coincide con el SHA probado |
| NR02 | Separación de ramas/entornos | Un LISTO_DEV o A5 se interpreta como orden para escribir QA/main/PROD |
| NR03 | Gobernanza Scope Lock | Check obligatorio del exact SHA no está `completed/success` |
| NR04 | CI causal | Build/test requerido skipped, cancelled, failed, queued o in_progress se cuenta PASS |
| NR05 | Autorización de cambio | No existe permiso independiente y explícito del propietario para entorno no-dev |
| NR06 | Configuración segura | Se copian tokens, secretos, .env sensibles o claves de producción al repositorio/CI |
| NR07 | JWT y revocación | Ambiente destino acepta credenciales expiradas, inactivas o de otro scope |
| NR08 | RBAC y propiedad | Un permiso de UI se trata como control suficiente del backend |
| NR09 | Tenancy A/B | Recursos, respuestas, cachés, archivos o jobs cruzan empresas/sucursales |
| NR10 | Contratos HTTP | Una ruta o DTO de destino rompe consumidores sin política de compatibilidad |
| NR11 | Persistencia MySQL | Migración destructiva, backup no verificado o transacción irreconciliable |
| NR12 | Datos comerciales | Inventario, ventas, saldos y documentos históricos pierden consistencia |
| NR13 | Observabilidad | No existen health checks, alertas, correlación y registro redactado por versión |
| NR14 | Rollback verificable | No hay reversión segura ni criterio explícito de abortar el despliegue |
| NR15 | Interfaces y accesibilidad | Cambios visibles introducen regresión responsive, navegación o foco sin control |
| NR16 | Evidencia institucional | BITACORA histórica se reescribe o revisión A5 es autoaprobada |

Los controles de seguridad, datos y runtime se verifican en el futuro mediante evidencia causal proporcional: .NET 10, Angular 22.2.1, MySQL 8.4, JWT/RBAC, tenant A/B y pruebas de integración cuando realmente correspondan. Aquí **no se ejecutó** ninguna de esas suites, migraciones o despliegues.

## 3. Pares de casos de aceptación futuros: P01–P10 y N01–N10

| Caso | Positivo que deberá documentarse y comprobarse | Negativo bloqueante |
| --- | --- | --- |
| P01/N01 | SHA y checksum de artefactos coinciden entre build probado y destino | Release sin SHA o artefacto mutable |
| P02/N02 | Conjunto de checks obligatorios concluido exitosamente para ese SHA | CI pendiente, fallida, cancelada u omitida tratada como éxito |
| P03/N03 | Aprobación del entorno y A5 de Task distinta al autor constan en evidencia | Propietario no autoriza cambio de entorno o revisor=autor |
| P04/N04 | Variables requeridas se comparan por nombres/tipos, sin exponer valores secretos | Copiar JWT, credenciales Aiven o llaves al commit o reporte |
| P05/N05 | Autenticación y autorización backend mantienen contrato de tenant/rol | Token de A o permiso oculto permite leer recurso de B |
| P06/N06 | Esquema MySQL compatible y rollback comprobado con snapshot aislado | DDL destructivo no reversible o integridad referencial perdida |
| P07/N07 | Smoke HTTP/Angular y Playwright reales pasan con fixtures sintéticos | Fuente .spec o navegador simulado se registra como E2E real |
| P08/N08 | Observabilidad correlaciona deployment, build, errores y readiness | 5xx, cola o consulta DB degradada sin diagnóstico/alarma |
| P09/N09 | Rollback devuelve versión funcional sin borrar negocio legítimo | Reset/force-push o borrado de datos/auditoría usado como reversión |
| P10/N10 | PLAN/COLA/BITACORA y readback documentan cada gate y responsable | Doble owner, estados inventados, dependencia DAG NO ignorada |

Estado de todos los pares: `FUTURO_NO_EJECUTADO`. Son oráculos para la fase posterior, no resultados de una prueba física.

## 4. Esquema de evidencia y separación de facultades

Toda futura ficha de promoción debe registrar: `RELEASE_CANDIDATE_ID`, `SOURCE_SHA`, `TARGET_ENV`, `ARTIFACT_DIGEST`, versiones de stack, matriz de variables no sensibles, lista de tests con `job_id/status/conclusion/head_sha`, riesgos P0/P1, decisión de aceptación, reviewer distinto del author, permiso expreso por entorno, ventana de reversión, resultado de salud y firma del responsable. Ninguna columna ni nota del Sheet sustituye las autorizaciones reales de infraestructura.

La cadena propuesta es: **DEV A0–A4** documentados → **A5 independiente** de otra Task Javier → autorización futura separada para QA → validación dirigida y paridad → autorización futura separada para main/PROD. El diseño de la cadena no habilita su ejecución automática. Las seis Tasks permanecen en DEV y no cambian su programación.

## 5. Gates de esta unidad documental y rollback

- **A0:** comprobación estática de alcance, exclusiones y NR01–NR16.
- **A1:** cobertura lógica de los diez pares P/N definidos; todos futuros y no ejecutados.
- **A2:** preservación documental de JWT, RBAC, pertenencia tenant A/B, datos financieros y secretos sin modificar runtime.
- **A3:** coherencia con arquitectura y documentos de gobierno; builds backend/UI/DB ajenos a dos Markdown son `NO_APLICA` motivado.
- **A4:** comprobar commit fast-forward con CAS de HEAD, diff exacto de dos Markdown, Scope Lock y procedencia CHANGELOG `completed/success` del SHA publicado, readback PLAN/COLA/BITACORA.
- **A5:** requiere una Task Javier distinta de `6af16bb9492c8191b8e8c7d606f11507`; `A5_PENDIENTE` después de LISTO_DEV. No habilita QA/main/PROD.

**Reversión del documento:** revert normal revisado del changeset aditivo en `dev`, preservando BITACORA y procedencia. Nunca force-push, reset destructivo, rollback de datos reales ni cambios de infraestructura por esta unidad.
