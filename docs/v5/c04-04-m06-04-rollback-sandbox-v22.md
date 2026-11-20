# C04-04.M06.04 — Ensayo de rollback MySQL 8.4 sin pérdida legítima (V22)

## PLAN D719 y alcance
Ensayar rollback o desactivación sin borrar datos legítimos. Entrega R2 exclusivamente test-only; no certifica PSP, cobros reales, despliegue, pagos ni modo mantenimiento de producción. Conforme al STANDARD V5 y Scope Lock STRICT.

## Prueba reproducible
- Una prueba de integración xUnit se ejecuta con Oracle EF Core 10 y MySQL 8.4 físico efímero en GitHub-hosted. Rechaza conexiones fuera de 127.0.0.1:3306 root, variable de entorno ausente y versiones distintas de MySQL 8.4. Usa esquema aleatorio y elimina exclusivamente ese esquema efímero en finally.
- Se crean dos reservas válidas originales, una en empresa A y otra en empresa B, con IDs persistidos. Se crean luego dos reservas adicionales dentro de una transacción y se revierte con RollbackAsync. Un DbContext independiente verifica físicamente que solo las dos reservas previas sobreviven, conserva IDs y que no persiste ninguna reserva revertida.
- Un intento de lookup entre empresas A/B con clave adivinada retorna Missing. Una petición cancelada devuelve OperationCanceledException sin insertar filas. Después de cancelar, una nueva operación legítima puede persistir para A, manteniendo B y los dos IDs originales intactos. Ningún Result autoriza PSP, charge/refund ni declara JWT verificado.
- CI: build Release .NET10, Scope Lock exact-SHA, Skill canónica y un TRX obligatorio total=1,passed=1,failed=0,notExecuted=0. Los checks queued, in_progress, skipped, cancelled o failed NO son PASS.

## Limitaciones y riesgo residual
El store C0404PaymentReservationStore es infraestructura aislada de prueba; no está integrado en API, AppDbContext comercial ni DI productiva. No prueba apagado de PSP real, JWT firmado + membresía MySQL, webhooks, refunds, reconciliación, restauración de backups reales, RPO/RTO ni datos Aiven. A3 demuestra solo transacción y rollback reales del sandbox. La CI global de migraciones fallida no se maquilla ni se descarta. A5 independiente por otra Task Javier y R3 no autorizado.

## Impacto y rollback de cambios
Solo nuevo test de integración, workflow GitHub Actions dedicado, este documento y sección aditiva del CHANGELOG_AI.md. No cambia API, JWT, RBAC, modelos de negocio, migraciones, Angular ni secretos. Rollback del cambio mediante git revert normal tras revalidar HEAD; BITACORA histórica no se borra. A0 diff y seguridad, A1 positivo/negativo, A2 tenant A/B, A3 MySQL efímero real, A4 check-runs exact-SHA y readback institucional, A5 independiente después de LISTO.
