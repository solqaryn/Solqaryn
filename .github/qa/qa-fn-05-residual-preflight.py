"""QA FN-05 residual: aggregate-only physical inventory, never migrate or mutate."""
import os
import re
import subprocess

TARGETS = (
    "Categorias", "CategoriasGalerias", "Marcas", "Modelos", "Colores", "Tallas",
    "TipoClientes", "MetodosPago", "Descuentos", "DescuentoProductos",
    "DescuentoCategorias", "DescuentoClientes", "DescuentoRoles",
    "HistorialUsoDescuentos", "VentaDescuentos", "Impuestos", "ImpuestoProductos",
    "ImpuestoCategorias", "ImpuestoOperaciones", "ImpuestoClientes",
    "ImpuestoProveedores", "HistorialAplicacionImpuestos", "VentaImpuestos",
    "CompraImpuestos", "DashboardKpiConfiguraciones", "MovimientosFinancieros",
    "RevisionesFinancieras", "CostosEnvio", "Sucursales",
    "ConfigEmpresas", "PlantillasCorreoEmpresa", "Suscripciones", "Planes",
    "PlanModulos", "PlanLimites", "Usuarios", "UsuarioEmpresas", "Roles",
    "Permisos", "RolPermisos", "UsuarioRolesPlataforma",
)
# Only candidate tenant-owned tables may drive a future ownership migration.
# Global catalog definitions, identities, memberships and grants are inventory-only.
CANDIDATES = set(TARGETS[:29]) - {"MetodosPago", "Sucursales"}
V1 = {
    "Productos", "ProductoVariantes", "ProductoImagenes", "Clientes", "Proveedores",
    "Compras", "Ventas", "CompraDetalles", "VentaDetalles", "Facturas",
    "MovimientosInventario", "FacturaDetalles", "FacturaPagos", "CompraDocumentos",
    "ExistenciasVariante", "ConsumosInsumos", "ConsumoInsumoDetalles",
    "AjustesInventario", "AjusteInventarioDetalles", "TransferenciasInventario",
    "TransferenciaInventarioDetalles", "RecepcionesCompra", "RecepcionCompraDetalles",
    "OrdenesCompra", "OrdenCompraDetalles", "SolicitudesCompra", "SolicitudCompraDetalles",
}

def require(ok, label):
    if not ok:
        raise RuntimeError(label)

def ident(value):
    require(re.fullmatch(r"[A-Za-z0-9_]+", value) is not None, "Invalid schema identifier")
    return "\x60" + value + "\x60"

def main():
    require(os.environ.get("CI") == "true"
            and os.environ.get("GITHUB_REPOSITORY") == "solqaryn/Solqaryn"
            and os.environ.get("GITHUB_REF") == "refs/heads/qa", "QA scope mismatch")
    bindings = {"DB_HOST": "solqaryn-mysql-solqaryn.h.aivencloud.com",
                "DB_PORT": "14402", "DB_NAME": "solqaryn_qa", "DB_USER": "solqaryn_qa_user"}
    require(all(os.environ.get(k) == v for k, v in bindings.items()), "QA binding mismatch")
    password = os.environ.get("DB_PASSWORD")
    require(bool(password), "Missing database authentication")
    env = dict(os.environ, MYSQL_PWD=password)
    args = ["mysql", "--protocol=TCP", "--connect-timeout=12", "--ssl-mode=REQUIRED",
            "-h", bindings["DB_HOST"], "-P", bindings["DB_PORT"], "-u", bindings["DB_USER"],
            "--batch", "--raw", "--skip-column-names", bindings["DB_NAME"]]
    def query(*statements):
        require(all(s.startswith("SELECT ") and ";" not in s for s in statements), "SELECT-only contract")
        sql = ("SET SESSION TRANSACTION READ ONLY;\n"
               "START TRANSACTION WITH CONSISTENT SNAPSHOT, READ ONLY;\n"
               + ";\n".join(statements) + ";\nCOMMIT;\n")
        result = subprocess.run(args, input=sql, text=True, capture_output=True,
                                env=env, timeout=120)
        require(result.returncode == 0, "Read-only metadata or aggregate query failed")
        return [line.split("\t") for line in result.stdout.splitlines()]

    identity = query("SELECT DATABASE(), SUBSTRING_INDEX(CURRENT_USER(),'@',1), VERSION(), @@transaction_read_only")
    require(len(identity) == 1 and identity[0][:2] == [bindings["DB_NAME"], bindings["DB_USER"]]
            and re.fullmatch(r"8\.4\.11(?:-.*)?", identity[0][2]) is not None
            and identity[0][3] == "1", "QA database identity or read-only transaction mismatch")
    history = query("SELECT COUNT(*), SUM(MigrationId='20261126170000_QaCommercialOwnershipV1') FROM __EFMigrationsHistory")
    require(history == [["110", "1"]], "Unexpected schema baseline; do not infer a migration")
    tables = {r[0] for r in query("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE'")}
    require(set(TARGETS) <= tables and V1 <= tables, "Expected QA table missing")
    cols = query("SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() ORDER BY TABLE_NAME, ORDINAL_POSITION")
    owners = {t for t,c in cols if c == "EmpresaId"}
    require(V1 <= owners, "Certified V1 ownership columns missing")
    keys = query("SELECT TABLE_NAME, CONSTRAINT_NAME, COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME, ORDINAL_POSITION FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_SCHEMA=DATABASE() AND REFERENCED_TABLE_SCHEMA=DATABASE() ORDER BY TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION")
    related = [row for row in keys if row[0] in TARGETS or row[3] in TARGETS]
    dependent = {row[0] for row in related if row[3] in CANDIDATES}
    inspect = sorted(set(TARGETS) | dependent)
    require(set(inspect) <= tables, "Dependent table missing")
    summaries = []
    for t in inspect:
        owned = t in owners
        total = "COUNT(*)"
        nulls = "COALESCE(SUM(EmpresaId IS NULL),0)" if owned else "COUNT(*)"
        summaries.append(f"SELECT '{t}', {total}, {int(owned)}, {nulls} FROM {ident(t)}")
    result = query(*summaries)
    require(len(result) == len(inspect), "Table aggregate count mismatch")
    for t,total,owner,nulls in result:
        require(t in inspect and all(n.isdigit() for n in (total,owner,nulls)), "Invalid aggregate")
        print(f"QA_FN05_RESIDUAL_TABLE {t} rows={total} owner_column={owner} unowned_or_unscoped={nulls}")
    index_rows = query("SELECT TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX, COALESCE(COLUMN_NAME,''), NON_UNIQUE FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX")
    for t,ix,ordinal,col,nonunique in index_rows:
        if t in inspect and nonunique == "0":
            print(f"QA_FN05_RESIDUAL_UNIQUE {t}.{ix} position={ordinal} column={col}")
    for child,fk,col,parent,pcol,ordinal in related:
        print(f"QA_FN05_RESIDUAL_FK {child}.{fk} position={ordinal} column={col} parent={parent}.{pcol}")

    # For single-ID edges to prospective owners, quantify unresolved owned-child
    # references, never assign ownership or return row values. Existing V1
    # constraints are preserved and are not a substitute for this inspection.
    grouped = {}
    for row in related:
        grouped.setdefault((row[0], row[1]), []).append(row)
    edges = []
    for (child,fk), rows in grouped.items():
        if len(rows) != 1:
            continue
        _,_,col,parent,pcol,_ = rows[0]
        if child not in owners or parent not in (CANDIDATES | V1) or pcol != "Id":
            continue
        unresolved = ("p.EmpresaId IS NULL OR p.EmpresaId<>c.EmpresaId" if parent in owners else "TRUE")
        edges.append(f"SELECT '{child}', '{fk}', COUNT(*) FROM {ident(child)} c "
                     f"JOIN {ident(parent)} p ON c.{ident(col)}=p.{ident(pcol)} "
                     f"WHERE c.EmpresaId IS NOT NULL AND ({unresolved})")
    if edges:
        for child,fk,count in query(*edges):
            require(count.isdigit(), "Invalid relationship aggregate")
            print(f"QA_FN05_RESIDUAL_REFERENCE {child}.{fk} unresolved_owned_children={count}")
    missing = sorted(CANDIDATES - owners)
    print("QA_FN05_RESIDUAL_OWNER_COLUMNS_MISSING=" + ",".join(missing))
    print("QA_FN05_RESIDUAL_PREFLIGHT=PASS")
    print("QA_FN05_RESIDUAL_DATABASE_WRITES=0")
    print("QA_FN05_RESIDUAL_CERTIFICATION=NOT_ESTABLISHED")

if __name__ == "__main__":
    main()
