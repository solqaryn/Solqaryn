# SOLQARYN V5 FINAL — C15-02.M01.04: riesgos, supuestos y decisiones de auditoría de arquitectura/migración

**Fecha local:** 2026-11-14. **Autor:** PRIMARY DEV 01 JAVIER (`6adbb9fff1548191a1bd5b30d9e9984a`). **Ámbito:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** análisis de riesgos POTENCIALES_NO_VERIFICADOS; no se declara hallazgo explotable, proveedor activo, inspección de credenciales ni migración ejecutada.

**Fuentes rectoras:** STANDARD V5 FINAL (Drive ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN/COLA/DAG (Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`), AGENTS.md, PROJECT_SCOPE_LOCK, AUTOMATION_STATUS, Skill canónica y referencias de arquitectura del HEAD `dev`. Dependencias documentales leídas: `docs/v5/c15-02-m01-01-alcance-no-regresion-auditoria-arquitectura-migracion.md`, `docs/v5/c15-02-m01-02-actores-interfaces-fronteras-auditoria-migracion.md` y `docs/v5/c15-02-m01-03-inventario-fuentes-evidencia-migracion.md`. El inventario M01.03 enumera 21 rutas Git, blobs exactos y doce brechas NO_ACREDITADAS. La condición operativa de M01.04 procede de PLAN fila 2201, COLA G/O=SI y DAG F=VALIDADO/G=SI, con claim CAS propio, sin liberación terminal.

## Matriz de 14 riesgos potenciales — sin prueba causal

| ID | Riesgo potencial | Superficie o referencia estática | Estado de evidencia | Control futuro verificable |
| --- | --- | --- | --- | --- |
| R01 | Suplantación por host/header/body | Límite JWT/tenant del API | NO_VERIFICADO | Comprobar host binding y 401/403 cross-tenant |
| R02 | BOLA/IDOR de archivos y recursos | AppDbContext + storage | NO_VERIFICADO | HTTP/SQL fixture A/B y ownership |
| R03 | Migración MySQL no reversible | EF/migrations | NO_VERIFICADO | Restore efímero/checksum/rollback antes de tocar datos |
| R04 | Paridad de proveedor de archivos incompleta | Cloudinary actual/R2 futuro | NO_VERIFICADO | Inventario real, cero comprobable, hashes y TTL |
| R05 | Exposición de secretos en CI | GitHub Actions/entornos | NO_VERIFICADO | Revisión de logs redactados y scopes |
| R06 | Reintento duplica efectos | Outbox/worker | NO_VERIFICADO | Idempotencia transaccional, crash/replay |
| R07 | DNS/TLS interrumpe sesiones | Host, certificados y CORS | NO_VERIFICADO | Ensayo de conmutación y reversión autorizado |
| R08 | Backup existe pero no restaura | Runbook DEV | NO_VERIFICADO | Restore descartable y RPO/RTO medido |
| R09 | Divergencia de configuración entre proveedores | Vercel/Render/Aiven objetivo | NO_VERIFICADO | Matriz declarada/observada, flags secretos por entorno |
| R10 | Ruta Angular oculta autorización insuficiente | Angular route guard/API | NO_VERIFICADO | UI negativa + API 403 con rol revocado |
| R11 | Evidencia CI de SHA distinto | GitHub workflows | NO_VERIFICADO | Check-runs completed/success exact-SHA |
| R12 | Entorno objetivo interpretado como ya activo | Hostinger/Cloudflare/R2 | NO_VERIFICADO | Readback autorizado sin credenciales en docs |
| R13 | A5 propia o reviewer retirado | Seis Tasks de Javier | NO_VERIFICADO | Task ID distinto, allowlist y BITACORA |
| R14 | Plan de rollback no accionable | C15-02 y runbook | NO_VERIFICADO | Ensayo revert/restore sin operación productiva |

Las filas son hipótesis verificables, no incidentes, vulnerabilidades demostradas ni prioridades P0/P1 certificadas. `NO_VERIFICADO` significa que faltan pruebas causales, no que el control sea inexistente. Se descarta la inferencia «no hay evidencia de fallo» → «seguridad certificada».

## Diez supuestos sujetos a confirmación

| ID | Supuesto falsable (no certificado) |
| --- | --- |
| S01 | Vercel/Render/Aiven siguen siendo referencias documentadas, no estados live comprobados |
| S02 | Cloudinary está declarada en dependencias, sin conteo remoto acreditado |
| S03 | La futura topología Hostinger/Cloudflare/R2 no está contratada ni desplegada por esta unidad |
| S04 | MySQL 8.4 es referencia arquitectónica; la versión de servicio vivo no se leyó aquí |
| S05 | Se dispone de 2340 IDs institucionales, pero esta unidad no certifica su cumplimiento integral |
| S06 | El JWT y el RBAC del servidor deben impedir acceso aunque falle Angular |
| S07 | Una prueba sólo será PASS cuando el job corresponda al SHA y al entorno correctos |
| S08 | Las pruebas de migración deben usar servicios efímeros sin acceso productivo |
| S09 | A5 distinta es necesaria antes de cualquier promoción y no la autoriza por sí sola |
| S10 | El inventario histórico puede ser cero, pero no se presupone cero sin verificación completa |

Cuando un supuesto no se pueda corroborar, la fase afectada queda `NO_GO`, pero otras microtareas documentales independientes pueden continuar. Ni existencia de código, ni nombre de un proveedor, ni una fila DAG sustituyen autorización para operar servicios reales.

## Diez decisiones abiertas: no autorizaciones implícitas

| ID | Decisión necesaria antes de pruebas/operaciones posteriores |
| --- | --- |
| D01 | Aprobar alcance de auditoría real y responsables por proveedor, sin conceder acceso automático |
| D02 | Elegir estrategia de binding DNS/TLS y host seguro con tests negativos |
| D03 | Determinar plan de backup/restore MySQL, criterios de datos y RPO/RTO medibles |
| D04 | Definir cómo inventariar Cloudinary y, si existe, migrar a R2 preservando ownership |
| D05 | Establecer artefactos/CI causales de Angular, .NET, MySQL y seguridad para cada etapa |
| D06 | Acordar criterio de rollback por proveedor, DNS y estado transaccional |
| D07 | Fijar protección de secretos y logs de migración con redacción y auditoría |
| D08 | Decidir si hay scope futuro de worker/proveedor y cómo se valida idempotencia sin nuevo dispatcher V5 |
| D09 | Definir matriz de escenarios A/B/JWT/RBAC, recursos públicos y expiración de links |
| D10 | Determinar autoridad para QA/main/PROD y pruebas independientes antes de cutover |

Toda decisión que implique dominio, secretos, MySQL vivo, Cloudflare/Hostinger/R2, QA, main o PROD requiere aprobación diferenciada y evidencia de rollback. Esta microtarea no compra ni activa proveedor alguno, no transfiere archivos, no programa Tasks ni implementa webhooks, dispatcher o workers.

## Ocho escenarios positivos y negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro | Negativo bloqueante |
| --- | --- | --- |
| PN01 | Host/tenant legítimo resuelve propietario correcto | Header o slug manipulado cambia tenant |
| PN02 | Recurso A autorizado sólo a A | Actor A consulta recurso B y obtiene datos |
| PN03 | Migración con restore y checksums exitosos en entorno efímero | Backup existente pero no restaurable se da por PASS |
| PN04 | Inventario Cloudinary/R2 posee conteo real y ownership | Se confunde DESCONOCIDO con cero |
| PN05 | JWT/roles impiden acceso revocado vía HTTP | Guard UI concede permiso backend |
| PN06 | Worker/retry conserva efecto único y tenant de origen | Reintento duplica o cruza tenant |
| PN07 | DNS/TLS es reversible bajo aprobación separada | Documento DEV provoca cutover real |
| PN08 | CI/Scope Lock/A5 coherentes con commit y autoría distintos | CHECK CANCELLED/SKIPPED, SHA distinto o autor propio PASS |

No se ejecutaron peticiones HTTP, SQL/MySQL de integración, Playwright, Docker, pruebas DNS/TLS, restores o consultas de objetos remotos. Las pruebas sólo serán PASS con actor, tenant, entorno, SHA exacto, observación, evidencia y reversión verificables. En particular, `INVENTARIO_DESCONOCIDO != CERO`, `OBJETIVO_V5 != DESPLEGADO` y `SKIPPED/CANCELLED/IN_PROGRESS != PASS`.

## Impacto, controles A0–A5 y reversión

- **Cambio limitado:** el presente Markdown y sección autocontenida de `CHANGELOG_AI.md` en dev; consumidor siguiente `C15-02.M01.05` para criterios observables y posteriormente M01.06 con gate terminal. NO libera M02 ni migraciones.
- **A0:** 14 riesgos, diez supuestos, diez decisiones diferenciados y trazados a las fuentes M01.01–03, sin duplicación de autoridad. Verificar ausencia de encabezados de CHANGELOG vacíos.
- **A1:** ocho casos positivos/negativos **futuros no ejecutados** para fundamentar aceptación; no se afirman pruebas funcionales.
- **A2:** no se modifica JWT/RBAC, aislamiento tenant A/B, API, DB, integraciones, secretos ni permisos; controles de seguridad pendientes de verificación posterior.
- **A3:** consistencia documental contra HEAD y blobs; builds .NET/Angular, MySQL/Playwright y pruebas reales no son prueba causal del diff Markdown y se clasifican `NO_APLICA`.
- **A4:** commit fast-forward/CAS dev; diff exclusivamente dos Markdown, Scope Lock y V5 provenance `completed/success` exact-SHA; readback PLAN/COLA/BITACORA para marcar LISTO_DEV. Otros checks queued/in_progress no se cuentan PASS.
- **A5:** revisión independiente obligatoria de otra Task Javier de TASK_ID distinto; no autoaprobación ni QA/main/PROD.
- **Rollback:** `git revert` normal tras HEAD vivo y control concurrente, sin borrar eventos históricos ni reescribir ramas.

**Resultado solicitado:** preparar decisiones y pruebas de auditoría. No se afirma seguridad operacional completa, migración ni paridad real de proveedores.
