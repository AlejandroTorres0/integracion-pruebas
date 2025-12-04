import { getShipmentById } from '../../../../../lib/services/shipmentService';
import type { PageLoad } from './$types';

export const load: PageLoad = async ({ params, fetch }) => {
  const { id } = params;

  // Fetch the shipment details, which include the logs
  const shipmentDetails = await getShipmentById(id, fetch);

  return {
    shipmentDetails
  };
};
