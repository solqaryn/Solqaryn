# GO LIVE — migración histórica SOLQARYN -> SOLQARYN


> `PROJECT_SCOPE_LOCK=STRICT`
> Alcance operativo: SOLQARYN / `solqaryn/Solqaryn` / `dev`.
## Propósito

Migrar la información histórica de SOLQARYN desde el **respaldo verificado** al primer tenant SOLQARYN de la nueva plataforma SOLQARYN.

## Precondiciones obligatorias

No ejecutar hasta que:

1. DEV esté 100% certificado;
2. los cambios certificados estén promovidos a `main`;
3. Render, Vercel, Aiven, Cloudinary, correo y DNS PROD estén certificados;
4. `solqaryn_prod` tenga el esquema final y permanezca sin la carga histórica;
5. exista backup/rollback del destino;
6. el propietario autorice explícitamente la migración.

## Fuente y destino

- Fuente: archivo de respaldo histórico verificado de SOLQARYN.
- Destino: `solqaryn_prod`.
- Tenant destino: SOLQARYN.
- Prohibido usar una base o deployment legacy vivo como fuente implícita.

## Secuencia

1. restaurar el respaldo en un entorno aislado de ensayo;
2. inventariar tablas, filas, claves y relaciones;
3. mapear datos al modelo multiempresa actual;
4. ejecutar transformación/migración en ensayo;
5. reconciliar conteos, saldos, inventario, facturas, clientes, productos y relaciones;
6. corregir incompatibilidades de forma reproducible;
7. generar evidencia de ensayo y rollback;
8. con autorización, ejecutar la misma migración contra `solqaryn_prod`;
9. ejecutar smoke, reconciliación y auditoría post-migración;
10. bloquear cualquier carga adicional si existe divergencia material.

La migración histórica es el último paso de datos, no una dependencia para desarrollar o certificar la plataforma.
