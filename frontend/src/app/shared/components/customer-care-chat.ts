import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface SupportMessage {
  from: 'agent' | 'customer';
  text: string;
  time: string;
}

@Component({
  selector: 'app-customer-care-chat',
  imports: [FormsModule],
  template: `
    <button class="support-launcher" type="button" (click)="toggle()" [attr.aria-expanded]="open()" aria-controls="support-chat">
      <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M20 11.5a8 8 0 0 1-8.5 8 8.7 8.7 0 0 1-3.7-.9L4 20l1.3-3.5A8 8 0 1 1 20 11.5Z"/><path d="M8.5 12h.01M12 12h.01M15.5 12h.01"/></svg>
      <span>Customer care</span>
    </button>

    @if(open()) {
      <section id="support-chat" class="support-chat" aria-label="Customer care chat">
        <header class="support-header">
          <div class="support-avatar">F</div>
          <div><b>FoodFlow Care</b><small><i></i> Online · usually replies instantly</small></div>
          <button type="button" (click)="open.set(false)" aria-label="Close customer care chat">×</button>
        </header>

        <div class="support-messages">
          @for(message of chat(); track $index) {
            <div class="support-message" [class.customer]="message.from === 'customer'">
              <p>{{message.text}}</p><time>{{message.time}}</time>
            </div>
          }
          @if(typing()) { <div class="support-message typing" aria-label="Customer care is typing"><span></span><span></span><span></span></div> }
        </div>

        @if(chat().length === 1) {
          <div class="support-suggestions">
            <button type="button" (click)="quickReply('Where is my order?')">Track my order</button>
            <button type="button" (click)="quickReply('I need help with a payment')">Payment help</button>
            <button type="button" (click)="quickReply('I want to cancel my order')">Cancel order</button>
          </div>
        }

        <form class="support-input" (ngSubmit)="send()">
          <input [(ngModel)]="draft" name="supportMessage" autocomplete="off" maxlength="300" placeholder="Type your message..." aria-label="Message customer care">
          <button type="submit" [disabled]="!draft.trim()" aria-label="Send message">
            <svg viewBox="0 0 24 24" aria-hidden="true"><path d="m3 3 18 9-18 9 4-9-4-9Zm4 9h14"/></svg>
          </button>
        </form>
      </section>
    }
  `
})
export class CustomerCareChatComponent {
  open = signal(false);
  typing = signal(false);
  draft = '';
  chat = signal<SupportMessage[]>(this.restore());

  toggle() { this.open.update(value => !value); }
  quickReply(text: string) { this.draft = text; this.send(); }

  send() {
    const text = this.draft.trim();
    if (!text) return;
    this.draft = '';
    this.add('customer', text);
    this.typing.set(true);
    window.setTimeout(() => {
      this.typing.set(false);
      this.add('agent', this.reply(text));
    }, 650);
  }

  private reply(message: string) {
    const text = message.toLowerCase();
    if (text.includes('where') || text.includes('track') || text.includes('late')) return 'You can see live progress under Orders. Open your current order and select View details. If it is delayed beyond the estimate, send me the order number.';
    if (text.includes('cancel')) return 'You can cancel from Orders while the restaurant has not started preparing it. Open the order and choose Cancel order.';
    if (text.includes('payment') || text.includes('refund')) return 'For payment or refund help, please send your order number and the payment issue. Refunds are returned to the original payment method.';
    if (text.includes('address')) return 'You can add or change delivery addresses from Dashboard → Addresses before placing an order.';
    if (text.includes('account') || text.includes('login')) return 'I can help with your account. Tell me whether the problem is signing in, registration, or updating your profile.';
    return 'Thanks for your message. Please share your order number and a short description so FoodFlow Care can help you.';
  }

  private add(from: SupportMessage['from'], text: string) {
    const message = { from, text, time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) };
    this.chat.update(messages => [...messages, message]);
    sessionStorage.setItem('foodflow-support-chat', JSON.stringify(this.chat()));
  }

  private restore(): SupportMessage[] {
    try {
      const saved = sessionStorage.getItem('foodflow-support-chat');
      if (saved) return JSON.parse(saved) as SupportMessage[];
    } catch {}
    return [{ from: 'agent', text: 'Hi! I’m with FoodFlow Care. How can I help you today?', time: 'Now' }];
  }
}
