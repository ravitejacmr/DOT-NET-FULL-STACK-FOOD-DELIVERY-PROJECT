import { Component, effect, HostListener, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ApiService } from '../../core/services/api.service';
@Component({selector:'app-navbar',imports:[RouterLink,RouterLinkActive],template:`
<header class="nav-wrap"><nav class="container navbar" aria-label="Main navigation">
  <a routerLink="/" class="brand"><span>F</span> FoodFlow</a>
  <button class="menu-toggle" (click)="open.set(!open())" aria-label="Toggle navigation">☰</button>
  <div class="nav-links" [class.open]="open()">
    @if(!auth.loggedIn() || auth.user()?.role==='customer') { <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{exact:true}">Home</a><a routerLink="/restaurants" routerLinkActive="active">Restaurants</a> }
    @if(auth.loggedIn()) { <a [routerLink]="auth.dashboardPath()">Dashboard</a> }
    @if(auth.user()?.role==='customer') { <a routerLink="/cart" class="cart-link">Cart @if(cartCount()>0){<span class="cart-badge" [attr.aria-label]="cartCount()+' items in cart'">{{cartCount()}}</span>}</a><a routerLink="/customer/orders">Orders</a><a routerLink="/customer/notifications">Notifications</a> }
    @if(auth.user()?.role==='restaurant_owner') { <a routerLink="/owner/menu">Menu</a><a routerLink="/owner/orders">Orders</a> }
    @if(auth.user()?.role==='delivery_partner') { <a routerLink="/delivery/available">Deliveries</a><a routerLink="/delivery/history">History</a> }
    @if(auth.user()?.role==='admin') { <a routerLink="/admin/users">Users</a><a routerLink="/admin/orders">Orders</a> }
    @if(!auth.loggedIn()) { <a routerLink="/login">Login</a><a routerLink="/register" class="btn small">Create account</a> }
    @else { <button class="link-button" (click)="auth.logout()">Logout</button> }
  </div>
</nav></header>`,styles:[``]})
export class NavbarComponent {
  readonly open=signal(false);
  readonly cartCount=signal(0);
  constructor(public auth:AuthService,private api:ApiService){
    effect(()=>{if(this.auth.user()?.role==='customer')this.loadCartCount();else this.cartCount.set(0);});
  }
  @HostListener('window:foodflow-cart-updated') refreshCartCount(){this.loadCartCount();}
  private loadCartCount(){this.api.cart().subscribe({next:cart=>this.cartCount.set(cart.items.reduce((total,item)=>total+item.quantity,0)),error:()=>this.cartCount.set(0)});}
}
