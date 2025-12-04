<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import type { FiltersState, ShipmentStatus } from '$lib/types';

  let id: string = '';
  let city: string = '';
  let status: ShipmentStatus | '' = '';
  // Estas variables siempre almacenarán la fecha en formato 'YYYY-MM-DD'
  let startDate: string = '';
  let endDate: string = '';

  const dispatch = createEventDispatcher<{ filterChange: FiltersState }>();

  let debounceTimer: number;

  function debouncedApplyFilters() {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(() => {
      applyFilters();
    }, 300); // 300ms delay
  }

  function applyFilters() {
    dispatch('filterChange', {
      id: id.toUpperCase(),
      city: city.toLowerCase(),
      status: status,
      startDate: startDate,
      endDate: endDate
    });
  }
</script>

<div class="filters-row">
  <div class="filter-grid">
    <input
      type="text"
      placeholder="Filtrar por ID..."
      bind:value={id}
      on:input={debouncedApplyFilters}
    />
    
    <input
      type="text"
      placeholder="Filtrar por destino..."
      bind:value={city}
      on:input={debouncedApplyFilters}
    />

    <select bind:value={status} on:change={applyFilters}>
      <option value="">Todos los estados</option>
      <option value="in_transit">En Tránsito</option>
      <option value="created">Creado</option>
      <option value="delivered">Entregado</option>
      <option value="cancelled">Cancelado</option>
      <option value="in_distribution">En Distribución</option>
      <option value="arrived">Arribado</option>
      <option value="reserved">Reservado</option>
    </select>

    <div class="date-filter">
      <label for="start-date">Desde:</label>
      <input
        type="date"
        id="start-date"
        bind:value={startDate}
        on:change={applyFilters}
      />
    </div>
    <div class="date-filter">
      <label for="end-date">Hasta:</label>
      <input
        type="date"
        id="end-date"
        bind:value={endDate}
        on:change={applyFilters}
      />
    </div>
  </div>

  <button class="search-button" type="button" on:click={applyFilters} aria-label="Buscar pedidos">Buscar</button>
</div>

<style>
  .filters-row {
    display: flex;
    gap: 1rem;
    align-items: center;
  }
  .filter-grid {
    flex: 1;
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    gap: 1rem;
    margin-bottom: 0; /* moved spacing to container */
    align-items: center;
  }
  input, select {
    padding: 0.5rem;
    background-color: var(--card);
    color: var(--text);
    border: 1px solid var(--border);
    border-radius: 4px;
    font-size: 14px;
    width: 100%;
    box-sizing: border-box;
  }
  .search-button {
    background-color: var(--button-bg);
    color: var(--text);
    padding: 0.75rem 1rem;
    border-radius: 4px;
    border: none;
    cursor: pointer;
    font-size: 14px;
  }
  .search-button:hover {
    background-color: var(--button-hover);
  }
  .search-button:active { transform: translateY(1px); }
  .date-filter {
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }
  .date-filter label {
    white-space: nowrap;
  }
  /* Estilo para que el calendario del date picker sea visible en tema oscuro */
  input[type="date"]::-webkit-calendar-picker-indicator {
    filter: invert(1);
  }
</style>