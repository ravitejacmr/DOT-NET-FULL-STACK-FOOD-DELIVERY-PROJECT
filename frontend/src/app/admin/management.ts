import { Component, Input, OnChanges, OnInit, signal } from '@angular/core';
import { TitleCasePipe } from '@angular/common';
import { Coupon, Order, Payment, Restaurant, Review, User } from '../core/models/models';
import { ApiService } from '../core/services/api.service';
import { ToastService } from '../core/services/toast.service';
type Row=User|Restaurant|Order|Payment|Coupon|Review;
@Component({imports:[TitleCasePipe],template:`
<section class="page-hero small"><div class="container"><span class="eyebrow">ADMINISTRATION</span><h1>{{resource|titlecase}}</h1></div></section>
<section class="section container"><div class="panel table-wrap"><table><thead><tr>@for(h of headers();track h){<th>{{h}}</th>}@if(resource==='users'){<th>Actions</th>}</tr></thead><tbody>@for(row of rows();track row.id){<tr>@for(key of keys();track key){<td>{{display(row,key)}}</td>}@if(resource==='users'){<td><button class="btn small" [class.danger]="user(row).is_active" (click)="toggle(user(row))">{{user(row).is_active?'Deactivate':'Activate'}}</button></td>}</tr>}</tbody></table>@if(!rows().length){<div class="empty compact">No records found.</div>}</div></section>`})
export class AdminManagementComponent implements OnInit,OnChanges{
  @Input() resource='users';rows=signal<Row[]>([]);headers=signal<string[]>([]);keys=signal<string[]>([]);
  constructor(private api:ApiService,private toast:ToastService){}
  ngOnInit(){this.load();}ngOnChanges(){this.load();}
  load(){if(this.resource==='users')this.api.adminUsers().subscribe(v=>this.set(v.results,['email','role','is_active'],['Email','Role','Active']));else if(this.resource==='restaurants')this.api.restaurants().subscribe(v=>this.set(v.results,['name','city','average_rating','is_active'],['Restaurant','City','Rating','Active']));else if(this.resource==='payments')this.api.payments().subscribe(v=>this.set(v.results,['order_number','amount','payment_method','status'],['Order','Amount','Method','Status']));else if(this.resource==='coupons')this.api.coupons().subscribe(v=>this.set(v.results,['code','discount_type','discount_value','is_active'],['Code','Type','Value','Active']));else if(this.resource==='reviews')this.api.reviews().subscribe(v=>this.set(v.results,['customer_name','rating','comment','created_at'],['Customer','Rating','Comment','Created']));else this.api.orders().subscribe(v=>this.set(v.results,['order_number','restaurant_name','status_label','grand_total'],['Order','Restaurant','Status','Total']));}
  set(rows:Row[],keys:string[],headers:string[]){this.rows.set(rows);this.keys.set(keys);this.headers.set(headers);}display(row:Row,key:string){return String((row as unknown as Record<string,unknown>)[key]??'—');}user(row:Row){return row as User;}toggle(u:User){this.api.updateAdminUser(u.id,{is_active:!u.is_active}).subscribe(()=>{this.toast.show('User status updated');this.load();});}
}
