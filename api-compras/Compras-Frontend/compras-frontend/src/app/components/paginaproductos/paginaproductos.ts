import { Component } from '@angular/core';
import { ProductsComponent } from '../products/products';
import { ProductsListComponent } from '../products-list/products-list';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-paginaproductos',
  standalone: true,
  imports: [CommonModule, ProductsListComponent],
  templateUrl: './paginaproductos.html',
  styleUrl: './paginaproductos.css'
})
export class Paginaproductos {

}
