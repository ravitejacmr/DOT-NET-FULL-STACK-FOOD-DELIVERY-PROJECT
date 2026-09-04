import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Address, AppNotification, Cart, CartItem, Category, Coupon, FoodItem, Order, Page, Payment, Restaurant, Review, User } from '../models/models';

@Injectable({providedIn:'root'})
export class ApiService {
  constructor(private http: HttpClient) {}
  private url(path:string): string { return `${environment.apiUrl}/${path}`; }
  restaurants(params:Record<string,string|number>={}):Observable<Page<Restaurant>> { return this.http.get<Page<Restaurant>>(this.url('restaurants/'), {params:new HttpParams({fromObject:params})}); }
  restaurant(id:number):Observable<Restaurant> { return this.http.get<Restaurant>(this.url(`restaurants/${id}/`)); }
  foods(params:Record<string,string|number|boolean>={}):Observable<Page<FoodItem>> { return this.http.get<Page<FoodItem>>(this.url('foods/'), {params:new HttpParams({fromObject:params as Record<string,string>})}); }
  categories(params:Record<string,string|number>={}):Observable<Page<Category>> { return this.http.get<Page<Category>>(this.url('categories/'), {params:new HttpParams({fromObject:params})}); }
  createCategory(data:Partial<Category>):Observable<Category> { return this.http.post<Category>(this.url('categories/'),data); }
  createRestaurant(data:Partial<Restaurant>):Observable<Restaurant> { return this.http.post<Restaurant>(this.url('restaurants/'),data); }
  updateRestaurant(id:number,data:Partial<Restaurant>):Observable<Restaurant> { return this.http.patch<Restaurant>(this.url(`restaurants/${id}/`),data); }
  createFood(data:Partial<FoodItem>):Observable<FoodItem> { return this.http.post<FoodItem>(this.url('foods/'), data); }
  updateFood(id:number,data:Partial<FoodItem>):Observable<FoodItem> { return this.http.patch<FoodItem>(this.url(`foods/${id}/`),data); }
  deleteFood(id:number):Observable<void> { return this.http.delete<void>(this.url(`foods/${id}/`)); }
  cart():Observable<Cart> { return this.http.get<Cart>(this.url('cart/')); }
  addToCart(food_item:number, quantity=1, replace=false):Observable<CartItem> { return this.http.post<CartItem>(this.url('cart/items/'), {food_item,quantity,replace}).pipe(tap(()=>this.cartChanged())); }
  updateCartItem(id:number, quantity:number):Observable<CartItem> { return this.http.patch<CartItem>(this.url(`cart/items/${id}/`),{quantity}).pipe(tap(()=>this.cartChanged())); }
  removeCartItem(id:number):Observable<void> { return this.http.delete<void>(this.url(`cart/items/${id}/`)).pipe(tap(()=>this.cartChanged())); }
  clearCart():Observable<void> { return this.http.delete<void>(this.url('cart/')).pipe(tap(()=>this.cartChanged())); }
  applyCoupon(code:string):Observable<{coupon:string;discount:number}> { return this.http.post<{coupon:string;discount:number}>(this.url('cart/coupon/'),{code}); }
  addresses():Observable<Page<Address>> { return this.http.get<Page<Address>>(this.url('auth/addresses/')); }
  createAddress(data:Partial<Address>):Observable<Address> { return this.http.post<Address>(this.url('auth/addresses/'),data); }
  deleteAddress(id:number):Observable<void> { return this.http.delete<void>(this.url(`auth/addresses/${id}/`)); }
  setDefaultAddress(id:number):Observable<Address> { return this.http.post<Address>(this.url(`auth/addresses/${id}/set_default/`),{}); }
  orders(params:Record<string,string>={}):Observable<Page<Order>> { return this.http.get<Page<Order>>(this.url('orders/'), {params}); }
  order(id:number):Observable<Order> { return this.http.get<Order>(this.url(`orders/${id}/`)); }
  placeOrder(delivery_address:number,payment_method:string,customer_notes=''):Observable<Order> { return this.http.post<Order>(this.url('orders/'),{delivery_address,payment_method,customer_notes}).pipe(tap(()=>this.cartChanged())); }
  transitionOrder(id:number,status:string):Observable<Order> { return this.http.post<Order>(this.url(`orders/${id}/transition/`),{status}); }
  cancelOrder(id:number):Observable<Order> { return this.http.post<Order>(this.url(`orders/${id}/cancel/`),{}); }
  payments():Observable<Page<Payment>> { return this.http.get<Page<Payment>>(this.url('payments/')); }
  mockPayment(id:number,success:boolean):Observable<Payment> { return this.http.post<Payment>(this.url(`payments/${id}/mock-checkout/`),{success}); }
  notifications():Observable<Page<AppNotification>> { return this.http.get<Page<AppNotification>>(this.url('notifications/')); }
  markRead(id:number):Observable<AppNotification> { return this.http.post<AppNotification>(this.url(`notifications/${id}/mark_read/`),{}); }
  markAllRead():Observable<unknown> { return this.http.post(this.url('notifications/mark_all_read/'),{}); }
  favorites():Observable<Page<{id:number;restaurant:number;restaurant_detail:Restaurant}>> { return this.http.get<Page<{id:number;restaurant:number;restaurant_detail:Restaurant}>>(this.url('favorites/')); }
  addFavorite(restaurant:number):Observable<unknown> { return this.http.post(this.url('favorites/'),{restaurant}); }
  removeFavorite(id:number):Observable<void> { return this.http.delete<void>(this.url(`favorites/${id}/`)); }
  reviews(params:Record<string,string|number>={}):Observable<Page<Review>> { return this.http.get<Page<Review>>(this.url('reviews/'),{params:new HttpParams({fromObject:params})}); }
  createReview(data:Partial<Review>):Observable<Review> { return this.http.post<Review>(this.url('reviews/'),data); }
  updateReview(id:number,data:Partial<Review>):Observable<Review> { return this.http.patch<Review>(this.url(`reviews/${id}/`),data); }
  deleteReview(id:number):Observable<void> { return this.http.delete<void>(this.url(`reviews/${id}/`)); }
  coupons():Observable<Page<Coupon>> { return this.http.get<Page<Coupon>>(this.url('coupons/')); }
  dashboard():Observable<Record<string,unknown>> { return this.http.get<Record<string,unknown>>(this.url('dashboard/')); }
  adminUsers(params:Record<string,string>={}):Observable<Page<User>> { return this.http.get<Page<User>>(this.url('dashboard/users/'),{params}); }
  updateAdminUser(id:number,data:Partial<User>):Observable<User> { return this.http.patch<User>(this.url(`dashboard/users/${id}/`),data); }
  availableDeliveries():Observable<Page<Order>> { return this.http.get<Page<Order>>(this.url('delivery/available/')); }
  acceptDelivery(id:number):Observable<Order> { return this.http.post<Order>(this.url(`delivery/${id}/accept/`),{}); }
  private cartChanged() { window.dispatchEvent(new Event('foodflow-cart-updated')); }
}
