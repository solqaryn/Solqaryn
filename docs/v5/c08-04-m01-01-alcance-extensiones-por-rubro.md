# SOLQARYN V5 — C08-04.M01.01 — Objetivo y no-regresión de extensiones por rubro

**Autor:** PRIMARY DEV 03 JAVIER (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Fecha:** 2026-11-14. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, bloque C08-04 M01.01; PLAN_MAESTRO fila 1262. **Rama:** solqaryn/Solqaryn@dev, Scope Lock STRICT. **Alcance de esta entrega:** DOCUMENTAL, no runtime.

## Objetivo verificable y frontera del contrato

Preparar la futura matriz de **capabilities por rubro y por empresa** para reservas, membresías, catálogo y sitios, reutilizando la identidad, recursos, seguridad y auditoría de la plataforma. Un módulo sectorial no puede duplicar el tenant, la entidad Empresa, los usuarios ni el sistema de permisos. Esta microtarea define objetivo, exclusiones, oráculos de no-regresión y dependencias para M01.02–05; **NO completa M01.06 ni certifica ningún módulo nuevo**.

| Rubro / capacidad futura | Núcleo a reutilizar | Riesgo de duplicación prohibido |
| --- | --- | --- |
| Reservas/agenda C08-01 | Empresa, sucursal, personal y autorización backend | Crear nueva identidad tenant por calendario |
| Membresía comercial C08-03 | Identidad, usuario, auditoría y permisos server-side | Confundir membresía gimnasio con UsuarioEmpresa o plan SaaS |
| Catálogo C07-01 y storefront C07-02 | Producto y propiedad empresarial real | Tratar `EmpresaId` legado NULL como acceso global |
| Sitios C06-02 | Host binding gobernado y configuraciones allowlisted | Derivar permisos privados de host, slug o ruta |
| Extensión C08-04 | Feature flag futuro por tenant, versionado, rollback | Código/SQL/HTML/JS arbitrario por configuración |

**Excluido:** crear módulos Angular, clases, tablas o migraciones, endpoints, workers, permisos RBAC, JWT, flags vivos, activaciones, proveedores, cobros, DNS, secretos, tareas, QA/main/PROD o datos productivos. El documento no concede autorización operativa para ninguno de ellos.

## Fuentes inspeccionadas en HEAD dev

| Fuente | Blob verificado | Lectura limitada |
| --- | --- | --- |
| `backend/src/Domain/Entities/Empresa.cs` | `c44940df7c30983a4c23b93338e0cb9367962069` | Raíz empresarial, activa/inactiva; no demuestra capabilities |
| `backend/src/Domain/Entities/Producto.cs` | `19632ce1aebe6a9cbc4379523367531852ee3e7e` | Propiedad `EmpresaId` nullable histórica, sin permiso implícito |
| `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas tienda y guardas privadas; no hay prueba de toggle sectorial |
| `docs/v5/c08-01-m01-01-alcance-reservas-agenda.md` | `26805ab07dd3f20cf986b6da4ebc24fb702fa8f6` | Agenda como contrato, NO integración ya operativa |
| `docs/v5/c07-01-m01-01-alcance-catalogo-universal.md` | `4f91db421fe4c5e7ad5e0ea080035eabe8f8c1dd` | Reutilización de catálogo y ownership |
| `docs/v5/c08-03-m01-05-criterios-aceptacion-membresias.md` | `380d9d096ed2543548c48646d9ee3d0ac6f414b6` | Criterios futuros NO_EJECUTADOS, sin lector/QR/cobro |

La existencia de esos archivos no es una prueba de integración entre sectores. Arquitectura actual: Angular 22.2.1, ASP.NET Core net10.0, MySQL/EF Core, JWT/RBAC y backend como autoridad de tenant.

## Invariantes NR01–NR12 (obligatorias en implementación futura)

- **NR01:** sólo Empresa activa validada en servidor obtiene contexto operativo.
- **NR02:** habilitar un rubro no concede permisos, roles ni membresías.
- **NR03:** tenant A jamás accede a registros de B, incluso con ID conocido.
- **NR04:** cada capability requiere identidad empresarial, estado y versión auditables.
- **NR05:** deshabilitar bloquea altas nuevas sin eliminar historial existente.
- **NR06:** usuario revocado o sucursal ajena siempre es NO_GO.
- **NR07:** slug/host/header/cookie/payload no definen autorización server-side.
- **NR08:** ownership histórico NULL no se transforma en permiso global.
- **NR09:** guardas UI no sustituyen controles del backend.
- **NR10:** configuración sectorial tipada y allowlisted, nunca código o SQL arbitrario.
- **NR11:** configuración ausente, inválida o caché fallida causa rechazo seguro.
- **NR12:** activación/reversión se auditan sin duplicación, borrado transversal ni promoción automática.

## Matriz positiva / negativa futura: NO_EJECUTADA

| Oráculo | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| P01/N01 | Tenant A con capability y RBAC habilitados usa su recurso | A consulta el ID de B y no obtiene contenido |
| P02/N02 | Operador autorizado accede sólo a su sede | Usuario revocado o sede ajena recibe rechazo |
| P03/N03 | Apagar capability impide nuevas altas, preservando registros | Apagado no borra historial ni afecta otra empresa |
| P04/N04 | Configuración tipada válida y auditada se acepta | Entrada HTML/JS/SQL/campo no allowlisted se rechaza |
| P05/N05 | Reintento idéntico es idempotente | Replay o concurrencia no generan doble efecto |
| P06/N06 | Rollback restaura versión aprobada y estado seguro | Fallo de caché/proveedor no activa permisos por defecto |

Estas pruebas **NO fueron corridas**. Se ejecutarán cuando una microtarea de código incluya suites .NET/Angular, aislamiento A/B, MySQL o Playwright según impacto. Ningún cumplimiento funcional, seguridad runtime, integración comercial ni permiso de promoción se deduce de esta tabla.

## Puertas de continuidad, evidencia y rollback

- **M01.02:** actores, entradas/salidas y superficies; **M01.03:** inventario exacto de controladores, servicios y pruebas; **M01.04:** riesgos y decisiones pendientes; **M01.05:** matriz final de aceptación.
- **M01.06 y M02–M06:** exigir gates, prerequisitos y A5 correspondientes; M01.01 no libera cierres, migraciones ni deployments.
- **A0–A3 de M01.01:** revisión documental de alcance, trazabilidad de fuentes, oráculos positivos/negativos futuros y preservación JWT/RBAC/tenant; runtime **NO_APLICA**, no PASS.
- **A4:** revisar diff Markdown, HEAD/fast-forward, Scope Lock y procedencia CHANGELOG `SUCCESS` sobre SHA exacto, BITACORA y readback PLAN/COLA. **A5:** independiente pendiente, autor no se aprueba.
- **Reversión:** `git revert` normal del commit documental, sin force-push ni operación de base de datos.
