import type {
    DashboardShipmentDto,
    PaginatedDashboardShipmentsResponse,
    ShippingDetail,
    Locality,
    FiltersState,
    CreateShippingRequest,
    CreateShippingResponse,
    ShipmentStatus,
    TransportMethods,
} from "$lib/types";
import { PUBLIC_BACKEND_API_KEY } from "$env/static/public"; // Keep this if PUBLIC_BACKEND_API_KEY is defined elsewhere
import { browser } from "$app/environment"; // Import 'browser'

// --- 1. CONFIGURACIÓN DE URLS ---
// Lógica para la URL de la API 
const API_BASE_URL = browser
    ? import.meta.env.VITE_PUBLIC_API_URL
    : import.meta.env.VITE_PRIVATE_API_URL;

// Configuración de Keycloak (como corre en el navegador, 'localhost' está bien)
const KEYCLOAK_URL =
    "http://localhost:8080/realms/ds-2025-realm/protocol/openid-connect/token";
const CLIENT_ID = "grupo-06";
const CLIENT_SECRET = "8dc00e75-ccea-4d1a-be3d-b586733e256c"; // El secreto que ya descubrimos

// --- 2. FUNCIÓN PARA OBTENER EL TOKEN (LA LLAVE) ---
async function getAuthToken(fetchFn: typeof fetch = fetch): Promise<string> {
    const body = new URLSearchParams();
    body.append("grant_type", "client_credentials");
    body.append("client_id", CLIENT_ID);
    body.append("client_secret", CLIENT_SECRET);

    try {
        const response = await fetchFn(KEYCLOAK_URL, {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: body,
        });

        if (!response.ok) {
            throw new Error(
                `Error al obtener token de Keycloak: ${response.statusText}`,
            );
        }

        const data = await response.json();
        return data.access_token;
    } catch (error) {
        console.error("Fallo grave en autenticación:", error);
        throw error; // Detenemos la ejecución si no hay token
    }
}

export async function getAllLocalities(
    fetchFn: typeof fetch = fetch,
): Promise<Locality[]> {
    const token = await getAuthToken(fetchFn);
    const response = await fetchFn(`${API_BASE_URL}/locality/getall`, {
        headers: {
            Authorization: `Bearer ${token}`, 
        },
    });
    if (!response.ok) {
        throw new Error(`Error al obtener localidades: ${response.statusText}`);
    }

    return await response.json();
}

export async function searchLocalities(
    query: string,
    page: number = 1,
    fetchFn: typeof fetch = fetch,
): Promise<Locality[]> {
    //No se va a buscar si el texto es corto, para no saturar la API
    if (query.length < 2) {
        return Promise.resolve([]);
    }

    const token = await getAuthToken(fetchFn);
    const params = new URLSearchParams({
        locality_name: query,
        limit: "20", //Numero de sugerencias a traer
        page: page.toString(),
    });

    const url = `${API_BASE_URL}/locality?${params.toString()}`;
    const response = await fetchFn(url, {
        headers: {
            Authorization: `Bearer ${token}`, // <--- Enviar Token
        },
    });
    if (!response.ok) {
        throw new Error(`Error al buscar localidades: ${response.statusText}`);
    }

    return await response.json();
}

export async function createShipment(
    shipment: CreateShippingRequest,
    fetchFn: typeof fetch = fetch,
): Promise<CreateShippingResponse> {
    const token = await getAuthToken(fetchFn);
    const response = await fetchFn(`${API_BASE_URL}/shipping`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`, // <--- REEMPLAZADO: "X-Internal-API-Key": PUBLIC_BACKEND_API_KEY,
        },
        body: JSON.stringify(shipment),
    });

    console.log(shipment);
    if (!response.ok) {
        const error = new Error(
            `Failed to create shipment: ${response.statusText}`,
        );
        (error as any).response = response;
        throw error;
    }

    // After creating a shipment, we might want to invalidate some caches.
    // For now, we'll just return the response.
    // A more advanced implementation could invalidate caches related to shipment lists.

    return await response.json();
}

export async function getTransportMethods(
    fetchFn: typeof fetch = fetch,
): Promise<TransportMethods[]> {
    const token = await getAuthToken(fetchFn);
    const response = await fetchFn(
        `${API_BASE_URL}/shipping/transport-methods`,{
        headers: {
            Authorization: `Bearer ${token}`, // <--- Enviar Token
        },
    });
    if (!response.ok) {
        throw new Error(
            `Error al obtener los métodos de transporte: ${response.statusText}`,
        );
    }
    const data = await response.json();
    return data.transport_methods;
}

// --- 3. FUNCIÓN DE DASHBOARD (MODIFICADA) ---
export async function getDashboardShipments(
    page: number = 1,
    pageSize: number = 10,
    filters: FiltersState,
    fetchFn: typeof fetch = fetch,
): Promise<PaginatedDashboardShipmentsResponse> {
    const params = new URLSearchParams({
        page: page.toString(),
        pageSize: pageSize.toString(),
    });

    if (filters.id) params.append("id", filters.id);
    if (filters.city) params.append("city", filters.city);
    if (filters.status) params.append("status", filters.status);
    if (filters.startDate) params.append("startDate", filters.startDate);
    if (filters.endDate) params.append("endDate", filters.endDate);

    const url = `${API_BASE_URL}/dashboard/shipments?${params.toString()}`;

    try {
        // A. Primero, conseguimos la llave
        const token = await getAuthToken(fetchFn);

        // B. Segundo, llamamos a la API con la llave
        const response = await fetchFn(url, {
            headers: {
                // Reemplazamos 'X-Internal-API-Key' por la autenticación correcta
                Authorization: `Bearer ${token}`,
            },
        });

        if (!response.ok) {
            if (response.status === 401 || response.status === 403) {
                console.error(
                    "¡Error de Autenticación! El token fue rechazado por la API.",
                    response.statusText,
                );
            }
            throw new Error(
                `Failed to fetch dashboard shipments: ${response.statusText}`,
            );
        }
        return await response.json();
    } catch (error) {
        console.error("Error en getDashboardShipments:", error);
        throw error; // Dejamos que el componente Svelte maneje el error
    }
}

// Esta función ahora usa automáticamente la nueva autenticación
export async function getAllShipments(
    page: number = 1,
    page_size: number = 10,
    filters: FiltersState,
    fetchFn: typeof fetch = fetch,
): Promise<PaginatedDashboardShipmentsResponse> {
    return getDashboardShipments(page, page_size, filters, fetchFn);
}

// Esta función es PÚBLICA (según la guía del profe), así que la dejamos como estaba.
export async function getShipmentById(
    id: string,
    fetchFn: typeof fetch = fetch,
): Promise<ShippingDetail | undefined> {
    const response = await fetchFn(`${API_BASE_URL}/shipping/${id}`); // Sin token
    if (!response.ok) {
        if (response.status === 404) {
            return undefined; // Shipment not found
        }
        throw new Error(
            `Failed to fetch shipment ${id}: ${response.statusText}`,
        );
    }
    const detailData: ShippingDetail = await response.json();
    return detailData;
}

export async function updateShipmentStatus(
    id: string,
    newStatus: ShipmentStatus,
    message: string,
    fetchFn: typeof fetch = fetch,
): Promise<boolean> {
    const token = await getAuthToken(fetchFn);
    const url = `${API_BASE_URL}/shipments/${id}/status`;

    const response = await fetchFn(url, {
        method: "PUT",
        headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
            new_status: newStatus,
            message: message,
        }),
    });

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(
            `Failed to update shipment status: ${response.statusText} - ${errorText}`,
        );
    }

    return true;
}
