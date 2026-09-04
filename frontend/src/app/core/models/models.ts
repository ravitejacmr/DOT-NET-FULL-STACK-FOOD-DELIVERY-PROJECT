export type UserRole = 'customer' | 'restaurant_owner' | 'delivery_partner' | 'admin';
export interface User { id: number; email: string; first_name: string; last_name: string; phone: string; role: UserRole; profile_image?: string; is_active: boolean; }
export interface Restaurant { id: number; owner: number; name: string; description: string; logo?: string; cover_image?: string; phone: string; email: string; address: string; city: string; state: string; postal_code: string; cuisine_type: string; opening_time: string; closing_time: string; minimum_order: string; delivery_fee: string; estimated_delivery_time: number; average_rating: string; is_active: boolean; is_open: boolean; is_favorite: boolean; }
export interface Category { id: number; restaurant: number; name: string; description: string; image?: string; is_active: boolean; }
export interface FoodItem { id: number; restaurant: number; restaurant_name: string; category: number; category_name: string; name: string; description: string; image?: string; original_price: string; discount_price?: string; price: string; is_vegetarian: boolean; is_available: boolean; preparation_time: number; rating: string; }
export interface CartItem { id: number; food_item: number; food: FoodItem; quantity: number; unit_price: string; subtotal: string; }
export interface Cart { id: number; restaurant?: number; coupon?: number; items: CartItem[]; subtotal: string; tax: string; delivery_fee: string; discount: number; grand_total: number; }
export interface Address { id: number; full_name: string; phone: string; house_number: string; street: string; landmark: string; city: string; state: string; postal_code: string; address_type: 'home'|'office'|'other'; is_default: boolean; }
export interface OrderItem { id: number; food_name: string; quantity: number; unit_price: string; subtotal: string; }
export interface StatusEvent { id: number; status: string; label: string; created_at: string; }
export interface Order { id: number; order_number: string; restaurant: number; restaurant_name: string; delivery_address_snapshot: Record<string,string>; items: OrderItem[]; status: string; status_label: string; status_events: StatusEvent[]; subtotal: string; tax: string; delivery_fee: string; discount: string; grand_total: string; payment_method: string; payment_status: string; created_at: string; }
export interface Payment { id: number; order: number; order_number: string; amount: string; payment_method: string; transaction_id?: string; status: string; payment_date?: string; }
export interface Coupon { id:number; code:string; discount_type:string; discount_value:string; minimum_order_amount:string; expiry_date:string; is_active:boolean; }
export interface Review { id: number; customer: number; customer_name: string; restaurant: number; food_item?: number; order: number; rating: number; comment: string; created_at: string; }
export interface AppNotification { id: number; title: string; message: string; notification_type: string; is_read: boolean; created_at: string; }
export interface DeliveryPartner { id: number; user: number; email: string; phone: string; vehicle_type: string; vehicle_number: string; driving_license_number: string; is_available: boolean; rating: string; }
export interface Page<T> { count: number; next: string|null; previous: string|null; results: T[]; }
export interface AuthResponse { access: string; refresh: string; user: User; }
