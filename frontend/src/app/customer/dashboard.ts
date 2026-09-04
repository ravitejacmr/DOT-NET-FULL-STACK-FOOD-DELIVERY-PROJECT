import { DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Order } from '../core/models/models';
import { ApiService } from '../core/services/api.service';
import { AuthService } from '../core/services/auth.service';

@Component({imports:[RouterLink,DatePipe],template:`
<section class="dashboard-page"><div class="container"><div class="welcome"><div><span class="eyebrow">CUSTOMER DASHBOARD</span><h1>Hello, {{auth.user()?.first_name}} 👋</h1><p>Ready to make today delicious?</p></div><a routerLink="/restaurants" class="btn">Order food</a></div>
<div class="metric-grid"><article><span>Active order</span><b>{{data()?.['current_order']?'1':'0'}}</b><small>In progress</small></article><article><span>Saved addresses</span><b>{{data()?.['saved_addresses']||0}}</b><small>Delivery locations</small></article><article><span>Favourites</span><b>{{data()?.['favorite_restaurants']||0}}</b><small>Restaurants</small></article><article><span>Notifications</span><b>{{data()?.['unread_notifications']||0}}</b><small>Unread updates</small></article></div>
<div class="panel"><div class="section-head"><h2>Recent orders</h2><a routerLink="/customer/orders">View all →</a></div>@for(o of recent();track o.id){<a class="order-row" [routerLink]="['/customer/orders',o.id]"><div><b>{{o.order_number}}</b><small>{{o.created_at|date:'medium'}}</small></div><span class="badge" [class]="o.status">{{o.status}}</span><b>₹{{o.grand_total}}</b></a>}@empty{<div class="empty compact">No orders yet. Your first favourite is waiting.</div>}</div></div></section>`})
export class CustomerDashboardComponent implements OnInit{data=signal<Record<string,unknown>|null>(null);constructor(private api:ApiService,public auth:AuthService){}ngOnInit(){this.api.dashboard().subscribe(v=>this.data.set(v));}recent():Order[]{return (this.data()?.['recent_orders'] as Order[])||[];}}
