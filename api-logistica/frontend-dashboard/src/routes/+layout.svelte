<script lang="ts">
  import '../app.css';
  import logoUrl from '$lib/assets/papa.png?url';

  // La lógica para calcular la altura del header ya no es necesaria con el nuevo diseño
</script>

<header>
  <div class="header-art-background">
    <!-- Generamos 20 cajas para la animación -->
    {#each { length: 20 } as _, i}
      <div class="box" style:--i={i}></div>
    {/each}
  </div>

  <div class="header-inner">
    <div class="logo">
      <img src={logoUrl} alt="Logo Gestor de Papas" />
      <h1>Gestor de Papas</h1>
    </div>
  </div>
</header>

<main>
  <slot />
</main>

<style>
  header {
    background-color: var(--header-bg);
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    z-index: 30;
    border-bottom: 1px solid var(--border);
    
    /* Contenedor para el arte y el contenido */
    display: flex;
    align-items: center;
    padding: 0 2rem;
    height: 80px; /* Altura fija para un look más definido */

    /* Importante para el posicionamiento del fondo de arte */
    overflow: hidden;
  }

  .header-art-background {
    position: absolute;
    top: 0;
    left: 0;
    width: 200%; /* Ancho doble para que la animación no tenga cortes */
    height: 100%;
    pointer-events: none; /* No interfiere con el ratón */
    
    /* Animación */
    animation: scroll-left 40s linear infinite;
  }

  .header-inner {
    position: relative; /* Se asegura que esté por encima del arte */
    z-index: 1;
    display: flex;
    align-items: center;
    gap: 1rem;
    width: 100%;
  }
  
  .logo {
    display: flex;
    align-items: center;
    gap: 1rem;
  }

  .logo img {
    height: 40px; /* Tamaño ajustado a la nueva altura del header */
    width: auto;
  }

  h1 {
    font-size: 1.5rem;
  }

  main {
    padding: 2rem;
    padding-top: calc(80px + 2rem); /* Altura del header + espacio */
  }

  /* --- Estilos para las Cajas Animadas --- */
  .box {
    position: absolute;
    /* Borde más visible */
    border: 1px solid hsla(var(--accent-hsl), 0.4); 
    /* Fondo sutil para dar cuerpo */
    background-color: hsla(var(--accent-hsl), 0.05);

    /* Usamos una variable --i (índice) para generar valores pseudo-aleatorios */
    width: calc(20px + (var(--i) * 13) % 40px);
    height: calc(20px + (var(--i) * 13) % 40px);
    /* Opacidad aumentada */
    opacity: calc(0.2 + (var(--i) * 7) % 0.4); 
    top: calc((var(--i) * 23) % 80%);
    left: calc((var(--i) * 41) % 95%);
  }
  .box:nth-child(even) {
    border-top: none;
    border-bottom-width: 2px; /* Damos más peso al fondo de la caja abierta */
  }

  /* Animación de scroll horizontal */
  @keyframes scroll-left {
    from {
      transform: translateX(0%);
    }
    to {
      transform: translateX(-50%); /* Mover hasta la mitad para crear el bucle */
    }
  }
</style>