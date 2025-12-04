<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import { searchLocalities } from '$lib/services/shipmentService';
  import type { Locality } from '$lib/types';

  export let selectedLocality: Locality | undefined = undefined;

  let inputValue = '';
  let searchResults: Locality[] = [];
  let isDropdownVisible = false;
  let isLoading = false;
  let debounceTimer: any;

  // --- ESTADOS PARA PAGINACIÓN ---
  let currentPage = 1;
  let hasMore = true;
  const PAGE_SIZE = 20;

  const dispatch = createEventDispatcher();

  async function performSearch(isPaginating = false) {
    if (inputValue.length < 2) {
      searchResults = [];
      return;
    }

    if (!isPaginating) {
      currentPage = 1;
      searchResults = [];
      hasMore = true;
    }
    
    isLoading = true;
    try {
      const newResults = await searchLocalities(inputValue, currentPage);
      searchResults = [...searchResults, ...newResults];
      
      if (newResults.length < PAGE_SIZE) {
        hasMore = false;
      }

    } catch (error) {
      console.error('Error al buscar localidades:', error);
      searchResults = [];
    } finally {
      isLoading = false;
    }
  }

  function onInput() {
    isDropdownVisible = true;
    clearTimeout(debounceTimer);

    if (!inputValue) {
      isDropdownVisible = false;
      searchResults = [];
      return;
    }

    debounceTimer = setTimeout(() => {
        performSearch(false);
    }, 300);
  }

  function selectLocality(locality: Locality) {
    inputValue = locality.locality_name;
    isDropdownVisible = false;
    dispatch('select', { locality: locality });
  }
  
  function handleScroll(event: Event) {
    const list = event.target as HTMLElement;
    const isAtBottom = list.scrollTop + list.clientHeight >= list.scrollHeight - 20;

    if (isAtBottom && hasMore && !isLoading) {
      currentPage++;
      performSearch(true);
    }
  }

  function handleFocus() {
    if(inputValue) {
        isDropdownVisible = true;
    }
  }

  function handleBlur() {
    setTimeout(() => {
      isDropdownVisible = false;
    }, 200);
  }
</script>

<div class="combobox-container">
  <input
    type="text"
    bind:value={inputValue}
    on:input={onInput}
    on:focus={handleFocus}
    on:blur={handleBlur}
    placeholder="Escriba para buscar una localidad..."
  />
  {#if isDropdownVisible}
    <ul class="dropdown" on:scroll={handleScroll}>
      {#if isLoading && searchResults.length === 0}
        <li class="disabled">Buscando...</li>
      {:else if searchResults.length === 0 && inputValue.length >= 2}
        <li class="disabled">No se encontraron resultados.</li>
      {:else}
        {#each searchResults as locality, i (locality.postal_code + locality.locality_name + i)}
          <li
            role="button"
            tabindex="0"
            on:mousedown={() => selectLocality(locality)}
            on:keydown={(e) => e.key === 'Enter' && selectLocality(locality)}
          >
            {locality.locality_name} {#if locality.state_name}({locality.state_name}){/if}
          </li>
        {/each}
        {#if isLoading && searchResults.length > 0}
            <li class="disabled">Cargando más...</li>
        {/if}
      {/if}
    </ul>
  {/if}
</div>

<style>
  .combobox-container {
    position: relative;
    width: 100%;
  }
  input {
    width: 100%;
    padding: 0.75rem;
    border-radius: 4px;
    border: 1px solid #555;
    background-color: var(--card);
    color: var(--text);
    font-size: 1rem;
    box-sizing: border-box;
  }
  .dropdown {
    position: absolute;
    width: 100%;
    list-style: none;
    margin: 4px 0 0 0;
    padding: 0;
    border: 1px solid #555;
    border-radius: 4px;
    background-color: var(--card);
    max-height: 220px;
    overflow-y: auto;
    z-index: 10;
  }
  .dropdown li {
    padding: 0.75rem;
    cursor: pointer;
  }
  .dropdown li:hover {
    background-color: #3b82f6;
    color: white;
  }
  .dropdown li.disabled {
    cursor: not-allowed;
    color: #888;
  }
</style>