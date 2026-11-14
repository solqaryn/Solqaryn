# SOLQARYN V5 FINAL — C02-04.M02.01: entidades, ownership e invariantes de consola global auditada

**Entrega real:** contrato **documental** de entidades, relaciones y límites de autorización para la futura consola global auditada C02-04. **Autor:** PRIMARY DEV 01 — JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`; 2026-11-14.

**Fuentes únicas:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO / COLA / DAG_TECNICO / BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md` y Skill canónica. El gate dinámico individual `DAG_TECNICO` está `F=VALIDADO/G=SI` para **solo reclamar esta unidad**; C02-04.M01.01–06 figuran LISTO con A5 independiente. No se obtiene permiso de implementar M03, MFA, JWT, migraciones, datos productivos, eventos, QA/main/PROD ni nuevos ejecutores.

## 1. Evidencia que existe y capacidades no demostradas

| Fuente de `dev` inspeccionada | Blob | Contrato observable, sin extrapolar |
| --- | --- | --- |
| `backend/src/Infrastructure/Services/PlataformaScopeService.cs` | `2a71a56750805ef513c7f312fb6c6c2d53a8c314` | Consulta por solicitud la autoridad global persistida; el JWT y `Usuario.RolId` no conceden autoridad global por sí solos |
| `backend/src/API/Controllers/PlataformaEmpresasController.cs` | `87fbf187b7993ba0f05edff393500f24d0856833` | Superficie `plataforma/empresas` con control de scope concreto; no equivale a acceso excepcional universal ni MFA reciente |
| `backend/src/Application/Security/PlatformSupportPolicy.cs` | `b103f0b0ec7b9169c060661f6a4456f8863152cf` | Allowlist DEV muy estrecha de lectura de catálogo; no abre rutas globales, ni operaciones de escritura |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía `UsuarioId/EmpresaId/RolId` activa y su rol son exclusivamente empresariales |

Los M01.01–06 se consultan como preparación [objetivo](c02-04-m01-01-objetivo-no-regresion-consola-global.md), [actores](c02-04-m01-02-actores-entradas-salidas-consola-global.md), [inventario](c02-04-m01-03-inventario-fuentes-contratos-consola-global.md), [riesgos](c02-04-m01-04-riesgos-supuestos-decisiones-consola-global.md), [aceptación](c02-04-m01-05-aceptacion-consola-global-auditada.md) y [bloqueos](c02-04-m01-06-bloqueo-rollback-liberacion-consola-global.md). Son contratos, no resultados ejecutables.

## 2. Modelo conceptual propuesto (NO entidades EF ni tablas creadas)

| Entidad o relación futura | Propietario de autoridad | Identidad y cardinalidad conceptual | Restricción invariante |
| --- | --- | --- | --- |
| Identidad operadora global | Servicio backend de plataforma apoyado en rol persistido vigente | Un `UsuarioId` puede intentar operaciones; la asignación global actual debe ser inequívoca | Un administrador ERP no hereda rol de plataforma; claims o `EmpresaId` del cliente no son autoridad |
| Contexto de empresa objetivo | Resolución backend de `EmpresaId` y recurso real | Una operación apunta a **una** empresa/recurso/acción exactos | No hay selección automática de B por sesión activa de A; no wildcard ni enumeración |
| Desafío MFA reciente | Verificador server-side todavía por diseñar | Un desafío ligado a actor, sesión, operación, nonce y ventana | OTP viejo, prueba ajena, desafío reutilizado o MFA caído: denegar |
| Reautenticación reciente | Autoridad de sesión server-side todavía por diseñar | Identidad, emisión, caducidad y versión de sesión | No aceptar cookie/JWT antiguo como equivalencia a MFA ni persistir credenciales |
| Solicitud excepcional | Dominio de autorización futuro | Identificador opaco, actor, empresa, recurso, acción, motivo sanitizado y ticket opcional validado | Una solicitud no es un permiso; motivo requerido, sin herencia ni elevación automática |
| Concesión excepcional temporal | Emisor backend futuro con aprobación específica si aplica | Grant único limitado a solicitud, TTL fijado por servidor, estado y revocación | Sin grant activo por omisión; sin ampliación de TTL/scope en reintento |
| Decisión de acceso puntual | Servicio de autorización final (no frontend) | Actor + operación + recurso + empresa + grant + instante actual | Validación en **cada** operación: rol, MFA, reauth, grant, vigencia, revocación y scope |
| Registro de auditoría crítica | Subsistema durable y segregado todavía por diseñar | Evento con ID, correlación, actor, scope, razón mínima, resultado y fecha UTC | Append-only, sin JWT/OTP/contraseñas/DSN/PII excesiva; denegar mutación si falla persistencia obligatoria |
| Política de soporte DEV | `PlatformSupportPolicy` real, separada de privilegios excepcionales | Lista explícita de rutas GET y módulos limitados | No se transforma en grant global, ni POST, ni alcance comercial cruzado |
| Membresía ERP `UsuarioEmpresa` | Capa de identidad tenant empresarial actual | `UsuarioId`, `EmpresaId`, `RolId`, `Activa` | Membresía revocada o de otra empresa no autoriza plataforma o B |

**Estados de diseño para una concesión temporal:** `PROPUESTA` → `APROBADA` solo mediante política adicional futura; `APROBADA` → `REVOCADA/EXPIRADA` es terminal para uso privilegiado. No se implementa esta máquina de estados. Una consulta o un documento no genera transiciones reales.

## 3. Invariantes obligatorias del contrato futuro

| ID | Invariante falsable |
| --- | --- |
| INV-01 | La identidad de plataforma se resuelve contra rol persistido **vigente** por solicitud y no desde JWT aislado, admin ERP o header. |
| INV-02 | El actor real, tenant objetivo, recurso, operación y motivo pertenecen a la misma solicitud autorizada; prohibidas identidades ambiguas. |
| INV-03 | Permisos de empresa A, sucursal o soporte DEV **no** autorizan operación sobre empresa B. |
| INV-04 | MFA validado recientemente y reautenticación reciente son pruebas independientes, vinculadas al actor/sesión/operación. |
| INV-05 | La ausencia de un factor, ticket exigible, motivo válido, auditoría o permiso siempre produce **DENY**. |
| INV-06 | Un grant de excepción contiene `tenantId/recurso/acción/expiración` exactos; sin comodín ni reutilización entre tenants. |
| INV-07 | Las expiraciones son autoritativas del servidor; un reloj de cliente o TTL reenviado no alarga concesiones. |
| INV-08 | Revocación de sesión/rol/grant invalida el siguiente intento aun con token o UI almacenados. |
| INV-09 | Cambio de tenant o carrera concurrente obliga a reevaluar scope inmediatamente antes de leer o modificar datos. |
| INV-10 | Idempotencia futura vincula actor, alcance, operación, contenido normalizado y versión; una clave igual con payload distinto se rechaza. |
| INV-11 | Correlación es opaca, acotada y no transporta secretos, correos, tokens ni identificadores privados en URL/log público. |
| INV-12 | La escritura privilegiada requiere confirmación transaccional y auditoría durable; éxito sin auditoría no es éxito. |
| INV-13 | Lecturas privilegiadas mínimas son paginadas, redactadas y `no-store`; un cache no cruza usuario, empresa o grant. |
| INV-14 | Un rollback, timeout o indisponibilidad de MFA/auditoría deja el permiso cerrado; nunca hace fallback a autorización simple. |
| INV-15 | Un auditor puede observar evidencia mínima segregada sin concederse permisos de plataforma o empresa. |
| INV-16 | Ninguna implementación futura suplanta `DAG_TECNICO`, A0–A4, A5 independiente o autorización de QA/main/PROD. |

## 4. Matriz simbólica de casos para implementación y prueba posteriores

Estos son **oráculos documentales**, NO pruebas xUnit, MySQL, HTTP, browser o CI ejecutadas por esta microtarea.

| Caso | Polaridad | Entrada propuesta | Resultado que una prueba futura debe demostrar |
| --- | --- | --- | --- |
| P01 | POS | Rol global vigente, MFA y reauth recientes, motivo validado, scope A preciso | Una acción expresamente autorizada solo sobre A, con auditoría íntegra |
| P02 | POS | Auditor segregado consulta evento autorizado | Evento mínimo con correlación y sin secretos, sin grants de mutación |
| P03 | POS | Revocación explícita de grant activo | Próxima solicitud con grant viejo falla |
| P04 | POS | Reintento idempotente idéntico autorizado | Un efecto y una evidencia única, jamás doble ejecución |
| N01 | NEG | Admin ERP A reclama plataforma/B por `Usuario.RolId` | 403, cero datos de B, cero grants |
| N02 | NEG | JWT `esAdministrador` falso o `X-Empresa-Id=B` arbitrario | 401/403 sin elevación ni lectura de B |
| N03 | NEG | MFA ausente, expirado o no ligado a solicitud | Denegación sin operación ni autorización temporal |
| N04 | NEG | Reauth vieja, sesión o rol global revocado | Denegación inmediata sobre estado actual |
| N05 | NEG | Motivo vacío, ticket inválido o secreto embebido | Validación segura, no grant ni log sensible |
| N06 | NEG | Grant para A usado sobre B/otro recurso/acción | Denegación y cero efectos interempresa |
| N07 | NEG | Grant caducado, revocado o prorrogado por cliente | Denegación, TTL no ampliado |
| N08 | NEG | Dos reintentos concurrentes con misma clave y contenido diferente | Conflicto sin duplicación ni ambigüedad |
| N09 | NEG | Falla de auditoría obligatoria o de MySQL en operación crítica | Rollback/deny, nunca respuesta de éxito ficticio |
| N10 | NEG | Soporte DEV ejecuta POST/DELETE o ruta fuera del catálogo GET | Denegación por allowlist y autorización real |
| N11 | NEG | Cache reusa respuesta global A para usuario B | No-store y separación; no exposición |
| N12 | NEG | Log contiene JWT, OTP, DSN o PII no necesaria | Falla de seguridad; sanitización verificable |
| N13 | NEG | Carrera de revocación o cambio de tenant durante petición | Revalidación, cero efecto con permiso caducado |
| N14 | NEG | MFA no disponible o timeout | Fail-closed, sin modo emergencia no autorizado |

## 5. Límites de entrega, pruebas y rollback

- **A0 documental**: revisar integridad de este contrato contra los seis M01 y cuatro blobs citados, formato/encabezados, ausencia de secretos, no superponer grants empresariales y globales.
- **A1 documental**: inspeccionar coherencia de 4 oráculos positivos y 14 negativos anteriores. No registrar como PASS de ejecución funcional.
- **A2 documental**: comprobar que todos los caminos de acceso indebido A/B, rol ERP y soporte DEV quedan explícitamente negados; ningún cambio a JWT, permisos o configuración real.
- **A3 documental**: relaciones consistentes con capas Domain–Application–Infrastructure–API sin crear interfaz, tabla, controller, worker, evento real o migración. `NO_APLICA` para tests runtime por ausencia de código modificado.
- **A4**: exigir commit fast-forward en `dev`, diff limitado a este documento y sección autocontenida de `CHANGELOG_AI.md`, check `verify-solqaryn-project-scope` y check ligero V5 **completed/success en SHA exacto**, y readback PLAN/COLA/BITACORA.
- **A5**: pendiente hasta auditoría de un TASK_ID Javier diferente del autor; no equivale a promoción a QA/main/PROD.

**Rollback de esta microtarea:** `git revert` normal del commit documental, preservando historial; BITACORA histórica append-only, nunca reescribirla. **Rollback futuro privilegiado:** requerirá revocación segura y estrategia transaccional probada antes de instalar una sola tabla o privilegio. No se concede autorización comercial ni IAM mediante este texto.
