# SOLQARYN V5 FINAL — C15-02.M02.06: dependencias técnicas y contrato de salida de arquitectura/migración

**Autor:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`; fecha 2026-11-14 America/Tegucigalpa; rama `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT.

## 1. Autoridad y naturaleza
Documento de cierre conceptual del lote M02 C15-02, no certificado de despliegue. Fuente única: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, documento `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO/COLA/DAG_TECNICO y BITACORA. Antes del claim exclusivo, M01.01–06 y M02.01–05 estaban LISTO con A5 independiente; DAG F=VALIDADO/G=SI y COLA G/O=SI; solo este scope se reclamó por owner CAS.

El contrato preserva las cinco contribuciones previas: M02.01 ENT01–16/INV01–16, M02.02 API01–12/ERR01–12, M02.03 DAT01–12/IDX01–12/MIG01–08/FX01–12, M02.04 S01–06/CFG01–14, M02.05 EV01–14/ID01–12. Cada artefacto es conceptual; sus pruebas negativas permanecen FUTURAS_NO_EJECUTADAS y no autorizan integración real.

## 2. Barreras de salida a implementación (OUT01–OUT16)

| ID | Gate invariable antes de operación posterior | Evidencia requerida |
| --- | --- | --- |
| OUT01 | Un tenant se deriva de sesión y membresía verificada en backend | Test A/B, nunca EmpresaId del cliente |
| OUT02 | Revocación RBAC actual impide operar con token obsoleto | Autorización negativa |
| OUT03 | Recursos y adaptadores tienen owner acreditado | Censo vivo, ausencia equivale NO_ACREDITADO |
| OUT04 | Empresas A/B permanecen aisladas en EF y API | Pruebas HTTP y consultas efímeras |
| OUT05 | FK compuestas no enlazan datos entre tenants | MySQL 8.4 efímero negativo |
| OUT06 | NULL/legado se pone en cuarentena sin atribución heurística | Fixtures con NULL |
| OUT07 | Migración es aditiva y reversible | DDL revisado, rollback ensayado |
| OUT08 | Backups registran checksum, custodia y tiempo UTC | Manifiesto real |
| OUT09 | Restore ejecutado en destino efímero coincide en conteos y hashes | Informe verificado, no solo backup |
| OUT10 | R2/archivos tienen inventario y ACL por tenant | Descargas A/B, expiración y revocación |
| OUT11 | DNS/TLS/host binding tiene autorización explícita | Pruebas de host falso y certificado |
| OUT12 | Secretos permanecen fuera de Git/evidencia | Escaneo redacción de artefactos |
| OUT13 | Eventos futuros usan clave idempotente scoped a tenant | Replay/dedupe transaccional real |
| OUT14 | CI requerida tiene completed/success sobre SHA exacto | Check-run GitHub, no CANCELLED/SKIPPED |
| OUT15 | A5 pertenece a una Task Javier distinta de autor con readback | PLAN K, BITACORA A:Z, diff y SHA |
| OUT16 | QA/main/PROD y cambio de proveedor requieren orden diferenciada | GO/NO_GO, owner, ventana y rollback |

La lista es **bloqueo técnico futuro**: ningún OUT pasa a PASS por la aprobación de esta documentación.

## 3. Contrato de reversión (RB01–RB08)

| ID | Acción futura controlada | Condición NO_GO |
| --- | --- | --- |
| RB01 | Congelar estado/versiones e inventario autorizado | Falta de baseline |
| RB02 | Respaldar esquema y datos con hash | Hash ausente |
| RB03 | Restaurar copia efímera y comparar | Restore no ensayado |
| RB04 | Ensayar fallo de migración con cancelación segura | Compensación incompleta |
| RB05 | Verificar recursos de A nunca pasan a B | Aislamiento insuficiente |
| RB06 | Preparar reversión de bindings DNS/TLS por etapa | Certificado/TTL no auditados |
| RB07 | Mantener adaptador proveedor antiguo disponible | Proveedor no reversible |
| RB08 | Registrar rollback con auditoría y autorización externa | Sin evento o doble revisor |

No se cambian ahora bases, proveedores, certificados o secretos. El rollback del **presente commit Markdown** es solo `git revert` normal y evento compensatorio append-only, sin force-push.

## 4. Evidencias futuras requeridas (EV01–EV08)

| ID | Evidencia verificable real | Estado aquí |
| --- | --- | --- |
| EV01 | Catálogo vivo Hostinger/Render/Vercel/Aiven/Cloudflare según permisos | NO_ACREDITADO |
| EV02 | Diagrama actualizado de redes, rutas, puertos y responsabilidades | PENDIENTE |
| EV03 | Pruebas de autorización JWT/RBAC/tenant A/B, HTTP y EF | NO_EJECUTADO |
| EV04 | CI MySQL 8.4 con FK, NULL, índices y reversión | NO_EJECUTADO |
| EV05 | Backup + restore contra checksum y conteos | NO_EJECUTADO |
| EV06 | Inventario de objetos, URL firmada, dueño y expiración | NO_ACREDITADO |
| EV07 | Verificación de TLS/DNS y cambio reversible autorizado | NO_EJECUTADO |
| EV08 | CI exact-SHA, A5 independiente y aprobación futura diferenciada | PENDIENTE_M03 |

No se inventan pruebas de penetración, métricas de rendimiento, migraciones, balance de recursos, ni un "cero archivos" deducido sin fuente real.

## 5. Validaciones A0–A5 y alcance posterior
A0: leer STANDARD, stack efectivo .NET10/Angular22.2.1/MySQL8.4, M01.06 y cinco contratos M02 más PLAN/COLA/DAG y riesgos de tenant. A1: comprobar estructura OUT16/RB8/EV8 y coherencia de positivos/negativos; los oráculos técnicos siguen sin ejecutarse. A2: revisión estática de límites JWT/RBAC/tenant y ausencia de cambios runtime/secrets. A3: correspondencia conceptual entre Domain/Application/Infrastructure/API, persistencia y adaptadores existentes, sin ejecutar conexiones. A4: publicación fast-forward mediante CAS, dos archivos Markdown, checks Scope Lock y procedencia V5 en SUCCESS exact-SHA y readback. A5: revisión **independiente** obligatoria por otra Task Javier antes de habilitar sucesora técnica.

C15-02.M03 requiere nuevo preflight DAG, alcance auténtico de código, tests .NET/Angular/MySQL causales cuando aplican y permisos. No crear workers, Tasks, event dispatcher, webhooks, migraciones, domains ni endpoints por este documento. Sin QA, main, producción, compra, DNS, secretos ni activación Hostinger/Cloudflare/R2.
