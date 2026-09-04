import { Injectable, signal } from '@angular/core';
export interface Toast { id:number; message:string; kind:'success'|'error'|'info'; }
@Injectable({providedIn:'root'})
export class ToastService {
  readonly toasts=signal<Toast[]>([]); private id=0;
  show(message:string,kind:Toast['kind']='success'):void { const id=++this.id; this.toasts.update(v=>[...v,{id,message,kind}]); setTimeout(()=>this.dismiss(id),3500); }
  dismiss(id:number):void { this.toasts.update(v=>v.filter(t=>t.id!==id)); }
}
