import { Component, Input } from '@angular/core';

@Component({
    selector: 'd4-menu',
    template: require('./menu.component.html'),
    styles: [require('./menu.component.css')],
    providers: []
})
export class MenuComponent {
    
    @Input() name: string;
    @Input() text: string;
    // TODO: Figure out what to do with actions
    @Input() action: string;

}
