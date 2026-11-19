# C07-04.M05.03 — Oracle EF Core / MySQL 8.4 aislado

- Ámbito: persistencia real de pedidos comerciales, cotizaciones, idempotencia y frontera de tenant; código runtime no modificado.
- Prueba: C0704M0503MySqlPedidosIntegrationTests, \`Category=Integration\`, .NET 10, proveedor Oracle MySql.EntityFrameworkCore y contenedor MySQL 8.4 en GitHub Actions.
- A1 positivo: dos empresas A/B insertan pedidos independientes con la misma clave de idempotencia en una base efímera, leyendo después por DbContext distinto.
- A1 negativo: duplicación de clave dentro de A debe fallar en el índice físico, nunca registrarse como nuevo pedido; cadena remota y puerto inesperado se rechazan.
- A2: consultas con contexto de tenant A nunca devuelven el pedido B ni viceversa; no usa JWT firmado ni servicio HTTP real.
- A3: cotización aceptada, cliente, producto, documento y pedido persisten en MySQL real; transacción revertida no conserva el pedido.
- A4: exigir job GitHub-hosted completed/success, TRX cuatro casos, Scope Lock/V5 exact-SHA, publicación fast-forward y readback institucional. Si falla la CI, mantener VALIDANDO y reparar causalmente, sin PASS ficticio.
- Fuera de alcance: pagos, inventario, emisión fiscal, Aiven, datos/secretos reales, API/HTTP/JWT firmado, revocación RBAC y aceptación completa del comercio. No declarar A0–A4 global ni LISTO por estas pruebas parciales.
- A5: revisión independiente por otra Task Javier; no puede autoaprobar el autor DEV06.
- Rollback: revertir sólo commit en dev tras revalidación de HEAD; la base efímera se elimina en finally, no hay migración comercial.
