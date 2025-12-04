<script lang="ts">
  import type { DashboardShipmentDto, ShipmentStatus, TransportType } from '$lib/types';
  import Icon from '$lib/components/Icon.svelte';

  export let shipments: DashboardShipmentDto[] = [];

  const statusNames: Record<ShipmentStatus, string> = {
    'created': 'Creado',
    'reserved': 'Reservado',
    'in_transit': 'En Tránsito',
    'delivered': 'Entregado',
    'cancelled': 'Cancelado',
    'in_distribution': 'En Distribución',
    'arrived': 'Arribado',
  };

  const statusColors: Record<ShipmentStatus, string> = {
    'created': '#f0e68c',
    'reserved': '#ffa07a',
    'in_transit': '#add8e6',
    'delivered': '#98fb98',
    'cancelled': '#f08080',
    'in_distribution': '#dda0dd',
    'arrived': '#8fbc8f',
  };

  function hexToRgba(hex: string, opacity = 1): string {
    if (!/^#([A-Fa-f0-9]{3}){1,2}$/.test(hex)) return 'rgba(0,0,0,0)';
    let c: any = hex.substring(1).split('');
    if (c.length === 3) c = [c[0], c[0], c[1], c[1], c[2], c[2]];
    c = '0x' + c.join('');
    return `rgba(${[(c>>16)&255, (c>>8)&255, c&255].join(',')},${opacity})`;
  }

  function getHighlightColor(hex: string, status: ShipmentStatus): string {
    if (!/^#([A-Fa-f0-9]{3}){1,2}$/.test(hex)) return '#000000';
    let c: any = hex.substring(1).split('');
    if (c.length === 3) c = [c[0], c[0], c[1], c[1], c[2], c[2]];
    c = '0x' + c.join('');
    let r = (c >> 16) & 255, g = (c >> 8) & 255, b = c & 255;
    r /= 255, g /= 255, b /= 255;
    const max = Math.max(r, g, b), min = Math.min(r, g, b);
    let h:number = 0, s:number, l:number = (max + min) / 2;
    if (max === min) { h = s = 0; } else {
        const d = max - min;
        s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
        switch (max) {
            case r: h = (g - b) / d + (g < b ? 6 : 0); break;
            case g: h = (b - r) / d + 2; break;
            case b: h = (r - g) / d + 4; break;
        }
        h /= 6;
    }
    let newLightness;
    if (['created', 'reserved', 'delivered', 'in_transit'].includes(status)) { newLightness = 0.1; } else { newLightness = l > 0.55 ? l - 0.5 : l + 0.5; }
    if (s === 0) { r = g = b = newLightness; } else {
        const hue2rgb = (p: number, q: number, t: number) => {
            if (t < 0) t += 1; if (t > 1) t -= 1;
            if (t < 1/6) return p + (q - p) * 6 * t;
            if (t < 1/2) return q;
            if (t < 2/3) return p + (q - p) * (2/3 - t) * 6;
            return p;
        };
        const q = newLightness < 0.5 ? newLightness * (1 + s) : newLightness + s - newLightness * s;
        const p = 2 * newLightness - q;
        r = hue2rgb(p, q, h + 1/3); g = hue2rgb(p, q, h); b = hue2rgb(p, q, h - 1/3);
    }
    r = Math.round(r * 255); g = Math.round(g * 255); b = Math.round(b * 255);
    return "#" + ((1 << 24) + (r << 16) + (g << 8) + b).toString(16).slice(1);
  }

  function formatDate(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
  }
</script>

<div class="shipment-list-container">
  <div class="header-grid">
    <div>ID Pedido</div>
    <div>Destino</div>
    <div>Estado</div>
    <div>Transporte</div>
    <div>Ingreso</div>
    <div>Entrega Est.</div>
  </div>

  {#if shipments.length > 0}
    <div class="shipment-body">
      {#each shipments as shipment}
        <a href="/shipments/{shipment.shipping_id}" class="shipment-row-card">
          <div class="cell id-cell">
            <span class="mobile-label">ID Pedido:</span>
            <span>#{shipment.shipping_id}</span>
          </div>
          <div class="cell">
            <span class="mobile-label">Destino:</span>
            <span>{shipment.delivery_address.locality_name}</span>
          </div>
          <div class="cell">
            <span class="mobile-label">Estado:</span>
            <span
              class="status-pill"
              style="background-color: {hexToRgba(statusColors[shipment.status] || '#ccc', 0.7)}; color: {getHighlightColor(statusColors[shipment.status] || '#ccc', shipment.status)}"
            >
              {statusNames[shipment.status] || shipment.status}
            </span>
          </div>
          <div class="cell transport-cell">
            <span class="mobile-label">Transporte:</span>
            <Icon name={shipment.transport_type} className="transport-icon" />
          </div>
          <div class="cell date-cell">
            <span class="mobile-label">Ingreso:</span>
            <span>{formatDate(shipment.created_at)}</span>
          </div>
          <div class="cell date-cell">
            <span class="mobile-label">Entrega Est.:</span>
            <span>{formatDate(shipment.estimated_delivery_at)}</span>
          </div>
        </a>
      {/each}
    </div>
  {:else}
    <div class="no-results-card">
      No se encontraron pedidos con los filtros seleccionados.
    </div>
  {/if}
</div>

<style>
  /* Contenedor principal y definición de la cuadrícula */
  .header-grid, .shipment-row-card {
    display: grid;
    grid-template-columns: 1fr 2fr 1.5fr 0.8fr 1fr 1fr;
    gap: 16px;
    align-items: center;
  }

  .shipment-list-container {
    width: 100%;
    margin-top: 1rem;
  }

  .header-grid {
    color: var(--muted);
    font-family: var(--font-heading);
    font-size: 0.8rem;
    text-transform: uppercase;
    letter-spacing: 1px;
    margin-bottom: 8px;
    border-bottom: 1px solid var(--border);
    padding: 0 16px 8px 16px;
  }

  .shipment-row-card {
    background-color: var(--card);
    border-radius: 8px;
    margin-bottom: 12px;
    padding: 16px;
    text-decoration: none;
    color: inherit;
    box-shadow: 0 2px 5px rgba(0,0,0,0.1);
    transition: transform 0.2s ease-out, box-shadow 0.2s ease-out;
  }

  .shipment-row-card:nth-child(even) {
    background-color: color-mix(in srgb, var(--card) 95%, #000 5%);
  }

  .shipment-row-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 8px 25px -5px hsla(var(--accent-hsl), 0.35);
  }

  .cell {
    display: flex;
    align-items: center;
    min-width: 0;
  }
  
  .id-cell {
    font-family: var(--font-heading);
    font-weight: 700;
    color: var(--accent);
  }

  .transport-cell {
    /* Quitamos el centrado para que se alinee a la izquierda como los demás */
  }

  .status-pill {
    padding: 4px 12px;
    border-radius: 9999px;
    font-weight: bold;
    display: inline-block;
    text-align: center;
    white-space: nowrap;
    font-size: 0.85rem;
  }

  .no-results-card {
    background-color: var(--card);
    padding: 2rem;
    text-align: center;
    border-radius: 8px;
    color: var(--muted);
  }

  .mobile-label { display: none; }

  @media (max-width: 768px) {
    .header-grid { display: none; }
    .shipment-row-card {
      display: block;
      padding: 12px;
    }
    .cell {
      display: flex;
      justify-content: space-between;
      padding: 8px 0;
      border-bottom: 1px solid var(--border);
    }
    .shipment-row-card .cell:last-child { border-bottom: none; }
    .mobile-label {
      display: inline;
      color: var(--muted);
      font-weight: bold;
    }
  }
</style>