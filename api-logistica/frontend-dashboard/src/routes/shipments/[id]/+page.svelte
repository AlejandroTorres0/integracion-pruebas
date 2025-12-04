<script lang="ts">
	import type { ShipmentStatus, ShippingDetail, TransportType } from '$lib/types';
	import { updateShipmentStatus } from '$lib/services/shipmentService';
    import { page } from '$app/stores';
	import { goto } from '$app/navigation';
    import Icon from '$lib/components/Icon.svelte';

	export let data;
	let { shipmentDetails: shipment }: { shipment: ShippingDetail } = data;

	let selectedStatus: ShipmentStatus = shipment.status;
	let statusMessage: string = '';
	let isLoading = false;
	let error: string | null = null;
	let successMessage: string | null = null;

	const statusNames: Record<ShipmentStatus, string> = {
		created: 'Creado',
		reserved: 'Reservado',
		in_transit: 'En Tránsito',
		delivered: 'Entregado',
		cancelled: 'Cancelado',
		in_distribution: 'En Distribución',
		arrived: 'Arribado'
	};

    const availableStatus: ShipmentStatus[] = [
        "created",
        "reserved",
        "in_transit",
        "delivered",
        "cancelled",
        "in_distribution",
        "arrived",
    ];

	const statusColors: Record<ShipmentStatus, string> = {
		created: '#f0e68c',
		reserved: '#ffa07a',
		in_transit: '#add8e6',
		delivered: '#98fb98',
		cancelled: '#f08080',
		in_distribution: '#dda0dd',
		arrived: '#8fbc8f'
	};

	function formatDate(dateString: string): string {
		if (!dateString) return 'N/A';
		const date = new Date(dateString);
		return date.toLocaleDateString('es-AR', {
			day: '2-digit',
			month: '2-digit',
			year: 'numeric',
			hour: '2-digit',
			minute: '2-digit'
		});
	}

	function hexToRgba(hex: string, opacity = 1): string {
		if (!/^#([A-Fa-f0-9]{3}){1,2}$/.test(hex)) return 'rgba(0,0,0,0)';
		let c: any = hex.substring(1).split('');
		if (c.length === 3) c = [c[0], c[0], c[1], c[1], c[2], c[2]];
		c = '0x' + c.join('');
		return `rgba(${[(c >> 16) & 255, (c >> 8) & 255, c & 255].join(',')},${opacity})`;
	}

	async function handleStatusUpdate() {
		isLoading = true;
		error = null;
		successMessage = null;

		try {
			await updateShipmentStatus($page.params.id, selectedStatus, statusMessage);
			successMessage = 'Estado actualizado con éxito.';
			// Refresh data after update
            window.location.reload();
		} catch (err) {
			if (err instanceof Error) {
				error = err.message;
			} else {
				error = 'Ocurrió un error inesperado al actualizar el estado.';
			}
		} finally {
			isLoading = false;
		}
	}
</script>

<div class="detail-container">
	<div class="header">
		<h1>Detalles del Pedido <span class="accent-text">#{shipment.shipping_id}</span></h1>
		<a href="/dashboard/shipments" class="back-link">← Volver al listado</a>
	</div>

	<!-- Grid Principal -->
	<div class="details-grid">
		<!-- Columna Izquierda -->
		<div class="grid-col">
			<div class="info-card">
				<h2>Información General</h2>
				<div class="info-grid">
					<div class="info-item">
						<span class="label">Estado</span>
						<span
							class="status-pill"
							style="background-color: {hexToRgba(
								statusColors[shipment.status] || '#ccc',
								0.7
							)};"
						>
							{statusNames[shipment.status] || shipment.status}
						</span>
					</div>
					<div class="info-item">
						<span class="label">Transporte</span>
						<Icon name={shipment.transport_type} className="transport-icon" />
					</div>
					<div class="info-item">
						<span class="label">Costo Total</span>
						<span>{shipment.currency} {shipment.total_cost.toFixed(2)}</span>
					</div>
					<div class="info-item">
						<span class="label">ID de Usuario</span>
						<span>{shipment.user_id}</span>
					</div>
					<div class="info-item full-width">
						<span class="label">Nº de Seguimiento</span>
						<span class="code">{shipment.tracking_number}</span>
					</div>
				</div>
			</div>

			<div class="info-card">
				<h2>Fechas</h2>
				<div class="info-grid">
					<div class="info-item">
						<span class="label">Creación</span>
						<span>{formatDate(shipment.created_at)}</span>
					</div>
					<div class="info-item">
						<span class="label">Últ. Actualización</span>
						<span>{formatDate(shipment.updated_at)}</span>
					</div>
					<div class="info-item full-width">
						<span class="label">Entrega Estimada</span>
						<span class="accent-text">{formatDate(shipment.estimated_delivery_at)}</span>
					</div>
				</div>
			</div>

            <div class="info-card">
                <h2>Actualizar Estado</h2>
                <form on:submit|preventDefault={handleStatusUpdate}>
                    <div class="form-group">
                        <label for="status-select">Nuevo Estado</label>
                        <select id="status-select" bind:value={selectedStatus} required>
                            {#each availableStatus as statusValue}
                                <option value={statusValue}>{statusNames[statusValue]}</option>
                            {/each}
                        </select>
                    </div>
                    <div class="form-group">
                        <label for="status-message">Mensaje (opcional)</label>
                        <input type="text" id="status-message" bind:value={statusMessage} placeholder="Ej: En camino al centro de distribución">
                    </div>
                    <button type="submit" class="submit-btn" disabled={isLoading}>
                        {#if isLoading}
                            <span class="spinner" />
                            Actualizando...
                        {:else}
                            Actualizar
                        {/if}
                    </button>
					{#if error}
						<p class="message error-message">{error}</p>
					{/if}
					{#if successMessage}
						<p class="message success-message">{successMessage}</p>
					{/if}
                </form>
            </div>
		</div>

		<!-- Columna Derecha -->
		<div class="grid-col">
            <div class="info-card">
				<h2>Direcciones</h2>
                <div class="address-item">
                    <span class="label">Origen</span>
                    <p>{shipment.departure_address.street} {shipment.departure_address.number}</p>
                    <p class="muted-text">{shipment.departure_address.locality_name}</p>
                </div>
                 <div class="address-item">
                    <span class="label">Destino</span>
                    <p>{shipment.delivery_address.street} {shipment.delivery_address.number}</p>
                    <p class="muted-text">{shipment.delivery_address.locality_name}</p>
                </div>
			</div>

			<div class="info-card">
				<h2>Productos</h2>
				<ul class="product-list">
					{#each shipment.products as product}
						<li>
							<span>ID Producto: <span class="code">{product.product_id}</span></span>
							<span>Cantidad: {product.quantity}</span>
						</li>
					{/each}
				</ul>
			</div>

			<div class="info-card">
				<h2>Historial de Estados</h2>
				<ul class="log-list">
					{#each shipment.logs.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()) as log}
						<li class="log-item">
							<div class="log-status">
								<span
									class="status-pill small"
									style="background-color: {hexToRgba(
										statusColors[log.status] || '#ccc',
										0.7
									)};"
								>
									{statusNames[log.status] || log.status}
								</span>
							</div>
							<div class="log-details">
								<span class="log-message">{log.message || 'Sin mensaje.'}</span>
								<span class="log-timestamp">{formatDate(log.timestamp)}</span>
							</div>
						</li>
					{/each}
				</ul>
			</div>
		</div>
	</div>
</div>

<style>
	.detail-container {
		width: 100%;
		max-width: 1200px;
		margin: 2rem auto;
		padding: 0 1rem;
	}

	.header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 2rem;
	}

	.header h1 {
		margin: 0;
	}

	.accent-text {
		color: var(--accent);
	}

	.back-link {
		color: var(--muted);
		text-decoration: none;
		transition: color 0.2s;
	}
	.back-link:hover {
		color: var(--accent);
	}

	.details-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
		gap: 1.5rem;
	}

	.grid-col {
		display: flex;
		flex-direction: column;
		gap: 1.5rem;
	}

	.info-card {
		background-color: var(--card);
		border: 1px solid var(--border);
		border-radius: 8px;
		padding: 1.5rem;
		box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
	}

	.info-card h2 {
		margin-top: 0;
		margin-bottom: 1.5rem;
		border-bottom: 1px solid var(--border);
		padding-bottom: 0.75rem;
	}

    .info-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1.25rem;
    }

    .info-item {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
    }

    .full-width {
        grid-column: 1 / -1;
    }

	.label {
		font-size: 0.8rem;
		color: var(--muted);
		text-transform: uppercase;
	}

	.code {
		font-family: monospace;
		background-color: var(--bg);
		padding: 2px 6px;
		border-radius: 4px;
	}

	.status-pill {
		padding: 4px 12px;
		border-radius: 9999px;
		font-weight: bold;
		font-size: 0.9rem;
		width: fit-content;
	}
    .status-pill.small {
        font-size: 0.75rem;
        padding: 3px 8px;
    }

    .address-item {
        margin-bottom: 1rem;
    }
    .address-item:last-child {
        margin-bottom: 0;
    }
    .address-item p {
        margin: 0;
    }
    .muted-text {
        color: var(--muted);
        font-size: 0.9rem;
    }

    .product-list, .log-list {
        list-style: none;
        padding: 0;
        margin: 0;
        display: flex;
        flex-direction: column;
        gap: 1rem;
    }

    .product-list li {
        display: flex;
        justify-content: space-between;
        padding: 0.5rem 0;
        border-bottom: 1px solid var(--border);
    }
     .product-list li:last-child {
        border-bottom: none;
    }

    .log-item {
        display: flex;
        align-items: center;
        gap: 1rem;
    }

    .log-details {
        display: flex;
        flex-direction: column;
    }

    .log-message {
        font-weight: 500;
    }

    .log-timestamp {
        font-size: 0.8rem;
        color: var(--muted);
    }

	/* Form Styles */
	.form-group {
		margin-bottom: 1rem;
	}
	input,
	select {
		width: 100%;
		padding: 0.75rem;
		background-color: var(--bg);
		color: var(--text);
		border: 1px solid var(--border);
		border-radius: 4px;
		font-size: 1rem;
		box-sizing: border-box;
	}
	.submit-btn {
		background-color: var(--accent);
		color: #fff;
		padding: 0.75rem 1.5rem;
		border: none;
		border-radius: 4px;
		font-size: 1rem;
		cursor: pointer;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: 0.5rem;
	}
	.submit-btn:disabled {
		background-color: var(--muted);
		cursor: not-allowed;
	}

	.message {
		margin-top: 1rem;
		padding: 0.75rem;
		border-radius: 4px;
		font-size: 0.9rem;
	}
	.error-message {
		background-color: hsla(0, 70%, 50%, 0.2);
		color: #ffcccc;
	}
	.success-message {
		background-color: hsla(120, 70%, 50%, 0.2);
		color: #ccffcc;
	}

	@keyframes spin { to { transform: rotate(360deg); } }
	.spinner {
		display: inline-block;
		width: 1em;
		height: 1em;
		border: 2px solid currentColor;
		border-right-color: transparent;
		border-radius: 50%;
		animation: spin 0.6s linear infinite;
	}
</style>