<script lang="ts">
  import { onMount, tick } from 'svelte';
  import { getDashboardShipments } from '../../../lib/services/shipmentService';
  import type { DashboardShipmentDto, FiltersState, PaginatedDashboardShipmentsResponse } from '$lib/types';

  import Filters from '$lib/components/dashboard/Filters.svelte';
  import ShipmentList from '$lib/components/dashboard/ShipmentList.svelte';

  let allShipments: DashboardShipmentDto[] = [];
  let currentPage: number = 1;
  let totalPages: number = 1;
  let isLoading: boolean = false;
  let hasMore: boolean = true;
  let observer: IntersectionObserver;
  let loadMoreElement: HTMLElement;

  const PAGE_SIZE = 100; // Define a page size for infinite scrolling

  let currentFilters: FiltersState = {
    id: '',
    city: '',
    status: '',
    startDate: '',
    endDate: ''
  };

  async function loadShipments() {
    if (isLoading || !hasMore) return;

    isLoading = true;
    try {
      const response: PaginatedDashboardShipmentsResponse = await getDashboardShipments(currentPage, PAGE_SIZE, currentFilters);
      allShipments = [...allShipments, ...response.shipments];
      totalPages = response.pagination.total_pages;
      hasMore = currentPage < totalPages;
      currentPage++;
    } catch (error) {
      console.error('Error loading shipments:', error);
      // Optionally, display an error message to the user
    } finally {
      isLoading = false;
    }
  }

  async function loadUntilFull() {
    if (isLoading || !hasMore) return;

    // Load the first page
    await loadShipments();
    await tick(); // Wait for DOM to update

    // Keep loading more pages as long as there is more data
    // and the content is not filling the screen.
    while (hasMore && !isLoading && loadMoreElement && loadMoreElement.getBoundingClientRect().top <= window.innerHeight) {
      await loadShipments();
      await tick(); // Wait for DOM to update
    }
  }

  async function handleFilterChange(event: CustomEvent<FiltersState>) {
    currentFilters = event.detail;
    // Reset pagination and reload from scratch when filters change
    allShipments = [];
    currentPage = 1;
    hasMore = true;
    await loadUntilFull();
  }

  onMount(async () => {
    await loadUntilFull();

    observer = new IntersectionObserver(
      (entries) => {
        const [entry] = entries;
        if (entry.isIntersecting && hasMore && !isLoading) {
          loadShipments();
        }
      },
      { threshold: 0.1 }
    );

    if (loadMoreElement) {
      observer.observe(loadMoreElement);
    }

    return () => {
      if (observer) {
        observer.disconnect();
      }
    };
  });
</script>

<h2>Dashboard de Pedidos</h2>
<p>Listado de Pedidos</p>

<Filters on:filterChange={handleFilterChange} />

<ShipmentList shipments={allShipments} />

{#if isLoading}
  <p>Cargando más pedidos...</p>
{:else if !hasMore && allShipments.length > 0}
  <p>No hay más pedidos para cargar.</p>
{/if}

<div bind:this={loadMoreElement} style="height: 1px; margin-top: -1px;"></div>
