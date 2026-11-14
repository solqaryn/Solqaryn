# SOLQARYN V5 — C09-02.M01.03: inventario de código y brechas de fidelización y reputación

**Microtarea:** C09-02.M01.03 · PLAN_MAESTRO fila 1336 · **Autor:** PRIMARY DEV 04 JAVIER (TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`) · **Fecha:** 2026-11-14 · **Rama:** `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. Objetivo M01.03: **leer contratos y código existentes, inventariar evidencias y formular brechas verificables**; no implementar programas, pagos, publicidad ni moderación.

## Antecedentes y frontera del análisis

- C09-02.M01.01: `docs/v5/c09-02-m01-01-alcance-fidelizacion-reputacion.md`, blob `1030c70296220626c5341f58d99e68e47b8a6cb2`; invariantes NR01–NR14 y P01–P08/N01–N08 futuros.
- C09-02.M01.02: `docs/v5/c09-02-m01-02-actores-interfaces-fidelizacion.md`, blob `7de37b4ccfa35cd07245975b97cf04dcca2b59a7`; actores A01–A14, interfaces I01–I12 y fronteras F01–F09.
- `C09-01.M01.03` inventaría CRM universal, pero un cliente/venta no implica ledger de puntos ni sistema de reseñas. No se verificó una búsqueda exhaustiva de clases; una brecha de evidencia **no afirma ausencia universal ni vulnerabilidad confirmada**.
- Este trabajo consultó archivos originales por `fetch_file` en GitHub sobre HEAD `b71849c2ed3c54a04acfa5a587c955eb20080ba7` y registró el blob exacto de cada ruta. La implementación posterior debe releer HEAD y sus pruebas.

## Inventario de 12 archivos reales y límites probatorios

| Ruta verificada en dev | Blob Git SHA-1 | Capacidad observable / límite |
| --- | --- | --- |
| `backend/src/Domain/Entities/Cliente.cs` | `8c6d4c4453e44f8c0be9475340465e1aadb3f787` | Cliente EmpresaId nullable de legado; no ledger ni consentimiento |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresia empresa y rol; no permiso de promocion/moderacion por defecto |
| `backend/src/Domain/Entities/TiendaCuentaCliente.cs` | `aa2bc1541afffd68aa980520178d2b6f0d5e7aad` | Cuenta tienda publica; no puntos ni alcance CRM compartido |
| `backend/src/Domain/Entities/Venta.cs` | `64ab3cd92e81031e74137f420cd748c564e28f01` | Venta con EmpresaId nullable historico; no evento de acumulacion confirmado |
| `backend/src/Domain/Entities/CreditoCliente.cs` | `651fb9f9530daded4a11611a7d58a1f1cc401183` | Credito comercial distinto de puntos y beneficios de lealtad |
| `backend/src/Domain/Entities/RegistroAuditoria.cs` | `d669ac397a78ba02a5ba19e2cfb4bc9c7a115462` | Auditoria general no constituye ledger de puntos |
| `backend/src/API/Controllers/ClientesController.cs` | `023d639f5c6ebf20519a22b175e2bf17c53e6d0d` | HTTP de clientes autorizado; no endpoint de fidelidad acreditado |
| `backend/src/Application/Services/ClienteService.cs` | `51661879903e96064557f1a1b4bd3f2c1c79a77b` | Servicio cliente; reglas fidelizacion no demostradas |
| `backend/src/Infrastructure/Repositories/ClienteRepository.cs` | `07bb969b52ea4273ac31975ebbc10d19af8a9d75` | Repositorio EF Cliente; no garantiza aislamiento de todo el futuro programa |
| `backend/src/API/Controllers/AuditoriaController.cs` | `82057f2acc571263ed7ed530eae0f81359b2f81a` | Superficie auditoria; no moderacion ni revocacion de consentimiento |
| `frontend/src/app/services/cliente.service.ts` | `e651c4b9027318333d15a08a035ed904f3b91073` | Consumo Angular de /clientes, no vistas de puntos/reviews |
| `frontend/src/app/core/guards/permiso.guard.ts` | `1052bd38041055cc81e84a14d5fe0364e169b1fe` | Guard de interfaz sin autoridad de autorizacion backend |

La existencia de los tipos `Cliente`, `Venta`, `CreditoCliente`, `RegistroAuditoria` y guards UI confirma **sustratos reutilizables**, no demuestra motores operativos de puntos, promos, reseñas, antifraude, consentimiento u opt-out. El aislamiento de cada futura operación requerirá pruebas runtime con empresas A/B, incluso si `ClientesController` contiene `[Authorize]`.

## G01–G12: preguntas de evidencia (NO defectos certificados)

| Brecha | Pregunta técnica por resolver | Evidencia requerida antes de cerrar M03/M05 |
| --- | --- | --- |
| G01 | Contrato Domain de movimiento/saldo de puntos separado de credito | Entidades, ledger, balance y tests de version |
| G02 | Reglas deterministas de acumulacion/reversion por venta confirmada | Prueba idempotente con venta anulada y carrera |
| G03 | Scoping de empresa/cliente y reconciliacion EmpresaId=NULL | Fixtures tenant A/B y negativos 403/404 |
| G04 | Autorizacion backend granular para concesion, canje y ajustes | JWT/RBAC, membresia, revocacion y tests HTTP |
| G05 | Integridad e indices del ledger ante concurrencia | Transacciones MySQL efimero y pruebas simultaneas |
| G06 | Modelo de promociones versionadas y allowlisted | DTO, fecha UTC, limites, negativos por tenant |
| G07 | Identidad y privacidad verificable de reseñas | Moderacion, sanitizacion, retencion y PII |
| G08 | Decisiones reversibles y apelables de moderacion | Motivo, auditor, version y restauracion |
| G09 | Consentimiento por canal y opt-out efectivo | Registro consentimientos, supresion y test reintento |
| G10 | Separacion entre credito, ventas, resenas, soporte y campanas | Revision contractual, no-regresion financiera y rollback |
| G11 | Aislamiento de cache, exportaciones y workers eventuales | Tenant-bound keys, rate limits y falla cerrada |
| G12 | Rutas/UX accesibles para beneficios y posventa | Pruebas keyboard, 320px, errores y permisos API |

## Oráculos positivos/negativos FUTUROS_NO_EJECUTADOS

| Caso | Futuro positivo esperado | Negativo obligatorio para fallar cerrado |
| --- | --- | --- |
| P01 / N01 | Saldo de A visible solo a Cliente A | Sesion A intenta acceder a puntos de B |
| P02 / N02 | Venta A confirmada acredita una sola vez | Replay, anulacion o doble confirmacion duplican puntos |
| P03 / N03 | Canje autorizado registra debito unico | Carrera, saldo insuficiente o version vieja no consumen saldo |
| P04 / N04 | Promocion tenant A valida vigencia y topes | Promocion B, expirada o clave arbitraria se rechaza |
| P05 / N05 | Reseña publica saneada y sin PII | HTML/script, autor falso o contenido privado se bloquean |
| P06 / N06 | Moderador A autorizado decide con trazabilidad | Lector o moderador B no altera reseñas |
| P07 / N07 | Posventa A se asocia a venta propia | Solicitar caso/venta de otro tenant no filtra datos |
| P08 / N08 | Opt-out efectivo bloquea envio segun canal | Reintento/cache/proveedor no ignora revocacion |

No se ejecutaron xUnit, API, MySQL, npm ni Playwright para **esta** unidad documental. P/N son criterios para fases futuras; GitHub Actions de gobernanza validan esta publicación, no la existencia o seguridad runtime de la funcionalidad propuesta.

## Impacto de arquitectura, protección y rollback

- **Domain/Application/API:** preservar `Cliente`, `Venta`, `CreditoCliente`, `UsuarioEmpresa`; nuevos contratos y DTO deben ser tipados, versionados e idempotentes sin elevar privilegios por tenant_id, slug, host, cookie, `EmpresaId` o header.
- **Infrastructure/MySQL 8.4:** no se añadieron tablas, índices ni migraciones. Diseño futuro de ledger requiere transacción por tenant, control de concurrencia, referencias a ventas y auditoría irreversible sin datos entre empresas.
- **Seguridad:** JWT/RBAC server-side y aislamiento A/B; datos históricos `EmpresaId=NULL` no conceden ownership. Revisión futura de CSRF/XSS, rate limit, PII, moderación, opt-out, cache y proveedores; ningún secreto se modifica.
- **Frontend Angular 22.2.1:** sin cambios UI. La app nunca reemplaza autorización backend; aceptación futura debe cubrir teclado, foco, estados, 320px y errores.
- **Observabilidad y eventos:** trazabilidad minimizada, idempotency/correlation futura; **no se crean webhooks, workers, eventos entre Tasks o campañas reales**.
- **Superficies cambiadas ahora:** únicamente este documento y `CHANGELOG_AI.md`. **Rollback:** revertir el commit de dev con commit inverso ordinario tras comparar HEAD; no revertir historial de otra Task.
- **Gates vigentes:** A0 estático, A1/A2/A3 documentales según evidencia, A4 Scope Lock + CHANGELOG sobre SHA exacto; A5 independiente por otra de las cinco Tasks Javier. No se habilita QA, main, PROD ni migración.

## Conclusión M01.03

Se inventariaron **12 rutas/blobs Git** y **12 preguntas de evidencia**. Las capacidades de fidelización y reputación se mantienen **NO_CERTIFICADAS_RUNTIME**. M01.04 deberá investigar riesgos/supuestos sin convertir estas preguntas en fallos reales; M01.05 definirá aceptación verificable. M02/M03 requieren diseño, implementación y pruebas concretas antes de declarar funciones listas.
