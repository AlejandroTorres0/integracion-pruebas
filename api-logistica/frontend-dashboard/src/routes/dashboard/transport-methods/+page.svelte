<script lang="ts">
  import { onMount } from 'svelte';
  import { getTransportMethods } from '../../../lib/services/shipmentService';
  import Icon from '../../../lib/components/Icon.svelte';

  // Definimos la estructura exacta que viene de tu API (DTO C#)
  interface TransportMethodData {
    type: string;          // Ej: "road", "air"
    name: string;          // Ej: "Road Transport"
    estimated_days: string; // Ej: "3-5 days"
  }

  let methods: TransportMethodData[] = [];
  let isLoading = true;
  let error: string | null = null;

  onMount(async () => {
    try {
      // TypeScript puede quejarse si 'TransportMethods' no está en types.ts, 
      // así que usamos 'any' o la interfaz local para recibir los datos.
      const data: any[] = await getTransportMethods();
      methods = data;
    } catch (e: any) {
      error = e.message || 'Error al cargar los métodos de transporte.';
    } finally {
      isLoading = false;
    }
  });

  // Función auxiliar para asegurarnos de que el icono exista
  function getIconName(type: string): string {
    return type ? type.toLowerCase() : 'road';
  }
</script>

<div class="container">
  <div class="header">
    <h1>Métodos de Transporte</h1>
    <p>Opciones de envío y tiempos estimados disponibles en el sistema.</p>
  </div>

  {#if isLoading}
    <div class="loading-state">
      <div class="spinner"></div>
      <p>Cargando información...</p>
    </div>
  {:else if error}
    <div class="error-msg">
      <p>⚠️ {error}</p>
    </div>
  {:else if methods.length === 0}
    <div class="empty-state">
      <p>No se encontraron métodos de transporte configurados.</p>
    </div>
  {:else}
    <div class="grid">
      {#each methods as method}
        <div class="card">
          <div class="card-header">
            <div class="icon-badge">
               <Icon name={getIconName(method.type)} />
            </div>
            <h3>{method.name}</h3>
          </div>
          
          <div class="card-body">
            <div class="detail-row">
              <span class="label">Tipo:</span>
              <span class="value capitalize">{method.type}</span>
            </div>
            <div class="detail-row">
              <span class="label">Tiempo Estimado:</span>
              <span class="value highlight">{method.estimated_days}</span>
            </div>
          </div>
        </div>
      {/each}
    </div>
  {/if}
</div>

<style>
  .container {
    padding: 2rem;
    max-width: 1200px;
    margin: 0 auto;
  }
  .header {
    margin-bottom: 2.5rem;
    border-bottom: 1px solid var(--border);
    padding-bottom: 1rem;
  }
  .header h1 { margin: 0; font-size: 1.8rem; color: var(--text); }
  .header p { margin: 0.5rem 0 0; color: var(--muted); }

  .grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 1.5rem;
  }

  .card {
    background-color: var(--card);
    border: 1px solid var(--border);
    border-radius: 12px;
    padding: 1.5rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
    transition: transform 0.2s, box-shadow 0.2s;
  }
  .card:hover {
    transform: translateY(-2px);
    box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
    border-color: var(--accent);
  }

  .card-header {
    display: flex;
    align-items: center;
    gap: 1rem;
    margin-bottom: 0.5rem;
  }
  .card-header h3 {
    margin: 0;
    font-size: 1.25rem;
    font-weight: 600;
  }

  .icon-badge {
    width: 48px;
    height: 48px;
    background-color: hsla(var(--accent-hsl), 0.1);
    color: var(--accent);
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.5rem;
  }

  .card-body {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    background-color: var(--bg); /* Fondo sutil para los detalles */
    padding: 1rem;
    border-radius: 8px;
  }

  .detail-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 0.95rem;
  }
  .label { color: var(--muted); font-weight: 500; }
  .value { font-weight: 600; color: var(--text); }
  
  .capitalize { text-transform: capitalize; }
  .highlight { color: var(--accent); }

  .loading-state, .empty-state, .error-msg {
    text-align: center;
    padding: 3rem;
    background: var(--card);
    border-radius: 8px;
    border: 1px dashed var(--border);
  }
  .error-msg { color: #ef4444; border-color: #ef4444; }
  
  /* Animación de carga simple */
  .spinner {
    border: 3px solid rgba(0,0,0,0.1);
    border-left-color: var(--accent);
    border-radius: 50%;
    width: 30px;
    height: 30px;
    animation: spin 1s linear infinite;
    margin: 0 auto 1rem;
  }
  @keyframes spin { 100% { transform: rotate(360deg); } }
</style>