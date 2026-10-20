# SOLQARYN — Sustitución segura de los checks obligatorios de dev

## Alcance y objetivo

La única autoridad ejecutable es **SOLQARYN PLAN COMERCIAL INICIAL UNIFICADO**. Los nombres históricos `Dictamen Fase 6` y `Dictamen Fase 7` del ruleset `SOLQARYN - Protección dev` (ID `22829243`) no pueden imponer fases o secuencias de otros planes. **Sus pruebas actuales de MySQL, EF Core, aislamiento empresarial y seguridad no deben eliminarse.**

La transición conserva los mismos jobs y dependencias técnicas del HEAD vigente, renombrando únicamente los dictámenes de entrada de CI y añadiendo dos puentes de compatibilidad temporales.

| Check heredado exigido | Check nuevo equivalente (job real, no alias) |
| --- | --- |
| `Dictamen Fase 6` | `SOLQARYN DEV - Integridad MySQL comercial` |
| `Dictamen Fase 7` | `SOLQARYN DEV - Seguridad runtime .NET 10` |

El check nuevo de integridad depende del job MySQL real `contracts`; el nuevo runtime depende de `canonical-build`, `mysql-contract`, `security-contract` y `docker-smoke`. Sus suites son las actuales del producto y no se convierten en fases del backlog comercial. Los puentes con nombres antiguos dependen del dictamen nuevo y **fallan** cuando éste falla, para que la protección existente no quede sin respaldo mientras se realiza la sustitución.

## Pasos de ejecución protegida

1. Fusionar la PR de transición en `dev` exclusivamente después de obtener CI exact-head verde con ambos nuevos dictámenes y ambos puentes antiguos **SUCCESS**; el ruleset vigente sigue obligatorio hasta ese momento.
2. Usar una cuenta con permiso de edición de rulesets. Abrir [Ruleset de dev](https://github.com/solqaryn/Solqaryn/rules/22829243), entrar a **Settings → Rules → Rulesets → SOLQARYN - Protección dev** y buscar `Require status checks to pass`.
3. Sustituir las dos entradas antiguas por los dos nombres nuevos **idénticos** (sin editar caracteres), y agregar `verify-solqaryn-project-scope` y `VAEP engine TASKS_ONLY` si están disponibles como checks permanentes. Verificar que su emisor sea GitHub Actions y conservar la política activa, pull request obligatorio, prohibición de borrado y force push.
4. Guardar. Consultar `GET /repos/solqaryn/Solqaryn/rulesets/22829243`: los campos `required_status_checks` ya no deben contener `Dictamen Fase 6` ni `Dictamen Fase 7`; deben contener ambos nombres nuevos. Demostrar una PR nueva con ambos SUCCESS antes de retirar los alias.
5. Sólo tras readback y prueba de una PR protegida, retirar los jobs de compatibilidad mediante otro cambio mínimo. Si la edición administrativa falla, NO desactivar el ruleset ni forzar un merge: informar la acción exacta al propietario.

## Rollback

Antes de guardar copiar las dos entradas previas. Si la nueva configuración no dispara los checks o bloquea un PR, restaurar las entradas previas y corregir causalmente el workflow; nunca desactivar la protección de `dev`.

**No hay ninguna nueva microtarea comercial, automación ejecutora, compra o cambio de aplicación asociado a esta transición.**
