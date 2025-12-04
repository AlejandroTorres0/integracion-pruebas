export type ShipmentStatus =
  | 'in_transit'
  | 'created'
  | 'delivered'
  | 'cancelled'
  | 'in_distribution'
  | 'arrived'
  | 'reserved';

export interface Shipment {
  id: string;
  destination: string;
  status: ShipmentStatus;
  entryDate: string; // Formato 'YYYY-MM-DD'
}

export interface PaginationData {
  current_page: number;
  total_pages: number;
  total_items: number;
  items_per_page: number;
}

export interface PaginatedShipmentsResponse {
  shipments: Shipment[];
  pagination: PaginationData;
}

/**
 * Define la estructura del objeto que maneja el estado de los filtros.
 */
export interface FiltersState {
  id: string; // Para el buscador por ID
  city: string;
  status: ShipmentStatus | '';
  startDate: string; // Para el filtro por fecha
  endDate: string;   // Para el filtro por fecha
}

export interface ProductQty {
  product_id: number;
  quantity: number;
}

export type TransportType =
  | 'road'
  | 'air'
  | 'sea'
  | 'rail';

export interface AddressReadDto {
  address_id: number;
  street: string;
  number: number;
  postal_code: string;
  locality_name: string;
}

export interface DashboardShipmentDto {
  shipping_id: number;
  order_id: number;
  user_id: number;
  products: ProductQty[];
  status: ShipmentStatus;
  transport_type: TransportType;
  estimated_delivery_at: string;
  created_at: string;
  delivery_address: AddressReadDto;
  departure_address: AddressReadDto;
}

export interface PaginatedDashboardShipmentsResponse {
  shipments: DashboardShipmentDto[];
  pagination: PaginationData;
}

export interface ShippingLogReadDto {
  timestamp: string;
  status: ShipmentStatus;
  message: string;
}

export interface ProductQtyReadDto {
  product_id: number;
  quantity: number;
}

export interface ShippingDetail {
  shipping_id: number;
  order_id: number;
  user_id: number;
  status: ShipmentStatus;
  tracking_number: string;
  carrier_name: string;
  total_cost: number;
  currency: string;
  estimated_delivery_at: string;
  created_at: string;
  updated_at: string;
  transport_type: TransportType;
  delivery_address: AddressReadDto;
  departure_address: AddressReadDto;
  products: ProductQtyReadDto[];
  logs: ShippingLogReadDto[];
}

export interface Locality {
  postal_code: string;
  locality_name: string;
  province: string;
  country: string;
}

export interface DeliveryAddressRequest {
  street: string;
  number: number;
  postal_code: string;
  locality_name: string;
}

export interface ProductRequest {
  id: number;
  quantity: number;
}

export interface CreateShippingRequest {
  order_id: number;
  user_id: number;
  delivery_address: DeliveryAddressRequest;
  transport_type: TransportType;
  products: ProductRequest[];
}

export interface CreateShippingResponse {
  shipping_id: number;
  status: ShipmentStatus;
  transport_type: TransportType;
  estimated_delivery_at: string;
}