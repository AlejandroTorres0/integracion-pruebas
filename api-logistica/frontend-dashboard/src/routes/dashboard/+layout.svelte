<script>
  import { onMount } from 'svelte';
  import { toggleTheme, isLight } from '$lib/theme.js';

  let light = false;

  function handleToggle() {
    toggleTheme();
    // pequeño delay para asegurar que el DOM se actualice
    light = isLight();
  }

  onMount(() => {
    const header = document.querySelector('header');
    const sidebar = document.querySelector('.sidebar');
    if (!header || !sidebar) return;

    // inicializar estado del label del tema
    light = isLight();

    const handleScroll = () => {
      const scrolledPastHeader = window.scrollY > header.offsetHeight;
      sidebar.classList.toggle('full', scrolledPastHeader);
    };

    window.addEventListener('scroll', handleScroll, { passive: true });
    // run once to set initial state
    handleScroll();

    return () => window.removeEventListener('scroll', handleScroll);
  });
</script>

<div class="layout">
  <aside class="sidebar">
    <details class="sidebar-details" open>
      <summary>Dashboard</summary>
      <nav>
        <ul>
          <li><a href="/dashboard/shipments">Pedidos</a></li>
          <li><a href="/dashboard/shipments/create">Crear Pedido</a></li>
          <li><a href="/dashboard/transport-methods">Transport Methods</a></li>
        </ul>
      </nav>
    </details>
    <div class="sidebar-footer">
      <button class="theme-toggle" on:click={handleToggle}>{light ? 'Modo claro' : 'Modo oscuro'}</button>
    </div>
  </aside>
  <main class="content">
    <slot></slot>
  </main>
</div>

<style>
  .layout {
    display: block; /* content flow independent because sidebar is fixed */
  }
  .sidebar {
    width: 200px;
    background-color: var(--card);
    padding: 1rem;
    border-right: 1px solid var(--border);
    position: fixed;
    left: 0;
    top: var(--header-height, 64px);
    height: calc(100vh - var(--header-height, 64px));
    overflow-y: auto;
    z-index: 20;
    display: flex;
    flex-direction: column;
  }

  /* Cuando el usuario ha scrolleado más allá del header, la sidebar ocupa toda la ventana */
  .sidebar.full {
    top: 0;
    height: 100vh;
  }
  h2 {
    color: var(--text);
    margin-bottom: 2rem;
  }
  nav ul {
    list-style: none;
    padding: 0;
  }
  nav a {
    display: block;
    padding: 0.75rem 1rem;
    color: var(--muted);
    text-decoration: none;
    border-radius: 4px;
    margin-bottom: 0.5rem;
  }
  nav a:hover {
    background-color: var(--button-bg);
    color: var(--text);
  }
  .sidebar-details summary {
    cursor: pointer;
    padding: 0.5rem 0;
    color: var(--text);
    font-weight: 600;
    list-style: none;
  }

  .sidebar-details summary::-webkit-details-marker {
    display: none;
  }

  .sidebar-details summary:after {
    content: '▾';
    float: right;
    transform: rotate(0deg);
    transition: transform 0.15s ease;
  }

  .sidebar-details[open] summary:after {
    transform: rotate(180deg);
  }
  .content {
    margin-left: 200px; /* espacio para la sidebar fija */
    padding: 2rem;
    overflow-y: auto;
    min-height: calc(100vh - var(--header-height, 64px));
  }
  .sidebar-details {
    flex: 1 1 auto;
    overflow: auto;
  }
  .sidebar-footer {
    margin-top: auto;
    padding-top: 0.5rem;
    border-top: 1px solid var(--border);
    display: flex;
    justify-content: center;
  }
  .theme-toggle {
    background: transparent;
    color: var(--text);
    border: 1px solid var(--border);
    padding: 0.4rem 0.6rem;
    border-radius: 6px;
    cursor: pointer;
    font-weight: 600;
    width: 100%;
    transform: translateY(-12px); /* subir un poco más el botón respecto al fondo */
  }
</style>