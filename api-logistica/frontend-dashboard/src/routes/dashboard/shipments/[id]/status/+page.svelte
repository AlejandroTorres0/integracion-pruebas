<script lang="ts">
  import type { PageData } from './$types';
  import { updateShipmentStatus } from '../../../../../lib/services/shipmentService';
  import type { ShipmentStatus } from '../../../../../lib/types';
  import { invalidateAll } from '$app/navigation';

  export let data: PageData;

  let { shipmentDetails } = data;

  // Lista de todos los estados posibles para el dropdown
  const allStatuses: ShipmentStatus[] = [
    'created',
    'reserved',
    'in_distribution',
    'in_transit',
    'arrived',
    'delivered',
    'cancelled',
  ];

  // Nombres amigables para mostrar en el UI
  const statusNames: Record<ShipmentStatus, string> = {
    created: 'Creado',
    reserved: 'Reservado',
    in_distribution: 'En Distribución',
    in_transit: 'En Tránsito',
    arrived: 'Arribado',
    delivered: 'Entregado',
    cancelled: 'Cancelado',
  };

  let selectedStatus: ShipmentStatus = shipmentDetails?.status || 'created';
  let message: string = 'Estado actualizado por operador de logística.';
  let isLoading = false;
  let errorMessage: string | null = null;
  let successMessage: string | null = null;

  async function handleSubmit() {
    if (!shipmentDetails) return;

    isLoading = true;
    errorMessage = null;
    successMessage = null;

    try {
      const success = await updateShipmentStatus(
        shipmentDetails.shipping_id.toString(),
        selectedStatus,
        message
      );

      if (success) {
        successMessage = '¡Estado actualizado con éxito!';
        // Invalida los datos de la página actual para forzar una recarga
        // y así mostrar el nuevo log.
        await invalidateAll();
      }
    } catch (error: any) {
      errorMessage = error.message || 'Ocurrió un error desconocido.';
    } finally {
      isLoading = false;
    }
  }

  function formatDate(isoString: string): string {
    if (!isoString) return '';
    const date = new Date(isoString);
    return date.toLocaleString('es-AR', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
    });
  }
</script>

<div class="status-container">
  {#if shipmentDetails}
    <h2>Cambiar Estado del Pedido #{shipmentDetails.shipping_id}</h2>
    <p>
      <strong>Estado Actual:</strong>
      <span class="current-status">{statusNames[shipmentDetails.status] || shipmentDetails.status}</span>
    </p>

    <div class="form-section">
      <form on:submit|preventDefault={handleSubmit}>
        <div class="form-group">
          <label for="status-select">Nuevo Estado</label>
          <select id="status-select" bind:value={selectedStatus}>
            {#each allStatuses as status}
              <option value={status}>{statusNames[status]}</option>
            {/each}
          </select>
        </div>

        <div class="form-group">
          <label for="message">Mensaje (opcional)</label>
          <textarea id="message" bind:value={message} rows="3"></textarea>
        </div>
        
        <button type="submit" disabled={isLoading} class="submit-btn">
          {isLoading ? 'Guardando...' : 'Guardar Cambio'}
        </button>
      </form>

      {#if errorMessage}
        <p class="error">{errorMessage}</p>
      {/if}
      {#if successMessage}
        <p class="success">{successMessage}</p>
      {/if}
    </div>

    <div class="logs-section">
      <h3>Historial de Estados</h3>
      {#if shipmentDetails.logs && shipmentDetails.logs.length > 0}
        <ul class="logs-list">
          {#each shipmentDetails.logs.slice().reverse() as log}
            <li>
              <strong>{formatDate(log.timestamp)}:</strong>
              <span class="log-status">{statusNames[log.status] || log.status}</span>
              - <span>{log.message}</span>
            </li>
          {/each}
        </ul>
      {:else}
        <p>No hay historial de estados para este pedido.</p>
      {/if}
    </div>
  {:else}
    <p>Cargando detalles del pedido...</p>
  {/if}
</div>

<style>
  .status-container {
    padding: 2rem;
    max-width: 800px;
    margin: 0 auto;
  }
  .current-status {
    font-weight: bold;
    padding: 4px 8px;
    border-radius: 4px;
    background-color: var(--accent);
    color: white;
  }
  .form-section {
    margin: 2rem 0;
    padding: 1.5rem;
    border: 1px solid var(--border);
    border-radius: 8px;
  }
  .form-group {
    margin-bottom: 1rem;
  }
  label {
    display: block;
    margin-bottom: 0.5rem;
    font-weight: 500;
  }
  select, textarea {
    width: 100%;
    padding: 0.75rem;
    border-radius: 4px;
    border: 1px solid #555;
    background-color: var(--card);
    color: var(--text);
    font-size: 1rem;
    box-sizing: border-box;
  }
  .submit-btn {
    padding: 0.75rem 1.5rem;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-weight: bold;
    font-size: 1rem;
    background-color: #3b82f6;
    color: white;
    width: 100%;
  }
  .submit-btn:disabled {
    background-color: #555;
    cursor: not-allowed;
  }
  .logs-section {
    margin-top: 2rem;
  }
  .logs-list {
    list-style: none;
    padding: 0;
    margin-top: 1rem;
    font-family: monospace;
    font-size: 0.9rem;
  }
  .logs-list li {
    padding: 0.5rem 0;
    border-bottom: 1px solid var(--border);
  }
  .log-status {
    font-weight: bold;
  }
  .error {
    color: #ef4444;
    margin-top: 1rem;
  }
  .success {
    color: #22c55e;
    margin-top: 1rem;
  }
</style>
