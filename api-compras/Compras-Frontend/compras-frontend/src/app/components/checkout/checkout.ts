// src/app/components/checkout/checkout.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { CartServiceFixed } from '../../services/cartservice-fixed';
import { ApiService } from '../../services/api';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './checkout.html',
  styleUrl: './checkout.css'
})
export class CheckoutComponent implements OnInit {
  cartTotal: number = 0;
  cartItemsCount: number = 0;
  loading: boolean = false;
  submitting: boolean = false;
  errorMessage: string = '';

  // Datos del formulario
  deliveryAddress = {
    street: '',
    city: '',
    state: '',
    postalCode: '',
    country: 'AR' // Siempre Argentina por defecto
  };

  transportType: string = 'truck'; // Valor por defecto

  // Opciones para el select de transporte
  transportOptions = [
    { value: 'truck', label: '🚚 Camión' },
    { value: 'boat', label: '🚢 Barco' },
    { value: 'plane', label: '✈️ Avión' }
  ];

  constructor(
    private cartService: CartServiceFixed,
    private apiService: ApiService,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadCartData();
  }

  async loadCartData() {
    try {
      this.loading = true;
      const cart = this.cartService.getItems();
      this.cartTotal = this.cartService.getCartTotal();
      this.cartItemsCount = cart.length;
      
      if (cart.length === 0) {
        this.errorMessage = 'Tu carrito está vacío. Agrega productos antes de continuar.';
      }
    } catch (error) {
      this.errorMessage = 'Error al cargar los datos del carrito';
      console.error('Error:', error);
    } finally {
      this.loading = false;
    }
  }

  validateForm(): boolean {
    if (this.cartItemsCount === 0) {
      this.errorMessage = 'No puedes realizar un pedido con el carrito vacío';
      return false;
    }

    if (!this.deliveryAddress.street.trim()) {
      this.errorMessage = 'La calle es requerida';
      return false;
    }

    if (!this.deliveryAddress.city.trim()) {
      this.errorMessage = 'La ciudad es requerida';
      return false;
    }

    if (!this.deliveryAddress.state.trim()) {
      this.errorMessage = 'La provincia es requerida';
      return false;
    }

    if (!this.deliveryAddress.postalCode.trim()) {
      this.errorMessage = 'El código postal es requerido';
      return false;
    }

    if (!this.transportType) {
      this.errorMessage = 'Selecciona un método de envío';
      return false;
    }

    this.errorMessage = '';
    return true;
  }

  async submitOrder() {
  if (!this.validateForm()) {
    return;
  }

  this.submitting = true;
  this.errorMessage = '';
  
  try {
    const orderData = {
      deliveryAddress: this.deliveryAddress,
      transportType: this.transportType
    };

    console.log('📦 Enviando orden:', orderData);
    
    // Usar subscribe en lugar de toPromise (mejor práctica en Angular)
    this.apiService.createOrder(orderData).subscribe({
      next: (response) => {
          console.log('✅ Orden creada exitosamente:', response);
  
          // Verificar si la respuesta tiene datos
          if (!response) {
            throw new Error('La respuesta del servidor está vacía');
          }
          
          // ✅ USAR EL CAMPO CORRECTO DE LA RESPUESTA
          const orderNumber = response.reservaId || response.shippingId || 'N/A';
          const shippingCost = response.shippingCost || 0;
          const estimatedDelivery = response.estimatedDelivery 
            ? new Date(response.estimatedDelivery).toLocaleDateString('es-AR') 
            : 'No disponible';
          
          // Mostrar mensaje de éxito con más detalles
          const successMessage = `
          🎉 ¡Pedido realizado con éxito!

          📦 Número de reserva: ${orderNumber}
          🚚 Número de envío: ${response.shippingId || 'N/A'}
          💰 Costo de envío: $${shippingCost}
          📅 Entrega estimada: ${estimatedDelivery}

        ${response.message || 'Tu pedido ha sido procesado exitosamente.'}
          `;
          
          alert(successMessage);
          
          // Limpiar carrito después de éxito
          this.cartService.clearCart();
          
          // Redirigir a la página de confirmación o historial
          setTimeout(() => {
            this.router.navigate(['/carrito']);
          }, 2000);
          
          this.submitting = false;

      },
      error: (error) => {
        console.error('❌ Error creando orden:', error);
        this.handleOrderError(error);
      },
      complete: () => {
        console.log('🏁 Llamada completada');
        this.submitting = false;
      }
    });
    
  } catch (error: any) {
    console.error('❌ Error inesperado:', error);
    this.errorMessage = 'Error inesperado: ' + error.message;
    this.submitting = false;
  }
}

private handleOrderError(error: any) {
  this.submitting = false;
  
  // Intentar extraer mensaje de error del backend
  let errorMsg = 'Error al procesar el pedido';
  
  if (error.status === 0) {
    errorMsg = 'No se pudo conectar con el servidor. Verifica tu conexión.';
  } else if (error.status === 400) {
    errorMsg = 'Datos inválidos: ' + (error.error?.message || 'Revisa la información');
  } else if (error.status === 401) {
    errorMsg = 'No estás autenticado. Por favor, inicia sesión nuevamente.';
  } else if (error.status === 500) {
    errorMsg = 'Error interno del servidor. Intenta nuevamente más tarde.';
  } else if (error.error?.message) {
    errorMsg = error.error.message;
  } else if (error.message) {
    errorMsg = error.message;
  }
  
  this.errorMessage = errorMsg;
}

  goBack() {
    this.router.navigate(['/carrito']);
  }
}