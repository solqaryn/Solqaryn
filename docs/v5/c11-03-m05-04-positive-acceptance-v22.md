# C11-03.M05.04 — aceptación positiva Outbox V22

- PLAN D: ejecutar caso positivo reproducible para el outbox; PLAN E expresa meta acumulativa de la fase y no certificación total de M05.
- R2. El xUnit nuevo prueba el servicio real de aplicación, repositorio Oracle EF y MySQL 8.4 **efímero**, con creación/migración temporal de base y borrado posterior.
- Positivo: primer registro, replay idéntico con mismo EventoId y una fila física; negativo: misma clave/payload distinto produce conflicto, tenant B rechazado, sin fuga de registros.
- Limitación explícita: IUsuarioScopeService es fixture de membresía. NO acredita JWT/HTTP/RBAC real, worker, pagos, PSP ni entrega externa. No se modifica runtime.
- GitHub Actions nuevo exige SHA exacto, Scope Lock, Release, servicio físico MySQL 8.4, TRX 1/1 0 fallos/0 skip. No declarar PASS hasta finalización real.
- Rollback: nuevo commit revert de los archivos aditivos, jamás force-push ni datos productivos. A5 corresponde a otra Task Javier.
