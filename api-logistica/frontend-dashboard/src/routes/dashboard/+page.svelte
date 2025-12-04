<h1>Bienvenido al Dashboard de Papas</h1>
<p>Seleccione una opción:</p>

<nav class="dashboard-options">
    <ul>
        <li><a href="/dashboard/shipments">Pedidos</a></li>
        <li><a href="/dashboard/shipments/create">Crear Pedido</a></li>
    </ul>
</nav>

<style>
    p {
        margin-bottom: 1rem;
    }

    .dashboard-options ul {
        list-style: none;
        padding: 0;
        display: flex;
        gap: 0.5rem;
        flex-wrap: wrap;
        margin-top: 1rem;
    }

    .dashboard-options li {
        margin: 0;
    }

    .dashboard-options a {
        display: inline-flex;
        align-items: center;
        padding: 0.5rem 0.75rem;
        color: #fff;
        text-decoration: none;
        background-color: #2b2b2b;
        border-radius: 6px;
        font-size: 0.95rem;
        transition:
            background-color 0.12s ease,
            transform 0.06s ease;
    }

    .dashboard-options a:hover {
        background-color: #3a3a3a;
        transform: translateY(-1px);
    }
</style>
