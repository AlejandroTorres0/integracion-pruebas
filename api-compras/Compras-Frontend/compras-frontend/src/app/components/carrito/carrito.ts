// carrito.ts - VERSIÓN MEJORADA
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CartServiceFixed } from '../../services/cartservice-fixed';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-carrito',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './carrito.html',
  styleUrl: './carrito.css'
})
export class CarritoComponent implements OnInit {
  loading: boolean = true;

  constructor(private cartService: CartServiceFixed) {}

  ngOnInit() {
    console.log('🔄 Cargando carrito desde backend...');
    this.loadCartFromBackend();
  }

  async loadCartFromBackend() {
    try {
      this.loading = true;
      await this.cartService.loadCartFromBackend();
      console.log('✅ Carrito cargado exitosamente');
    } catch (error) {
      console.error('❌ Error cargando carrito:', error);
    } finally {
      this.loading = false;
    }
  }

  get cart() {
    return this.cartService.getItems();
  }

  validateQuantity(item: any) {
    this.cartService.validateQuantity(item);
  }

  removeFromCart(index: number) {
    this.cartService.removeFromCart(index);
  }

  getCartTotal(): number {
    return this.cartService.getCartTotal();
  }
}