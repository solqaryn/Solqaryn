# SOLQARYN — Evidencia temporal para atribución comercial histórica

**Proyecto y entorno:** `solqaryn/Solqaryn`, exclusivamente `dev` / Aiven `solqaryn_dev`. **Estado:** diagnóstico no destructivo; **no** es autorización de backfill.

## Hechos validados antes de la auditoría temporal

- El esquema tiene cinco columnas `EmpresaId?` con sus cinco FK e índices: evidencia `TENANT_REAL_SCHEMA_EXPAND=PASS`, [GitHub Actions #38406239720](https://github.com/solqaryn/Solqaryn/actions/runs/38406239720).
- Los 19 registros de las cinco raíces siguen con `EmpresaId NULL`, conservados íntegros.
- El informe [GitHub Actions #38409321128](https://github.com/solqaryn/Solqaryn/actions/runs/38409321128) identificó 19 autores de creación y membresías **actuales** únicas, así como 14 referencias con owner incompleto. Ninguna de esas pruebas establece a qué empresa pertenecía un registro **en su fecha de origen**.
- El respaldo cifrado y el restore del mismo artefacto se verificaron en [GitHub Actions #38399423450](https://github.com/solqaryn/Solqaryn/actions/runs/38399423450).

## Qué comprueba el nuevo diagnóstico

`.github/workflows/dev-tenant-commercial-temporal-provenance.yml` se ejecuta sobre `dev` en el Environment `DEV` y compara, sin leer ni imprimir PII, las fechas de creación de cada raíz con las fechas de las membresías `UsuarioEmpresas` y de `Empresas`. Por tabla muestra cantidades agregadas: rows huérfanas, autor ausente, membresías creadas/modificadas después del dato y empresas existentes después del dato. También informa la cobertura agregada de `RegistrosAuditoria.EmpresaId`.

**Interpretación obligatoria:** una sola membresía anterior en la base actual **no es prueba concluyente** de que fuera válida históricamente; cambios anteriores a los registros de auditoría o importaciones pueden carecer de historia suficiente. Cuando falte prueba temporal, mantener `EmpresaId NULL` y la cuarentena. No deducir dueño mediante `MIN(EmpresaId)`, único tenant actual, usuario actual, rol global, encabezado HTTP, nombre visible de la tienda o fecha aproximada.

## Criterios para un backfill futuro, fuera del alcance de este workflow

1. Evidencia primaria e individualizable verificable: registro de alta/importación con empresa original, auditoría empresarial histórica o respaldo confiable con autor, fecha y empresa, reconciliado con las cinco raíces y relaciones descendientes.
2. Mapa de procedencia y excepciones **sin exponer PII ni IDs de clientes en logs públicos**; cada conflicto o ambigüedad permanece sin asignar.
3. Backup cifrado vigente de DEV y restauración ensayada, transacción idempotente y restricciones FK/sumas/recibos verificadas antes y después.
4. Solo después de resolver todas las filas y dependencias, aplicar filtros y escrituras por tenant, pruebas con empresas A/B, documentos, tienda, jobs y certificados exact-head.
5. Conservar `UnscopedCommercialTenantGuardMiddleware` y bloqueo de segunda empresa hasta cierre de P0, sin modificar QA/PROD.

**Prohibiciones del workflow:** no DML, DDL, restore, backfill, visualización de filas, publicación de PII ni modificación de secretos. La ejecución exitosa significa únicamente que el diagnóstico terminó.
