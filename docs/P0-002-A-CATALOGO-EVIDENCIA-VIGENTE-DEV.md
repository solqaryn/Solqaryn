# P0-002.A — Catálogo de evidencia vigente por capa (DEV)

**Origen comercial único:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, documento institucional de Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. **Microtarea:** P0-002.A, fila 4 de PLAN_MAESTRO. **Corte de lectura:** 2026-10-25T16:22:03.967Z. **Reposición:** solqaryn/Solqaryn, rama dev, commit d3ed287d4bf3748d039223bdfc5b3909170b24cb. Este corte es una fotografía verificable, no un monitor, un despliegue ni autorización de QA/PROD.

## Regla de evidencia

1. Un PR MERGED identifica integración de cambios, **no** certifica por sí solo seguridad operativa, ejecución E2E, datos, salud, despliegue ni UX. Una aprobación debe acreditarse por separado si se pretende cierre de otra microtarea.
2. CI solo se atribuye al **SHA exacto** en que se consultó. El HEAD del plan de 19-10-2026 no describe la rama actual. Conclusiones de los días 12–15 de octubre se tratan como historia y jamás como estado vivo.
3. Despliegues de Render/Vercel, flags de producto, Aiven, Cloudinary, pagos, MySQL, QA y PROD quedan NO_VERIFICADOS aquí salvo readback nativo específico. Nunca inferir READY de un check GitHub.
4. Las filas L01–L15 son etiquetas de origen para clasificación y trazabilidad, no work items paralelos ni permisos para ejecutar planes anteriores. Las brechas se encaminan a microtareas P0/P1 del plan actual.

## Pruebas nativas del corte

- GitHub GET dev: d3ed287d4bf3748d039223bdfc5b3909170b24cb. Scope Lock STRICT, AUTOMATION_RUNTIME=ACTIVE_DEV, VAEP_DISPATCH_AUTHORIZED=FALSE, JULES_WORKERS_AUTHORIZED=0.
- Skill canónica y referencia de arquitectura de .agents/skills/solqaryn-project-governance coinciden exactamente con las cargadas (Skill comparada sin front matter YAML). Frontend Angular 22.2.1, Material 22.2.1, Zone.js 0.16.3; API net10.0; EF Core 10.0.12 y MySql.EntityFrameworkCore 10.0.9 declarados en HEAD.
- Tres checks obligatorios **SUCCESS sobre ese mismo SHA**: [Scope Lock](https://github.com/solqaryn/Solqaryn/actions/runs/40210859755/job/121221571414), [Seguridad multiempresa](https://github.com/solqaryn/Solqaryn/actions/runs/40210859767/job/121221571976), [Build y pruebas backend](https://github.com/solqaryn/Solqaryn/actions/runs/40210859767/job/121221571865). No se reutilizan checks de otro SHA.
- GitHub GET [#3741](https://github.com/solqaryn/Solqaryn/pull/3741): MERGED=true, merge d3ed287d4bf3748d039223bdfc5b3909170b24cb, revisión institucional APPROVED del commit 5eb08b8ed3769cdeda31a1796bb082d4604aab11; [#3738](https://github.com/solqaryn/Solqaryn/pull/3738): MERGED=true; [#3736](https://github.com/solqaryn/Solqaryn/pull/3736): MERGED=true. Esto demuestra integración de actas/base y autoridad, no paridad de proveedores.
- PLAN_MAESTRO!H25 y COLA!E25: P0-012.B=LISTO; COLA!G4=SI para esta microtarea. **Desalineación ajena a este scope:** al momento de lectura, P0-001.B seguía PENDIENTE en PLAN_MAESTRO!H3 pese al merge de #3741. Su propietario debe conciliar con readback; P0-002.A no sobrescribe esa fila.

## Catálogo L01–L15 (PR verificados mediante GET actual)

| Lista / Capa | Evidencia GitHub integrada o estado confirmado | Brecha viva / límite de la inferencia |
| --- | --- | --- |
| L01 — Stack y promoción | Angular/.NET/EF verificados en archivos; [#3583](https://github.com/solqaryn/Solqaryn/pull/3583) y [#3574](https://github.com/solqaryn/Solqaryn/pull/3574) hoy CLOSED y **NO MERGED**. | No repetir upgrades; ningún estado de QA/PROD se autoriza ni se infiere. |
| L02 — Roles, membresía y selector | [#3605](https://github.com/solqaryn/Solqaryn/pull/3605), [#3669](https://github.com/solqaryn/Solqaryn/pull/3669) y [#3683](https://github.com/solqaryn/Solqaryn/pull/3683) MERGED a dev. | UX del selector, reactivación y permisos globales por tenant requieren validación dirigida. |
| L03 — Tenancy transversal | #3605 MERGED. | La foto de octubre anterior a #3605 no constituye deuda actual; falta evidencia E2E específica de rutas A/B. |
| L04 — Seguridad empresarial | [#3596](https://github.com/solqaryn/Solqaryn/pull/3596), [#3597](https://github.com/solqaryn/Solqaryn/pull/3597), #3605 MERGED. | La integración de fixes no certifica todos los escenarios de aislamiento, migraciones o datos. |
| L05 — Reportes, adjuntos y jobs | [#3614](https://github.com/solqaryn/Solqaryn/pull/3614), [#3615](https://github.com/solqaryn/Solqaryn/pull/3615), [#3616](https://github.com/solqaryn/Solqaryn/pull/3616), [#3640](https://github.com/solqaryn/Solqaryn/pull/3640), [#3648](https://github.com/solqaryn/Solqaryn/pull/3648) MERGED. | No afirmar transacciones, E2E, consumidores de Cloudinary ni jobs completos sin prueba viva. |
| L06 — Consola y superadministrador | [#3653](https://github.com/solqaryn/Solqaryn/pull/3653) y [#3654](https://github.com/solqaryn/Solqaryn/pull/3654) MERGED. | Extensión de alcance entre módulos requiere tenant explícito, RBAC y auditoría. |
| L07 — Storefront A/B | [#3682](https://github.com/solqaryn/Solqaryn/pull/3682) MERGED. | Identidad pública no prueba catálogo, carrito ni checkout comercial habilitado. |
| L08 — Configuración pública y alta | #3682 y #3683 MERGED. | Flags operativos, rollback MySQL en vivo y exposición real no certificados aquí. |
| L09 — HMAC y vínculo público | #3682 MERGED. | Sin readback HTTP actual de Host, nonce, replay, store key, CORS o despliegue. |
| L10 — Frontend plataforma | [#3659](https://github.com/solqaryn/Solqaryn/pull/3659) y [#3660](https://github.com/solqaryn/Solqaryn/pull/3660) MERGED. | La navegación y accesibilidad globales requieren regresión/E2E vigente. |
| L11 — Limitaciones públicas antiguas | #3682 y #3683 MERGED. | No duplicar implementaciones resueltas por PR. Reabrir solo con defecto reproducible. |
| L12 — Seguridad duplicada | #3596, #3597 y #3605 MERGED. | La clasificación vieja no impone segundas correcciones sin evidencia causal. |
| L13 — Auditoría antigua | Es un corte histórico del plan, anterior a PR de roles/alta actual. | No adoptar su etiqueta PENDIENTE como decisión operativa actual. |
| L14 — Cloudinary y promoción | [#3575](https://github.com/solqaryn/Solqaryn/pull/3575) y [#3576](https://github.com/solqaryn/Solqaryn/pull/3576) MERGED en dev; #3574/#3583 NO MERGED. | 11 imágenes físicas, QA/PROD y estado de proveedores: NO_VERIFICADOS. |
| L15 — Etapas de roles y capacidades | #3669, #3683 y [#3678](https://github.com/solqaryn/Solqaryn/pull/3678) MERGED. | Auditar solo rutas/capacidades aún sin verificación proporcional. |

## Prueba dirigida y caso negativo obligatorio

**PASS — identidad y CI:** GET de los tres checks exact-SHA de dev = SUCCESS, con URLs y nombres distintos; PR #3741 y PR #3738 MERGED; documentación canónica vigente accesible. **PASS — caso negativo real:** el plan comercial conserva como foto histórica que #3583/#3574 estaban abiertas; GET actual devuelve CLOSED/MERGED=false para ambas. Por tanto la regla “usar foto del 12–15 octubre como estado actual” resulta **RECHAZADA**. No actualizar la fotografía antigua ni simular que una rama cerrada fue liberada.

**NO_VERIFICADO — paridad de proveedores:** [P0-001.B](https://github.com/solqaryn/Solqaryn/blob/d3ed287d4bf3748d039223bdfc5b3909170b24cb/docs/P0-001-B-VALIDACION-PARIDAD-DEV.md) registra Render live / Vercel CANCELED para un SHA anterior; ello es una **negativa puntual**, no prueba del estado de despliegue para d3ed287d4bf3748d039223bdfc5b3909170b24cb. Requiere readback específico en scope correspondiente antes de cualquier afirmación positiva.

## Impacto, revisión y rollback

Documento de evidencias únicamente. No modifica Angular, backend, Domain, Application, Infrastructure, APIs/DTO, MySQL, RBAC/JWT, PII, secretos, jobs, clientes, servicios externos, deploys ni flags. Preserva integridad, aislamiento A/B y CI. La prueba causal es reconciliar GET actual con fuentes L01–L15, **no reinstalar componentes certificados**. Si un resultado cambia, realizar nuevo corte en lugar de alterar retroactivamente esta fotografía.

Cierre P0-002.A: revisión independiente del SHA final del PR, tres checks requeridos SUCCESS, merge protegido de dev y lectura posterior de PLAN_MAESTRO, COLA y BITACORA antes de LISTO. El estado permanece VALIDANDO hasta ese cierre. Rollback: revertir solo este changeset documental por PR protegido, sin borrar eventos ni afectar código de producto.
