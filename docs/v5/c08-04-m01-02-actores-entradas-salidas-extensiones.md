# SOLQARYN V5 — C08-04.M01.02 — Actores, entradas, salidas y superficies de extensiones sectoriales

**Autor:** PRIMARY DEV 03 JAVIER (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Fecha:** 2026-11-14. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, bloque C08-04 fase M01.02, PLAN_MAESTRO fila 1263. **Rama:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Naturaleza:** análisis documental NO_EJECUTADO en runtime.

## 1. Contrato y alcance de esta microtarea

La futura capacidad sectorial se debe desplegar como **capabilities opt-in por empresa** sobre los mismos contratos de identidad, permisos, catálogo, UX, auditoría y transacciones existentes. Esta M01.02 registra actores, interfaces, entradas y salidas, sin crear entidades sectoriales, licencias, flags activos, menús nuevos, endpoints ni datos. El antecedente `C08-04.M01.01` (blob `41dd2a10d667124c021791278d43ceab3dc02c35`) fija NR01–NR12; esta unidad no reclama A5 de su propio autor ni libera M01.06.

## 2. Actores y superficies A01–A14, contratos FUTUROS

| Actor | Entrada prevista | Salida esperada / límite |
| --- | --- | --- |
| A01 Propietario de empresa | Solicitud de rubro permitido | Cambio autorizado, auditado, sin alterar permisos |
| A02 Admin de plataforma | Catálogo de capabilities allowlisted | Versión revisable, sin manipular tenancy productiva desde UI |
| A03 Administrador de sucursal | Contexto de sucursal aprobada | Vista sólo de módulos habilitados en su empresa |
| A04 Operador de reservas | Calendario/recurso de su empresa | Disponibilidad sin exposición cross-tenant |
| A05 Profesional asignado | Horarios propios autorizados | Vista acotada, no permisos globales |
| A06 Cliente de reserva | Franjas públicas permitidas | Sólo datos públicos, sin identidad interna |
| A07 Operador de membresías | Estado comercial y vigencia | Acceso al sector y empresa propios, sin privilegio SaaS |
| A08 Cliente de membresía | Consulta de vigencia propia | Sin lectura de otros socios o empresa |
| A09 Operador de catálogo | Producto/servicio propiedad de empresa | Reutilización sin duplicar inventario ni precios |
| A10 Cliente de storefront | Petición de página, slug o artículo | Proyección pública autorizada, no autorización privada |
| A11 Soporte autorizado | Diagnóstico con permiso explícito | Traza mínima sin PII ajena, sin impersonación implícita |
| A12 Backend de aplicación | JWT, UsuarioEmpresa, política y estado capability | Decisión definitiva fail-closed por recurso/acción |
| A13 Persistencia/auditoría | Operación transaccional y actor resuelto | Historial segregado y reversible, nunca borrado accidental |
| A14 Pruebas/CI | Fixtures A/B, permisos y estado habilitado/deshabilitado | Evidencia reproducible en SHA; no tráfico productivo |

Los actores anteriores son roles del **contrato futuro**, no una afirmación de que ya existan como permisos, tablas o endpoints operativos.

## 3. Interfaces I01–I12: entrada, salida y frontera

| Interfaz futura | Entrada | Salida verificable y autoridad |
| --- | --- | --- |
| I01 Resolución tenant | Credencial, solicitud de empresa | Tenant resuelto sólo por backend y membresía activa |
| I02 Autorización | Recurso, acción, rol efectivo | Permitir/denegar server-side, nunca por bandera UI |
| I03 Consulta capabilities | Tenant efectivo y catálogo tipado | Sólo módulos habilitados para ese tenant y rol |
| I04 Cambio capability | Comando autenticado, versión y motivo | Registro auditado; no efectos cross-tenant |
| I05 Navegación Angular | Capabilities proyectadas y permisos UI | Menús accesibles; API sigue aplicando RBAC |
| I06 Reserva | Servicio, horario, sucursal/recurso | Sólo cita propia, sin solapamiento eventual |
| I07 Membresía | Titular, vigencia y estado | Identidad comercial separada de UsuarioEmpresa |
| I08 Catálogo | Producto, servicio o atributo | Ownership explícito, compatibilidad con ERP existente |
| I09 Sitio público | Host/slug normalizado, binding | Proyección pública, sin identidad privada inferida |
| I10 Auditoría | Correlation ID, actor, antes/después | Evento idempotente mínimo, sin secretos |
| I11 Desactivación | Motivo, versión y tenant | Nuevas operaciones bloqueadas, historial preservado |
| I12 Reversión | Versión previamente autorizada | Recuperación coherente, sin mutar QA/main/PROD |

Estos doce contratos son de **análisis**, no contratos de API publicados; tipos, rutas, payloads y políticas finales se decidirán en M02/M03 con evidencia.

## 4. Fronteras comprobadas en código y documentos actuales

| Archivo leído en dev | Blob SHA | Hallazgo limitado |
| --- | --- | --- |
| `backend/src/Domain/Entities/Empresa.cs` | `c44940df7c30983a4c23b93338e0cb9367962069` | Raíz Empresa con estado activo/inactivo |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Usuario, Empresa, rol y pertenencia; no módulo sectorial |
| `frontend/src/app/core/guards/permiso.guard.ts` | `1052bd38041055cc81e84a14d5fe0364e169b1fe` | Guarda de navegación; no sustituye auth backend |
| `frontend/src/app/core/interceptors/auth.interceptor.ts` | `4bdf19135ed1932a99fd09ace1f900bcccc5b25b` | `X-Empresa-Id` solicitado no concede autoridad |
| `docs/v5/c08-01-m01-02-actores-interfaces-reservas-agenda.md` | `912cd97789de5faaacbe9e9abef1b5553a49a009` | Actores de agenda FUTUROS, no disponibilidad certificada |
| `docs/v5/c08-04-m01-01-alcance-extensiones-por-rubro.md` | `41dd2a10d667124c021791278d43ceab3dc02c35` | Doce invariantes y 6 positivos/negativos no ejecutados |

## 5. Escenarios futuros positivos y negativos (NO_EJECUTADOS)

| ID | Flujo positivo futuro | Negativo que debe fallar cerrado |
| --- | --- | --- |
| T01 | Admin A activa capability permitida con versión | Rol insuficiente o tenant B no puede activarla |
| T02 | Operador A obtiene sólo menús y endpoints autorizados | Modificar ruta Angular o header tenant no concede acción |
| T03 | Desactivar servicio preserva reservas/cobros históricos | UI deshabilitada no puede seguir operando API privada |
| T04 | Miembro A consulta vigencia propia con permiso | Persona ajena no obtiene datos ni por ID conocido |
| T05 | Configuración allowlisted soporta rollback idempotente | Campo arbitrario/replay no modifica configuración |
| T06 | Error de proveedor genera estado seguro y traza | Error no activa módulos ni revela secretos/PII |

## 6. Dependencias, criterios de cierre y reversión

M01.03 contrastará controladores, consultas, contratos y tests exactos; M01.04 registrará riesgos, supuestos y decisiones; M01.05 precisará aceptación. M01.06/M02–M06 permanecen sujetos a sus gates; ninguna referencia documental anticipada autoriza JWT/RBAC, migraciones, worker, API, despliegue o cliente real.

**A0–A3 documentales:** fuentes y roles contrastados, positivos/negativos definidos como FUTUROS, invariantes tenant/JWT/RBAC preservadas y coherencia transversal de contratos; pruebas de runtime NO_APLICA, no PASS inventado. **A4:** revisión de diff de dos Markdown, commit FF/CAS, Scope Lock y V5 provenance exact-SHA, PLAN/COLA/BITACORA readback. **A5:** debe realizar otra Task Javier sobre evidencia final. **Rollback:** git revert normal del commit; no se han modificado datos, código ni servicios. QA/main/PROD quedan excluidos.
