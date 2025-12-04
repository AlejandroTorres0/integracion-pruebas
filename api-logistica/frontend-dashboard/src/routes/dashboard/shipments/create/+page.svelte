<script lang="ts">
    import { createShipment, getTransportMethods } from "../../../../lib/services/shipmentService";
    import type {
        Locality,
        TransportType,
        CreateShippingRequest,
        TransportMethods,
    } from "../../../../lib/types";
    import { goto } from "$app/navigation";
    import { onMount } from "svelte";
    import LocalityCombobox from "../../../../lib/components/dashboard/LocalityCombobox.svelte";
    import Icon from "$lib/components/Icon.svelte";

    let order_id: number;
    let user_id: number;
    let street: string = "";
    let number: number;
    let postal_code: string = "";
    let locality_name: string = "";
    let transport_type: TransportType = "air";
    let products: { id: number; quantity: number }[] = [{ id: 0, quantity: 1 }];
    
    let errorMessage: string | null = null;
    let successMessage: string | null = null;
    let isLoading: boolean = false;

    let selectedLocality: Locality | undefined;

    let transportMethods: TransportMethods[] = [];
    let transportLoading = true;
    let transportError: string | null = null;

    const transportNameMap: Record<string, string> = {
        'Road Transport': 'road',
        'Sea Freight': 'sea',
        'Rail Freight': 'rail',
        'Air Freight': 'air'
    };

    onMount(async () => {
        try {
            transportMethods = await getTransportMethods();
            if (transportMethods.length > 0) {
                const defaultMethod = transportMethods[0];
                transport_type = (transportNameMap[defaultMethod.name] || 'air') as TransportType;
            }
        } catch (error) {
            if (error instanceof Error) {
                transportError = error.message;
            } else {
                transportError = "Ocurrió un error inesperado al cargar los métodos de transporte.";
            }
        } finally {
            transportLoading = false;
        }
    });

    function handleLocalitySelect(event: CustomEvent<{ locality: Locality }>) {
        const locality = event.detail.locality;
        selectedLocality = locality;
        locality_name = locality.locality_name;
        postal_code = locality.postal_code;
    }

    function addProduct() {
        products = [...products, { id: 0, quantity: 1 }];
    }

    function removeProduct(index: number) {
        products = products.filter((_, i) => i !== index);
    }

    async function handleSubmit() {
        errorMessage = null;
        successMessage = null;
        isLoading = true;

        if (!selectedLocality) {
            errorMessage = "Por favor, seleccione una localidad.";
            isLoading = false;
            return;
        }

        const newShipment: CreateShippingRequest = {
            order_id,
            user_id,
            delivery_address: {
                street,
                number,
                postal_code,
                locality_name,
            },
            transport_type,
            products,
        };

        try {
            const response = await createShipment(newShipment);
            successMessage = `Pedido #${response.shipping_id} creado con éxito.`;
            setTimeout(() => {
                goto("/dashboard/shipments");
            }, 2000);
        } catch (error: any) {
            console.error("Error creating shipment:", error);
            if (error.response) {
                try {
                    const body = await error.response.json();
                    errorMessage = `Error al crear el pedido: ${body.message || 'Error desconocido del servidor.'}`;
                } catch (e) {
                    errorMessage = "Error al procesar la respuesta del servidor.";
                }
            } else {
                 errorMessage = "Error de red o CORS. Verifique la conexión y la configuración del servidor.";
            }
        } finally {
            isLoading = false;
        }
    }
</script>

<div class="create-container">
    <div class="header">
		<h1>Crear Nuevo Pedido</h1>
		<a href="/dashboard/shipments" class="back-link">← Volver al listado</a>
	</div>

    <form on:submit|preventDefault={handleSubmit}>
        <div class="details-grid">
            <!-- Columna Izquierda -->
            <div class="grid-col">
                <div class="info-card">
                    <h2>Información General</h2>
                    <div class="form-grid">
                        <div class="form-group">
                            <label for="order_id">Order ID</label>
                            <input type="number" id="order_id" bind:value={order_id} required />
                        </div>
                        <div class="form-group">
                            <label for="user_id">User ID</label>
                            <input type="number" id="user_id" bind:value={user_id} required />
                        </div>
                        <div class="form-group full-width">
                            <label for="transport_type">Método de Transporte</label>
                            {#if transportLoading}
                                <p>Cargando transportes...</p>
                            {:else if transportError}
                                <p class="error-text">{transportError}</p>
                            {:else}
                                <div class="transport-options">
                                    {#each transportMethods as method}
                                        <label class="transport-option" class:selected={transport_type === transportNameMap[method.name]}>
                                            <input type="radio" bind:group={transport_type} name="transport_type" value={transportNameMap[method.name]}>
                                            <Icon name={transportNameMap[method.name] || 'road'} />
                                            <span>{method.name}</span>
                                        </label>
                                    {/each}
                                </div>
                            {/if}
                        </div>
                    </div>
                </div>

                <div class="info-card">
                    <h2>Dirección de Entrega</h2>
                    <div class="form-grid">
                        <div class="form-group full-width">
                            <label for="locality">Localidad</label>
                            <LocalityCombobox on:select={handleLocalitySelect} />
                        </div>
                        <div class="form-group">
                            <label for="street">Calle</label>
                            <input type="text" id="street" bind:value={street} required />
                        </div>
                         <div class="form-group">
                            <label for="number">Número</label>
                            <input type="number" id="number" bind:value={number} required />
                        </div>
                        <div class="form-group">
                            <label for="postal_code">Código Postal</label>
                            <input type="text" id="postal_code" bind:value={postal_code} readonly placeholder="Se autocompleta con la localidad" />
                        </div>
                    </div>
                </div>
            </div>

            <!-- Columna Derecha -->
            <div class="grid-col">
                <div class="info-card">
                    <h2>Productos</h2>
                    <div class="product-list">
                        {#each products as product, i}
                            <div class="product-item">
                                <div class="form-group">
                                    <label for="product_id_{i}">Product ID</label>
                                    <input type="number" id="product_id_{i}" bind:value={product.id} required />
                                </div>
                                <div class="form-group">
                                    <label for="quantity_{i}">Cantidad</label>
                                    <input type="number" id="quantity_{i}" bind:value={product.quantity} min="1" required />
                                </div>
                                {#if products.length > 1}
                                <button type="button" on:click={() => removeProduct(i)} class="remove-btn" title="Eliminar producto">
                                    <Icon name="trash" />
                                </button>
                                {/if}
                            </div>
                        {/each}
                    </div>
                     <button type="button" on:click={addProduct} class="add-btn">Añadir Producto</button>
                </div>

                <div class="info-card">
                     <h2>Confirmar</h2>
                    {#if errorMessage}
                        <p class="message error-message">{errorMessage}</p>
                    {/if}
                    {#if successMessage}
                        <p class="message success-message">{successMessage}</p>
                    {/if}
                    <button type="submit" class="submit-btn" disabled={isLoading}>
                        {#if isLoading}
                            <span class="spinner" />
                            Creando...
                        {:else}
                            Crear Pedido
                        {/if}
                    </button>
                </div>
            </div>
        </div>
    </form>
</div>

<style>
	.create-container {
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
	.header h1 { margin: 0; }
	.back-link {
		color: var(--muted);
		text-decoration: none;
		transition: color 0.2s;
	}
	.back-link:hover { color: var(--accent); }

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

    .form-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1rem;
    }
    .form-group {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
    }
    .full-width {
        grid-column: 1 / -1;
    }
    label {
		font-size: 0.8rem;
		color: var(--muted);
		text-transform: uppercase;
        font-weight: bold;
	}
    input, select {
		width: 100%;
		padding: 0.75rem;
		background-color: var(--bg);
		color: var(--text);
		border: 1px solid var(--border);
		border-radius: 4px;
		font-size: 1rem;
		box-sizing: border-box;
        transition: border-color 0.2s, box-shadow 0.2s;
	}
    input:focus, select:focus {
        outline: none;
        border-color: var(--accent);
        box-shadow: 0 0 0 3px hsla(var(--accent-hsl), 0.3);
    }
    input:read-only {
        background-color: color-mix(in srgb, var(--bg) 50%, #000 50%);
        cursor: not-allowed;
    }

    .transport-options {
        display: flex;
        gap: 1rem;
    }
    .transport-option {
        flex: 1;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 0.5rem;
        padding: 1rem;
        border: 2px solid var(--border);
        border-radius: 8px;
        cursor: pointer;
        transition: border-color 0.2s, background-color 0.2s;
    }
    .transport-option input[type="radio"] {
        display: none;
    }
    .transport-option:hover {
        border-color: var(--accent);
    }
    .transport-option.selected {
        border-color: var(--accent);
        background-color: hsla(var(--accent-hsl), 0.1);
    }
    .transport-option :global(.transport-icon) {
        font-size: 2rem;
    }

    .product-list {
        display: flex;
        flex-direction: column;
        gap: 1rem;
    }
    .product-item {
        display: grid;
        grid-template-columns: 3fr 2fr auto;
        gap: 1rem;
        align-items: flex-end;
    }
    .remove-btn, .add-btn {
        padding: 0.75rem;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-weight: bold;
        font-size: 1rem;
        display: flex;
        align-items: center;
        justify-content: center;
    }
    .remove-btn {
        background-color: #ef444420;
        color: #ef4444;
        border: 1px solid #ef4444;
    }
    .add-btn {
        margin-top: 1rem;
        width: 100%;
        background-color: hsla(var(--accent-hsl), 0.2);
        color: var(--accent);
        border: 1px dashed var(--accent);
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
        width: 100%;
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
	.error-message, .error-text {
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