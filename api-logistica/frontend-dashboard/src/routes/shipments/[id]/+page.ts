import { getShipmentById } from '../../../lib/services/shipmentService';
import type { ShippingDetail } from '$lib/types';

// Cache to store shipment details
const shipmentCache = new Map<string, ShippingDetail>();

export const load = async ({ params, fetch }) => {
  const { id } = params;

  // Check if the shipment details are already in the cache
  if (shipmentCache.has(id)) {
    return {
      shipmentDetails: shipmentCache.get(id)
    };
  }

  // If not in cache, fetch the details
  const shipmentDetails = await getShipmentById(id, fetch);

  // Store in cache for future requests
  if (shipmentDetails) {
    shipmentCache.set(id, shipmentDetails);
  }

  return {
    shipmentDetails
  };
};