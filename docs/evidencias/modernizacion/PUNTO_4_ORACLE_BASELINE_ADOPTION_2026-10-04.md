# Punto 4 — Estrategia de baseline y adopción para Oracle EF10

Fecha: 2026-10-04  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Dictamen

**Estado: REABIERTO para revalidar el alcance ampliado del probe.** La decisión de arquitectura sigue siendo baseline/adopción; Oracle EF10 no se declara drop-in para las 107 migraciones históricas. No se reejecuta esa historia bajo Oracle ni se reescribe. Una restauración física canónica crea el esquema y una migración Oracle EF10 de baseline lo adopta; en bases existentes la adopción no genera DDL.

## Certificación reproducible

- El workflow [Oracle baseline migration probe](../../../.github/workflows/modernization-oracle-baseline-probe.yml) conserva el baseline creado por la historia Pomelo, produce un dump SQL físico temporal sin secretos, lo restaura a un esquema nuevo y prepara una copia efímera de backend con `net10.0`, EF Core `10.0.12` y Oracle `MySql.EntityFrameworkCore 10.0.9`.
- La copia excluye del build Oracle las migraciones acopladas al provider histórico. Genera una migración Oracle de baseline que falla cerrada si no encuentra las tablas esperadas, registra la adopción y rechaza rollback destructivo.
- En el esquema vacío restaurado, `database update`, `has-pending-model-changes` y generación del script Oracle de baseline pasaron.
- El esquema Oracle restaurado se comparó con el generado por las migraciones Pomelo en columnas/tipos/nullability/defaults/collation, índices, foreign keys y CHECK constraints; el gate exige equivalencia y conteo de tablas.
- La misma adopción se aplicó a un esquema creado por Pomelo; el hash de columnas antes/después fue idéntico, certificando `ORACLE_BASELINE_ADOPTION_NO_DDL=PASS`.
- Run `37230813349` sobre HEAD `4ea2142fb76458d58c0f699e782395e44e3a85c1` terminó `success`; los veinte pasos, incluidas generación EF10, adopción fresh/existing, comparación física y dictamen, terminaron `success`. [Ejecución del probe](https://github.com/solqaryn/Solqaryn/actions/runs/37230813349).
- La comparación de código confirma que el área de persistencia y los `.csproj` de provider usados por esa corrida no cambiaron; sin embargo, el workflow sí se amplió después, por lo que el run histórico no certifica las nuevas pruebas.

## Revalidación del alcance ampliado — 2026-10-05

Después del run `37230813349`, el workflow añadió generación/verificación del paquete SQL Oracle, aplicación del paquete a una base completamente vacía, comprobación `has-pending-model-changes` tras bootstrap, equivalencia física más amplia (incluyendo tablas/engine/collation y objetos programables), rollback Oracle→Pomelo con hash de esquema/datos/usuario/historial y retención de artefactos de 90 días. El run histórico no ejecutó esos pasos.

Se amplía el disparador de push a `backend/**`, además del workflow y reportes específicos P4–P6. El Punto 4 queda abierto hasta que el workflow actualizado termine success y pruebe fresh bootstrap, adopción sin DDL, equivalencia y rollback en una base MySQL descartable.

## Límites del cierre

- Las 107 migraciones históricas permanecen como historia Pomelo y no se convierten ni reescriben en este punto.
- El probe genera baseline y migración en el runner y los retiene como artefactos de CI. Aún no certifica el bootstrap operativo empaquetado para una instalación productiva limpia: eso corresponde al Punto 6.
- Esta solución no cambia `.csproj` productivos, configuración runtime, datos, QA, `main` ni PROD; Fase 7 sigue sin ejecutarse.

**Punto 4: ABIERTO — pendiente del probe actualizado.** Fase 7 no ejecutada; sin cambios de `.csproj` productivos, QA, `main` ni PROD.

MAPA_ARQUITECTURA: SIN_CAMBIO.
